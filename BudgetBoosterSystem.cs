using System;
using System.Runtime.CompilerServices;
using Game;
using Game.City;
using Game.Economy;
using Game.Simulation;
using Unity.Entities;

namespace BudgetBooster
{
    /// <summary>
    /// The main ECS system that monitors the city budget and adds money as needed.
    /// Registered to run during the GameSimulation phase.
    /// </summary>
    [UpdateAfter(typeof(CitySystem))]
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
        private bool _hasLoggedInitialization = false;

        // Constants for game state logging (every ~10 update intervals in debug mode)
        private const int GameStateLogFrequency = 10;

        // Boost calculation constants now live in the reloadable logic assembly (BoostLogic).

        /// <summary>
        /// Gets whether the system is properly initialized and ready to function.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets whether the CitySystem reference is valid.
        /// </summary>
        public bool HasValidCitySystem => _citySystem != null;

        /// <summary>
        /// Gets whether city expense data is available. When false, Percentage mode scales BoostAmount
        /// instead of actual expenses.
        /// </summary>
        public bool HasValidBudgetSystem => _budgetSystem != null;

        /// <summary>
        /// Gets the total number of boosts applied this session.
        /// </summary>
        public int TotalBoostsApplied => SessionStatistics.TotalBoosts;

        /// <summary>
        /// Gets the total money added this session.
        /// </summary>
        public long TotalMoneyAdded => SessionStatistics.TotalMoneyAdded;

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

                // Initialize the PlayerMoneyAccessor for reflection-based money access
                Mod.Log.Info("[Budget Booster] Initializing PlayerMoneyAccessor...");
                bool accessorResult = PlayerMoneyAccessor.Initialize();
                Mod.Log.Info($"[Budget Booster] PlayerMoneyAccessor.Initialize() returned: {accessorResult}");
                
