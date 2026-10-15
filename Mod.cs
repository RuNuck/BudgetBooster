using System;
using BudgetBooster.Localization;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace BudgetBooster
{
    /// <summary>
    /// Budget Booster - A cheat mod for Cities: Skylines II
    /// Uses the official Colossal Order modding framework.
    /// Automatically adds money to keep your city budget positive!
    /// </summary>
    public class Mod : IMod
    {
        public const string ModName = "Budget Booster";
        public const string ModVersion = "1.4.1";

        public static readonly ILog Log = LogManager.GetLogger(ModName);

        private ModSettings? _settings;

        /// <summary>
        /// Static accessor for shared settings instance.
        /// </summary>
        public static ModSettings? Settings { get; private set; }

        /// <summary>
        /// Indicates whether the mod loaded successfully.
        /// </summary>
        public static bool IsLoaded { get; private set; } = false;

        /// <summary>
        /// Stores any loading error for diagnostics.
        /// </summary>
        public static string? LoadError { get; private set; } = null;

        /// <summary>
        /// Called when the mod is loaded.
        /// </summary>
        public void OnLoad(UpdateSystem updateSystem)
        {
            Log.Info($"========================================");
            Log.Info($"[{ModName}] v{ModVersion} loading...");
            Log.Info($"[{ModName}] Load time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Log.Info($"[{ModName}] CLR Version: {Environment.Version}");
            Log.Info($"[{ModName}] OS: {Environment.OSVersion}");
            Log.Info($"========================================");

            try
            {
                // Validate updateSystem
                if (updateSystem == null)
                {
                    string error = "UpdateSystem is null - mod cannot function!";
                    Log.Error($"[{ModName}] {error}");
                    LoadError = error;
                    return;
                }
                Log.Info($"[{ModName}] UpdateSystem valid: {updateSystem.GetType().FullName}");

                // Register our system directly with the UpdateSystem (CS2 native approach)
                try
                {
                    updateSystem.UpdateAt<BudgetBoosterSystem>(SystemUpdatePhase.GameSimulation);
                    Patches.MarkSystemRegistered();
                    Log.Info($"[{ModName}] BudgetBoosterSystem registered with GameSimulation phase");
                }
                catch (Exception ex)
                {
                    Log.Error($"[{ModName}] Failed to register BudgetBoosterSystem: {ex.Message}");
                    Log.Error($"[{ModName}] Stack trace: {ex.StackTrace}");
                    Patches.RecordError(ex.Message);
                    LoadError = $"System registration failed: {ex.Message}";
                    return;
                }

                // Initialize settings with defensive error handling
                try
                {
                    _settings = new ModSettings(this);
                    _settings.RegisterInOptionsUI();
                    Log.Info($"[{ModName}] Settings initialized and registered in Options UI");
                }
                catch (Exception ex)
                {
                    Log.Error($"[{ModName}] Failed to initialize settings: {ex.Message}");
                    LoadError = $"Settings initialization failed: {ex.Message}";
                    return;
                }

                // Load saved settings with validation
                try
                {
                    // The section name MUST match the file name declared by [FileLocation] on ModSettings.
                    // Passing ModName here (with its space) is what produced a second, orphaned section.
                    AssetDatabase.global.LoadSettings(ModSettings.SettingsSection, _settings, new ModSettings(this));
                    Log.Info($"[{ModName}] Settings loaded from storage (section '{ModSettings.SettingsSection}')");

                    // Carry values over from the pre-1.2.1 section name, once.
                    MigrateLegacySettingsSection();

                    // Validate and correct any out-of-bounds settings
                    bool settingsValid = _settings.ValidateAndCorrect();
                    if (!settingsValid)
                    {
                        Log.Info($"[{ModName}] Some settings were corrected and will be saved");
                        _settings.ApplyAndSave();
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"[{ModName}] Failed to load saved settings, using defaults: {ex.Message}");
                    _settings.SetDefaults();
                }

                // Set static settings reference for BudgetBoosterSystem to use
                Settings = _settings;

                // Load the reloadable boost logic. This is the only load path - release builds use it
                // too, so the code that ships is the code that gets exercised during development.
                if (!LogicHost.LoadInitial(this))
                {
                    Log.Error($"[{ModName}] Failed to load boost logic: {LogicHost.LastError}");
                    Log.Error($"[{ModName}] The mod will run but apply no boosts until {LogicHost.LogicAssemblyFileName} loads.");
                }

                // Register localization
                try
                {
                    GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(_settings));
                    Log.Info($"[{ModName}] Localization registered");
                }
                catch (Exception ex)
                {
                    Log.Warn($"[{ModName}] Failed to register localization: {ex.Message}");
                    // Non-fatal - continue loading
                }

                // Mark as loaded
                IsLoaded = true;
                LoadError = null;

                // Log startup summary
                LogStartupSummary();

                Log.Info($"[{ModName}] v{ModVersion} loaded successfully!");
            }
            catch (Exception ex)
            {
                string error = $"Critical error during load: {ex.Message}";
                Log.Error($"[{ModName}] {error}");
                Log.Error($"[{ModName}] Stack trace: {ex.StackTrace}");
                LoadError = error;
                IsLoaded = false;
            }
        }

        /// <summary>
        /// Versions up to 1.2.0 saved the settings file as "BudgetBooster.coc" (from [FileLocation]) but
        /// read and wrote the section inside it as "Budget Booster" (from ModName). Correcting the section
        /// name would otherwise strand every existing user's configuration and statistics in the old
        /// section, so pull them across once and stamp the schema version.
        ///
        /// Both sections live in the same .coc file, so the legacy values can be read back through the
        /// normal settings API rather than by parsing the file by hand.
        /// </summary>
        private void MigrateLegacySettingsSection()
        {
            if (_settings == null)
                return;

            if (_settings.SettingsVersion >= ModSettings.CurrentSettingsVersion)
                return;

            try
            {
                // Read the old section. If it is absent, LoadSettings leaves this instance at its defaults.
                var legacy = new ModSettings(this);
                AssetDatabase.global.LoadSettings(ModSettings.LegacySettingsSection, legacy, new ModSettings(this));

                // A defaulted instance is indistinguishable from "there was nothing to migrate", so only
                // overwrite when the legacy section actually differs from a pristine default.
                if (legacy.ValuesEqual(new ModSettings(this)))
                {
                    Log.Info($"[{ModName}] No legacy settings found in section '{ModSettings.LegacySettingsSection}' - nothing to migrate");
                }
                else
                {
                    _settings.CopyFrom(legacy);
                    Log.Info($"[{ModName}] Migrated settings from legacy section '{ModSettings.LegacySettingsSection}' to '{ModSettings.SettingsSection}'");
                    Log.Info($"[{ModName}] Migrated values - Enabled: {_settings.Enabled}, Mode: {_settings.BoostMode}, Boost: ${_settings.BoostAmount:N0}, Target: ${_settings.TargetSurplus:N0}");
                }

                _settings.SettingsVersion = ModSettings.CurrentSettingsVersion;
                _settings.ApplyAndSave();
            }
            catch (Exception ex)
            {
                // Non-fatal: the mod still runs, it just starts from whatever the new section held.
                Log.Warn($"[{ModName}] Legacy settings migration failed, continuing with current values: {ex.Message}");
            }
        }

        /// <summary>
        /// Logs a summary of the mod's startup configuration.
        /// </summary>
        private void LogStartupSummary()
        {
            if (_settings == null) return;

            Log.Info($"[{ModName}] === Startup Configuration ===");
            Log.Info($"[{ModName}] Enabled: {_settings.Enabled}");
            Log.Info($"[{ModName}] Boost Mode: {_settings.BoostMode}");
            Log.Info($"[{ModName}] Boost Amount: ${_settings.BoostAmount:N0}");
            Log.Info($"[{ModName}] Debug Mode: {_settings.DebugMode}");

            if (_settings.DebugMode)
            {
                Log.Info($"[{ModName}] Target Surplus: ${_settings.TargetSurplus:N0}");
                Log.Info($"[{ModName}] Boost Percentage: {_settings.BoostPercentage}%");
                Log.Info($"[{ModName}] Max Boost Per Cycle: ${_settings.MaxBoostPerCycle:N0}");
                Log.Info($"[{ModName}] Update Interval: {_settings.UpdateInterval} ticks");
            }

            Log.Info($"[{ModName}] ==============================");
        }

        /// <summary>
        /// Gets a comprehensive diagnostic report of the mod's current state.
        /// </summary>
        public static string GetDiagnosticReport()
        {
            var report = $@"
========== BUDGET BOOSTER DIAGNOSTIC REPORT ==========
Mod Version: {ModVersion}
Mod Loaded: {IsLoaded}
Load Error: {LoadError ?? "None"}

{Patches.GetDiagnosticSummary()}

Settings Available: {(Settings != null ? "Yes" : "No")}
{(Settings != null ? Settings.GetDiagnosticSummary() : "Settings not loaded")}
======================================================";

            return report;
        }

        /// <summary>
        /// Logs a full diagnostic report to the game log.
        /// </summary>
        public static void LogDiagnosticReport()
        {
            Log.Info(GetDiagnosticReport());
        }

        /// <summary>
        /// Called when the mod is disposed.
        /// </summary>
        public void OnDispose()
        {
            Log.Info($"[{ModName}] disposing...");

            try
            {
                if (_settings != null)
                {
                    _settings.UnregisterInOptionsUI();
                    _settings = null;
                    Settings = null;
                    Log.Info($"[{ModName}] Settings unregistered and cleared");
                }

                // Reset patch tracking
                Patches.Reset();

                // Clear session statistics so a mod reload starts from zero
                SessionStatistics.Reset();

                // Drop the logic reference. The assembly itself stays loaded - Mono cannot unload it.
                LogicHost.Reset();

                IsLoaded = false;
                LoadError = null;

                Log.Info($"[{ModName}] disposed successfully.");
            }
            catch (Exception ex)
            {
                Log.Error($"[{ModName}] Error during disposal: {ex.Message}");
            }
        }
    }
}
