using System;
using System.Runtime.CompilerServices;
using Game;
using Game.City;
using Game.Simulation;
using Unity.Entities;

namespace BudgetBooster
{
    /// <summary>
    /// The main ECS system that monitors the city budget and adds money as needed.
    /// Registered to run during the GameSimulation phase, which updates once per simulation tick and
    /// not at all while the game is paused.
    /// </summary>
    public partial class BudgetBoosterSystem : GameSystemBase
    {
        // System references
        private CitySystem? _citySystem;
        private CityServiceBudgetSystem? _budgetSystem;

        // State tracking
        private int _tickCounter = 0;
        private int _gameStateLogCounter = 0;
        private int _expenseReadFailures = 0;
        private bool _hasLoggedMissingLogic = false;
        private bool _isInitialized = false;
        private bool _hasLoggedMissingSettings = false;
        private bool _hasLoggedNotInitialized = false;

        // Why the last cycle was skipped, or null if it ran. Logging only when this changes keeps a
        // persistent condition (Unlimited Money, say) to one line instead of one per cycle.
        private string? _lastSkipReason;

        // Set by the Run Diagnostics button, consumed at the top of OnUpdate. Deliberately a static
        // field rather than a ModSettings property: every public get/set property on ModSettings is
        // saved to BudgetBooster.coc, which made an earlier version run diagnostics a session late.
        private static volatile bool _cityDiagnosticsRequested;

        // Constants for game state logging (every ~10 update intervals in debug mode)
        private const int GameStateLogFrequency = 10;

        // Boost calculation constants live in the reloadable logic assembly (BoostFormulas).

        /// <summary>
        /// Asks for the city half of the diagnostics report. Safe to call from the settings UI: it only
        /// sets a flag, and the report itself is written on the next simulation tick, where reading
        /// the EntityManager is safe. Nothing is written while the game is paused or no city is loaded.
        /// </summary>
        public static void RequestCityDiagnostics()
        {
            _cityDiagnosticsRequested = true;
        }

        protected override void OnCreate()
        {
            Mod.Log.Info("[Budget Booster] === BudgetBoosterSystem.OnCreate() START ===");
            base.OnCreate();

            try
            {
                Mod.Log.Info("[Budget Booster] Getting CitySystem reference...");
                // Get reference to the city system
                _citySystem = World.GetExistingSystemManaged<CitySystem>();

                if (_citySystem == null)
                {
                    Mod.Log.Error("[Budget Booster] BudgetBoosterSystem: CitySystem is null! System will not function.");
                    Mod.Log.Error("[Budget Booster] This may indicate the game is still loading or World is not ready.");
                    _isInitialized = false;
                    return;
                }
                Mod.Log.Info($"[Budget Booster] CitySystem obtained: {_citySystem.GetType().FullName}");

                // Get reference to the budget system, used by Percentage mode for real expense data.
                // Optional: if it is unavailable the mod still runs, Percentage mode just scales
                // BoostAmount instead of actual expenses.
                Mod.Log.Info("[Budget Booster] Getting CityServiceBudgetSystem reference...");
                _budgetSystem = World.GetExistingSystemManaged<CityServiceBudgetSystem>();

                if (_budgetSystem == null)
                {
                    Mod.Log.Warn("[Budget Booster] CityServiceBudgetSystem is null - Percentage mode will scale BoostAmount instead of city expenses");
                }
                else
                {
                    Mod.Log.Info($"[Budget Booster] CityServiceBudgetSystem obtained: {_budgetSystem.GetType().FullName}");
                }

                _isInitialized = true;
                Mod.Log.Info("[Budget Booster] === BudgetBoosterSystem.OnCreate() COMPLETE ===");
            }
            catch (Exception ex)
            {
                Mod.Log.Error($"[Budget Booster] BudgetBoosterSystem.OnCreate failed: {ex.Message}");
                Mod.Log.Error($"[Budget Booster] Stack trace: {ex.StackTrace}");
                _isInitialized = false;
            }
        }

