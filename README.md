# Budget Booster for Cities: Skylines II (Official Framework)

A cheat mod that adds **in-game sliders** for automatic budget boosting. It keeps your city budget positive so you don't have to worry about finances.

## Features

- **Settings UI** in the game's Options menu: an enable/disable toggle, a boost mode selector (Auto-Balance, Fixed Boost, Percentage), and sliders for boost amount, target surplus and update frequency.
- **Session Statistics**, updated in real time: total boosts applied this session, total money added, the last boost amount and your current balance. A button resets them.
- **Notifications** when a boost happens. You can turn them off, pick one of three verbosity levels (Minimal, Normal, Detailed), and set a threshold for what Minimal reports.
- **Debug & Diagnostics**: a debug mode for detailed logging, a Run Diagnostics button that reports system status, Harmony patch status tracking, and settings validation that auto-corrects bad values.

There are three boost modes:

- Auto-Balance only adds money when you're in the red.
- Fixed Boost adds a constant income regardless of budget status.
- Percentage scales the boost with your city's expenses.

## Where the Settings Are

After installing, go to **Options → Mods → Budget Booster**. You'll find the master toggle, the mode dropdown and the boost sliders there. All settings save automatically.

## Installation

### Using Official Modding Toolchain (Recommended)

1. Install the official CS2 modding toolchain via the game's Options menu
2. Build this mod (see Building section)
3. The mod will auto-deploy to your local mods folder

### Manual Installation

