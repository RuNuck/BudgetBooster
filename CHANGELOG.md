# Changelog

All notable changes to Budget Booster are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.5.0] - 2026-10-15

### Changed
- **Percentage boosts are now much smaller.** `GetTotalExpenses()` is a per-day total, and since
  v1.4.0 the mod added Boost Percentage of it on every cycle. At the default interval of 256 ticks a
  day has 1,024 cycles, so the mod paid about 1,024 times what the setting said: 25% of $1,000,000 a
  day came to $250,000 per cycle instead of $244. The expense path now adds
  `daily expenses * percentage / 100 * ticks per cycle / ticks per day`, so Boost Percentage means
  that share of what the city spends. The $10,000 per-cycle minimum no longer applies to this path,
  since a floor on every cycle would overpay in the same way. The fallback path (no expense data:
  Boost Percentage of Boost Amount per cycle, at least $10,000) is unchanged, and so are the preset
  values. The old amounts cannot be reached in this mode any more (100% now means all of what the
  city spends); use Fixed Boost for larger amounts.
- `IBoostContext` gained `TicksPerCycle` and `TicksPerDay`, and `IBoostLogic` gained
  `DescribePercentagePath`. These live in the host, so a restart is needed, not a logic reload.
- The logic assembly is now deployed as `BudgetBooster.Logic.bin`. The game's mod loader loads
  every `.dll` in a mod folder, so `BudgetBooster.Logic.dll` was also loaded once by the game as a
  frozen copy beside the reloadable one. The build deletes a leftover `BudgetBooster.Logic.dll`.
- Run Diagnostics writes the parts that need no city straight away: mod version, load error, boost
  logic status, settings and session statistics. The city figures (balance, Unlimited Money flag, raw
  expenses and income, and which Percentage path is active) follow as a `CITY DIAGNOSTICS` block on
  the next simulation tick, which needs a loaded, unpaused city. The tooltip says so.
- Reload Boost Logic now swaps the new logic in before the Enabled and initialization checks, so a
  reload also takes effect with the mod switched off. Diagnostics already ran before those checks.
- **Log Boost Cycles** is labelled **Log Applied Boosts** and writes one line per cycle that adds
  money. It used to write two lines on every cycle, boost or not; a 13.8 MB log was observed.
  Persistent reasons for skipping a cycle are logged once, when they start.
- The preset tooltips list every setting each preset changes, with its value. Sandbox Mode no
  longer calls $5M "the maximum boost" (the slider goes to $10M).
- Auto-Balance is described the same way everywhere: below Target Surplus it adds what it takes to
  get back to Target Surplus plus Boost Amount, capped by Max Boost Per Cycle. The README, tooltips
  and Paradox Mods description had three different versions.

### Fixed
- Boosts that hit the $2,000,000,000 balance ceiling were recorded at the full requested amount, so
  the statistics overstated money added and Current Balance could show more than $2B. The mod now
  records and reports only what actually landed, and skips the cycle when nothing did.
- The host now clamps every boost to Max Boost Per Cycle and to the room left below the ceiling
  before calling `PlayerMoney.Add`. That method adds without overflow checks, so a logic build asking
  for more than about $147M at the ceiling would have wrapped the balance to -$2B.
- The balance was read through reflection on the private `m_Money` field, which ignored the game's
  Unlimited Money option, and a failed read counted as $0, so Auto-Balance paid out on every cycle.
  The mod now uses the public `PlayerMoney.money` getter and `m_Unlimited` field, and does nothing
  while Unlimited Money is on.
- The legacy settings migration was gated on `CurrentSettingsVersion`, so the next version bump
  would have run it again and overwritten current settings with v1.2.0 values. Each migration step
  now runs only for files older than the version it upgrades to.
- Run Diagnostics was a saved setting, so a click took effect on the next tick or, if the game was
  closed first, in the next session. The request is now an in-memory flag.
- The diagnostics report printed `PlayerMoney API: Working` even after a failed read, and never
  said which Percentage path was in use. It now reports the real state.
- Load errors never reached the user: the stored load error was never printed, and the mod logged
  `loaded successfully!` even when the boost logic failed to load. It now logs
  `loaded with errors:` in that case, and Run Diagnostics shows the load error.
- The shutdown log always said 0 boosts, because statistics were cleared before they were logged.
  The English locale source was never removed on dispose. Both fixed.
- `dotnet clean` only removed `BudgetBooster.dll` from the mods folder. It now also removes
  `BudgetBooster.Logic.bin`, a stale `BudgetBooster.Logic.dll`, and the deployed README.
