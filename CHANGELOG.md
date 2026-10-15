# Changelog

All notable changes to Budget Booster are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
