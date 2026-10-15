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
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.PresetCasualBuilder)), "Turns the mod on in Auto-Balance mode. Sets Boost Amount $250K, Target Surplus $100K, Update Interval 512 ticks (about 2.8 in-game minutes), Max Boost Per Cycle $1M, and Minimal notifications for boosts of $500K or more. A top-up from just under $100K is about $250K, so you only hear about bigger rescues." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.PresetSandboxMode)), "Sandbox Mode" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.PresetSandboxMode)), "Turns the mod on in Fixed Boost mode. Sets Boost Amount $5M, Update Interval 64 ticks (about 21 in-game seconds), Max Boost Per Cycle $50M, and turns notifications off. Adds $5M every cycle." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.PresetGrowingCity)), "Growing City" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.PresetGrowingCity)), "Turns the mod on in Percentage mode. Sets Boost Percentage 25% (a quarter of what your city spends each day), Boost Amount $500K (used only if expense data is unavailable: $125K per cycle), Update Interval 256 ticks, Max Boost Per Cycle $5M, and Normal notifications." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.PresetEmergencyOnly)), "Emergency Only" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.PresetEmergencyOnly)), "Turns the mod on in Auto-Balance mode. Sets Target Surplus $0, Boost Amount $50K, Update Interval 256 ticks, Max Boost Per Cycle $500K, and Detailed notifications. Adds money only when your balance is below $0: enough to get back to $0, plus $50K." },

                // Boost Settings (Sliders)
                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.BoostAmount)), "Boost Amount ($)" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.BoostAmount)), "Auto-Balance: added on top of what it takes to get back to Target Surplus. Fixed Boost: added every cycle. Percentage: only used when expense data is unavailable, as the base the percentage is taken from." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.TargetSurplus)), "Target Surplus ($)" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.TargetSurplus)), "Auto-Balance only. When your balance is below this, the mod adds enough to get back to it, plus Boost Amount, up to Max Boost Per Cycle." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.BoostPercentage)), "Boost Percentage" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.BoostPercentage)), "Share of your city's daily expenses to add, spread across the day's cycles. At 10%, a city spending $1,000,000 a day gets about $100,000 a day. Each cycle's share is rounded down to whole dollars, so small shares at short Update Intervals come to $0: at 16 ticks and 10%, a city spending under $163,840 a day gets nothing. If expense data is unavailable, adds this percentage of Boost Amount every cycle instead (at least $10,000)." },

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
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.MinimalNotificationThreshold)), "Only show notifications for boosts of this amount or larger (when using Minimal level)." },

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

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.LogBoostCycles)), "Log Applied Boosts" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.LogBoostCycles)), "Write one log line for each cycle that adds money: mode, balance before and after, and the amount asked for and applied. Cycles that add nothing are not logged." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.LogGameState)), "Log Game State" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.LogGameState)), "Periodically log game state including current balance." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.RunDiagnostics)), "Run Diagnostics" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.RunDiagnostics)), "Write a status report to Logs\\Budget Booster.log: mod version, load errors, boost logic, settings and session statistics straight away. City figures (balance, Unlimited Money, expenses, which Percentage path is used) follow once a city is loaded and unpaused." },

                { _settings.GetOptionLabelLocaleID(nameof(ModSettings.ReloadBoostLogic)), "Reload Boost Logic" },
                { _settings.GetOptionDescLocaleID(nameof(ModSettings.ReloadBoostLogic)), "Reload the boost calculation from disk without restarting the game. Intended for mod development: rebuild the logic project (deployed as BudgetBooster.Logic.bin), then click this. The new logic takes over on the next simulation tick, so a city must be loaded and unpaused. Your settings and statistics are unaffected." },
            };
        }

        public void Unload() { }
    }
}