        protected override void OnUpdate()
        {
            try
            {
                // Apply any queued logic swap here, at the top of the update and before any cycle
                // work begins. Reloads are requested from the settings UI on a different thread, so
                // this is the safe point where nothing is mid-flight. It runs before the Enabled
                // check so a reload takes effect even while boosting is switched off.
                LogicHost.ApplyPendingSwap();

                // Get settings from the shared Mod instance
                var settings = Mod.Settings;

                // Defensive null check for settings
                if (settings == null)
                {
                    LogDebugOnce(ref _hasLoggedMissingSettings, "[Budget Booster] Settings are null - waiting for initialization");
                    return;
                }

                // Handle a diagnostics request, also before the Enabled check: the report matters
                // most when the mod looks like it is doing nothing.
                if (_cityDiagnosticsRequested)
                {
                    _cityDiagnosticsRequested = false;
                    RunCityDiagnostics(settings);
                }

                // Check if mod is enabled
                if (!settings.Enabled)
                    return;

                // Validate system state. OnCreate has already logged the reason as an error.
                if (!_isInitialized || _citySystem == null)
                {
                    LogDebugOnce(ref _hasLoggedNotInitialized, "[Budget Booster] System not initialized - no boosts will be applied");
                    return;
                }

                // Only run on configured interval to avoid performance impact. OnUpdate runs once per
                // simulation tick, so UpdateInterval is the number of ticks per boost cycle.
                _tickCounter++;
                if (_tickCounter < settings.UpdateInterval)
                    return;

                _tickCounter = 0;

                // Perform the boost cycle
                PerformBoostCycle(settings);

                // Log game state periodically in debug mode
                if (settings.DebugMode && settings.LogGameState)
                {
                    _gameStateLogCounter++;
                    if (_gameStateLogCounter >= GameStateLogFrequency)
                    {
                        _gameStateLogCounter = 0;
                        LogGameState(settings);
                    }
                }
            }
            catch (Exception ex)
            {
                Mod.Log.Error($"[Budget Booster] OnUpdate exception: {ex.Message}");
                Mod.Log.Error($"[Budget Booster] Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Performs a single boost cycle, checking balance and applying boost if needed.
        /// </summary>
        private void PerformBoostCycle(ModSettings settings)
        {
            var cityEntity = _citySystem!.City;

            if (cityEntity == Entity.Null)
            {
                LogSkipReason("City entity is null");
                return;
            }

            // Check if entity has PlayerMoney component
            if (!EntityManager.HasComponent<PlayerMoney>(cityEntity))
            {
                LogSkipReason("City entity missing PlayerMoney component");
                return;
            }

            PlayerMoney playerMoney;
            try
            {
                playerMoney = EntityManager.GetComponentData<PlayerMoney>(cityEntity);
            }
            catch (Exception ex)
            {
                LogSkipReason($"Failed to get PlayerMoney component: {ex.Message}", isWarning: true);
                return;
            }

            // With the game's Unlimited Money option the balance reads as the ceiling and the real
            // figure is meaningless, so there is nothing to boost and nothing worth recording.
            if (playerMoney.m_Unlimited)
            {
                LogSkipReason("Unlimited Money is on - boosts are paused until it is turned off");
                return;
            }

            if (_lastSkipReason != null)
            {
                _lastSkipReason = null;
                Mod.Log.Info("[Budget Booster] Boost cycles running again");
            }

            long currentMoney = playerMoney.money;

            // Delegate the decision to the reloadable logic assembly, then hold it to the host's
            // limits: never more than Max Boost Per Cycle, never past the game's balance ceiling.
            long requested = CalculateBoostViaLogic(currentMoney, settings);
            int boost = BoostLimits.ClampBoost(requested, currentMoney, settings.MaxBoostPerCycle);

            if (boost <= 0)
            {
                // Still update the current balance even when no boost applied
                SessionStatistics.CurrentBalance = currentMoney;
                return;
            }

            try
            {
                playerMoney.Add(boost);
                EntityManager.SetComponentData(cityEntity, playerMoney);
            }
            catch (Exception ex)
            {
                Mod.Log.Error($"[Budget Booster] Failed to apply boost: {ex.Message}");
                return;
            }

            // Record what actually landed, not what was asked for. The game clamps the balance, so
            // the two can differ.
            long newBalance = playerMoney.money;
            long applied = newBalance - currentMoney;

            if (applied <= 0)
            {
                SessionStatistics.CurrentBalance = newBalance;
                return;
            }

            // Update session statistics (single source of truth for UI display)
            SessionStatistics.RecordBoost(applied, newBalance);

            // One line per applied boost. Cycles that add nothing are not logged at all.
            if (settings.DebugMode && settings.LogBoostCycles)
            {
                Mod.Log.Info($"[Budget Booster] Boost cycle - Mode: {settings.BoostMode}, Balance: ${currentMoney:N0} -> ${newBalance:N0}, Requested: ${requested:N0}, Applied: ${applied:N0}");
            }

            // Handle notifications based on settings
            SendBoostNotification(settings, applied, newBalance);
        }

        /// <summary>
        /// Logs why a cycle was skipped, but only when the reason differs from the last cycle's.
        /// </summary>
        /// <param name="reason">What stopped the cycle.</param>
        /// <param name="isWarning">True for failures, which also belong in the game's Player.log.</param>
        private void LogSkipReason(string reason, bool isWarning = false)
        {
            if (reason == _lastSkipReason)
                return;

            _lastSkipReason = reason;

            string message = $"[Budget Booster] Skipping boost cycles: {reason}";
            if (isWarning)
                Mod.Log.Warn(message);
            else
                Mod.Log.Info(message);
        }

        /// <summary>
        /// Sends a notification about a boost based on the notification settings.
        /// </summary>
        /// <param name="settings">The mod settings.</param>
        /// <param name="boostAmount">The amount of money that was added.</param>
        /// <param name="newBalance">The new balance after the boost.</param>
        private void SendBoostNotification(ModSettings settings, long boostAmount, long newBalance)
        {
            // Check if notifications are enabled
            if (!settings.EnableNotifications)
                return;

            // Determine if we should show this notification based on verbosity level
            bool shouldNotify = false;
            string notificationMessage = string.Empty;

            switch (settings.NotificationVerbosity)
            {
                case ModSettings.NotificationLevel.Minimal:
                    // Only notify for boosts at or above the threshold
                    if (boostAmount >= settings.MinimalNotificationThreshold)
                    {
                        shouldNotify = true;
                        notificationMessage = $"[Budget Booster] Large boost applied: +${boostAmount:N0}";
                    }
                    break;

                case ModSettings.NotificationLevel.Normal:
                    // Always notify with basic info
                    shouldNotify = true;
                    notificationMessage = $"[Budget Booster] Boost applied: +${boostAmount:N0}";
                    break;

                case ModSettings.NotificationLevel.Detailed:
                    // Always notify with full details including balance
                    shouldNotify = true;
                    notificationMessage = $"[Budget Booster] Boost applied: +${boostAmount:N0} | New balance: ${newBalance:N0} | Session total: ${SessionStatistics.TotalMoneyAdded:N0}";
                    break;

                default:
                    // Default to Normal behavior
                    shouldNotify = true;
                    notificationMessage = $"[Budget Booster] Boost applied: +${boostAmount:N0}";
                    break;
            }

            // Send the notification (via log - appears in Logs\Budget Booster.log)
            if (shouldNotify && !string.IsNullOrEmpty(notificationMessage))
            {
                Mod.Log.Info(notificationMessage);
            }
        }

        /// <summary>
        /// Reads the raw expense figure from CityServiceBudgetSystem, with no interpretation.
        /// </summary>
        /// <remarks>
        /// The value is returned exactly as the game reports it, including its sign. Interpreting it
        /// is the reloadable logic assembly's job - see BoostLogic.GetDailyExpenses. Keeping raw game
        /// data on the host side and interpretation on the reloadable side is what makes this class of
        /// bug fixable without a restart.
        /// </remarks>
        /// <returns>True if a value was read.</returns>
        private bool TryReadRawExpenses(out int rawExpenses)
        {
            rawExpenses = 0;

            if (_budgetSystem == null)
                return false;

            try
            {
                rawExpenses = _budgetSystem.GetTotalExpenses();
                return true;
            }
            catch (Exception ex)
            {
                _expenseReadFailures++;
                if (_expenseReadFailures % 100 == 1)
                {
                    Mod.Log.Warn($"[Budget Booster] Failed to read city expenses: {ex.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Reads the raw income figure from CityServiceBudgetSystem, with no interpretation.
        /// </summary>
        private bool TryReadRawIncome(out int rawIncome)
        {
            rawIncome = 0;

            if (_budgetSystem == null)
                return false;

            try
            {
                rawIncome = _budgetSystem.GetTotalIncome();
                return true;
            }
            catch (Exception ex)
            {
                _expenseReadFailures++;
                if (_expenseReadFailures % 100 == 1)
                {
                    Mod.Log.Warn($"[Budget Booster] Failed to read city income: {ex.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Hands the decision to the reloadable logic assembly.
        /// </summary>
        /// <remarks>
        /// If no logic is loaded, or it throws, this returns 0 rather than guessing. A silent host-side
        /// fallback would defeat the purpose: it would mask a broken logic build behind plausible
        /// behaviour, which is precisely the failure mode this split exists to avoid.
        /// </remarks>
        private long CalculateBoostViaLogic(long currentMoney, ModSettings settings)
        {
            var logic = LogicHost.Current;

            if (logic == null)
            {
                if (!_hasLoggedMissingLogic)
                {
                    _hasLoggedMissingLogic = true;
                    Mod.Log.Error($"[Budget Booster] No boost logic loaded - no boosts will be applied. {LogicHost.LastError}");
                }
                return 0;
            }

            try
            {
                return logic.CalculateBoost(new BoostContext(this, currentMoney, settings));
            }
            catch (Exception ex)
            {
                Mod.Log.Error($"[Budget Booster] Boost logic threw, skipping this cycle: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Per-cycle view of raw game data handed to the reloadable logic.
        /// </summary>
        private sealed class BoostContext : IBoostContext
        {
            private readonly BudgetBoosterSystem _system;

            public BoostContext(BudgetBoosterSystem system, long currentMoney, ModSettings settings)
            {
                _system = system;
                CurrentMoney = currentMoney;
                Settings = settings;
            }

            public long CurrentMoney { get; }

            public ModSettings Settings { get; }

            // OnUpdate counts one tick per call and runs a cycle every UpdateInterval calls.
            public int TicksPerCycle => Settings.UpdateInterval;

            public int TicksPerDay => TimeSystem.kTicksPerDay;

            public bool TryGetRawTotalExpenses(out int rawExpenses) => _system.TryReadRawExpenses(out rawExpenses);

            public bool TryGetRawTotalIncome(out int rawIncome) => _system.TryReadRawIncome(out rawIncome);

            public void LogInfo(string message) => Mod.Log.Info(message);

            public void LogWarn(string message) => Mod.Log.Warn(message);
        }

        /// <summary>
        /// Logs game state information for debugging.
        /// </summary>
        private void LogGameState(ModSettings settings)
        {
            try
            {
                var cityEntity = _citySystem!.City;
                if (cityEntity == Entity.Null)
                {
                    Mod.Log.Info("[Budget Booster] Game State: City entity not available");
                    return;
                }

                if (!EntityManager.HasComponent<PlayerMoney>(cityEntity))
                {
                    Mod.Log.Info("[Budget Booster] Game State: PlayerMoney component not available");
                    return;
                }

                var playerMoney = EntityManager.GetComponentData<PlayerMoney>(cityEntity);
                bool haveExpenses = TryReadRawExpenses(out int rawExpenses);
                long totalExpenses = haveExpenses ? -(long)rawExpenses : 0;

                Mod.Log.Info($@"[Budget Booster] === Game State Report ===
  Current Balance: ${playerMoney.money:N0}
  Unlimited Money: {playerMoney.m_Unlimited}
  City Expenses: ${totalExpenses:N0}/day (raw: {(haveExpenses ? rawExpenses.ToString("N0") : "unavailable")})
  Logic Generation: {LogicHost.Generation} - {LogicHost.Current?.Describe() ?? "none loaded"}
  Session Boosts Applied: {SessionStatistics.TotalBoosts}
  Session Money Added: ${SessionStatistics.TotalMoneyAdded:N0}
  Last Boost: ${SessionStatistics.LastBoostAmount:N0}
  System Status: {(_isInitialized ? "Initialized" : "Not Initialized")}
  CitySystem: {(_citySystem != null ? "Available" : "Null")}
  BudgetSystem: {(_budgetSystem != null ? "Available" : "Null")}
  Notifications: {(settings.EnableNotifications ? settings.NotificationVerbosity.ToString() : "Disabled")}
================================");
            }
            catch (Exception ex)
            {
                Mod.Log.Error($"[Budget Booster] Failed to log game state: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes the city half of the Run Diagnostics report: the figures that need the EntityManager
        /// and a loaded city. The rest of the report is written immediately by the button itself, see
        /// <see cref="Mod.LogDiagnosticReport"/>.
        /// </summary>
        private void RunCityDiagnostics(ModSettings settings)
        {
            Mod.Log.Info("[Budget Booster] ========== CITY DIAGNOSTICS ==========");

            // System status
            Mod.Log.Info($"[Budget Booster] System Initialized: {_isInitialized}");
            Mod.Log.Info($"[Budget Booster] CitySystem Reference: {(_citySystem != null ? "Valid" : "NULL")}");

            long currentMoney = 0;

            // City entity status
            if (_citySystem != null)
            {
                var cityEntity = _citySystem.City;
                Mod.Log.Info($"[Budget Booster] City Entity: {(cityEntity != Entity.Null ? "Valid" : "NULL")}");

                if (cityEntity != Entity.Null)
                {
                    bool hasPlayerMoney = EntityManager.HasComponent<PlayerMoney>(cityEntity);
                    Mod.Log.Info($"[Budget Booster] Has PlayerMoney Component: {hasPlayerMoney}");

                    if (hasPlayerMoney)
                    {
                        try
                        {
                            var playerMoney = EntityManager.GetComponentData<PlayerMoney>(cityEntity);
                            currentMoney = playerMoney.money;
                            Mod.Log.Info($"[Budget Booster] Current Balance: ${currentMoney:N0}");
                            Mod.Log.Info($"[Budget Booster] Unlimited Money: {playerMoney.m_Unlimited}{(playerMoney.m_Unlimited ? " (boosts are paused)" : "")}");
                        }
                        catch (Exception ex)
                        {
                            Mod.Log.Error($"[Budget Booster] PlayerMoney read FAILED - {ex.Message}");
                        }
                    }
                }
            }

            // City expense data (used by Percentage mode)
            if (_budgetSystem == null)
            {
                Mod.Log.Info("[Budget Booster] City Expenses API: Unavailable (Percentage mode scales BoostAmount)");
            }
            else
            {
                // Report the game's values UNMODIFIED. Sign conventions are the single most
                // common source of wrong-but-plausible readings, so show what the API actually
                // returned rather than what we made of it.
                string rawExp = TryReadRawExpenses(out int re) ? re.ToString("N0") : "unavailable";
                string rawInc = TryReadRawIncome(out int ri) ? ri.ToString("N0") : "unavailable";

                Mod.Log.Info($"[Budget Booster] Raw GetTotalExpenses(): {rawExp}");
                Mod.Log.Info($"[Budget Booster] Raw GetTotalIncome():   {rawInc}");
                Mod.Log.Info($"[Budget Booster] Expense Read Failures: {_expenseReadFailures:N0}");
            }

            // Which Percentage path the live logic would take. Asked of the logic itself, so this
            // line cannot disagree with what it actually does.
            Mod.Log.Info($"[Budget Booster] Percentage path: {DescribePercentagePath(currentMoney, settings)}");

            Mod.Log.Info($"[Budget Booster] Ticks Per Cycle: {settings.UpdateInterval}, Ticks Per Day: {TimeSystem.kTicksPerDay:N0}, Tick Counter: {_tickCounter}");
            Mod.Log.Info("[Budget Booster] ======================================");
        }

        /// <summary>
        /// Asks the live logic which Percentage path it would take right now.
        /// </summary>
        private string DescribePercentagePath(long currentMoney, ModSettings settings)
        {
            var logic = LogicHost.Current;
            if (logic == null)
                return "unknown (no boost logic loaded)";

            try
            {
                return logic.DescribePercentagePath(new BoostContext(this, currentMoney, settings));
            }
            catch (Exception ex)
            {
                return $"unknown (boost logic threw: {ex.Message})";
            }
        }

        /// <summary>
        /// Logs a debug message only once (using a flag).
        /// Uses AggressiveInlining for performance in hot paths.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogDebugOnce(ref bool flag, string message)
        {
            if (!flag)
            {
                Mod.Log.Info(message);
                flag = true;
            }
        }

        protected override void OnDestroy()
        {
            // Session totals are logged by Mod.OnDispose. This can run after it, when they are
            // already cleared, so they are not repeated here.
            Mod.Log.Info("[Budget Booster] BudgetBoosterSystem destroyed.");
            base.OnDestroy();
        }
    }
}