- The Minimal Notification Threshold tooltip said "larger than"; a boost of exactly the threshold
  also notifies.

### Removed
- `PlayerMoneyAccessor.cs`, 440 lines of reflection, replaced by the game's public API.
- `Patches.cs` and every mention of Harmony in code and in the README's feature list. The mod never
  used Harmony.
- The saved `DiagnosticsRequested` setting. Existing `.coc` files still load; the stale key is
  dropped on the next save.
- Unused members: `BudgetBoosterSystem.GetSystemDiagnostics`, `IsInitialized`,
  `HasValidCitySystem`, `HasValidBudgetSystem`, `TotalBoostsApplied`, `TotalMoneyAdded`, and
  `LogicHost.HasPendingSwap`.
- `[UpdateAfter(typeof(CitySystem))]` on `BudgetBoosterSystem`. CS2's `UpdateSystem` ignores it.
- The per-cycle Percentage debug lines from the logic, and the extra debug line logged for each
  boost when notifications were off.
- The Unity and Visual Studio template entries in `.gitignore`. It now lists only what this repo
  produces.

## [1.4.1] - 2026-10-15

### Fixed
- The README's install and build steps now cover `BudgetBooster.Logic.dll`. They only mentioned
  `BudgetBooster.dll`, and without the logic DLL the mod loads but never adds money. The build steps
  now point at `BudgetBooster.sln`, which builds both.
- The README and the Debug Mode and Run Diagnostics tooltips sent users to `Player.log`. The mod
  writes to `Logs\Budget Booster.log`. The README's list of log lines to look for also quoted two
  lines the code never writes; it now lists real ones.
- Update Interval was documented as "256 ticks = about 1 in-game day". A day is 262,144 simulation
  ticks (`TimeSystem.kTicksPerDay`) and the boost system runs once per tick, so 256 ticks is about
  1.4 in-game minutes. The README, the tooltip and the preset comments now give real figures.
  Behaviour is unchanged; only the description was wrong.

### Removed
- The README's MIT License line. The repository never had a LICENSE file.

### Changed
- Preset button labels no longer start with an emoji, and the four preset tooltips now say what
  each preset sets instead of selling it.
- Plainer wording in the README, this changelog and the Paradox Mods description. No documented
  facts changed.

## [1.4.0] - 2026-08-12

### Added
- **Hot-reloadable boost logic.** The boost calculation now lives in a separate
  `BudgetBooster.Logic.dll`, loaded from a byte array by `LogicHost`. Rebuild it and press
  **Reload Boost Logic** in the mod's Debug settings to swap the calculation while the game runs,
  without restarting or reloading your city. Settings and session statistics are untouched.
- Raw API values in the diagnostics report (`Raw GetTotalExpenses()`, `Raw GetTotalIncome()`) and in
  the periodic game state report. Sign conventions are the most common source of readings that look
  plausible but are wrong. The v1.3.0 expense bug survived a clean build and a success log line
  because only the interpreted value was ever printed.

### Changed
- Split responsibilities along the reload seam: the host owns *plumbing* (ECS lookups, reflection,
  raw API calls) and the logic assembly owns *interpretation* (sign handling, fallbacks, arithmetic).
  Every defect found so far has been interpretation, so this puts that class of bug on the
  reloadable side.
- The host has no fallback boost calculation. If logic is missing or throws, no boost is applied and
  the host reports the failure. A silent host-side fallback would hide a broken logic build behind
  plausible behaviour.

### Notes
- The byte-load is the **only** load path, and release builds use it too. A development-only loading
  route that differs from the shipped one is how a bug survives testing.
- Mono cannot unload assemblies, so each reload leaks the previous copy (a few tens of KB). That is a
  small cost next to restarting a city, and it does not affect players, who load once.
- Changes to `IBoostLogic`, `IBoostContext`, `ModSettings`, localization, or the ECS system still
  require a full restart. Only the logic assembly's internals are reloadable.

## [1.3.0] - 2026-08-12

### Added
- **Percentage mode now uses real city expenses.** `GetTotalCityExpenses()` returned a hardcoded `0`,
  so the mode silently took its fallback path on every cycle since it was introduced and never
  scaled with a city. It now reads `GetTotalExpenses()` from `Game.Simulation.CityServiceBudgetSystem`,
  the public `ICityServiceBudgetSystem` the in-game budget panel uses.
- `HasValidBudgetSystem` on `BudgetBoosterSystem`, and expense figures plus read-failure counts in the
  diagnostics report.

