namespace BudgetBooster
{
    /// <summary>
    /// The stable contract between the host assembly and the reloadable logic assembly.
    ///
    /// This interface lives in the HOST (BudgetBooster.dll), which is never reloaded, so every
    /// generation of BudgetBooster.Logic.dll shares one type identity for it. If this interface were
    /// defined in the reloadable assembly, each reload would produce a structurally identical but
    /// distinct type and the cast in <see cref="LogicHost"/> would fail.
    ///
    /// Changing anything in this file requires a full game restart, not a reload.
    /// </summary>
    public interface IBoostLogic
    {
        /// <summary>
        /// Identifies this build of the logic assembly. Logged on every successful reload so it is
        /// obvious which generation is live.
        /// </summary>
        string Describe();

        /// <summary>
        /// Decides how much money to add this cycle.
        /// </summary>
        /// <param name="ctx">Raw game data and services supplied by the host.</param>
        /// <returns>Amount to add, or 0 for no boost.</returns>
        long CalculateBoost(IBoostContext ctx);

        /// <summary>
        /// Says which Percentage mode path <see cref="CalculateBoost"/> would take with this context,
        /// for the Run Diagnostics report. The logic answers this itself so the report cannot
        /// disagree with the calculation.
        /// </summary>
        /// <param name="ctx">Raw game data and services supplied by the host.</param>
        /// <returns>For example "expenses ($1,000,000/day)" or "fallback (Boost Amount)".</returns>
        string DescribePercentagePath(IBoostContext ctx);
    }

    /// <summary>
    /// Everything the reloadable logic is allowed to see, supplied by the host each cycle.
    ///
    /// The split is deliberate: the host owns *plumbing* (ECS lookups, game constants, raw API calls) and
    /// the logic owns *interpretation* (sign conventions, fallbacks, arithmetic). Every defect this
    /// mod has hit so far - PlayerMoney living in the wrong namespace, GetTotalExpenses returning a
    /// negative figure, the clamp that discarded it - was interpretation. Putting that side behind the
    /// reload seam is the entire point.
    ///
    /// Accordingly this exposes RAW values. Do not pre-interpret them in the host, or the interesting
    /// bugs move back to the side that needs a restart.
    /// </summary>
    public interface IBoostContext
    {
        /// <summary>The city's current money balance, from the game's PlayerMoney.money.</summary>
        long CurrentMoney { get; }

        /// <summary>
        /// Simulation ticks between boost cycles. The host runs one cycle every this many ticks, so a
        /// per-day figure covers TicksPerDay / TicksPerCycle cycles.
        /// </summary>
        int TicksPerCycle { get; }

        /// <summary>Simulation ticks in one in-game day (the game's TimeSystem.kTicksPerDay).</summary>
        int TicksPerDay { get; }

        /// <summary>
        /// The unmodified return value of CityServiceBudgetSystem.GetTotalExpenses(), a per-day total.
        /// Note this is NEGATIVE for a city with expenses - the logic assembly is responsible for
        /// interpreting the sign. False when the budget system is unavailable.
        /// </summary>
        bool TryGetRawTotalExpenses(out int rawExpenses);

        /// <summary>The unmodified return value of CityServiceBudgetSystem.GetTotalIncome().</summary>
        bool TryGetRawTotalIncome(out int rawIncome);

        /// <summary>Current user settings. Owned by the host; treat as read-only.</summary>
        ModSettings Settings { get; }

        /// <summary>Writes an informational line to the mod log.</summary>
        void LogInfo(string message);

        /// <summary>Writes a warning line to the mod log.</summary>
        void LogWarn(string message);
    }
}
