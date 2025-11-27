using System;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownWallBuilding.Core.Services
{
    /// <summary>
    /// Service Locator pattern for centralized service management.
    /// Avoids singleton abuse and provides better testability.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        private static bool isQuitting = false;

        /// <summary>
        /// Register a service implementation for a given interface type.
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);

            if (services.ContainsKey(type))
            {
                Debug.LogWarning($"Service of type {type.Name} is already registered. Overwriting.");
                services[type] = service;
            }
            else
            {
                services.Add(type, service);
                Debug.Log($"Service registered: {type.Name}");
            }
        }

        /// <summary>
        /// Unregister a service.
        /// </summary>
        public static void Unregister<T>() where T : class
        {
            var type = typeof(T);

            if (services.ContainsKey(type))
            {
                services.Remove(type);
                Debug.Log($"Service unregistered: {type.Name}");
            }
        }

        /// <summary>
        /// Get a registered service. Throws exception if not found.
        /// </summary>
        public static T Get<T>() where T : class
        {
            if (isQuitting)
            {
                Debug.LogWarning($"Attempting to get service {typeof(T).Name} during application quit.");
                return null;
            }

            var type = typeof(T);

            if (services.TryGetValue(type, out var service))
            {
                return service as T;
            }

            throw new Exception($"Service of type {type.Name} not registered!");
        }

        /// <summary>
        /// Try to get a service, returns null if not found.
        /// </summary>
        public static T TryGet<T>() where T : class
        {
            var type = typeof(T);
            return services.TryGetValue(type, out var service) ? service as T : null;
        }

        /// <summary>
        /// Check if a service is registered.
        /// </summary>
        public static bool IsRegistered<T>() where T : class
        {
            return services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Clear all registered services. Use with caution!
        /// </summary>
        public static void Clear()
        {
            services.Clear();
            Debug.Log("All services cleared from ServiceLocator.");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Reset statics for domain reload disabled in editor
            services.Clear();
            isQuitting = false;
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            Application.quitting += () => isQuitting = true;
        }
    }
}
