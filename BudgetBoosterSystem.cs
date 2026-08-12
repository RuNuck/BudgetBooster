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
        private bool _isInitialized = false;
        private bool _hasLoggedInitialization = false;

        // Constants for game state logging (every ~10 update intervals in debug mode)
        private const int GameStateLogFrequency = 10;

        // Minimum boost amount for Percentage mode (ensures meaningful contribution)
        private const int MinimumPercentageModeBoost = 10000;

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

            // Calculate boost based on mode
            long boostAmount = CalculateBoost(currentMoney, settings);

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

            // Send the notification (via log - appears in Player.log)
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
        /// Gets the city's total expenses from CityServiceBudgetSystem.
        /// </summary>
        /// <remarks>
        /// Expense data is read via the public ICityServiceBudgetSystem interface - the same one the
        /// in-game budget panel uses. Earlier versions returned a hardcoded 0 on the assumption that
        /// this was not queryable by mods, which left Percentage mode permanently on its fallback path.
        ///
        /// Returning 0 remains meaningful: it signals "no usable expense figure" and makes
        /// <see cref="CalculateBoost"/> fall back to scaling BoostAmount instead. That happens when the
        /// budget system is unavailable, when a read fails, and legitimately in a brand-new city that
        /// has not incurred any expenses yet.
        /// </remarks>
        /// <returns>Total city expenses, or 0 if unavailable.</returns>
        private long GetTotalCityExpenses()
        {
            if (_budgetSystem == null)
                return 0;

            try
            {
                int expenses = _budgetSystem.GetTotalExpenses();
                return expenses > 0 ? expenses : 0;
            }
            catch (Exception ex)
            {
                // Log occasionally rather than every cycle, matching the money read behaviour.
                _expenseReadFailures++;
                if (_expenseReadFailures % 100 == 1)
                {
                    Mod.Log.Warn($"[Budget Booster] Failed to read city expenses: {ex.Message}");
                }
                return 0;
            }
        }

        /// <summary>
        /// Calculates how much money to add based on current settings and balance.
        /// </summary>
        private long CalculateBoost(long currentMoney, ModSettings settings)
        {
            // Validate settings before calculation
            if (settings.BoostAmount <= 0)
            {
                Mod.Log.Warn("[Budget Booster] BoostAmount is invalid (<=0), skipping boost");
                return 0;
            }

            long boost = 0;

            switch (settings.BoostMode)
            {
                case ModSettings.BoostModeType.AutoBalance:
                    // Only boost if we're below the target surplus
                    if (currentMoney < settings.TargetSurplus)
                    {
                        // Calculate how much we need to reach target + boost
                        boost = settings.TargetSurplus - currentMoney + settings.BoostAmount;
                    }
                    break;

                case ModSettings.BoostModeType.FixedBoost:
                    // Always add the fixed boost amount
                    boost = settings.BoostAmount;
                    break;

                case ModSettings.BoostModeType.Percentage:
                    // Calculate boost as a percentage of the city's total expenses
                    // This allows the boost to scale with your city's actual running costs
                    if (settings.BoostPercentage <= 0)
                    {
                        Mod.Log.Warn("[Budget Booster] BoostPercentage is invalid (<=0), using minimum");
                        boost = MinimumPercentageModeBoost;
                    }
                    else
                    {
                        // Try to get actual city expenses
                        long totalExpenses = GetTotalCityExpenses();

                        if (totalExpenses > 0)
                        {
                            // Calculate boost based on actual city expenses
                            // e.g., if expenses are $100,000/month and percentage is 10%, boost = $10,000
                            boost = totalExpenses * settings.BoostPercentage / 100;

                            if (settings.DebugMode)
                            {
                                Mod.Log.Info($"[Budget Booster] Percentage mode: {settings.BoostPercentage}% of ${totalExpenses:N0} expenses = ${boost:N0}");
                            }
                        }
                        else
                        {
                            // Fallback: use BoostAmount as base if expenses unavailable
                            // This ensures the mode still works even if expense data isn't accessible
                            boost = (long)settings.BoostAmount * settings.BoostPercentage / 100;

                            if (settings.DebugMode)
                            {
                                Mod.Log.Info($"[Budget Booster] Percentage mode (fallback): {settings.BoostPercentage}% of ${settings.BoostAmount:N0} BoostAmount = ${boost:N0}");
                            }
                        }

                        // Enforce minimum boost to ensure meaningful contribution
                        if (boost < MinimumPercentageModeBoost)
                        {
                            boost = MinimumPercentageModeBoost;
                        }
                    }
                    break;

                default:
                    // Default to auto-balance behavior
                    Mod.Log.Warn($"[Budget Booster] Unknown BoostMode: {settings.BoostMode}, using AutoBalance");
                    if (currentMoney < settings.TargetSurplus)
                    {
                        boost = settings.TargetSurplus - currentMoney + settings.BoostAmount;
                    }
                    break;
            }

            // Cap the boost to prevent massive jumps
            if (settings.MaxBoostPerCycle > 0 && boost > settings.MaxBoostPerCycle)
            {
                boost = settings.MaxBoostPerCycle;
            }

            // Ensure non-negative result
            return Math.Max(0, boost);
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
                long totalExpenses = GetTotalCityExpenses();

                Mod.Log.Info($@"[Budget Booster] === Game State Report ===
  Current Balance: ${currentMoney:N0}
  City Expenses: ${totalExpenses:N0}{(totalExpenses == 0 ? (_budgetSystem == null ? " (system unavailable)" : " (none recorded yet)") : "")}
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
                        long expenses = GetTotalCityExpenses();
                        Mod.Log.Info($"[Budget Booster] City Expenses API: Working - Total expenses: ${expenses:N0}");
                        Mod.Log.Info($"[Budget Booster] Expense Read Failures: {_expenseReadFailures:N0}");
                    }
                }
            }

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
