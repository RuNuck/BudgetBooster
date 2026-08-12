using System.Threading;

namespace BudgetBooster
{
    /// <summary>
    /// In-memory statistics for the current game session.
    ///
    /// These deliberately do NOT live on <see cref="ModSettings"/>. Any public get/set property on a
    /// ModSetting is written to BudgetBooster.coc, and [SettingsUIHidden] only hides a property from the
    /// options UI - it does not opt out of serialization. Up to v1.2.1 the counters were stored there, so
    /// the "Session Statistics" panel accumulated across restarts and reported lifetime totals under
    /// session labels.
    ///
    /// Being static, these reset when the process exits, which matches the wording used in the UI
    /// ("since the game started"). <see cref="Mod.OnDispose"/> also clears them so a mod reload starts fresh.
    ///
    /// Thread-safe to match the access patterns used by <see cref="PlayerMoneyAccessor"/> and
    /// <see cref="Patches"/>.
    /// </summary>
    public static class SessionStatistics
    {
        private static int _totalBoosts;
        private static long _totalMoneyAdded;
        private static long _lastBoostAmount;
        private static long _currentBalance;

        /// <summary>
        /// Number of boosts applied since the game started.
        /// </summary>
        public static int TotalBoosts => Volatile.Read(ref _totalBoosts);

        /// <summary>
        /// Total money added since the game started.
        /// </summary>
        public static long TotalMoneyAdded => Interlocked.Read(ref _totalMoneyAdded);

        /// <summary>
        /// The most recent boost amount, or 0 if no boost has been applied yet.
        /// </summary>
        public static long LastBoostAmount => Interlocked.Read(ref _lastBoostAmount);

        /// <summary>
        /// The last known city balance. A live readout rather than a tally, so
        /// <see cref="Reset"/> deliberately leaves it alone.
        /// </summary>
        public static long CurrentBalance
        {
            get => Interlocked.Read(ref _currentBalance);
            set => Interlocked.Exchange(ref _currentBalance, value);
        }

        /// <summary>
        /// Records a boost against the session totals.
        /// </summary>
        /// <param name="boostAmount">The amount of money added.</param>
        /// <param name="newBalance">The balance after the boost was applied.</param>
        public static void RecordBoost(long boostAmount, long newBalance)
        {
            Interlocked.Increment(ref _totalBoosts);
            Interlocked.Add(ref _totalMoneyAdded, boostAmount);
            Interlocked.Exchange(ref _lastBoostAmount, boostAmount);
            Interlocked.Exchange(ref _currentBalance, newBalance);
        }

        /// <summary>
        /// Clears the boost tallies. Leaves <see cref="CurrentBalance"/> in place, since it reflects the
        /// city's actual money rather than anything this mod has counted.
        /// </summary>
        public static void Reset()
        {
            Interlocked.Exchange(ref _totalBoosts, 0);
            Interlocked.Exchange(ref _totalMoneyAdded, 0);
            Interlocked.Exchange(ref _lastBoostAmount, 0);
        }
    }
}
