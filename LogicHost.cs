using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.SceneFlow;

namespace BudgetBooster
{
    /// <summary>
    /// Loads and hot-swaps the reloadable logic assembly.
    ///
    /// How this works, and why it works despite assemblies being unloadable:
    ///
    /// Unity's Mono cannot unload an assembly - there is no collectible AssemblyLoadContext and no
    /// unloadable AppDomain in a player build. So this never tries. Instead it loads a FRESH copy of
    /// BudgetBooster.Logic.dll from a byte array on each reload, creates a new IBoostLogic instance,
    /// and swaps the reference the ECS system reads. The previous assembly stays loaded forever and
    /// leaks a few tens of KB per reload, which is irrelevant next to being able to iterate without
    /// restarting a city.
    ///
    /// Reading the file into a byte[] first matters twice over: it avoids locking the DLL so a build
    /// can overwrite it, and Assembly.Load(byte[]) yields a distinct assembly per call. A recompile
    /// always changes the module MVID, so each load is genuinely a new assembly rather than a cached
    /// one.
    ///
    /// Swaps are QUEUED, not applied immediately. The settings button that requests a reload fires
    /// from the UI, while the logic is consumed from BudgetBoosterSystem.OnUpdate on the simulation
    /// thread. Applying the swap at the top of the next OnUpdate gives a known-safe point where no
    /// cycle is mid-flight.
    ///
    /// The host always loads the logic this way, in release as well as during development. There is
    /// deliberately no separate "normal" path: a dev-only loading route that differs from the shipped
    /// one is exactly how a bug survives testing.
    /// </summary>
    public static class LogicHost
    {
        /// <summary>File name of the reloadable assembly, expected alongside the host DLL.</summary>
        public const string LogicAssemblyFileName = "BudgetBooster.Logic.dll";

        private static IBoostLogic? _current;
        private static IBoostLogic? _pending;
        private static volatile bool _hasPending;

        private static int _generation;
        private static string? _lastError;
        private static string? _logicPath;
        private static IMod? _mod;

        /// <summary>The live logic instance, or null if none has loaded successfully.</summary>
        public static IBoostLogic? Current => _current;

        /// <summary>How many times logic has been loaded. 1 after the initial load.</summary>
        public static int Generation => Volatile.Read(ref _generation);

        /// <summary>Last load failure, or null if the last attempt succeeded.</summary>
        public static string? LastError => _lastError;

        /// <summary>True when a reload has been requested but not yet applied.</summary>
        public static bool HasPendingSwap => _hasPending;

