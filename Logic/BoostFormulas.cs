namespace BudgetBooster.Logic
{
    /// <summary>
    /// The boost arithmetic, as pure functions over primitives.
    ///
    /// <see cref="BoostLogic"/> reads settings and game data, then calls these to do the sums. Keeping
    /// the sums free of ModSettings and game types means they can be checked outside the game.
    /// Everything uses long math, so no input the settings sliders allow can overflow.
    /// </summary>
    public static class BoostFormulas
    {
        /// <summary>
        /// Floor for the Percentage fallback path, so a small percentage of Boost Amount still does
        /// something noticeable. Not applied to the expense path: that path runs every cycle, and a
        /// per-cycle floor would add far more than the configured share of a day's expenses.
        /// </summary>
        public const int MinimumFallbackBoost = 10000;

        /// <summary>
        /// Auto-Balance: below the target, add what it takes to get back to the target plus the
        /// configured boost. At or above the target, add nothing.
        /// </summary>
        public static long AutoBalance(long currentMoney, long targetSurplus, long boostAmount)
        {
            if (currentMoney >= targetSurplus)
                return 0;

            return targetSurplus - currentMoney + boostAmount;
        }

        /// <summary>
        /// Percentage, expense path: the given percentage of one day's expenses, scaled down to one
        /// cycle's share of the day.
        /// </summary>
        /// <remarks>
        /// Multiplies everything before the single division so integer truncation happens once.
        /// Worst case is 2^31 * 100 * 1024, about 2.2e14, well inside a long.
        /// </remarks>
        /// <param name="dailyExpenses">Positive per-day expense total.</param>
        /// <param name="percentage">Share of the expenses to add, 1 to 100.</param>
        /// <param name="ticksPerCycle">Simulation ticks between boost cycles.</param>
        /// <param name="ticksPerDay">Simulation ticks in one in-game day.</param>
        /// <returns>Boost for one cycle, or 0 when any input is not positive.</returns>
        public static long PercentageOfDailyExpenses(long dailyExpenses, int percentage, int ticksPerCycle, int ticksPerDay)
        {
            if (dailyExpenses <= 0 || percentage <= 0 || ticksPerCycle <= 0 || ticksPerDay <= 0)
                return 0;

            return dailyExpenses * percentage * ticksPerCycle / (100L * ticksPerDay);
        }

        /// <summary>
        /// Percentage, fallback path used when no expense figure is available: the given percentage
        /// of Boost Amount per cycle, never less than <see cref="MinimumFallbackBoost"/>.
        /// </summary>
        public static long PercentageFallback(long boostAmount, int percentage)
        {
            long boost = boostAmount * percentage / 100;
            return boost < MinimumFallbackBoost ? MinimumFallbackBoost : boost;
        }

        /// <summary>
        /// Applies the Max Boost Per Cycle cap (when positive) and never returns a negative amount.
        /// </summary>
        public static long CapToMaxPerCycle(long boost, long maxBoostPerCycle)
        {
            if (maxBoostPerCycle > 0 && boost > maxBoostPerCycle)
                boost = maxBoostPerCycle;

            return boost < 0 ? 0 : boost;
        }
    }
}
