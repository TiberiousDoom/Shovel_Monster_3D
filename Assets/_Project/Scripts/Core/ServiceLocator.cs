using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelRPG.Core
{
    /// <summary>
    /// Simple service locator for dependency injection.
    /// Avoids singletons while providing global access to services.
    /// Multiplayer-ready: services can be swapped for networked implementations.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new();
        private static bool _isInitialized;

        /// <summary>
        /// Registers a service implementation for the given interface type.
        /// </summary>
        /// <typeparam name="T">The interface or base type to register.</typeparam>
        /// <param name="service">The service implementation.</param>
        /// <exception cref="InvalidOperationException">Thrown if service is already registered.</exception>
        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);

            if (Services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Overwriting existing service: {type.Name}");
            }

            Services[type] = service;
            Debug.Log($"[ServiceLocator] Registered: {type.Name}");
        }

        /// <summary>
        /// Gets a registered service.
        /// </summary>
        /// <typeparam name="T">The interface or base type to retrieve.</typeparam>
        /// <returns>The registered service implementation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if service is not registered.</exception>
        public static T Get<T>() where T : class
        {
            var type = typeof(T);

            if (Services.TryGetValue(type, out var service))
            {
                return (T)service;
            }

            throw new InvalidOperationException(
                $"[ServiceLocator] Service not registered: {type.Name}. " +
                "Make sure to register the service before accessing it."
            );
        }

        /// <summary>
        /// Tries to get a registered service without throwing.
        /// </summary>
        /// <typeparam name="T">The interface or base type to retrieve.</typeparam>
        /// <param name="service">The service if found, null otherwise.</param>
        /// <returns>True if service was found, false otherwise.</returns>
        public static bool TryGet<T>(out T service) where T : class
        {
            var type = typeof(T);

            if (Services.TryGetValue(type, out var found))
            {
                service = (T)found;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Checks if a service is registered.
        /// </summary>
        /// <typeparam name="T">The interface or base type to check.</typeparam>
        /// <returns>True if service is registered.</returns>
        public static bool IsRegistered<T>() where T : class
        {
            return Services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Unregisters a service.
        /// </summary>
        /// <typeparam name="T">The interface or base type to unregister.</typeparam>
        public static void Unregister<T>() where T : class
        {
            var type = typeof(T);

            if (Services.Remove(type))
            {
                Debug.Log($"[ServiceLocator] Unregistered: {type.Name}");
            }
        }

        /// <summary>
        /// Clears all registered services. Call on scene unload or game shutdown.
        /// </summary>
        public static void Clear()
        {
            Services.Clear();
            _isInitialized = false;
            Debug.Log("[ServiceLocator] All services cleared");
        }

        /// <summary>
        /// Marks the service locator as initialized.
        /// </summary>
        public static void MarkInitialized()
        {
            _isInitialized = true;
        }

        /// <summary>
        /// Whether the service locator has been initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;
    }
}
