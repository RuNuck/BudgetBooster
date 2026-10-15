using System.Collections.Generic;
using Colossal;

namespace BudgetBooster.Localization
{
    /// <summary>
    /// English localization for the Budget Booster settings UI.
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
                { _settings.GetOptionGroupLocaleID(ModSettings.GroupNotifications), "Notifications" },
                { _settings.GetOptionGroupLocaleID(ModSettings.GroupStatistics), "Session Statistics" },
                { _settings.GetOptionGroupLocaleID(ModSettings.GroupAdvanced), "Advanced Settings" },

                // General Settings
                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.Enabled)), "Enable Budget Booster" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.Enabled)), "Turn the budget booster on or off." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.BoostMode)), "Boost Mode" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.BoostMode)), "How the budget booster adds money to your city." },

                // Enum values for BoostModeType
                { _settings.GetEnumValueLocaleID(ModSettings.BoostModeType.AutoBalance), "Auto-Balance" },
                { _settings.GetEnumValueLocaleID(ModSettings.BoostModeType.FixedBoost), "Fixed Boost" },
                { _settings.GetEnumValueLocaleID(ModSettings.BoostModeType.Percentage), "Percentage" },

                // Preset Configurations Group
                { _settings.GetOptionGroupLocaleID(ModSettings.GroupPresets), "Quick Presets" },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.PresetCasualBuilder)), "Casual Builder" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.PresetCasualBuilder)), "Auto-Balance mode with a gentle boost ($250K). Tops up your balance when it runs low, but leaves some challenge." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.PresetSandboxMode)), "Sandbox Mode" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.PresetSandboxMode)), "Fixed mode with the maximum boost ($5M). Unlimited money for building freely. Notifications disabled." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.PresetGrowingCity)), "Growing City" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.PresetGrowingCity)), "Percentage mode (25%). The boost scales with your city, so you don't have to manage finances closely." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.PresetEmergencyOnly)), "Emergency Only" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.PresetEmergencyOnly)), "Minimal intervention. Only adds money when your balance hits $0." },

                // Boost Settings (Sliders)
                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.BoostAmount)), "Boost Amount ($)" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.BoostAmount)), "Base amount used for calculations. Auto-Balance: added when balance is low. Fixed Boost: added every cycle. Percentage: base for percentage calculation." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.TargetSurplus)), "Target Surplus ($)" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.TargetSurplus)), "Minimum balance to maintain. When your balance falls below this, the Boost Amount is added to bring you back up." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.BoostPercentage)), "Boost Percentage" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.BoostPercentage)), "Percentage of your city's total expenses to add each cycle (e.g., 10% of $100,000 expenses = $10,000 boost). If expense data is unavailable, this scales Boost Amount instead." },

                // Notification Settings
                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.EnableNotifications)), "Enable Notifications" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.EnableNotifications)), "Show log notifications when budget boosts are applied." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.NotificationVerbosity)), "Notification Level" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.NotificationVerbosity)), "How much detail to show in notifications." },

                // Notification Level Enum values
                { _settings.GetEnumValueLocaleID(ModSettings.NotificationLevel.Minimal), "Minimal" },
                { _settings.GetEnumValueLocaleID(ModSettings.NotificationLevel.Normal), "Normal" },
                { _settings.GetEnumValueLocaleID(ModSettings.NotificationLevel.Detailed), "Detailed" },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.MinimalNotificationThreshold)), "Minimal Notification Threshold ($)" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.MinimalNotificationThreshold)), "Only show notifications for boosts larger than this amount (when using Minimal level)." },

                // Statistics Settings
                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.TotalBoostsDisplay)), "Total Boosts This Session" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.TotalBoostsDisplay)), "Number of times a boost has been applied since the game started." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.TotalMoneyAddedDisplay)), "Total Money Added" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.TotalMoneyAddedDisplay)), "Total amount of money added by Budget Booster this session." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.LastBoostAmountDisplay)), "Last Boost Amount" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.LastBoostAmountDisplay)), "The amount of money added in the most recent boost." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.CurrentBalanceDisplay)), "Current Balance" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.CurrentBalanceDisplay)), "Your city's current money balance (last known)." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.ResetStatistics)), "Reset Statistics" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.ResetStatistics)), "Reset session statistics to zero." },

                // Advanced Settings
                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.MaxBoostPerCycle)), "Max Boost Per Cycle ($)" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.MaxBoostPerCycle)), "Maximum amount of money that can be added in a single update cycle." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.UpdateInterval)), "Update Interval" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.UpdateInterval)), "Simulation ticks between boost checks. A full in-game day is 262,144 ticks, so 16 is about 5 in-game seconds, 256 about 1.4 minutes, and 1024 about 5.6 minutes." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.ResetSettings)), "Reset to Defaults" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.ResetSettings)), "Reset all Budget Booster settings to their default values." },
                { _settings.GetOptionWarningLocaleID(nameof(ModSettings.ResetSettings)), "Are you sure you want to reset all settings to defaults?" },

                // Debug Settings
                { _settings.GetOptionGroupLocaleID(ModSettings.GroupDebug), "Debug & Diagnostics" },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.DebugMode)), "Enable Debug Mode" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.DebugMode)), "Enable detailed logging for troubleshooting. Output goes to Logs\\Budget Booster.log." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.LogBoostCycles)), "Log Boost Cycles" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.LogBoostCycles)), "Log detailed information every time a boost is applied." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.LogGameState)), "Log Game State" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.LogGameState)), "Periodically log game state including current balance." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.RunDiagnostics)), "Run Diagnostics" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.RunDiagnostics)), "Run a diagnostic check and write detailed system status to Logs\\Budget Booster.log." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.ReloadBoostLogic)), "Reload Boost Logic" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.ReloadBoostLogic)), "Reload the boost calculation from disk without restarting the game. Intended for mod development: rebuild BudgetBooster.Logic.dll, then click this. Your settings and statistics are unaffected." },
            };
        }

        public void Unload() { }
    }
}
