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
        public const string ModVersion = "1.5.0";

        /// <summary>Locale the English strings are registered under, and removed from on dispose.</summary>
        private const string LocaleId = "en-US";

        public static readonly ILog Log = LogManager.GetLogger(ModName);

        private ModSettings? _settings;
        private LocaleEN? _locale;

        /// <summary>
        /// Static accessor for shared settings instance.
        /// </summary>
        public static ModSettings? Settings { get; private set; }

        /// <summary>
        /// Indicates whether OnLoad ran to completion. It can be true while <see cref="LoadError"/> is
        /// set, when a non-fatal step (loading the boost logic) failed.
        /// </summary>
        public static bool IsLoaded { get; private set; } = false;

        /// <summary>
        /// The first error met while loading, or null. Shown by Run Diagnostics.
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
                    Log.Info($"[{ModName}] BudgetBoosterSystem registered with GameSimulation phase");
                }
                catch (Exception ex)
                {
                    Log.Error($"[{ModName}] Failed to register BudgetBoosterSystem: {ex.Message}");
                    Log.Error($"[{ModName}] Stack trace: {ex.StackTrace}");
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

                    // Bring settings saved by older versions up to date.
                    MigrateSettings();

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
                // Not fatal: the settings UI still works, and Reload Boost Logic can recover it.
                if (!LogicHost.LoadInitial(this))
                {
                    Log.Error($"[{ModName}] Failed to load boost logic: {LogicHost.LastError}");
                    Log.Error($"[{ModName}] The mod will run but apply no boosts until {LogicHost.LogicAssemblyFileName} loads.");
                    LoadError = $"Boost logic failed to load: {LogicHost.LastError}";
                }

                // Register localization. The instance is kept so OnDispose can remove it again.
                try
                {
                    _locale = new LocaleEN(_settings);
                    GameManager.instance.localizationManager.AddSource(LocaleId, _locale);
                    Log.Info($"[{ModName}] Localization registered");
                }
                catch (Exception ex)
                {
                    _locale = null;
                    Log.Warn($"[{ModName}] Failed to register localization: {ex.Message}");
                    // Non-fatal - continue loading
                }

                // Mark as loaded
                IsLoaded = true;

                // Log startup summary
                LogStartupSummary();

                if (LoadError == null)
                {
                    Log.Info($"[{ModName}] v{ModVersion} loaded successfully!");
                }
                else
                {
                    Log.Warn($"[{ModName}] v{ModVersion} loaded with errors: {LoadError}");
                }
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
        /// Upgrades settings saved by older versions, one step per schema version.
        ///
        /// Each step is gated on the version it upgrades FROM and stamps the version it upgrades TO, so
        /// a file only ever runs the steps it has not had yet. To add a migration, append a step below
        /// and then raise <see cref="ModSettings.CurrentSettingsVersion"/> to match. Never gate a step on
        /// CurrentSettingsVersion itself: an older build did, and raising the constant would have re-run
        /// the legacy copy and overwritten current settings with values from v1.2.0.
        /// </summary>
        private void MigrateSettings()
        {
            if (_settings == null)
                return;

            int startVersion = _settings.SettingsVersion;
            if (startVersion >= ModSettings.CurrentSettingsVersion)
                return;

            try
            {
                // 0 -> 1: carry values over from the pre-1.2.1 section name.
                if (_settings.SettingsVersion < 1)
                {
                    CopyLegacySettingsSection();
                    _settings.SettingsVersion = 1;
                }
            }
            catch (Exception ex)
            {
                // Non-fatal: the mod still runs, it just starts from whatever the new section held. The
                // failed step is not stamped, so it is tried again on the next launch.
                Log.Warn($"[{ModName}] Settings migration from version {_settings.SettingsVersion} failed, continuing with current values: {ex.Message}");
            }

            if (_settings.SettingsVersion != startVersion)
            {
                _settings.ApplyAndSave();
            }
        }

        /// <summary>
        /// Migration step 0 -> 1.
        ///
        /// Versions up to 1.2.0 saved the settings file as "BudgetBooster.coc" (from [FileLocation]) but
        /// read and wrote the section inside it as "Budget Booster" (from ModName). Correcting the section
        /// name would otherwise strand every existing user's configuration in the old section, so pull
        /// it across.
        ///
        /// Both sections live in the same .coc file, so the legacy values can be read back through the
        /// normal settings API rather than by parsing the file by hand.
        /// </summary>
        private void CopyLegacySettingsSection()
        {
            if (_settings == null)
                return;

            // Read the old section. If it is absent, LoadSettings leaves this instance at its defaults.
            var legacy = new ModSettings(this);
            AssetDatabase.global.LoadSettings(ModSettings.LegacySettingsSection, legacy, new ModSettings(this));

            // A defaulted instance is indistinguishable from "there was nothing to migrate", so only
            // overwrite when the legacy section actually differs from a pristine default.
            if (legacy.ValuesEqual(new ModSettings(this)))
            {
                Log.Info($"[{ModName}] No legacy settings found in section '{ModSettings.LegacySettingsSection}' - nothing to migrate");
                return;
            }

            _settings.CopyFrom(legacy);
            Log.Info($"[{ModName}] Migrated settings from legacy section '{ModSettings.LegacySettingsSection}' to '{ModSettings.SettingsSection}'");
            Log.Info($"[{ModName}] Migrated values - Enabled: {_settings.Enabled}, Mode: {_settings.BoostMode}, Boost: ${_settings.BoostAmount:N0}, Target: ${_settings.TargetSurplus:N0}");
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
        /// Gets the part of the diagnostic report that needs no city: version, load state, boost
        /// logic, settings and session statistics. Safe to build from the settings UI.
        /// </summary>
        public static string GetDiagnosticReport()
        {
            var settings = Settings;

            var report = $@"
========== BUDGET BOOSTER DIAGNOSTIC REPORT ==========
Mod Version: {ModVersion}
Mod Loaded: {IsLoaded}
Load Error (at startup): {LoadError ?? "None"}

{LogicHost.GetDiagnosticSummary()}

Settings Available: {(settings != null ? "Yes" : "No")}
{(settings != null ? settings.GetDiagnosticSummary() : "Settings not loaded")}

City figures (balance, Unlimited Money, expenses, Percentage path) follow as a CITY DIAGNOSTICS
block on the next simulation tick. They need a loaded city that is not paused.
======================================================";

            return report;
        }

        /// <summary>
        /// Logs the non-city part of the diagnostic report to the mod log, immediately.
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
                // Log the totals before they are cleared below.
                Log.Info($"[{ModName}] Session totals: {SessionStatistics.TotalBoosts:N0} boosts, ${SessionStatistics.TotalMoneyAdded:N0} added");

                if (_locale != null)
                {
                    GameManager.instance?.localizationManager?.RemoveSource(LocaleId, _locale);
                    _locale = null;
                }

                if (_settings != null)
                {
                    _settings.UnregisterInOptionsUI();
                    _settings = null;
                    Settings = null;
                    Log.Info($"[{ModName}] Settings unregistered and cleared");
                }

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