1. Build the solution to get two DLLs: `BudgetBooster.dll` and `BudgetBooster.Logic.dll`
2. Copy both to: `%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Mods\BudgetBooster\`
3. Launch the game

The mod needs both files. Without `BudgetBooster.Logic.dll` it still loads and shows its settings, but it never adds money, and the log says `Failed to load boost logic`.

## Usage

1. Start or load a city
2. Open the **Options** menu (Escape → Options)
3. Go to the **Mods** tab
4. Find **Budget Booster**
5. Set the sliders the way you want them

### Settings Explained

| Setting | Range | Description |
|---------|-------|-------------|
| **Enable Budget Booster** | On/Off | Master toggle |
| **Boost Mode** | Dropdown | Auto-Balance, Fixed Boost, or Percentage |
| **Boost Amount** | $10K - $10M | Money added per cycle |
| **Target Surplus** | $0 - $5M | Balance threshold (Auto-Balance mode) |
| **Boost Percentage** | 1% - 100% | Percentage of city expenses (Percentage mode) |
| **Max Boost Per Cycle** | $100K - $100M | Safety cap |
| **Update Interval** | 16 - 1024 ticks | Simulation ticks between checks (256 is about 1.4 in-game minutes) |
| **Enable Notifications** | On/Off | Show log notifications for boosts |
| **Notification Level** | Minimal/Normal/Detailed | Verbosity of boost notifications |
| **Debug Mode** | On/Off | Enable detailed logging to `Budget Booster.log` |

### Boost Modes

**Auto-Balance** is the one I'd recommend for most players. It monitors your budget and only steps in when the balance drops below Target Surplus. When it does, it adds enough to recover, plus Boost Amount.

**Fixed Boost** is the sandbox mode. It adds Boost Amount every update cycle, so you get a guaranteed income stream. Use it for creative building where money shouldn't matter.

**Percentage** is the scaling mode. It queries actual city expenses from `CityServiceBudgetSystem` and calculates the boost as a percentage of total city expenses, so larger cities with higher expenses get proportionally more. If expense data is unavailable, it falls back to Boost Amount. It suits growing cities where you want the boost to scale with the city.

## Quick Presets

The settings menu has one-click presets:

| Preset | Mode | Boost | Best For |
|--------|------|-------|----------|
| **Casual Builder** | Auto-Balance | $250K | Light help that still leaves some challenge |
| **Sandbox Mode** | Fixed Boost | $5M | Building without limits |
| **Growing City** | Percentage (25%) | Scales | A boost that grows with the city |
| **Emergency Only** | Auto-Balance | $50K at $0 | A safety net against bankruptcy and nothing more |

## Building from Source

### Requirements

- Visual Studio 2022 (17.8+) or JetBrains Rider (2021.3.3+)
- Cities: Skylines II with modding toolchain installed
- .NET Standard 2.1 SDK

### Build Steps

1. Open `BudgetBooster.sln` in your IDE. It holds both projects: the host (`BudgetBooster.csproj`) and the boost logic (`Logic/BudgetBooster.Logic.csproj`). Building only the host project gives you a mod that never adds money.
2. Check that the game paths match your system. Each project has its own copy, in `BudgetBooster.csproj` and `Logic/BudgetBooster.Logic.csproj`:
   ```xml
   <CitiesPath>C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II</CitiesPath>
   ```
   Or pass it once on the command line, which reaches both: `dotnet build -p:CitiesPath="D:\Games\Cities Skylines II"`
3. Build the solution (or run `dotnet build` in the repo root, which picks up the `.sln`)
4. Both DLLs are copied to your local mods folder automatically

### Using Official Toolchain

If you've installed the modding toolchain via the game, its environment variables are already set, so you can just build and the paths resolve on their own.

## File Structure

```
BudgetBooster/
├── BudgetBooster.sln        # Solution: builds the host and the logic assembly together
├── BudgetBooster.csproj     # Host project (BudgetBooster.dll)
├── Mod.cs                   # Main mod entry point (IMod implementation) with diagnostics
├── ModSettings.cs           # Settings with UI attributes, validation and presets
├── SessionStatistics.cs     # In-memory session counters
├── BudgetBoosterSystem.cs   # ECS system that applies boosts with expense tracking
├── PlayerMoneyAccessor.cs   # Dedicated reflection accessor with error recovery
├── IBoostLogic.cs           # Interface between the host and the reloadable logic
├── LogicHost.cs             # Loads BudgetBooster.Logic.dll and handles Reload Boost Logic
├── Patches.cs               # System registration tracking
├── Logic/
│   ├── BudgetBooster.Logic.csproj  # Logic project (BudgetBooster.Logic.dll)
│   └── BoostLogic.cs        # The boost calculation
├── Localization/
│   └── LocaleEN.cs          # English localization strings
├── Properties/
│   └── PublishConfiguration.xml  # Paradox Mods listing
├── CHANGELOG.md             # Version history
└── README.md                # This file
```

## Frequently Asked Questions (FAQ)

### General Questions

**Q: Is this mod considered cheating?**
A: Yes. It's a cheat mod, made to take the financial pressure out of the game. If you want the challenge of managing your city's finances, this mod isn't for you.

**Q: Will this mod disable achievements?**
A: Cities: Skylines II currently doesn't have an achievement system tied to mods. If achievements are added in the future, economy-affecting mods like this one may disable them.

**Q: Can I use this mod mid-game?**
A: Yes. You can install and enable Budget Booster on any existing save. The mod doesn't modify your save file directly. It only adds money during gameplay.

**Q: What happens if I uninstall the mod?**
A: Your city keeps any money that was added. The mod doesn't remove money when uninstalled, and your save file stays compatible with the base game.

### Configuration Questions

**Q: Which preset should I use?**
A:
- New to the mod: start with **Casual Builder** for a balanced setup.
- Want unlimited building money: use **Sandbox Mode**.
- Want the boost to grow with your city: try **Growing City**.
- Want as little help as possible: use **Emergency Only** as a bankruptcy safety net.

**Q: What does "Update Interval" mean?**
A: It's how often (in game ticks) the mod checks your balance and applies boosts. Lower numbers mean more frequent checks. The game runs 262,144 ticks per in-game day, so these are short gaps: 16 ticks is about 5 in-game seconds, 256 about 1.4 in-game minutes, and the maximum of 1024 about 5.6 in-game minutes.

**Q: Is Percentage mode using my actual city expenses?**
A: Yes, as of v1.3.0. It reads your city's total expenses from the game's `CityServiceBudgetSystem`,
the same source the in-game budget panel uses. Earlier versions always fell back to scaling your Boost
Amount instead. If expenses read as $0 (a brand-new city, or the budget system being unavailable), it
still falls back to that calculation. **Run Diagnostics** reports which path is in use.

**Q: Why did my statistics reset to zero?**
A: Session statistics count only the current game session and start at zero each time you launch
the game. Before v1.2.2 they were saved to disk by mistake, so they accumulated forever while the
UI described them as per-session. Your settings are unaffected. Only the counters reset.

**Q: My settings aren't saving. What should I check?**
A: Check that you have write permissions to:
`%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\BudgetBooster.coc`

### Technical Questions

**Q: Is this mod compatible with other economy mods?**
A: It may conflict with mods that also modify `PlayerMoney` (like Economy Rebalance, RealEco). Test carefully if you use more than one economy mod.

**Q: Why does the mod use reflection?**
A: The `PlayerMoney.m_Money` field is private. The mod reads the current balance through cached reflection in `PlayerMoneyAccessor`. This is the safest way to access game internals without Harmony patches.

**Q: Will the mod break after game updates?**
A: Possibly. Major updates (especially economy changes) may require a mod update. After patches like Bridges & Ports or Bike Patch, check for an updated version.

## Troubleshooting

### Mod Not Appearing in Options

1. Make sure both DLLs are in the correct folder:
   `%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Mods\BudgetBooster\` should contain `BudgetBooster.dll` and `BudgetBooster.Logic.dll`
2. Check the game's log file for errors (see "Checking Logs" below)
3. Check that you're on a compatible game version (1.4.x)
4. Try a clean rebuild (`dotnet clean && dotnet build`)
5. Restart the game completely (not just reload)

### Settings Not Saving

The settings file is at:
```
%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\BudgetBooster.coc
```

All values live in a single section named `BudgetBooster`. If you also see a section named
`Budget Booster` (with a space), that is leftover data from v1.2.0 or earlier. v1.2.1 migrates
it across automatically on first launch, and after that the old section is ignored.

Things to try:
- Make sure you have write permissions to this folder
- Check whether antivirus is blocking file writes
- Delete the settings file and let the mod recreate it with defaults
- Run the game as administrator (as a temporary test)

### Boosts Not Being Applied

1. Check that the mod is **Enabled** in settings
2. In **Auto-Balance** mode, boosts only apply when balance < Target Surplus
3. Check the **Update Interval**. Higher values mean less frequent boosts
4. Enable **Debug Mode** and check `Budget Booster.log` for cycle information
5. If the log says `Failed to load boost logic`, `BudgetBooster.Logic.dll` is missing or broken. The rest of that log line says which
6. Click **Run Diagnostics** to generate a full system report

### Sliders Not Responding

1. Close and reopen the Options menu
2. Try toggling the mod off and on
3. Check for conflicts with other mods that modify the options UI
4. Click **Reset to Defaults** and reconfigure

### Balance Shows $0 or Incorrect Value

This means the `PlayerMoney` reflection may be failing:
1. Click **Run Diagnostics** in Debug settings
2. Check `Budget Booster.log` for "PlayerMoneyAccessor" messages
3. After a game update, the mod may need recompilation
4. If it persists, report the issue with your game version

### Checking Logs

The mod writes everything to its own log file (warnings and errors are also copied to the game's Player.log):
```
%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Logs\Budget Booster.log
```

To check it:
1. Enable **Debug Mode** in mod settings
2. Play for a few minutes with the mod enabled
3. Open `Budget Booster.log` in a text editor
4. Look at the lines tagged `[Budget Booster]`

What to look for:
- `PlayerMoneyAccessor: Field 'm_Money' cached` means balance reading works
- `Settings loaded from storage` means settings are being read
- `Logic loaded (generation 1)` means the boost logic DLL was found and loaded
- `Failed to load boost logic` means `BudgetBooster.Logic.dll` is missing or broken, and no boosts will be applied
- Other error messages will tell you what's failing

### Debugging Issues

1. Enable **Debug Mode** in the mod settings
2. Enable **Log Boost Cycles** for detailed per-cycle logging
3. Enable **Log Game State** for periodic balance reports
4. Click **Run Diagnostics** to generate a full system report
5. Open `Budget Booster.log` in: `%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Logs\`
6. Look for `[Budget Booster]` entries in the log
7. The diagnostic report shows:
   - System initialization status
   - PlayerMoneyAccessor status and success rate
   - PlayerMoney API availability
   - CityServiceBudgets (expense data) availability
   - Current settings and session statistics

### Reporting Issues

When reporting a bug, please include:
1. Game version (e.g., 1.4.2f1)
2. Mod version (shown in settings title)
3. Relevant sections of `Budget Booster.log`
4. Output from **Run Diagnostics**
5. Steps to reproduce the issue

## Compatibility

- Game version: Cities: Skylines II 1.4.x (tested with 1.4.2f1)
- Minimum version: 1.1.0f1 (official modding toolchain required)
- Framework: official Colossal Order Modding Toolchain
- Conflicts: may conflict with other economy mods (e.g., Economy Rebalance, RealEco)

After major game updates (like Bridges & Ports, Bike Patch), the mod may need recompilation. Check for updated versions after patches.

## Technical Details

The mod uses the official modding API:
- `IMod` interface for lifecycle management
- `ModSetting` with `[SettingsUISlider]` for in-game UI
- `GameSystemBase` for ECS system registration
- `SystemUpdatePhase.GameSimulation` for safe data access

## Credits

- Colossal Order for the modding framework
- The Cities: Skylines modding community, whose documentation this mod was researched from

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for the full history.

### v1.3.0
- Added: Percentage mode now scales with your city's real expenses via `CityServiceBudgetSystem`. It previously always used a hardcoded fallback, so it never actually scaled with the city.
- Fixed: Docs and tooltip no longer claim city expense data is inaccessible to mods.

### v1.2.2
- Fixed: "Session Statistics" persisted to disk and reported lifetime totals under session labels. The counters now live in memory and reset each time you launch the game.

### v1.2.1
- Fixed: Settings were written to a file named `BudgetBooster` but loaded from a section named `Budget Booster`, leaving two competing sections in `BudgetBooster.coc`. Both now use the single `BudgetBooster` identifier.
- Added: One-time migration that carries settings and statistics over from the old section.
- Fixed: Documented settings path corrected (the `ModsSettings\BudgetBooster\` folder was never used).

### v1.2.0
- Added: Quick Preset buttons (Casual Builder, Sandbox Mode, Growing City, Emergency Only)
- Added: FAQ section in documentation
- Refactored: PlayerMoneyAccessor class for centralized reflection with error recovery
- Improved: Expanded troubleshooting guide with step-by-step solutions
- Improved: Diagnostics now include PlayerMoneyAccessor status and success rate
- Updated: Copyright year to 2026
- Updated: Documentation to reflect new file structure

### v1.1.0
- Fixed: Percentage mode now queries actual city expenses via CityServiceBudgets
- Added: Notification system with three verbosity levels (Minimal, Normal, Detailed)
- Added: Session statistics display (total boosts, money added, last boost, current balance)
- Added: Debug & Diagnostics section with system diagnostics
- Added: Settings validation with automatic correction of out-of-bounds values
- Added: Harmony patch status tracking for troubleshooting
- Improved: Error handling throughout with detailed logging
- Improved: Game compatibility updated for 1.4.x (Bike Patch)
- Improved: Localization strings for all new settings

### v1.0.0
- Initial release
- In-game settings UI with sliders
- Three boost modes
- Configurable update interval
