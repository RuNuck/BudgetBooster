# Budget Booster - Cities: Skylines II Mod (Official Framework)

A cheat mod that adds **in-game sliders** to control automatic budget boosting! Keep your city budget positive without worrying about finances.

## Features

✨ **In-Game Settings UI** - Sliders and controls directly in the game's Options menu!
- Enable/disable toggle
- Boost mode selector (Auto-Balance, Fixed Boost, Percentage)
- Configurable boost amount slider
- Target surplus slider
- Update frequency control

📊 **Session Statistics** - Track your boosts in real-time!
- Total boosts applied this session
- Total money added
- Last boost amount
- Current balance display
- Reset statistics button

🔔 **Notification System** - Know when boosts happen!
- Enable/disable notifications
- Three verbosity levels: Minimal, Normal, Detailed
- Configurable threshold for minimal notifications

🔧 **Debug & Diagnostics** - Troubleshoot with ease!
- Debug mode for detailed logging
- Run Diagnostics button for system status
- Harmony patch status tracking
- Settings validation with auto-correction

🎮 **Three Boost Modes**:
- **Auto-Balance**: Only adds money when you're in the red
- **Fixed Boost**: Constant income regardless of budget status
- **Percentage**: Scales boost with your city's expenses

## Screenshots

After installing, go to **Options → Mods → Budget Booster** to access the settings:

- Master toggle to enable/disable
- Sliders to adjust boost amounts
- Mode selector dropdown
- All settings save automatically!

## Installation

### Using Official Modding Toolchain (Recommended)

1. Install the official CS2 modding toolchain via the game's Options menu
2. Build this mod (see Building section)
3. The mod will auto-deploy to your local mods folder

### Manual Installation

