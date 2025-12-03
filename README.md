# Budget Booster - Cities: Skylines II Mod (Official Framework)

A cheat mod that adds **in-game sliders** to control automatic budget boosting! Keep your city budget positive without worrying about finances.

## Features

✨ **In-Game Settings UI** - Sliders and controls directly in the game's Options menu!
- Enable/disable toggle
- Boost mode selector (Auto-Balance, Fixed Boost, Percentage)
- Configurable boost amount slider
- Target surplus slider
- Update frequency control

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
| **Boost Percentage** | 1% - 100% | Expense percentage (Percentage mode) |
| **Max Boost Per Cycle** | $100K - $100M | Safety cap |
| **Update Interval** | 16 - 1024 ticks | Check frequency (~256 = 1 day) |

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
- Boost scales with your expenses
- Larger cities get proportionally more
- Good for growing cities

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
├── Mod.cs                 # Main mod entry point (IMod implementation)
├── ModSettings.cs         # Settings with UI attributes
├── BudgetBoosterSystem.cs # ECS system that applies boosts
├── BudgetBooster.csproj   # Project file
└── README.md              # This file
```

## Troubleshooting

### Mod Not Appearing in Options

1. Ensure the DLL is in the correct location
2. Check the game's log file for errors
3. Try rebuilding with a clean build

### Settings Not Saving

The settings file is at:
```
%LOCALAPPDATA%Low\Colossal Order\Cities Skylines II\ModsSettings\BudgetBooster\
```

Ensure write permissions to this folder.

### Sliders Not Responding

1. Close and reopen the Options menu
2. Try toggling the mod off and on
3. Check for conflicts with other mods

## Compatibility

- **Game Version**: Cities: Skylines II 1.1.0+
- **Framework**: Official Colossal Order Modding Toolchain
- **Conflicts**: May conflict with other economy mods

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

### v1.0.0
- Initial release
- In-game settings UI with sliders
- Three boost modes
- Configurable update interval
