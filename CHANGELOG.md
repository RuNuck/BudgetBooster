# Changelog

All notable changes to Budget Booster are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
- Comprehensive FAQ section in documentation

### Changed
- Refactored `PlayerMoneyAccessor` for centralized reflection with error recovery
- Expanded troubleshooting guide with step-by-step solutions
- Diagnostics now include `PlayerMoneyAccessor` status and success rate
- Updated copyright year to 2026

## [1.1.0]

### Added
- Notification system with three verbosity levels (Minimal, Normal, Detailed)
- Session statistics display (total boosts, money added, last boost, current balance)
- Debug & Diagnostics section with comprehensive system diagnostics
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
