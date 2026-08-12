using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BudgetBooster
{
    /// <summary>
    /// Dedicated accessor class for PlayerMoney operations.
    /// Uses purely runtime-based type lookup to avoid compile-time dependencies.
    /// Encapsulates reflection logic with comprehensive error handling and recovery.
    /// </summary>
    public static class PlayerMoneyAccessor
    {
        // Cached reflection field for PlayerMoney.m_Money
        private static FieldInfo? _moneyField;
        
        // Cached PlayerMoney type (loaded at runtime)
        private static Type? _playerMoneyType;
        
        // Cached field type for optimized conversion
        private static Type? _fieldType;
        
        private static volatile bool _fieldLookupAttempted;
        private static volatile bool _fieldLookupSucceeded;

        // Error tracking for diagnostics (thread-safe)
        private static string? _lastError;
        private static int _successfulReads;
        private static int _failedReads;

        // Alternative field names to try (readonly for performance)
        private static readonly string[] AlternativeFieldNames = { "_money", "money", "Money", "_Money" };

        /// <summary>
        /// Logs all loaded assemblies for debugging purposes.
        /// </summary>
        private static void LogLoadedAssemblies()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Mod.Log.Info($"[Budget Booster] === LOADED ASSEMBLIES ({assemblies.Length} total) ===");
                
                foreach (var asm in assemblies)
                {
                    try
                    {
                        var name = asm.GetName();
                        // Log Game-related assemblies in detail
                        if (name.Name != null && (name.Name.Contains("Game") || name.Name.Contains("Colossal") || name.Name.Contains("BudgetBooster")))
                        {
                            Mod.Log.Info($"[Budget Booster]   [RELEVANT] {name.Name} v{name.Version} @ {asm.Location}");
                        }
                    }
                    catch
                    {
                        // Skip assemblies that throw on GetName
                    }
                }
                
                Mod.Log.Info("[Budget Booster] === END LOADED ASSEMBLIES ===");
            }
            catch (Exception ex)
            {
                Mod.Log.Warn($"[Budget Booster] Failed to enumerate assemblies: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets whether the money field was successfully cached.
        /// </summary>
        public static bool IsFieldCached
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _fieldLookupSucceeded && _moneyField != null;
        }

        /// <summary>
        /// Gets the last error message, if any.
        /// </summary>
        public static string? LastError => _lastError;

        /// <summary>
        /// Gets the count of successful money reads (thread-safe).
        /// </summary>
        public static int SuccessfulReads => Volatile.Read(ref _successfulReads);

        /// <summary>
        /// Gets the count of failed money reads (thread-safe).
        /// </summary>
        public static int FailedReads => Volatile.Read(ref _failedReads);

        /// <summary>
        /// Initializes the accessor by caching the reflection field.
        /// Uses runtime type lookup to avoid compile-time dependencies.
        /// Call this early in mod initialization for optimal performance.
        /// </summary>
        /// <returns>True if initialization succeeded, false otherwise.</returns>
        public static bool Initialize()
        {
            Mod.Log.Info("[Budget Booster] PlayerMoneyAccessor.Initialize() called");
            
            if (_fieldLookupAttempted)
            {
                Mod.Log.Info($"[Budget Booster] PlayerMoneyAccessor already attempted, returning cached result: {_fieldLookupSucceeded}");
                return _fieldLookupSucceeded;
            }

            _fieldLookupAttempted = true;
            Mod.Log.Info("[Budget Booster] PlayerMoneyAccessor starting first-time initialization...");

            // Log all loaded assemblies for debugging
            LogLoadedAssemblies();

            try
            {
                // Find PlayerMoney type at runtime from Game assembly
                Mod.Log.Info("[Budget Booster] Searching for PlayerMoney type...");
                _playerMoneyType = FindPlayerMoneyType();
                if (_playerMoneyType == null)
                {
                    _lastError = "PlayerMoney type not found in Game assembly";
                    _fieldLookupSucceeded = false;
                    Mod.Log.Error($"[Budget Booster] PlayerMoneyAccessor: {_lastError}");
                    return false;
                }

                // Primary field name
                _moneyField = _playerMoneyType.GetField("m_Money",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (_moneyField != null)
                {
                    _fieldType = _moneyField.FieldType;
                    _fieldLookupSucceeded = true;
                    Mod.Log.Info($"[Budget Booster] PlayerMoneyAccessor: Field 'm_Money' cached (type: {_fieldType.Name})");
                    return true;
                }

                // Fallback: Try alternative field names (in case of game updates)
                foreach (var altName in AlternativeFieldNames)
                {
                    _moneyField = _playerMoneyType.GetField(altName,
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

                    if (_moneyField != null)
                    {
                        _fieldType = _moneyField.FieldType;
                        _fieldLookupSucceeded = true;
                        Mod.Log.Info($"[Budget Booster] PlayerMoneyAccessor: Field '{altName}' cached (alternative name)");
                        return true;
                    }
                }

                // Log available fields for debugging
                var fields = _playerMoneyType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                Mod.Log.Warn($"[Budget Booster] PlayerMoneyAccessor: Could not find money field. Available fields: {string.Join(", ", Array.ConvertAll(fields, f => f.Name))}");
                
                _lastError = "Money field not found in PlayerMoney struct";
                _fieldLookupSucceeded = false;
                return false;
            }
            catch (Exception ex)
            {
                _lastError = $"Initialization failed: {ex.Message}";
                _fieldLookupSucceeded = false;
                Mod.Log.Error($"[Budget Booster] PlayerMoneyAccessor: {_lastError}");
                return false;
            }
        }

        /// <summary>
        /// Finds the PlayerMoney type at runtime from loaded assemblies.
        /// Searches Game assembly and falls back to any loaded assembly.
        /// Also tries alternative type names in case of game updates.
        /// </summary>
        private static Type? FindPlayerMoneyType()
        {
            Mod.Log.Info("[Budget Booster] FindPlayerMoneyType() starting...");
            
            // Known type names to try (in case of game updates renaming/moving types)
            string[] typeNamesToTry = {
                "Game.Economy.PlayerMoney",
                "Game.Economy.Money",
                "Game.Economy.PlayerBudget",
                "Game.Economy.CityMoney",
                "Game.City.PlayerMoney",
                "Game.Simulation.PlayerMoney",
                "Game.Prefabs.PlayerMoney",
                "Game.Companies.PlayerMoney"
            };
            
            Mod.Log.Info($"[Budget Booster] Will try {typeNamesToTry.Length} type name variations");

            // Try to find Game assembly first
            Assembly? gameAssembly = null;
            int assemblyCount = 0;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                assemblyCount++;
                var asmName = assembly.GetName().Name;
                
                if (asmName == "Game")
                {
                    gameAssembly = assembly;
                    Mod.Log.Info($"[Budget Booster] Found Game assembly: {assembly.FullName}");
                    Mod.Log.Info($"[Budget Booster] Game assembly location: {assembly.Location}");
                    
                    // Try known type names
                    foreach (var typeName in typeNamesToTry)
                    {
                        Mod.Log.Info($"[Budget Booster] Trying type: {typeName}");
                        var type = assembly.GetType(typeName);
                        if (type != null)
                        {
                            Mod.Log.Info($"[Budget Booster] SUCCESS! Found {typeName} in Game assembly");
                            Mod.Log.Info($"[Budget Booster] Type details: {type.FullName}, IsValueType={type.IsValueType}, IsClass={type.IsClass}");
                            return type;
                        }
                        else
                        {
                            Mod.Log.Info($"[Budget Booster] Type {typeName} not found");
                        }
                    }
                    break;
                }
            }
            
            Mod.Log.Info($"[Budget Booster] Searched {assemblyCount} assemblies, Game assembly found: {gameAssembly != null}");

            // Log what Economy types exist for debugging
            if (gameAssembly != null)
            {
                Mod.Log.Info("[Budget Booster] Enumerating ALL types in Game assembly to find Money-related types...");
                try
                {
                    var economyTypes = new System.Collections.Generic.List<string>();
                    var allTypeCount = 0;
                    var errorCount = 0;
                    
                    Type[] types;
                    try
                    {
                        types = gameAssembly.GetTypes();
                        Mod.Log.Info($"[Budget Booster] Game assembly has {types.Length} types");
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        Mod.Log.Warn($"[Budget Booster] ReflectionTypeLoadException - loading partial types");
                        types = ex.Types.Where(t => t != null).ToArray()!;
                        Mod.Log.Info($"[Budget Booster] Loaded {types.Length} types (some failed)");
                        foreach (var loaderEx in ex.LoaderExceptions.Take(5))
                        {
                            Mod.Log.Warn($"[Budget Booster] Loader exception: {loaderEx?.Message}");
                        }
                    }
                    
                    foreach (var type in types)
                    {
                        allTypeCount++;
                        try
                        {
                            if (type.Namespace != null && type.Namespace.Contains("Economy"))
                            {
                                economyTypes.Add(type.FullName ?? type.Name);
                            }
                            // Also check for types with "Money" in name
                            if (type.Name.Contains("Money"))
                            {
                                economyTypes.Add($"[Money] {type.FullName ?? type.Name}");
                            }
                            // Check for Player-related types in case it was renamed
                            if (type.Name.Contains("Player") && type.Namespace != null && type.Namespace.Contains("Game"))
                            {
                                economyTypes.Add($"[Player] {type.FullName ?? type.Name}");
                            }
                        }
                        catch
                        {
                            errorCount++;
                        }
                    }
                    
                    Mod.Log.Info($"[Budget Booster] Scanned {allTypeCount} types, {errorCount} errors during enumeration");
                    
                    if (economyTypes.Count > 0)
                    {
                        // Log first 20 types to avoid spam
                        var toLog = economyTypes.Count > 20 
                            ? economyTypes.GetRange(0, 20) 
                            : economyTypes;
                        Mod.Log.Info($"[Budget Booster] Economy/Money types found ({economyTypes.Count} total): {string.Join(", ", toLog)}");
                    }
                    else
                    {
                        Mod.Log.Warn("[Budget Booster] No Economy or Money types found in Game assembly!");
                    }
                }
                catch (Exception ex)
                {
                    Mod.Log.Warn($"[Budget Booster] Could not enumerate types: {ex.Message}");
                }
            }

            // Fallback: search all assemblies for any type with Money in name
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var typeName in typeNamesToTry)
                    {
                        var type = assembly.GetType(typeName);
                        if (type != null)
                        {
                            Mod.Log.Info($"[Budget Booster] Found {typeName} in {assembly.GetName().Name} assembly");
                            return type;
                        }
                    }
                }
                catch
                {
                    // Skip assemblies that throw
                }
            }

            return null;
        }

        /// <summary>
        /// Converts the raw field value to a long, handling different numeric types.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ConvertToLong(object value)
        {
            // Fast path for common types (avoid Convert.ToInt64 overhead)
            if (_fieldType == typeof(long))
                return (long)value;
            if (_fieldType == typeof(int))
                return (int)value;
            if (_fieldType == typeof(double))
                return (long)(double)value;
            if (_fieldType == typeof(float))
                return (long)(float)value;
            
            // Fallback
            return Convert.ToInt64(value);
        }

        /// <summary>
        /// Gets the current money value from a PlayerMoney component.
        /// Uses cached FieldInfo with optimized type conversion.
        /// </summary>
        /// <param name="playerMoney">The PlayerMoney component (boxed).</param>
        /// <param name="money">The money value if successful.</param>
        /// <returns>True if the read succeeded, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetMoney(object playerMoney, out long money)
        {
            money = 0;

            // Ensure field is cached
            if (!_fieldLookupAttempted)
            {
                Initialize();
            }

            // Use cached field
            if (_moneyField != null && _fieldLookupSucceeded)
            {
                try
                {
                    var value = _moneyField.GetValue(playerMoney);
                    if (value != null)
                    {
                        money = ConvertToLong(value);
                        Interlocked.Increment(ref _successfulReads);
                        _lastError = null;
                        return true;
                    }
                    _lastError = "Field value was null";
                }
                catch (Exception ex)
                {
                    _lastError = $"Read failed: {ex.Message}";
                }
            }

            Interlocked.Increment(ref _failedReads);
            return false;
        }

        /// <summary>
        /// Gets the current money value, returning a default value on failure.
        /// </summary>
        /// <param name="playerMoney">The PlayerMoney component (boxed).</param>
        /// <param name="defaultValue">The value to return if reading fails.</param>
        /// <returns>The money value or the default value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetMoneyOrDefault(object playerMoney, long defaultValue = 0)
        {
            return TryGetMoney(playerMoney, out long money) ? money : defaultValue;
        }

        /// <summary>
        /// Resets the accessor state. Useful for testing or re-initialization.
        /// </summary>
        public static void Reset()
        {
            _moneyField = null;
            _fieldType = null;
            _fieldLookupAttempted = false;
            _fieldLookupSucceeded = false;
            _lastError = null;
            Interlocked.Exchange(ref _successfulReads, 0);
            Interlocked.Exchange(ref _failedReads, 0);
        }

        /// <summary>
        /// Gets a diagnostic summary of the accessor state.
        /// </summary>
        public static string GetDiagnosticSummary()
        {
            int successful = SuccessfulReads;
            int failed = FailedReads;
            int total = successful + failed;
            
            return $@"PlayerMoneyAccessor Status:
  Field Lookup Attempted: {_fieldLookupAttempted}
  Field Cached: {_fieldLookupSucceeded}
  Field Name: {_moneyField?.Name ?? "N/A"}
  Field Type: {_fieldType?.Name ?? "N/A"}
  Successful Reads: {successful:N0}
  Failed Reads: {failed:N0}
  Success Rate: {(total > 0 ? (100.0 * successful / total).ToString("F1") : "N/A")}%
  Last Error: {_lastError ?? "None"}";
        }
    }
}
