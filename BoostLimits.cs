using Game.City;

namespace BudgetBooster
{
    /// <summary>
    /// The host's last word on how much money may be added in one cycle, whatever the logic asked for.
    ///
    /// The logic assembly is reloadable and may be mid-edit, so the host never trusts its answer
    /// blindly. PlayerMoney.Add does an unchecked int add before clamping to the game's limits, so an
    /// unclamped request near the ceiling would wrap a rich city to -2,000,000,000.
    ///
    /// Primitive-only on purpose, so it can be checked outside the game. <see cref="MaxMoney"/> is a
    /// compile-time constant copied from Game.dll, not a runtime lookup.
    /// </summary>
    public static class BoostLimits
    {
        /// <summary>The game's balance ceiling (PlayerMoney.kMaxMoney, 2,000,000,000).</summary>
        public const int MaxMoney = PlayerMoney.kMaxMoney;

        /// <summary>
        /// Clamps a requested boost to what is safe to pass to PlayerMoney.Add.
        /// </summary>
        /// <param name="requested">What the logic asked for.</param>
        /// <param name="currentMoney">The balance before the boost.</param>
        /// <param name="maxBoostPerCycle">The Max Boost Per Cycle setting.</param>
        /// <returns>
        /// min(requested, maxBoostPerCycle, MaxMoney - currentMoney), or 0 when that is not positive.
        /// Always fits an int.
        /// </returns>
        public static int ClampBoost(long requested, long currentMoney, int maxBoostPerCycle)
        {
            long boost = requested;

            if (boost > maxBoostPerCycle)
                boost = maxBoostPerCycle;

            long headroom = (long)MaxMoney - currentMoney;
            if (boost > headroom)
                boost = headroom;

            if (boost <= 0)
                return 0;

            return boost > int.MaxValue ? int.MaxValue : (int)boost;
        }
    }
}
