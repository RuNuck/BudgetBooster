using System;
using System.Runtime.CompilerServices;

namespace BudgetBooster
{
    /// <summary>
    /// Static class to track system registration status.
    /// Note: CS2 uses native UpdateSystem.UpdateAt() for system registration,
    /// so Harmony patches are not needed for this mod.
    /// Thread-safe using volatile fields.
    /// </summary>
    public static class Patches
    {
        // Use volatile for thread-safe reads without locks
        private static volatile bool _isSystemRegistered;
        private static volatile string? _lastError;
        private static DateTime? _registeredAt;
        private static readonly object _lock = new object();

        /// <summary>
        /// Indicates whether the BudgetBoosterSystem was successfully registered.
        /// </summary>
        public static bool IsSystemRegistered
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isSystemRegistered;
        }

        /// <summary>
        /// Stores any error message from the registration attempt.
        /// </summary>
        public static string? LastError => _lastError;

        /// <summary>
        /// Timestamp of when the system was registered.
        /// </summary>
        public static DateTime? RegisteredAt
        {
            get
            {
                lock (_lock)
                {
                    return _registeredAt;
                }
            }
        }

        /// <summary>
        /// Resets tracking state.
        /// </summary>
        internal static void Reset()
        {
            lock (_lock)
            {
                _isSystemRegistered = false;
                _lastError = null;
                _registeredAt = null;
            }
        }

        /// <summary>
        /// Records successful system registration.
        /// </summary>
        internal static void MarkSystemRegistered()
        {
            lock (_lock)
            {
                _isSystemRegistered = true;
                _registeredAt = DateTime.Now;
            }
        }

        /// <summary>
        /// Records an error during registration.
        /// </summary>
        internal static void RecordError(string error)
        {
            _lastError = error;
        }

        /// <summary>
        /// Gets a diagnostic summary of registration status.
        /// </summary>
        public static string GetDiagnosticSummary()
        {
            var registeredAt = RegisteredAt;
            return $@"System Registration Status:
  System Registered: {IsSystemRegistered}
  Registered At: {(registeredAt.HasValue ? registeredAt.Value.ToString("HH:mm:ss") : "N/A")}
  Last Error: {LastError ?? "None"}";
        }
    }
}