### Fixed
- Documentation and the in-game Boost Percentage tooltip claimed city expense data was not exposed to
  mods. It is exposed, and always has been.

### Notes
- The budget system is treated as optional. If it cannot be obtained, or a read throws, or the city has
  genuinely recorded no expenses yet, Percentage mode falls back to scaling Boost Amount exactly as
  before. **Run Diagnostics** reports which path is active.
- Existing Percentage mode users will see larger boosts than before, because the calculation now scales
  against real expenses rather than the Boost Amount slider. Lower Boost Percentage to compensate.

## [1.2.2] - 2026-08-12

### Fixed
- **"Session Statistics" reported lifetime totals.** The four counters were public get/set
  properties on `ModSettings`, and every such property is written to `BudgetBooster.coc`.
  `[SettingsUIHidden]` only hides a property from the options UI, it does not opt out of
  serialization. The panel therefore accumulated across restarts while labelled "this session"
  and "since the game started". Observed in the wild: 95 boosts / $163,267,755 persisted in a
  session that applied exactly one boost.

### Changed
- Moved `TotalBoostsApplied`, `TotalMoneyAdded`, `LastBoostAmount`, and `CurrentBalance` out of
  `ModSettings` into a new in-memory `SessionStatistics` class. Being static, they now last for
  the lifetime of the process, which is what the UI has always claimed.
- `SetDefaults()` no longer resets statistics. `LoadSettings` constructs throwaway default
  instances, so resetting from there would have wiped live counters mid-session. The
  "Reset Statistics" button remains the way to clear them.
- The v1.2.1 settings migration no longer carries statistics across, since they are no longer
  settings.

### Notes
- Statistics carried over from earlier versions are not preserved; the first launch of 1.2.2
  starts the counters at zero. The stale keys are dropped from the `.coc` file on first save.

## [1.2.1] - 2026-08-12

### Fixed
- **Settings section name mismatch.** `ModSettings` declared its storage file with
  `[FileLocation(nameof(BudgetBooster))]` while `Mod.OnLoad` loaded the section as
  `Mod.ModName` (`"Budget Booster"`, with a space). Because `[FileLocation]` names the *file*
  and `LoadSettings` names a *section inside* it, the mod accumulated two competing sections in
  `BudgetBooster.coc`. Every other installed mod has exactly one section; this one had two.
  Both now derive from a single `ModSettings.SettingsSection` constant.
- Corrected the settings-file path in the README. It documented
  `ModsSettings\BudgetBooster\`, a folder the mod never wrote to; the real location is
  `BudgetBooster.coc` in the game's user-data root.
- Added a `G:\SteamLibrary` fallback to `CitiesPath` in the project file. With no `CSII_*`
  environment variables set, the existing `C:` and `D:` fallbacks matched nothing on a `G:`
  install and reference resolution failed. `CSII_INSTALLATIONPATH` still takes precedence.

### Added
- One-time migration of settings and statistics from the pre-1.2.1 `Budget Booster` section
  into `BudgetBooster`, so existing configurations and boost totals survive the rename.
  Guarded by a new persisted `SettingsVersion` so it runs exactly once, and skipped when the
  legacy section is absent or holds nothing but defaults.
- `ModSettings.CopyFrom` and `ModSettings.ValuesEqual` to support the migration.

### Notes
- The orphaned `Budget Booster` section may remain in the `.coc` file after migration. It is
  no longer read and is harmless; it can be deleted by hand.

## [1.2.0]

### Added
- Quick Preset buttons (Casual Builder, Sandbox Mode, Growing City, Emergency Only)
- FAQ section in documentation

### Changed
- Refactored `PlayerMoneyAccessor` for centralized reflection with error recovery
- Expanded troubleshooting guide with step-by-step solutions
- Diagnostics now include `PlayerMoneyAccessor` status and success rate
- Updated copyright year to 2026

## [1.1.0]

### Added
- Notification system with three verbosity levels (Minimal, Normal, Detailed)
- Session statistics display (total boosts, money added, last boost, current balance)
- Debug & Diagnostics section with system diagnostics
- Settings validation with automatic correction of out-of-bounds values
- Harmony patch status tracking for troubleshooting

### Changed
- Percentage mode reworked to target city expenses (see known issues)
- Improved error handling throughout with detailed logging
- Game compatibility updated for 1.4.x (Bike Patch)
- Localization strings for all new settings

## [1.0.0]

### Added
- Initial release
- In-game settings UI with sliders
- Three boost modes
- Configurable update interval
