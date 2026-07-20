using System;
using System.Collections.Generic;

namespace Pulsebound.Core.Services
{
    /// <summary>
    /// Lightweight service locator. Systems register themselves once (usually in a
    /// bootstrap scene) and everyone else resolves interfaces here instead of holding
    /// hard references or leaking singletons across the codebase.
    ///
    /// Registration is explicit which keeps it testable: a test (or the editor's
    /// instant-playtest) can register fakes before exercising a system.
    /// </summary>
    public static class Services
    {
        private static readonly Dictionary<Type, object> Registry = new();

        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            Registry[typeof(T)] = service;
        }

        public static void Unregister<T>() where T : class
        {
            Registry.Remove(typeof(T));
        }

        public static T Get<T>() where T : class
        {
            if (Registry.TryGetValue(typeof(T), out var service))
                return (T)service;
            throw new InvalidOperationException(
                $"Service '{typeof(T).Name}' is not registered. Register it in the bootstrap before use.");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Registry.TryGetValue(typeof(T), out var raw))
            {
                service = (T)raw;
                return true;
            }
            service = null;
            return false;
        }

        public static bool IsRegistered<T>() where T : class => Registry.ContainsKey(typeof(T));

        /// <summary>Clears every registration. Use between scene reloads / tests.</summary>
        public static void Clear() => Registry.Clear();
    }
}
