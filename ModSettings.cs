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
    [FileLocation(SettingsSection)]
    [SettingsUIGroupOrder(GroupGeneral, GroupPresets, GroupBoostSettings, GroupNotifications, GroupStatistics, GroupAdvanced, GroupDebug)]
    [SettingsUIShowGroupName(GroupGeneral, GroupPresets, GroupBoostSettings, GroupNotifications, GroupStatistics, GroupAdvanced, GroupDebug)]
    public class ModSettings : ModSetting
    {
        /// <summary>
        /// The single identifier used for BOTH the settings file name (via <see cref="FileLocationAttribute"/>)
        /// and the section name passed to AssetDatabase.LoadSettings.
        ///
        /// These must stay identical. Using two different strings (as earlier versions did, where the file
        /// was named "BudgetBooster" but the section was loaded as "Budget Booster") writes two separate
        /// sections into the same .coc file and orphans whichever one is no longer read.
        /// </summary>
        public const string SettingsSection = nameof(BudgetBooster);

        /// <summary>
        /// The section name used by versions up to 1.2.0, which loaded settings under Mod.ModName
        /// ("Budget Booster", with a space) while writing the file as "BudgetBooster".
        /// Retained only so <see cref="Mod"/> can migrate those values into <see cref="SettingsSection"/> once.
        /// </summary>
        public const string LegacySettingsSection = "Budget Booster";

        /// <summary>
        /// Bumped whenever a settings migration is added. Persisted values below this trigger migration.
        /// </summary>
        public const int CurrentSettingsVersion = 1;

        public const string SectionMain = "Main";
        public const string GroupGeneral = "General";
        public const string GroupPresets = "Presets";
        public const string GroupBoostSettings = "BoostSettings";
        public const string GroupNotifications = "Notifications";
        public const string GroupStatistics = "Statistics";
        public const string GroupAdvanced = "Advanced";
        public const string GroupDebug = "Debug";

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
        // PRESET CONFIGURATIONS (Quick Setup Buttons)
        // ============================================================

        /// <summary>
        /// Casual Builder preset - Gentle boost, low frequency.
        /// Perfect for players who want minimal financial worry while building.
        /// </summary>
        [SettingsUISection(SectionMain, GroupPresets)]
        [SettingsUIButton]
        public bool PresetCasualBuilder
        {
            set
            {
                ApplyPresetCasualBuilder();
                Mod.Log.Info("[Budget Booster] Applied 'Casual Builder' preset");
            }
        }

        /// <summary>
        /// Sandbox Mode preset - Maximum boost, constant income.
        /// Unlimited money for creative building.
        /// </summary>
        [SettingsUISection(SectionMain, GroupPresets)]
        [SettingsUIButton]
        public bool PresetSandboxMode
        {
            set
            {
                ApplyPresetSandboxMode();
                Mod.Log.Info("[Budget Booster] Applied 'Sandbox Mode' preset");
            }
        }

        /// <summary>
        /// Growing City preset - Scaling boost with percentage mode.
        /// Boost grows proportionally with your city.
        /// </summary>
        [SettingsUISection(SectionMain, GroupPresets)]
        [SettingsUIButton]
        public bool PresetGrowingCity
        {
            set
            {
                ApplyPresetGrowingCity();
                Mod.Log.Info("[Budget Booster] Applied 'Growing City' preset");
            }
        }

        /// <summary>
        /// Emergency Only preset - Minimal intervention, only saves from bankruptcy.
        /// For players who want a safety net without feeling like cheating.
        /// </summary>
        [SettingsUISection(SectionMain, GroupPresets)]
        [SettingsUIButton]
        public bool PresetEmergencyOnly
        {
            set
            {
                ApplyPresetEmergencyOnly();
                Mod.Log.Info("[Budget Booster] Applied 'Emergency Only' preset");
            }
        }

        /// <summary>
        /// Applies the Casual Builder preset configuration.
        /// </summary>
        private void ApplyPresetCasualBuilder()
        {
            Enabled = true;
            BoostMode = BoostModeType.AutoBalance;
            BoostAmount = 250000;      // $250K boost
            TargetSurplus = 100000;    // Keep $100K surplus
            UpdateInterval = 512;       // ~2 days between checks
            MaxBoostPerCycle = 1000000; // Max $1M per cycle
            EnableNotifications = true;
            NotificationVerbosity = NotificationLevel.Minimal;
            MinimalNotificationThreshold = 500000;
            ApplyAndSave();
        }

        /// <summary>
        /// Applies the Sandbox Mode preset configuration.
        /// </summary>
        private void ApplyPresetSandboxMode()
        {
            Enabled = true;
            BoostMode = BoostModeType.FixedBoost;
            BoostAmount = 5000000;      // $5M boost
            UpdateInterval = 64;         // Very frequent (~6 hours)
            MaxBoostPerCycle = 50000000; // Max $50M per cycle
            EnableNotifications = false; // Don't spam notifications
            ApplyAndSave();
        }

        /// <summary>
        /// Applies the Growing City preset configuration.
        /// </summary>
        private void ApplyPresetGrowingCity()
        {
            Enabled = true;
            BoostMode = BoostModeType.Percentage;
            BoostAmount = 500000;       // Base $500K for percentage calc
            BoostPercentage = 25;       // 25% of base = $125K per cycle
            UpdateInterval = 256;        // ~1 day between checks
            MaxBoostPerCycle = 5000000;  // Max $5M per cycle
            EnableNotifications = true;
            NotificationVerbosity = NotificationLevel.Normal;
            ApplyAndSave();
        }

        /// <summary>
        /// Applies the Emergency Only preset configuration.
        /// </summary>
        private void ApplyPresetEmergencyOnly()
        {
            Enabled = true;
            BoostMode = BoostModeType.AutoBalance;
            BoostAmount = 50000;        // Small $50K boost
            TargetSurplus = 0;          // Only when at $0 or negative
            UpdateInterval = 256;        // ~1 day between checks
            MaxBoostPerCycle = 500000;   // Max $500K per cycle
            EnableNotifications = true;
            NotificationVerbosity = NotificationLevel.Detailed;
            ApplyAndSave();
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
        // NOTIFICATION SETTINGS
        // ============================================================

        /// <summary>
        /// Enable notifications when boosts are applied.
        /// </summary>
        [SettingsUISection(SectionMain, GroupNotifications)]
        public bool EnableNotifications { get; set; } = true;

        /// <summary>
        /// Notification verbosity level.
        /// </summary>
        [SettingsUISection(SectionMain, GroupNotifications)]
        [SettingsUIHideByCondition(typeof(ModSettings), nameof(HideNotificationOptions))]
        public NotificationLevel NotificationVerbosity { get; set; } = NotificationLevel.Normal;

        public enum NotificationLevel
        {
            /// <summary>
            /// Only show notifications for large boosts.
            /// </summary>
            Minimal,

            /// <summary>
            /// Show standard boost notifications.
            /// </summary>
            Normal,

            /// <summary>
            /// Show detailed notifications with balance info.
            /// </summary>
            Detailed
        }

        /// <summary>
        /// Minimum boost amount to trigger a notification (in Minimal mode).
        /// </summary>
        [SettingsUISection(SectionMain, GroupNotifications)]
        [SettingsUISlider(min = 10000, max = 1000000, step = 10000, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUIHideByCondition(typeof(ModSettings), nameof(HideMinimalThreshold))]
        public int MinimalNotificationThreshold { get; set; } = 100000;

        /// <summary>
        /// Helper to hide notification sub-options when notifications are disabled.
        /// </summary>
        public bool HideNotificationOptions => !EnableNotifications;

        /// <summary>
        /// Helper to hide minimal threshold when not in Minimal mode or notifications disabled.
        /// </summary>
        public bool HideMinimalThreshold => !EnableNotifications || NotificationVerbosity != NotificationLevel.Minimal;

        // ============================================================
        // STATISTICS (Read-only display in settings)
        // ============================================================

        /// <summary>
        /// Total number of boosts applied this session.
        /// </summary>
        [SettingsUISection(SectionMain, GroupStatistics)]
        [SettingsUIDisableByCondition(typeof(ModSettings), nameof(AlwaysTrue))]
        public string TotalBoostsDisplay => $"{TotalBoostsApplied:N0} boosts";

        /// <summary>
        /// Total money added this session.
        /// </summary>
        [SettingsUISection(SectionMain, GroupStatistics)]
        [SettingsUIDisableByCondition(typeof(ModSettings), nameof(AlwaysTrue))]
        public string TotalMoneyAddedDisplay => $"${TotalMoneyAdded:N0}";

        /// <summary>
        /// Last boost amount.
        /// </summary>
        [SettingsUISection(SectionMain, GroupStatistics)]
        [SettingsUIDisableByCondition(typeof(ModSettings), nameof(AlwaysTrue))]
        public string LastBoostAmountDisplay => LastBoostAmount > 0 ? $"${LastBoostAmount:N0}" : "No boosts yet";

        /// <summary>
        /// Current balance (last known).
        /// </summary>
        [SettingsUISection(SectionMain, GroupStatistics)]
        [SettingsUIDisableByCondition(typeof(ModSettings), nameof(AlwaysTrue))]
        public string CurrentBalanceDisplay => $"${CurrentBalance:N0}";

        /// <summary>
        /// Button to reset statistics.
        /// </summary>
        [SettingsUISection(SectionMain, GroupStatistics)]
        [SettingsUIButton]
        public bool ResetStatistics
        {
            set
            {
                ResetStats();
                Mod.Log.Info("[Budget Booster] Statistics have been reset.");
            }
        }

        /// <summary>
        /// Helper property that always returns true (for disabling read-only fields).
        /// </summary>
        public bool AlwaysTrue => true;

        // Statistics backing fields (not persisted, session-only)
        [SettingsUIHidden]
        public int TotalBoostsApplied { get; set; } = 0;

        [SettingsUIHidden]
        public long TotalMoneyAdded { get; set; } = 0;

        [SettingsUIHidden]
        public long LastBoostAmount { get; set; } = 0;

        [SettingsUIHidden]
        public long CurrentBalance { get; set; } = 0;

        /// <summary>
        /// Resets session statistics.
        /// </summary>
        public void ResetStats()
        {
            TotalBoostsApplied = 0;
            TotalMoneyAdded = 0;
            LastBoostAmount = 0;
        }

        /// <summary>
        /// Records a boost in the statistics.
        /// </summary>
        public void RecordBoost(long boostAmount, long newBalance)
        {
            TotalBoostsApplied++;
            TotalMoneyAdded += boostAmount;
            LastBoostAmount = boostAmount;
            CurrentBalance = newBalance;
        }

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
        // DEBUG SETTINGS
        // ============================================================

        /// <summary>
        /// Enable debug mode for detailed logging.
        /// </summary>
        [SettingsUISection(SectionMain, GroupDebug)]
        public bool DebugMode { get; set; } = false;

        /// <summary>
        /// Log every boost cycle (can be verbose).
        /// </summary>
        [SettingsUISection(SectionMain, GroupDebug)]
        [SettingsUIHideByCondition(typeof(ModSettings), nameof(HideDebugOptions))]
        public bool LogBoostCycles { get; set; } = true;

        /// <summary>
        /// Log game state information periodically.
        /// </summary>
        [SettingsUISection(SectionMain, GroupDebug)]
        [SettingsUIHideByCondition(typeof(ModSettings), nameof(HideDebugOptions))]
        public bool LogGameState { get; set; } = true;

        /// <summary>
        /// Helper to hide debug sub-options when debug mode is off.
        /// </summary>
        public bool HideDebugOptions => !DebugMode;

        /// <summary>
        /// Button to run diagnostics and log system status.
        /// </summary>
        [SettingsUISection(SectionMain, GroupDebug)]
        [SettingsUIButton]
        public bool RunDiagnostics
        {
            set
            {
                DiagnosticsRequested = true;
                Mod.Log.Info("[Budget Booster] Diagnostics requested - check log for results");
            }
        }

        /// <summary>
        /// Internal flag to signal diagnostics should run.
        /// </summary>
        [SettingsUIHidden]
        public bool DiagnosticsRequested { get; set; } = false;

        /// <summary>
        /// Persisted schema version, used to run one-time settings migrations.
        /// Stays 0 for settings written before migrations existed (v1.2.0 and earlier).
        /// Deliberately NOT set by <see cref="SetDefaults"/> - a freshly defaulted instance must read as
        /// "unmigrated" so <see cref="Mod"/> can detect an absent section and pull legacy values in.
        /// </summary>
        [SettingsUIHidden]
        public int SettingsVersion { get; set; } = 0;

        // ============================================================
        // VALIDATION CONSTANTS
        // ============================================================

        public const int MinBoostAmount = 10000;
        public const int MaxBoostAmountValue = 10000000;
        public const int MinTargetSurplus = 0;
        public const int MaxTargetSurplus = 5000000;
        public const int MinBoostPercentage = 1;
        public const int MaxBoostPercentage = 100;
        public const int MinMaxBoostPerCycle = 100000;
        public const int MaxMaxBoostPerCycle = 100000000;
        public const int MinUpdateInterval = 16;
        public const int MaxUpdateInterval = 1024;

        // ============================================================
        // METHODS
        // ============================================================

        public override void SetDefaults()
        {
            // General settings
            Enabled = true;
            BoostMode = BoostModeType.AutoBalance;
            BoostAmount = 100000;
            TargetSurplus = 50000;
            BoostPercentage = 10;

            // Notification settings
            EnableNotifications = true;
            NotificationVerbosity = NotificationLevel.Normal;
            MinimalNotificationThreshold = 100000;

            // Advanced settings
            MaxBoostPerCycle = 10000000;
            UpdateInterval = 256;

            // Debug settings
            DebugMode = false;
            LogBoostCycles = true;
            LogGameState = true;
            DiagnosticsRequested = false;

            // Reset statistics
            ResetStats();
        }

        /// <summary>
        /// Copies every persisted value from another settings instance onto this one.
        /// Used to carry values across when migrating from <see cref="LegacySettingsSection"/>.
        /// </summary>
        /// <param name="source">The instance to copy values from.</param>
        public void CopyFrom(ModSettings source)
        {
            if (source == null)
                return;

            // General
            Enabled = source.Enabled;
            BoostMode = source.BoostMode;

            // Boost settings
            BoostAmount = source.BoostAmount;
            TargetSurplus = source.TargetSurplus;
            BoostPercentage = source.BoostPercentage;

            // Notifications
            EnableNotifications = source.EnableNotifications;
            NotificationVerbosity = source.NotificationVerbosity;
            MinimalNotificationThreshold = source.MinimalNotificationThreshold;

            // Advanced
            MaxBoostPerCycle = source.MaxBoostPerCycle;
            UpdateInterval = source.UpdateInterval;

            // Debug
            DebugMode = source.DebugMode;
            LogBoostCycles = source.LogBoostCycles;
            LogGameState = source.LogGameState;

            // Statistics
            TotalBoostsApplied = source.TotalBoostsApplied;
            TotalMoneyAdded = source.TotalMoneyAdded;
            LastBoostAmount = source.LastBoostAmount;
            CurrentBalance = source.CurrentBalance;
        }

        /// <summary>
        /// Compares every persisted value against another instance.
        /// Used to tell "the legacy section held real user data" from "the legacy section was absent,
        /// so LoadSettings handed back a defaulted instance".
        /// </summary>
        /// <param name="other">The instance to compare against.</param>
        /// <returns>True if all persisted values match.</returns>
        public bool ValuesEqual(ModSettings other)
        {
            if (other == null)
                return false;

            return Enabled == other.Enabled
                && BoostMode == other.BoostMode
                && BoostAmount == other.BoostAmount
                && TargetSurplus == other.TargetSurplus
                && BoostPercentage == other.BoostPercentage
                && EnableNotifications == other.EnableNotifications
                && NotificationVerbosity == other.NotificationVerbosity
                && MinimalNotificationThreshold == other.MinimalNotificationThreshold
                && MaxBoostPerCycle == other.MaxBoostPerCycle
                && UpdateInterval == other.UpdateInterval
                && DebugMode == other.DebugMode
                && LogBoostCycles == other.LogBoostCycles
                && LogGameState == other.LogGameState
                && TotalBoostsApplied == other.TotalBoostsApplied
                && TotalMoneyAdded == other.TotalMoneyAdded
                && LastBoostAmount == other.LastBoostAmount
                && CurrentBalance == other.CurrentBalance;
        }

        /// <summary>
        /// Validates all settings are within acceptable bounds.
        /// Returns true if all valid, false if any were corrected.
        /// </summary>
        public bool ValidateAndCorrect()
        {
            bool allValid = true;

            // Validate BoostAmount
            if (BoostAmount < MinBoostAmount)
            {
                Mod.Log.Warn($"[Budget Booster] BoostAmount ({BoostAmount}) below minimum ({MinBoostAmount}), correcting.");
                BoostAmount = MinBoostAmount;
                allValid = false;
            }
            else if (BoostAmount > MaxBoostAmountValue)
            {
                Mod.Log.Warn($"[Budget Booster] BoostAmount ({BoostAmount}) above maximum ({MaxBoostAmountValue}), correcting.");
                BoostAmount = MaxBoostAmountValue;
                allValid = false;
            }

            // Validate TargetSurplus
            if (TargetSurplus < MinTargetSurplus)
            {
                Mod.Log.Warn($"[Budget Booster] TargetSurplus ({TargetSurplus}) below minimum ({MinTargetSurplus}), correcting.");
                TargetSurplus = MinTargetSurplus;
                allValid = false;
            }
            else if (TargetSurplus > MaxTargetSurplus)
            {
                Mod.Log.Warn($"[Budget Booster] TargetSurplus ({TargetSurplus}) above maximum ({MaxTargetSurplus}), correcting.");
                TargetSurplus = MaxTargetSurplus;
                allValid = false;
            }

            // Validate BoostPercentage
            if (BoostPercentage < MinBoostPercentage)
            {
                Mod.Log.Warn($"[Budget Booster] BoostPercentage ({BoostPercentage}) below minimum ({MinBoostPercentage}), correcting.");
                BoostPercentage = MinBoostPercentage;
                allValid = false;
            }
            else if (BoostPercentage > MaxBoostPercentage)
            {
                Mod.Log.Warn($"[Budget Booster] BoostPercentage ({BoostPercentage}) above maximum ({MaxBoostPercentage}), correcting.");
                BoostPercentage = MaxBoostPercentage;
                allValid = false;
            }

            // Validate MaxBoostPerCycle
            if (MaxBoostPerCycle < MinMaxBoostPerCycle)
            {
                Mod.Log.Warn($"[Budget Booster] MaxBoostPerCycle ({MaxBoostPerCycle}) below minimum ({MinMaxBoostPerCycle}), correcting.");
                MaxBoostPerCycle = MinMaxBoostPerCycle;
                allValid = false;
            }
            else if (MaxBoostPerCycle > MaxMaxBoostPerCycle)
            {
                Mod.Log.Warn($"[Budget Booster] MaxBoostPerCycle ({MaxBoostPerCycle}) above maximum ({MaxMaxBoostPerCycle}), correcting.");
                MaxBoostPerCycle = MaxMaxBoostPerCycle;
                allValid = false;
            }

            // Validate UpdateInterval
            if (UpdateInterval < MinUpdateInterval)
            {
                Mod.Log.Warn($"[Budget Booster] UpdateInterval ({UpdateInterval}) below minimum ({MinUpdateInterval}), correcting.");
                UpdateInterval = MinUpdateInterval;
                allValid = false;
            }
            else if (UpdateInterval > MaxUpdateInterval)
            {
                Mod.Log.Warn($"[Budget Booster] UpdateInterval ({UpdateInterval}) above maximum ({MaxUpdateInterval}), correcting.");
                UpdateInterval = MaxUpdateInterval;
                allValid = false;
            }

            if (!allValid)
            {
                Mod.Log.Info("[Budget Booster] Settings validation complete - some values were corrected.");
            }

            return allValid;
        }

        /// <summary>
        /// Gets a diagnostic summary of current settings.
        /// </summary>
        public string GetDiagnosticSummary()
        {
            return $@"
=== Budget Booster Settings Diagnostic ===
Enabled: {Enabled}
Boost Mode: {BoostMode}
Boost Amount: ${BoostAmount:N0}
Target Surplus: ${TargetSurplus:N0}
Boost Percentage: {BoostPercentage}%
Max Boost Per Cycle: ${MaxBoostPerCycle:N0}
Update Interval: {UpdateInterval} ticks

--- Notification Settings ---
Enable Notifications: {EnableNotifications}
Notification Verbosity: {NotificationVerbosity}
Minimal Threshold: ${MinimalNotificationThreshold:N0}

--- Session Statistics ---
Total Boosts Applied: {TotalBoostsApplied:N0}
Total Money Added: ${TotalMoneyAdded:N0}
Last Boost Amount: ${LastBoostAmount:N0}
Current Balance: ${CurrentBalance:N0}

--- Debug Settings ---
Debug Mode: {DebugMode}
Log Boost Cycles: {LogBoostCycles}
Log Game State: {LogGameState}
==========================================";
        }
    }

}
