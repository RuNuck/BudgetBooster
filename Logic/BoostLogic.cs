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
    ///   - Anything inside these method bodies, and in <see cref="BoostFormulas"/>
    ///   - New private helpers, new types within this assembly
    ///
    /// What still requires a full restart:
    ///   - Changes to IBoostLogic or IBoostContext (they live in the host)
    ///   - Changes to ModSettings, localization, or the ECS system itself
    /// </summary>
    public sealed class BoostLogic : IBoostLogic
    {
        /// <summary>
        /// Bump this when iterating so the log makes it obvious which build is live. This is the
        /// fastest way to confirm a reload actually took effect.
        /// </summary>
        private const string BuildTag = "BoostLogic 1.5.0";

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
                    boost = BoostFormulas.AutoBalance(ctx.CurrentMoney, settings.TargetSurplus, settings.BoostAmount);
                    break;

                case ModSettings.BoostModeType.FixedBoost:
                    boost = settings.BoostAmount;
                    break;

                case ModSettings.BoostModeType.Percentage:
                    boost = CalculatePercentage(ctx, settings);
                    break;

                default:
                    ctx.LogWarn($"[Budget Booster] Unknown BoostMode: {settings.BoostMode}, using AutoBalance");
                    boost = BoostFormulas.AutoBalance(ctx.CurrentMoney, settings.TargetSurplus, settings.BoostAmount);
                    break;
            }

            // Cap to prevent massive jumps. The host enforces the same cap again before applying.
            return BoostFormulas.CapToMaxPerCycle(boost, settings.MaxBoostPerCycle);
        }

        public string DescribePercentagePath(IBoostContext ctx)
        {
            return TryGetExpensePath(ctx, out long dailyExpenses)
                ? $"expenses (${dailyExpenses:N0}/day)"
                : "fallback (Boost Amount)";
        }

        /// <summary>
        /// Adds Boost Percentage of the city's daily expenses, spread across the cycles in a day.
        /// Falls back to a percentage of Boost Amount per cycle when no usable expense figure exists.
        /// </summary>
        /// <remarks>
        /// GetTotalExpenses() is a per-DAY total, and this runs once per cycle (UpdateInterval ticks,
        /// 256 by default, against 262,144 ticks per day). Up to 1.4.1 the full daily percentage was
        /// added every cycle, about 1,024 times the intended amount at the default interval.
        /// </remarks>
        private static long CalculatePercentage(IBoostContext ctx, ModSettings settings)
        {
            if (settings.BoostPercentage <= 0)
            {
                ctx.LogWarn("[Budget Booster] BoostPercentage is invalid (<=0), using minimum");
                return BoostFormulas.MinimumFallbackBoost;
            }

            if (TryGetExpensePath(ctx, out long dailyExpenses))
            {
                return BoostFormulas.PercentageOfDailyExpenses(dailyExpenses, settings.BoostPercentage, ctx.TicksPerCycle, ctx.TicksPerDay);
            }

            // No usable expense figure - scale BoostAmount instead so the mode still works.
            return BoostFormulas.PercentageFallback(settings.BoostAmount, settings.BoostPercentage);
        }

        /// <summary>
        /// Decides between the expense path and the fallback. Shared by the calculation and by
        /// <see cref="DescribePercentagePath"/> so the diagnostics report cannot disagree with it.
        /// </summary>
        /// <returns>True when expenses are known and the tick figures can scale them to a cycle.</returns>
        private static bool TryGetExpensePath(IBoostContext ctx, out long dailyExpenses)
        {
            dailyExpenses = GetDailyExpenses(ctx);
            return dailyExpenses > 0 && ctx.TicksPerCycle > 0 && ctx.TicksPerDay > 0;
        }

        /// <summary>
        /// Converts the game's raw expense figure into a positive per-day magnitude.
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
        /// <returns>Positive expense total per day, or 0 when unavailable.</returns>
        private static long GetDailyExpenses(IBoostContext ctx)
        {
            if (!ctx.TryGetRawTotalExpenses(out int raw))
                return 0;

            long expenses = -(long)raw;
            return expenses > 0 ? expenses : 0;
        }
    }
}
