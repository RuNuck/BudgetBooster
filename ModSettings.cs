using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;

namespace BudgetBooster
{
    /// <summary>
    /// Settings for the Budget Booster mod.
    /// These appear in the game's Options menu under "Mods".
    /// </summary>
    [FileLocation(nameof(BudgetBooster))]
    [SettingsUIGroupOrder(GroupGeneral, GroupBoostSettings, GroupAdvanced)]
    [SettingsUIShowGroupName(GroupGeneral, GroupBoostSettings, GroupAdvanced)]
    public class ModSettings : ModSetting
    {
        public const string SectionMain = "Main";
        public const string GroupGeneral = "General";
        public const string GroupBoostSettings = "BoostSettings";
        public const string GroupAdvanced = "Advanced";

        public ModSettings(IMod mod) : base(mod)
        {
        }

        // ============================================================
        // GENERAL SETTINGS
        // ============================================================

        /// <summary>
        /// Master toggle for the mod.
        /// </summary>
        [SettingsUISection(SectionMain, GroupGeneral)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The boost mode selector.
        /// </summary>
        [SettingsUISection(SectionMain, GroupGeneral)]
        public BoostModeType BoostMode { get; set; } = BoostModeType.AutoBalance;

        public enum BoostModeType
        {
            /// <summary>
            /// Only adds money when budget goes negative.
            /// </summary>
            AutoBalance,
            
            /// <summary>
            /// Adds a fixed amount every update cycle.
            /// </summary>
            FixedBoost,
            
            /// <summary>
            /// Adds money based on current expenses (percentage).
            /// </summary>
            Percentage
        }

        // ============================================================
        // BOOST SETTINGS (SLIDERS!)
        // ============================================================

        /// <summary>
        /// Amount of money to add per boost cycle.
        /// Slider range: 10,000 to 10,000,000
        /// </summary>
        [SettingsUISection(SectionMain, GroupBoostSettings)]
        [SettingsUISlider(min = 10000, max = 10000000, step = 10000, scalarMultiplier = 1, unit = Unit.kInteger)]
        public int BoostAmount { get; set; } = 100000;

        /// <summary>
        /// Helper property to hide TargetSurplus when not in AutoBalance mode.
        /// </summary>
        public bool HideTargetSurplus => BoostMode != BoostModeType.AutoBalance;

        /// <summary>
        /// Target budget surplus to maintain in AutoBalance mode.
        /// Slider range: 0 to 5,000,000
        /// </summary>
        [SettingsUISection(SectionMain, GroupBoostSettings)]
        [SettingsUISlider(min = 0, max = 5000000, step = 10000, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIHideByCondition(typeof(ModSettings), nameof(HideTargetSurplus))]
        public int TargetSurplus { get; set; } = 50000;

        /// <summary>
        /// Helper property to hide BoostPercentage when not in Percentage mode.
        /// </summary>
        public bool HideBoostPercentage => BoostMode != BoostModeType.Percentage;

        /// <summary>
        /// Percentage of Boost Amount to add (for Percentage mode).
        /// Slider range: 1% to 100%
        /// </summary>
        [SettingsUISection(SectionMain, GroupBoostSettings)]
        [SettingsUISlider(min = 1, max = 100, step = 1, scalarMultiplier = 1, unit = Unit.kPercentage)]
        [SettingsUIHideByCondition(typeof(ModSettings), nameof(HideBoostPercentage))]
        public int BoostPercentage { get; set; } = 10;

        // ============================================================
        // ADVANCED SETTINGS
        // ============================================================

        /// <summary>
        /// Maximum boost per update cycle.
        /// Slider range: 100,000 to 100,000,000
        /// </summary>
        [SettingsUISection(SectionMain, GroupAdvanced)]
        [SettingsUISlider(min = 100000, max = 100000000, step = 100000, scalarMultiplier = 1, unit = Unit.kInteger)]
        public int MaxBoostPerCycle { get; set; } = 10000000;

        /// <summary>
        /// How often to apply the boost (in game ticks).
        /// Lower = more frequent. Slider range: 16 to 1024
        /// </summary>
        [SettingsUISection(SectionMain, GroupAdvanced)]
        [SettingsUISlider(min = 16, max = 1024, step = 16, scalarMultiplier = 1, unit = Unit.kInteger)]
        public int UpdateInterval { get; set; } = 256;

        /// <summary>
        /// Button to reset all settings to defaults.
        /// </summary>
        [SettingsUISection(SectionMain, GroupAdvanced)]
        [SettingsUIButton]
        [SettingsUIConfirmation]
        public bool ResetSettings
        {
            set
            {
                SetDefaults();
                ApplyAndSave();
            }
        }

        // ============================================================
        // METHODS
        // ============================================================

        public override void SetDefaults()
        {
            Enabled = true;
            BoostMode = BoostModeType.AutoBalance;
            BoostAmount = 100000;
            TargetSurplus = 50000;
            BoostPercentage = 10;
            MaxBoostPerCycle = 10000000;
            UpdateInterval = 256;
        }
    }

    /// <summary>
    /// Localization for the settings UI.
    /// </summary>
    public class LocaleEN : IDictionarySource
    {
        private readonly ModSettings _settings;

        public LocaleEN(ModSettings settings)
        {
            _settings = settings;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Tab/Section names
                { _settings.GetSettingsLocaleID(), "Budget Booster" },
                { _settings.GetOptionTabLocaleID(ModSettings.SectionMain), "Main" },
                
                // Group names
                { _settings.GetOptionGroupLocaleID(ModSettings.GroupGeneral), "General Settings" },
                { _settings.GetOptionGroupLocaleID(ModSettings.GroupBoostSettings), "Boost Settings" },
                { _settings.GetOptionGroupLocaleID(ModSettings.GroupAdvanced), "Advanced Settings" },
                
                // General Settings
                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.Enabled)), "Enable Budget Booster" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.Enabled)), "Turn the budget booster on or off." },
                
                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.BoostMode)), "Boost Mode" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.BoostMode)), "How the budget booster adds money to your city." },
                
                // Enum values
                { _settings.GetEnumValueLocaleID(ModSettings.BoostModeType.AutoBalance), "Auto-Balance" },
                { _settings.GetEnumValueLocaleID(ModSettings.BoostModeType.FixedBoost), "Fixed Boost" },
                { _settings.GetEnumValueLocaleID(ModSettings.BoostModeType.Percentage), "Percentage" },

                // Enum descriptions
                { _settings.GetEnumDescLocaleID(ModSettings.BoostModeType.AutoBalance), "Maintains a minimum balance - adds money only when needed" },
                { _settings.GetEnumDescLocaleID(ModSettings.BoostModeType.FixedBoost), "Regularly adds a fixed amount regardless of current balance" },
                { _settings.GetEnumDescLocaleID(ModSettings.BoostModeType.Percentage), "Adds money calculated as a percentage of the Boost Amount" },
                
                // Boost Settings (Sliders)
                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.BoostAmount)), "Boost Amount ($)" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.BoostAmount)), "Base amount used for calculations. Auto-Balance: added when balance is low. Fixed Boost: added every cycle. Percentage: base for percentage calculation." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.TargetSurplus)), "Target Surplus ($)" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.TargetSurplus)), "Minimum balance to maintain. When your balance falls below this, the Boost Amount is added to bring you back up." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.BoostPercentage)), "Boost Percentage" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.BoostPercentage)), "Percentage of the Boost Amount to add each cycle (e.g., 10% of $100,000 = $10,000 added)." },
                
                // Advanced Settings
                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.MaxBoostPerCycle)), "Max Boost Per Cycle ($)" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.MaxBoostPerCycle)), "Maximum amount of money that can be added in a single update cycle." },
                
                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.UpdateInterval)), "Update Interval" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.UpdateInterval)), "How often boosts are applied (in-game time): 16 = ~2 hours, 256 = ~1 day, 1024 = ~4 days" },
                
                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.ResetSettings)), "Reset to Defaults" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.ResetSettings)), "Reset all Budget Booster settings to their default values." },
                { _settings.GetOptionWarningLocaleID(nameof(ModSettings.ResetSettings)), "Are you sure you want to reset all settings to defaults?" },
            };
        }

        public void Unload() { }
    }
}