1. Build the mod to get `BudgetBooster.dll`
2. Copy to: `%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Mods\BudgetBooster\`
3. Launch the game

## Usage

1. Start or load a city
2. Open the **Options** menu (Escape → Options)
3. Go to the **Mods** tab
4. Find **Budget Booster**
5. Use the sliders to configure your preferred settings!

### Settings Explained

| Setting | Range | Description |
|---------|-------|-------------|
| **Enable Budget Booster** | On/Off | Master toggle |
| **Boost Mode** | Dropdown | Auto-Balance, Fixed Boost, or Percentage |
| **Boost Amount** | $10K - $10M | Money added per cycle |
| **Target Surplus** | $0 - $5M | Balance threshold (Auto-Balance mode) |
| **Boost Percentage** | 1% - 100% | Percentage of city expenses (Percentage mode) |
| **Max Boost Per Cycle** | $100K - $100M | Safety cap |
| **Update Interval** | 16 - 1024 ticks | Check frequency (~256 = 1 day) |
| **Enable Notifications** | On/Off | Show log notifications for boosts |
| **Notification Level** | Minimal/Normal/Detailed | Verbosity of boost notifications |
| **Debug Mode** | On/Off | Enable detailed logging to Player.log |

### Boost Modes

**Auto-Balance** (Recommended for most players)
- Monitors your budget
- Only kicks in when balance drops below Target Surplus
- Adds just enough to recover + Boost Amount
- Feels the most "natural"

**Fixed Boost** (Sandbox mode)
- Adds Boost Amount every update cycle
- Guaranteed income stream
- Great for creative building

**Percentage** (Scaling mode)
- Queries actual city expenses from CityServiceBudgets
- Boost calculated as percentage of total city expenses
- Larger cities with higher expenses get proportionally more
- Falls back to Boost Amount if expense data unavailable
- Good for growing cities that want natural scaling

## Quick Presets

For convenience, the mod includes **one-click preset configurations** in the settings menu:

| Preset | Mode | Boost | Best For |
|--------|------|-------|----------|
| 🎨 **Casual Builder** | Auto-Balance | $250K | Stress-free building with some challenge |
| 🏗️ **Sandbox Mode** | Fixed Boost | $5M | Unlimited creative building |
| 📈 **Growing City** | Percentage (25%) | Scales | Organic growth, less micromanagement |
| 🚨 **Emergency Only** | Auto-Balance | $50K at $0 | Safety net that feels fair |

## Building from Source

### Requirements

- Visual Studio 2022 (17.8+) or JetBrains Rider (2021.3.3+)
- Cities: Skylines II with modding toolchain installed
- .NET Standard 2.1 SDK

### Build Steps

1. Open `BudgetBooster.csproj` in your IDE
2. Verify the paths in the `.csproj` match your system:
   ```xml
   <CitiesPath>C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II</CitiesPath>
   ```
3. Build the solution
4. The DLL is automatically copied to your local mods folder

### Using Official Toolchain

If you've installed the modding toolchain via the game:
1. The environment variables are set automatically
2. Just build and the paths resolve automatically

## File Structure

```
BudgetBooster/
├── Mod.cs                   # Main mod entry point (IMod implementation) with diagnostics
├── ModSettings.cs           # Settings with UI attributes, validation, presets, and statistics
├── BudgetBoosterSystem.cs   # ECS system that applies boosts with expense tracking
├── PlayerMoneyAccessor.cs   # Dedicated reflection accessor with error recovery
├── Patches.cs               # System registration tracking
├── BudgetBooster.csproj     # Project file
├── Localization/
│   └── LocaleEN.cs          # English localization strings
└── README.md                # This file
```

## Frequently Asked Questions (FAQ)

### General Questions

**Q: Is this mod considered cheating?**
A: Yes! This is explicitly a cheat mod designed to remove financial stress from gameplay. If you want the challenge of managing your city's finances, this mod isn't for you.

**Q: Will this mod disable achievements?**
A: Cities: Skylines II currently doesn't have an achievement system tied to mods. However, if achievements are added in the future, economy-affecting mods like this may disable them.

**Q: Can I use this mod mid-game?**
A: Absolutely! You can install and enable Budget Booster on any existing save. The mod doesn't modify your save file directly - it just adds money during gameplay.

**Q: What happens if I uninstall the mod?**
A: Your city keeps any money that was added. The mod doesn't remove money when uninstalled. Your save file remains compatible with the base game.

### Configuration Questions

**Q: Which preset should I use?**
A: 
- New to the mod? Start with **Casual Builder** for a balanced experience.
- Want unlimited creativity? Use **Sandbox Mode**.
- Want the mod to grow with your city? Try **Growing City**.
- Want minimal help? Use **Emergency Only** as a bankruptcy safety net.

**Q: What does "Update Interval" mean?**
A: It's how often (in game ticks) the mod checks your balance and applies boosts. Lower numbers = more frequent checks. 256 ticks ≈ 1 in-game day.

**Q: Why isn't Percentage mode using my actual city expenses?**
A: The current CS2 modding API doesn't expose city expense data. Percentage mode uses your Boost Amount as a base for the percentage calculation instead. This is noted in the diagnostics.

**Q: My settings aren't saving!**
A: Check that you have write permissions to:
`%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\BudgetBooster.coc`

### Technical Questions

**Q: Is this mod compatible with other economy mods?**
A: It may conflict with mods that also modify `PlayerMoney` (like Economy Rebalance, RealEco). Test carefully if using multiple economy mods.

**Q: Why does the mod use reflection?**
A: The `PlayerMoney.m_Money` field is private. We use cached reflection via `PlayerMoneyAccessor` to read the current balance. This is the safest way to access game internals without Harmony patches.

**Q: Will the mod break after game updates?**
A: Possibly. Major updates (especially economy changes) may require a mod update. After patches like Bridges & Ports or Bike Patch, check for an updated version.

## Troubleshooting

### Mod Not Appearing in Options

1. Ensure the DLL is in the correct location:
   `%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Mods\BudgetBooster\BudgetBooster.dll`
2. Check the game's log file for errors (see "Checking Logs" below)
3. Verify you're using a compatible game version (1.4.x)
4. Try rebuilding with a clean build (`dotnet clean && dotnet build`)
5. Restart the game completely (not just reload)

### Settings Not Saving

The settings file is at:
```
%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\BudgetBooster.coc
```

All values live in a single section named `BudgetBooster`. If you also see a section named
`Budget Booster` (with a space), that is leftover data from v1.2.0 or earlier — v1.2.1 migrates
it across automatically on first launch, after which the old section is ignored.

**Solutions:**
- Ensure write permissions to this folder
- Check if antivirus is blocking file writes
- Delete the settings file and let the mod recreate it with defaults
- Run the game as administrator (temporary test)

### Boosts Not Being Applied

1. Check that the mod is **Enabled** in settings
2. In **Auto-Balance** mode, boosts only apply when balance < Target Surplus
3. Check the **Update Interval** - higher values mean less frequent boosts
4. Enable **Debug Mode** and check `Player.log` for cycle information
5. Click **Run Diagnostics** to generate a full system report

### Sliders Not Responding

1. Close and reopen the Options menu
2. Try toggling the mod off and on
3. Check for conflicts with other mods that modify the options UI
4. Click **Reset to Defaults** and reconfigure

### Balance Shows $0 or Incorrect Value

This indicates the `PlayerMoney` reflection may be failing:
1. Click **Run Diagnostics** in Debug settings
2. Check `Player.log` for "PlayerMoneyAccessor" messages
3. After a game update, the mod may need recompilation
4. Report the issue with your game version if it persists

### Checking Logs

The game log is located at:
```
%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Logs\Player.log
```

**How to check:**
1. Enable **Debug Mode** in mod settings
2. Play for a few minutes with the mod enabled
3. Open `Player.log` in a text editor
4. Search for `[Budget Booster]` entries

**What to look for:**
- `BudgetBoosterSystem initialized and ready!` - Good, mod is working
- `PlayerMoneyAccessor: Field 'm_Money' cached successfully` - Balance reading works
- `Settings loaded from storage` - Settings are being read
- Error messages will indicate what's failing

### Debugging Issues

1. Enable **Debug Mode** in the mod settings
2. Enable **Log Boost Cycles** for detailed per-cycle logging
3. Enable **Log Game State** for periodic balance reports
4. Click **Run Diagnostics** to generate a full system report
5. Check `Player.log` at: `%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\Logs\`
6. Look for `[Budget Booster]` entries in the log
7. The diagnostic report will show:
   - System initialization status
   - PlayerMoneyAccessor status and success rate
   - PlayerMoney API availability
   - CityServiceBudgets (expense data) availability
   - Current settings and session statistics

### Reporting Issues

When reporting bugs, please include:
1. Game version (e.g., 1.4.2f1)
2. Mod version (shown in settings title)
3. Relevant sections of `Player.log`
4. Output from **Run Diagnostics**
5. Steps to reproduce the issue

## Compatibility

- **Game Version**: Cities: Skylines II 1.4.x (tested with 1.4.2f1)
- **Minimum Version**: 1.1.0f1 (official modding toolchain required)
- **Framework**: Official Colossal Order Modding Toolchain
- **Conflicts**: May conflict with other economy mods (e.g., Economy Rebalance, RealEco)
- **Note**: After major game updates (like Bridges & Ports, Bike Patch), the mod may need recompilation. Check for updated versions after patches.

## Technical Details

This mod uses the official modding API:
- `IMod` interface for lifecycle management
- `ModSetting` with `[SettingsUISlider]` for in-game UI
- `GameSystemBase` for ECS system registration
- `SystemUpdatePhase.GameSimulation` for safe data access

## Credits

- Colossal Order for the modding framework
- Cities: Skylines modding community
- Based on research from community documentation

## License

MIT License - Free to use, modify, and distribute!

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for the full history.

### v1.2.1
- **Fixed**: Settings were written to a file named `BudgetBooster` but loaded from a section named `Budget Booster`, leaving two competing sections in `BudgetBooster.coc`. Both now use the single `BudgetBooster` identifier.
- **Added**: One-time migration that carries settings and statistics over from the old section.
- **Fixed**: Documented settings path corrected (the `ModsSettings\BudgetBooster\` folder was never used).

### v1.2.0
- **Added**: Quick Preset buttons (Casual Builder, Sandbox Mode, Growing City, Emergency Only)
- **Added**: Comprehensive FAQ section in documentation
- **Refactored**: PlayerMoneyAccessor class for centralized reflection with error recovery
- **Improved**: Expanded troubleshooting guide with step-by-step solutions
- **Improved**: Diagnostics now include PlayerMoneyAccessor status and success rate
- **Updated**: Copyright year to 2026
- **Updated**: Documentation to reflect new file structure

### v1.1.0
- **Fixed**: Percentage mode now queries actual city expenses via CityServiceBudgets
- **Added**: Notification system with three verbosity levels (Minimal, Normal, Detailed)
- **Added**: Session statistics display (total boosts, money added, last boost, current balance)
- **Added**: Debug & Diagnostics section with comprehensive system diagnostics
- **Added**: Settings validation with automatic correction of out-of-bounds values
- **Added**: Harmony patch status tracking for troubleshooting
- **Improved**: Error handling throughout with detailed logging
- **Improved**: Game compatibility updated for 1.4.x (Bike Patch)
- **Improved**: Localization strings for all new settings

### v1.0.0
- Initial release
- In-game settings UI with sliders
- Three boost modes
- Configurable update interval
