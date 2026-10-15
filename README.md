# Budget Booster for Cities: Skylines II (Official Framework)

A cheat mod that adds **in-game sliders** for automatic budget boosting. It keeps your city budget positive so you don't have to worry about finances.

## Features

- **Settings UI** in the game's Options menu: an enable/disable toggle, a boost mode selector (Auto-Balance, Fixed Boost, Percentage), and sliders for boost amount, target surplus and update frequency.
- **Session Statistics**, updated in real time: total boosts applied this session, total money added, the last boost amount and your current balance. A button resets them.
- **Notifications** when a boost happens. You can turn them off, pick one of three verbosity levels (Minimal, Normal, Detailed), and set a threshold for what Minimal reports.
- **Debug & Diagnostics**: a debug mode for detailed logging, a Run Diagnostics button that reports system status, and settings validation that auto-corrects bad values.

There are three boost modes:

- Auto-Balance only adds money when your balance is below Target Surplus.
- Fixed Boost adds a constant income regardless of budget status.
- Percentage pays back a share of what your city spends each day.

While the game's own Unlimited Money option is on, the mod does nothing.

## Where the Settings Are

After installing, go to **Options → Mods → Budget Booster**. You'll find the master toggle, the mode dropdown and the boost sliders there. All settings save automatically.

## Installation

### Using Official Modding Toolchain (Recommended)

1. Install the official CS2 modding toolchain via the game's Options menu
2. Build this mod (see Building section)
3. The mod will auto-deploy to your local mods folder

### Manual Installation

