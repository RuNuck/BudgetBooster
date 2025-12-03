using System;
using Game;
using Game.City;
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
        private CitySystem? _citySystem;
        private int _tickCounter = 0;

        protected override void OnCreate()
        {
            base.OnCreate();

            // Get reference to the city system
            _citySystem = World.GetExistingSystemManaged<CitySystem>();

            if (_citySystem == null)
            {
                Mod.Log.Error("BudgetBoosterSystem: CitySystem is null! System will not function.");
                return;
            }

            Mod.Log.Info("BudgetBoosterSystem initialized and ready!");
        }

        protected override void OnUpdate()
        {
            // Get settings from the shared Mod instance
            var settings = Mod.Settings;
            if (settings == null)
                return;

            // Check if mod is enabled
            if (!settings.Enabled)
                return;

            // Check if we have the required systems
            if (_citySystem == null)
                return;

            // Only run on configured interval to avoid performance impact
            _tickCounter++;
            if (_tickCounter < settings.UpdateInterval)
                return;

            _tickCounter = 0;

            // Get current player money
            var entityManager = EntityManager;
            var cityEntity = _citySystem.City;

            if (cityEntity == Entity.Null)
                return;

            // Check if entity has PlayerMoney component
            if (!entityManager.HasComponent<PlayerMoney>(cityEntity))
                return;

            var playerMoney = entityManager.GetComponentData<PlayerMoney>(cityEntity);

            // Get current balance using reflection or the raw value
            // PlayerMoney stores money in a specific format
            long currentMoney = GetCurrentMoney(playerMoney);

            // Calculate boost based on mode
            long boostAmount = CalculateBoost(currentMoney, settings);

            if (boostAmount > 0)
            {
                // Apply the boost
                int boostInt = (int)System.Math.Min(boostAmount, int.MaxValue);
                playerMoney.Add(boostInt);
                entityManager.SetComponentData(cityEntity, playerMoney);

                Mod.Log.Info($"Budget Booster: Added ${boostAmount:N0} to city funds");
            }
        }

        /// <summary>
        /// Gets the current money value from the PlayerMoney component.
        /// </summary>
        private long GetCurrentMoney(PlayerMoney playerMoney)
        {
            // PlayerMoney has internal storage - we need to get the raw value
            // Using GetRaw() method if available, otherwise estimate from component
            try
            {
                // Try to get the raw money value
                return (long)playerMoney.GetRaw();
            }
            catch (Exception ex)
            {
                // Log the exception instead of silently swallowing it
                Mod.Log.Warn($"BudgetBoosterSystem: Failed to get current money value: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Calculates how much money to add based on current settings and balance.
        /// </summary>
        private long CalculateBoost(long currentMoney, ModSettings settings)
        {
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
                    // For percentage mode, we'd ideally look at expenses
                    // For now, use BoostAmount as a base
                    // In a full implementation, you'd query the economy system
                    boost = settings.BoostAmount * settings.BoostPercentage / 100;
                    if (boost < 10000) boost = 10000; // Minimum boost
                    break;

                default:
                    // Default to auto-balance behavior
                    if (currentMoney < settings.TargetSurplus)
                    {
                        boost = settings.TargetSurplus - currentMoney + settings.BoostAmount;
                    }
                    break;
            }

            // Cap the boost to prevent massive jumps
            if (boost > settings.MaxBoostPerCycle)
            {
                boost = settings.MaxBoostPerCycle;
            }

            return boost;
        }

        protected override void OnDestroy()
        {
            Mod.Log.Info("BudgetBoosterSystem destroyed");
            base.OnDestroy();
        }
    }
}