                if (!accessorResult)
                {
                    Mod.Log.Warn("[Budget Booster] PlayerMoneyAccessor initialization failed - balance reading may not work");
                    Mod.Log.Warn($"[Budget Booster] Last error: {PlayerMoneyAccessor.LastError}");
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
                // Get settings from the shared Mod instance
                var settings = Mod.Settings;

                // Defensive null check for settings
                if (settings == null)
                {
                    LogDebugOnce(ref _hasLoggedInitialization, "[Budget Booster] Settings are null - waiting for initialization");
                    return;
                }

                // Handle diagnostics request
                if (settings.DiagnosticsRequested)
                {
                    RunDiagnostics(settings);
                    settings.DiagnosticsRequested = false;
                }

                // Check if mod is enabled
                if (!settings.Enabled)
                    return;

                // Validate system state
                if (!_isInitialized)
                {
                    LogDebugConditional(settings, "[Budget Booster] System not initialized, skipping update");
                    return;
                }

                // Check if we have the required systems
                if (_citySystem == null)
                {
                    LogDebugConditional(settings, "[Budget Booster] CitySystem is null, skipping update");
                    return;
                }

                // Apply any queued logic swap here, at the top of the update and before any cycle
                // work begins. Reloads are requested from the settings UI on a different thread, so
                // this is the safe point where nothing is mid-flight.
                LogicHost.ApplyPendingSwap();

                // Only run on configured interval to avoid performance impact
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
            // Get current player money with defensive checks
            var entityManager = EntityManager;

            if (entityManager == null)
            {
                LogDebugConditional(settings, "[Budget Booster] EntityManager is null");
                return;
            }

            var cityEntity = _citySystem!.City;

            if (cityEntity == Entity.Null)
            {
                LogDebugConditional(settings, "[Budget Booster] City entity is null");
                return;
            }

            // Check if entity has PlayerMoney component
            if (!entityManager.HasComponent<PlayerMoney>(cityEntity))
            {
                LogDebugConditional(settings, "[Budget Booster] City entity missing PlayerMoney component");
                return;
            }

            PlayerMoney playerMoney;
            try
            {
                playerMoney = entityManager.GetComponentData<PlayerMoney>(cityEntity);
            }
            catch (Exception ex)
            {
                Mod.Log.Error($"[Budget Booster] Failed to get PlayerMoney component: {ex.Message}");
                return;
            }

            // Get current balance
            long currentMoney = GetCurrentMoney(playerMoney);

            // Delegate the decision to the reloadable logic assembly.
            long boostAmount = CalculateBoostViaLogic(currentMoney, settings);

            // Log debug info for this cycle
            if (settings.DebugMode && settings.LogBoostCycles)
            {
                Mod.Log.Info($"[Budget Booster] Boost cycle - Current: ${currentMoney:N0}, Mode: {settings.BoostMode}, Calculated boost: ${boostAmount:N0}");
            }

            if (boostAmount > 0)
            {
                try
                {
                    // Apply the boost
                    int boostInt = (int)Math.Min(boostAmount, int.MaxValue);
                    playerMoney.Add(boostInt);
                    entityManager.SetComponentData(cityEntity, playerMoney);

                    // Get new balance after boost
                    long newBalance = currentMoney + boostInt;

                    // Update session statistics (single source of truth for UI display)
                    SessionStatistics.RecordBoost(boostInt, newBalance);

                    // Handle notifications based on settings
                    SendBoostNotification(settings, boostInt, newBalance);
                }
                catch (Exception ex)
                {
                    Mod.Log.Error($"[Budget Booster] Failed to apply boost: {ex.Message}");
                }
            }
            else
            {
                // Still update the current balance even when no boost applied
                SessionStatistics.CurrentBalance = currentMoney;

                if (settings.DebugMode && settings.LogBoostCycles)
                {
                    Mod.Log.Info($"[Budget Booster] No boost needed - Current balance: ${currentMoney:N0}");
                }
            }
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
            {
                // Still log in debug mode even if notifications are off
                if (settings.DebugMode && settings.LogBoostCycles)
                {
                    Mod.Log.Info($"[Budget Booster] [DEBUG] Boost applied: +${boostAmount:N0} (notifications disabled)");
                }
                return;
            }

            // Determine if we should show this notification based on verbosity level
            bool shouldNotify = false;
            string notificationMessage = string.Empty;

            switch (settings.NotificationVerbosity)
            {
                case ModSettings.NotificationLevel.Minimal:
                    // Only notify for boosts above the threshold
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
        /// Gets the current money value from the PlayerMoney component.
        /// Delegates to PlayerMoneyAccessor for centralized reflection handling.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetCurrentMoney(PlayerMoney playerMoney)
        {
            if (PlayerMoneyAccessor.TryGetMoney(playerMoney, out long money))
            {
                return money;
            }

            // Log error only occasionally to avoid spam (check before string allocation)
            int failedReads = PlayerMoneyAccessor.FailedReads;
            if (failedReads % 100 == 1)
            {
                Mod.Log.Warn($"[Budget Booster] Failed to read money: {PlayerMoneyAccessor.LastError}");
            }
            return 0;
        }

        /// <summary>
        /// Reads the raw expense figure from CityServiceBudgetSystem, with no interpretation.
        /// </summary>
        /// <remarks>
        /// The value is returned exactly as the game reports it, including its sign. Interpreting it
        /// is the reloadable logic assembly's job - see BoostLogic.GetTotalExpenses. Keeping raw game
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
                long currentMoney = GetCurrentMoney(playerMoney);
                bool haveExpenses = TryReadRawExpenses(out int rawExpenses);
                long totalExpenses = haveExpenses ? -(long)rawExpenses : 0;

                Mod.Log.Info($@"[Budget Booster] === Game State Report ===
  Current Balance: ${currentMoney:N0}
  City Expenses: ${totalExpenses:N0} (raw: {(haveExpenses ? rawExpenses.ToString("N0") : "unavailable")})
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
        /// Runs comprehensive diagnostics and logs the results.
        /// </summary>
        private void RunDiagnostics(ModSettings settings)
        {
            Mod.Log.Info("[Budget Booster] ========== DIAGNOSTIC REPORT ==========");

            // System status
            Mod.Log.Info($"[Budget Booster] System Initialized: {_isInitialized}");
            Mod.Log.Info($"[Budget Booster] CitySystem Reference: {(_citySystem != null ? "Valid" : "NULL")}");
            Mod.Log.Info($"[Budget Booster] EntityManager: {(EntityManager != null ? "Valid" : "NULL")}");
            Mod.Log.Info($"[Budget Booster] World: {(World != null ? "Valid" : "NULL")}");

            // City entity status
            if (_citySystem != null)
            {
                var cityEntity = _citySystem.City;
                Mod.Log.Info($"[Budget Booster] City Entity: {(cityEntity != Entity.Null ? "Valid" : "NULL")}");

                if (cityEntity != Entity.Null && EntityManager != null)
                {
                    bool hasPlayerMoney = EntityManager.HasComponent<PlayerMoney>(cityEntity);
                    Mod.Log.Info($"[Budget Booster] Has PlayerMoney Component: {hasPlayerMoney}");

                    if (hasPlayerMoney)
                    {
                        try
                        {
                            var playerMoney = EntityManager.GetComponentData<PlayerMoney>(cityEntity);
                            long currentMoney = GetCurrentMoney(playerMoney);
                            Mod.Log.Info($"[Budget Booster] Current Balance: ${currentMoney:N0}");
                            Mod.Log.Info($"[Budget Booster] PlayerMoney API: Working");
                        }
                        catch (Exception ex)
                        {
                            Mod.Log.Error($"[Budget Booster] PlayerMoney API: FAILED - {ex.Message}");
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
                }
            }

            // Reloadable logic status
            Mod.Log.Info(LogicHost.GetDiagnosticSummary());

            // PlayerMoneyAccessor status
            Mod.Log.Info(PlayerMoneyAccessor.GetDiagnosticSummary());

            // Session statistics
            Mod.Log.Info($"[Budget Booster] Session Boosts Applied: {SessionStatistics.TotalBoosts}");
            Mod.Log.Info($"[Budget Booster] Session Money Added: ${SessionStatistics.TotalMoneyAdded:N0}");
            Mod.Log.Info($"[Budget Booster] Current Tick Counter: {_tickCounter}");

            // Settings summary
            Mod.Log.Info(settings.GetDiagnosticSummary());

            // Harmony patch status (check from the Patches class)
            Mod.Log.Info($"[Budget Booster] System Registered: {Patches.IsSystemRegistered}");

            Mod.Log.Info("[Budget Booster] ========================================");
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

        /// <summary>
        /// Logs a debug message only if debug mode is enabled.
        /// Check debug flag before calling to avoid string allocation when disabled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LogDebugConditional(ModSettings settings, string message)
        {
            if (settings.DebugMode)
            {
                Mod.Log.Info(message);
            }
        }

        /// <summary>
        /// Gets a diagnostic summary for external use.
        /// </summary>
        public string GetSystemDiagnostics()
        {
            return $@"BudgetBoosterSystem Status:
  Initialized: {_isInitialized}
  CitySystem: {(_citySystem != null ? "Valid" : "Null")}
  Total Boosts: {SessionStatistics.TotalBoosts}
  Total Money Added: ${SessionStatistics.TotalMoneyAdded:N0}";
        }

        protected override void OnDestroy()
        {
            Mod.Log.Info($"[Budget Booster] BudgetBoosterSystem destroyed. Session stats: {SessionStatistics.TotalBoosts} boosts, ${SessionStatistics.TotalMoneyAdded:N0} added.");
            base.OnDestroy();
        }
    }
}
