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
        public const string ModVersion = "1.0.0";

        public static readonly ILog Log = LogManager.GetLogger(ModName, true)
            .SetShowsErrorsInUI(false);

        private ModSettings? _settings;

        /// <summary>
        /// Static accessor for shared settings instance.
        /// </summary>
        public static ModSettings? Settings { get; private set; }

        /// <summary>
        /// Called when the mod is loaded.
        /// </summary>
        public void OnLoad(UpdateSystem updateSystem)
        {
            Log.Info($"{ModName} v{ModVersion} loading...");

            // Initialize settings
            _settings = new ModSettings(this);
            _settings.RegisterInOptionsUI();

            // Load saved settings
            AssetDatabase.global.LoadSettings(ModName, _settings, new ModSettings(this));

            // Set static settings reference for BudgetBoosterSystem to use
            Settings = _settings;

            // Register localization
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(_settings));

            // Note: System registration is handled via Harmony patch in Patches.cs

            Log.Info($"{ModName} loaded successfully!");
        }

        /// <summary>
        /// Called when the mod is disposed.
        /// </summary>
        public void OnDispose()
        {
            Log.Info($"{ModName} disposed.");
            
            if (_settings != null)
            {
                _settings.UnregisterInOptionsUI();
                _settings = null;
                Settings = null;
            }
        }
    }
}