        /// <summary>
        /// Resolves where BudgetBooster.Logic.dll should live: next to the host assembly, inside the
        /// mod's own install folder. Nothing outside that folder is ever loaded.
        /// </summary>
        /// <remarks>
        /// Assembly.Location is USELESS here. Cities: Skylines II loads mod assemblies from byte
        /// arrays itself, so Location is an empty string for this very assembly - visible in the log
        /// as "[RELEVANT] BudgetBooster v1.4.0.0 @ " with nothing after the @.
        ///
        /// The supported route is to ask the game where it loaded us from:
        /// ModManager.TryGetExecutableAsset(IMod, out ExecutableAsset) hands back the asset whose
        /// path is the on-disk DLL. The Location check is kept only as a harmless fallback.
        /// </remarks>
        private static string? ResolveLogicPath()
        {
            if (_logicPath != null)
                return _logicPath;

            try
            {
                string? dir = null;

                // Preferred: ask the game's mod manager for our own executable asset.
                if (_mod != null)
                {
                    var modManager = GameManager.instance?.modManager;

                    if (modManager == null)
                    {
                        _lastError = "ModManager unavailable while resolving logic path";
                    }
                    else if (modManager.TryGetExecutableAsset(_mod, out ExecutableAsset asset) && asset != null)
                    {
                        string assetPath = asset.path;
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            dir = Path.GetDirectoryName(assetPath);
                            Mod.Log.Info($"[{Mod.ModName}] Mod asset path: {assetPath}");
                        }
                    }
                    else
                    {
                        _lastError = "ModManager could not resolve this mod's executable asset";
                    }
                }

                // Fallback, for the case where the host was loaded from disk after all.
                if (string.IsNullOrEmpty(dir))
                {
                    string hostLocation = typeof(LogicHost).Assembly.Location;
                    if (!string.IsNullOrEmpty(hostLocation))
                    {
                        dir = Path.GetDirectoryName(hostLocation);
                    }
                }

                if (string.IsNullOrEmpty(dir))
                {
                    _lastError ??= "Could not determine the mod's directory";
                    return null;
                }

                _logicPath = Path.Combine(dir, LogicAssemblyFileName);
                _lastError = null;
                return _logicPath;
            }
            catch (Exception ex)
            {
                _lastError = $"Failed to resolve logic path: {ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// Loads the logic assembly and installs it immediately. Used for the initial load during
        /// OnLoad, before any simulation is running.
        /// </summary>
        /// <returns>True if logic is now live.</returns>
        /// <param name="mod">
        /// The mod instance, used to ask the game's ModManager where this mod was loaded from.
        /// Required, because CS2 byte-loads mod assemblies and Assembly.Location is therefore empty.
        /// </param>
        public static bool LoadInitial(IMod mod)
        {
            _mod = mod;

            if (!TryBuildInstance(out IBoostLogic? logic) || logic == null)
                return false;

            _current = logic;
            Interlocked.Increment(ref _generation);
            Mod.Log.Info($"[{Mod.ModName}] Logic loaded (generation {Generation}): {SafeDescribe(logic)}");
            return true;
        }

        /// <summary>
        /// Loads a fresh copy of the logic assembly and queues it to replace the live instance at the
        /// next safe point. Safe to call from the settings UI.
        /// </summary>
        /// <returns>True if the new logic was built and queued.</returns>
        public static bool RequestReload()
        {
            if (!TryBuildInstance(out IBoostLogic? logic) || logic == null)
            {
                Mod.Log.Warn($"[{Mod.ModName}] Reload failed, keeping generation {Generation}: {_lastError}");
                return false;
            }

            _pending = logic;
            _hasPending = true;
            Mod.Log.Info($"[{Mod.ModName}] Reload staged, will apply at next update: {SafeDescribe(logic)}");
            return true;
        }

        /// <summary>
        /// Applies a queued swap. Call this at the top of OnUpdate, where no boost cycle is in flight.
        /// </summary>
        /// <returns>True if a swap was applied.</returns>
        public static bool ApplyPendingSwap()
        {
            if (!_hasPending)
                return false;

            _hasPending = false;

            var incoming = _pending;
            _pending = null;

            if (incoming == null)
                return false;

            _current = incoming;
            Interlocked.Increment(ref _generation);
            Mod.Log.Info($"[{Mod.ModName}] Logic swapped in (generation {Generation}): {SafeDescribe(incoming)}");
            return true;
        }

        /// <summary>
        /// Reads the logic assembly from disk and constructs the single IBoostLogic implementation
        /// inside it. Never throws; failures land in <see cref="LastError"/>.
        /// </summary>
        private static bool TryBuildInstance(out IBoostLogic? logic)
        {
            logic = null;

            string? path = ResolveLogicPath();
            if (path == null)
                return false;

            if (!File.Exists(path))
            {
                _lastError = $"Logic assembly not found at {path}";
                return false;
            }

            try
            {
                // Read to memory so the file is not locked and a build can overwrite it.
                byte[] bytes = File.ReadAllBytes(path);
                Assembly assembly = Assembly.Load(bytes);

                Type? implementation = assembly
                    .GetTypes()
                    .FirstOrDefault(t => typeof(IBoostLogic).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

                if (implementation == null)
                {
                    _lastError = $"No IBoostLogic implementation found in {LogicAssemblyFileName}";
                    return false;
                }

                if (Activator.CreateInstance(implementation) is not IBoostLogic instance)
                {
                    _lastError = $"Could not construct {implementation.FullName} as IBoostLogic";
                    return false;
                }

                logic = instance;
                _lastError = null;
                return true;
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Most likely cause: the logic assembly was built against a different host contract.
                string detail = ex.LoaderExceptions.FirstOrDefault()?.Message ?? ex.Message;
                _lastError = $"Type load failed (rebuild both assemblies?): {detail}";
                return false;
            }
            catch (Exception ex)
            {
                _lastError = $"Load failed: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Calls Describe() defensively - the logic assembly is the part most likely to be mid-edit.
        /// </summary>
        private static string SafeDescribe(IBoostLogic logic)
        {
            try
            {
                return logic.Describe() ?? "(null description)";
            }
            catch (Exception ex)
            {
                return $"(Describe threw: {ex.Message})";
            }
        }

        /// <summary>Diagnostic summary for the Run Diagnostics report.</summary>
        public static string GetDiagnosticSummary()
        {
            return $@"Reloadable Logic Status:
  Logic Path: {_logicPath ?? "(unresolved)"}
  Loaded: {(_current != null ? "Yes" : "No")}
  Generation: {Generation}
  Pending Swap: {_hasPending}
  Description: {(_current != null ? SafeDescribe(_current) : "N/A")}
  Last Error: {_lastError ?? "None"}";
        }

        /// <summary>Clears state so a mod reload starts clean.</summary>
        public static void Reset()
        {
            _current = null;
            _pending = null;
            _hasPending = false;
            _logicPath = null;
            _lastError = null;
            _mod = null;
            Interlocked.Exchange(ref _generation, 0);
        }
    }
}