1. Build the solution. You get `bin\Debug\BudgetBooster.dll` and `Logic\bin\Debug\BudgetBooster.Logic.dll`
2. Copy `BudgetBooster.dll` to: `%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Mods\BudgetBooster\`
3. Copy `BudgetBooster.Logic.dll` to the same folder and rename it to `BudgetBooster.Logic.bin`
4. Launch the game

The mod folder should hold `BudgetBooster.dll` and `BudgetBooster.Logic.bin`. Without the `.bin` the mod still loads and shows its settings, but it never adds money, and the log says `Failed to load boost logic`.

The logic file is a `.bin` on purpose. The game loads every `.dll` in a mod folder by itself, so a `BudgetBooster.Logic.dll` there would get loaded twice, once by the game as a copy that never reloads. If one is left over from an older version, delete it. A build does this for you.

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
| **Boost Amount** | $10K - $10M | Fixed Boost: added every cycle. Auto-Balance: added on top of the top-up. Percentage: base for the fallback only |
| **Target Surplus** | $0 - $5M | Balance Auto-Balance tops you back up to |
| **Boost Percentage** | 1% - 100% | Share of daily city expenses paid back (Percentage mode) |
| **Max Boost Per Cycle** | $100K - $100M | Safety cap |
| **Update Interval** | 16 - 1024 ticks | Simulation ticks between checks (256 is about 1.4 in-game minutes) |
| **Enable Notifications** | On/Off | Show log notifications for boosts |
| **Notification Level** | Minimal/Normal/Detailed | Verbosity of boost notifications |
| **Debug Mode** | On/Off | Enable detailed logging to `Budget Booster.log` |

### Boost Modes

**Auto-Balance** is the one I'd recommend for most players. It monitors your budget and only steps in when the balance drops below Target Surplus. When it does, it adds what it takes to get back to Target Surplus, plus Boost Amount, up to Max Boost Per Cycle. At a balance of -$2,000,000 with the default settings, that is $2,150,000.

**Fixed Boost** is the sandbox mode. It adds Boost Amount every update cycle, so you get a guaranteed income stream. Use it for creative building where money shouldn't matter.

**Percentage** is the scaling mode. It reads your city's daily expenses from `CityServiceBudgetSystem` and pays back Boost Percentage of them, spread across the cycles in a day. At 25%, a city spending $1,000,000 a day gets about $250,000 a day, which at the default interval of 256 ticks is $244 per cycle. Larger cities with higher expenses get proportionally more. Each cycle's share is rounded down to whole dollars, so small shares at short intervals come to $0: at an interval of 16 ticks and 10%, a city spending under $163,840 a day gets nothing. If expense data is unavailable, it adds Boost Percentage of Boost Amount every cycle instead, at least $10,000. It suits growing cities where you want the boost to scale with the city.

In every mode the mod never adds more than Max Boost Per Cycle in one cycle, and never pushes your balance past the game's $2,000,000,000 ceiling.

## Quick Presets

The settings menu has one-click presets:

| Preset | Mode | Boost | Interval | Max Per Cycle | Notifications | Best For |
|--------|------|-------|----------|---------------|---------------|----------|
| **Casual Builder** | Auto-Balance | Back to $100K, plus $250K | 512 ticks | $1M | Minimal, $500K and up | Light help that still leaves some challenge |
| **Sandbox Mode** | Fixed Boost | $5M every cycle | 64 ticks | $50M | Off | Building without limits |
| **Growing City** | Percentage | 25% of daily expenses (fallback base $500K) | 256 ticks | $5M | Normal | A boost that grows with the city |
| **Emergency Only** | Auto-Balance | Back to $0, plus $50K | 256 ticks | $500K | Detailed | A safety net against bankruptcy and nothing more |

Every preset also turns the mod on. Settings a preset does not list keep their current values.

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
4. `BudgetBooster.dll`, `BudgetBooster.Logic.bin` and this README are copied to your local mods folder automatically. A stale `BudgetBooster.Logic.dll` there is deleted. `dotnet clean` removes all of them again.

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
├── BudgetBoosterSystem.cs   # ECS system that reads the balance and applies boosts
├── BoostLimits.cs           # Host-side clamp: Max Boost Per Cycle and the $2B ceiling
├── IBoostLogic.cs           # Interface between the host and the reloadable logic
├── LogicHost.cs             # Loads BudgetBooster.Logic.bin and handles Reload Boost Logic
├── Logic/
│   ├── BudgetBooster.Logic.csproj  # Logic project (built as .dll, deployed as BudgetBooster.Logic.bin)
│   ├── BoostLogic.cs        # The boost calculation
│   └── BoostFormulas.cs     # The arithmetic behind it, as plain functions
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
A: Yes, as of v1.4.0. It reads your city's daily expenses from the game's `CityServiceBudgetSystem`,
the same source the in-game budget panel uses. Earlier versions always fell back to scaling your Boost
Amount instead. If expenses read as $0 (a brand-new city, or the budget system being unavailable), it
still falls back to that calculation. **Run Diagnostics** reports which path is in use, as a line like
`Percentage path: expenses ($1,000,000/day)` or `Percentage path: fallback (Boost Amount)`.

**Q: Why did my Percentage boosts get so much smaller in v1.5.0?**
A: Versions 1.4.0 and 1.4.1 added the chosen percentage of a whole day's expenses every cycle. A day
has 1,024 cycles at the default interval, so the mod paid out about 1,000 times what the setting
said. v1.5.0 spreads the percentage across the day as intended. Raise Boost Percentage for more, up
to 100% of what the city spends, or use Fixed Boost for larger amounts.

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

**Q: Does the mod work with the game's Unlimited Money option?**
A: It stays out of the way. With Unlimited Money on, the game reports your balance as $2,000,000,000
whatever it really is, so the mod adds nothing and records nothing until you turn the option off. The
log says `Skipping boost cycles: Unlimited Money is on` once when this starts.

**Q: Will the mod break after game updates?**
A: Possibly. Major updates (especially economy changes) may require a mod update. After patches like Bridges & Ports or Bike Patch, check for an updated version.

## Troubleshooting

### Mod Not Appearing in Options

1. Make sure both files are in the correct folder:
   `%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Mods\BudgetBooster\` should contain `BudgetBooster.dll` and `BudgetBooster.Logic.bin`
2. Check the game's log file for errors (see "Checking Logs" below)
3. Check that you're on a compatible game version (1.6.x)
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
4. Check that the game's **Unlimited Money** option is off. While it is on, the mod adds nothing
5. Boosts only happen while a city is loaded and the game is not paused
6. Enable **Debug Mode** and **Log Applied Boosts**, then check `Budget Booster.log` for `Boost cycle` lines
7. If the log says `Failed to load boost logic`, `BudgetBooster.Logic.bin` is missing or broken. The rest of that log line says which
8. Click **Run Diagnostics** to generate a full system report

### Sliders Not Responding

1. Close and reopen the Options menu
2. Try toggling the mod off and on
3. Check for conflicts with other mods that modify the options UI
4. Click **Reset to Defaults** and reconfigure

### Current Balance Shows $0 or an Old Value

**Current Balance** in Session Statistics is the balance the mod saw on its last cycle. It stays at $0
until a cycle has run in a loaded, unpaused city, and it stops updating while the mod is disabled or
Unlimited Money is on.
1. Click **Run Diagnostics** in Debug settings, with a city loaded and the game unpaused
2. In `Budget Booster.log`, the `CITY DIAGNOSTICS` block shows `Current Balance` and `Unlimited Money` as the game reports them
3. If those look wrong after a game update, the mod may need recompilation
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
- `v1.5.0 loading...` near the top tells you which version is running
- `Settings loaded from storage` means settings are being read
- `Logic loaded (generation 1)` means the boost logic file was found and loaded
- `Failed to load boost logic` means `BudgetBooster.Logic.bin` is missing or broken, and no boosts will be applied
- `loaded successfully!` means everything loaded. `loaded with errors:` is followed by what failed
- `Skipping boost cycles:` says why the mod is idle (for example Unlimited Money), once per change
- Other error messages will tell you what's failing

### Debugging Issues

1. Enable **Debug Mode** in the mod settings
2. Enable **Log Applied Boosts** for one line per boost (cycles that add nothing are not logged)
3. Enable **Log Game State** for periodic balance reports
4. Click **Run Diagnostics** to generate a full system report
5. Open `Budget Booster.log` in: `%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Logs\`
6. Look for `[Budget Booster]` entries in the log
7. The diagnostic report is written in two parts. Straight away, `BUDGET BOOSTER DIAGNOSTIC REPORT` shows:
   - Mod version, whether it loaded, and any load error
   - Boost logic status: file path, generation, last error
   - Current settings and session statistics
8. Once a city is loaded and unpaused, `CITY DIAGNOSTICS` follows with:
   - System initialization status
   - Current balance and the Unlimited Money flag
   - Raw expense and income figures from `CityServiceBudgetSystem`
   - Which Percentage path is in use (`expenses ($X/day)` or `fallback (Boost Amount)`)

### Reporting Issues

When reporting a bug, please include:
1. Game version (e.g., 1.6.0f1)
2. Mod version (the `Mod Version` line from **Run Diagnostics**, or the `v1.5.0 loading...` line at the top of `Budget Booster.log`)
3. Relevant sections of `Budget Booster.log`
4. Output from **Run Diagnostics**
5. Steps to reproduce the issue

## Compatibility

- Game version: Cities: Skylines II 1.6.x (built against 1.6.0f1)
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

### v1.5.0
- Changed: Percentage mode pays back the chosen share of daily expenses, spread across the day. It used to pay that share of a whole day's expenses every cycle, so boosts in this mode are now much smaller.
- Fixed: Boosts at the $2B ceiling are counted as what was actually added, and the balance can no longer wrap to -$2B.
- Fixed: The mod stays idle while Unlimited Money is on, and reads the balance through the game's public API instead of reflection.
- Changed: The logic file is deployed as `BudgetBooster.Logic.bin`.

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
