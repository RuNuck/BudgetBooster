using System;

namespace BudgetBooster.Logic
{
    /// <summary>
    /// The reloadable half of Budget Booster: everything that INTERPRETS game data and decides how
    /// much money to add.
    ///
    /// This assembly is loaded from bytes by <see cref="LogicHost"/> and can be swapped while the game
    /// runs. Rebuild it, click "Reload Boost Logic" in the mod settings, and the next boost cycle uses
    /// the new code without restarting the game or reloading the city.
    ///
    /// What can change here and be reloaded:
    ///   - Anything inside these method bodies
    ///   - New private helpers, new types within this assembly
    ///
    /// What still requires a full restart:
    ///   - Changes to IBoostLogic or IBoostContext (they live in the host)
    ///   - Changes to ModSettings, localization, or the ECS system itself
    /// </summary>
    public sealed class BoostLogic : IBoostLogic
    {
        /// <summary>
        /// Minimum boost for Percentage mode, so a tiny percentage of small expenses still does
        /// something noticeable.
        /// </summary>
        private const int MinimumPercentageModeBoost = 10000;

        /// <summary>
        /// Bump this when iterating so the log makes it obvious which build is live. This is the
        /// fastest way to confirm a reload actually took effect.
        /// </summary>
        private const string BuildTag = "BoostLogic 1.4.0";

        public string Describe() => BuildTag;

        public long CalculateBoost(IBoostContext ctx)
        {
            var settings = ctx.Settings;

            if (settings.BoostAmount <= 0)
            {
                ctx.LogWarn("[Budget Booster] BoostAmount is invalid (<=0), skipping boost");
                return 0;
            }

            long boost;

            switch (settings.BoostMode)
            {
                case ModSettings.BoostModeType.AutoBalance:
                    boost = CalculateAutoBalance(ctx.CurrentMoney, settings);
                    break;

                case ModSettings.BoostModeType.FixedBoost:
                    boost = settings.BoostAmount;
                    break;

                case ModSettings.BoostModeType.Percentage:
                    boost = CalculatePercentage(ctx, settings);
                    break;

                default:
                    ctx.LogWarn($"[Budget Booster] Unknown BoostMode: {settings.BoostMode}, using AutoBalance");
                    boost = CalculateAutoBalance(ctx.CurrentMoney, settings);
                    break;
            }

            // Cap to prevent massive jumps.
            if (settings.MaxBoostPerCycle > 0 && boost > settings.MaxBoostPerCycle)
            {
                boost = settings.MaxBoostPerCycle;
            }

            return Math.Max(0, boost);
        }

        /// <summary>
        /// Tops the balance back up to the target surplus, plus the configured boost on top.
        /// </summary>
        private static long CalculateAutoBalance(long currentMoney, ModSettings settings)
        {
            if (currentMoney >= settings.TargetSurplus)
                return 0;

            return settings.TargetSurplus - currentMoney + settings.BoostAmount;
        }

        /// <summary>
        /// Scales the boost against the city's real expenses, falling back to scaling BoostAmount when
        /// no usable expense figure is available.
        /// </summary>
        private static long CalculatePercentage(IBoostContext ctx, ModSettings settings)
        {
            if (settings.BoostPercentage <= 0)
            {
                ctx.LogWarn("[Budget Booster] BoostPercentage is invalid (<=0), using minimum");
                return MinimumPercentageModeBoost;
            }

            long expenses = GetTotalExpenses(ctx);
            long boost;

            if (expenses > 0)
            {
                boost = expenses * settings.BoostPercentage / 100;

                if (settings.DebugMode)
                {
                    ctx.LogInfo($"[Budget Booster] Percentage mode: {settings.BoostPercentage}% of ${expenses:N0} expenses = ${boost:N0}");
                }
            }
            else
            {
                // No usable expense figure - scale BoostAmount instead so the mode still works.
                boost = (long)settings.BoostAmount * settings.BoostPercentage / 100;

                if (settings.DebugMode)
                {
                    ctx.LogInfo($"[Budget Booster] Percentage mode (fallback): {settings.BoostPercentage}% of ${settings.BoostAmount:N0} BoostAmount = ${boost:N0}");
                }
            }

            return boost < MinimumPercentageModeBoost ? MinimumPercentageModeBoost : boost;
        }

        /// <summary>
        /// Converts the game's raw expense figure into a positive magnitude.
        /// </summary>
        /// <remarks>
        /// CityServiceBudgetSystem.GetTotalExpenses() returns a NEGATIVE number. Internally it
        /// accumulates "total -= expenses[i]" over positive per-source costs, and GetBalance() is
        /// defined as GetTotalIncome() + GetTotalExpenses(), which only yields net profit if the
        /// expense total is negative. An earlier build clamped with "raw > 0 ? raw : 0" and therefore
        /// discarded every valid reading.
        ///
        /// This interpretation living in the reloadable assembly is the whole point of the split: the
        /// same class of mistake is now a rebuild-and-click away from being corrected, rather than a
        /// full game restart.
        /// </remarks>
        /// <returns>Positive expense total, or 0 when unavailable.</returns>
        private static long GetTotalExpenses(IBoostContext ctx)
        {
            if (!ctx.TryGetRawTotalExpenses(out int raw))
                return 0;

            long expenses = -(long)raw;
            return expenses > 0 ? expenses : 0;
        }
    }
}
