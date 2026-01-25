/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Utility
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Caches the components for quick lookup.
    /// </summary>
    public static class CacheUtility
    {
        private static Dictionary<GameObject, Dictionary<Type, object>> s_GameObjectComponentMap = new Dictionary<GameObject, Dictionary<Type, object>>();
        private static Dictionary<GameObject, Dictionary<Type, object>> s_GameObjectParentComponentMap = new Dictionary<GameObject, Dictionary<Type, object>>();
        private static Dictionary<GameObject, Dictionary<Type, object[]>> s_GameObjectComponentsMap = new Dictionary<GameObject, Dictionary<Type, object[]>>();

        /// <summary>
        /// Returns a cached component reference for the specified type.
        /// </summary>
        /// <param name="gameObject">The GameObject (or child GameObject) to get the component reference of.</param>
        /// <returns>The cached component reference.</returns>
        public static T GetCachedComponent<T>(this GameObject gameObject)
        {
            Dictionary<Type, object> typeComponentMap;
            // Return the cached component if it exists.
            if (s_GameObjectComponentMap.TryGetValue(gameObject, out typeComponentMap)) {
                object targetObject;
                if (typeComponentMap.TryGetValue(typeof(T), out targetObject)) {
                    return (T)targetObject;
                }
            } else {
                // The cached component doesn't exist for the specified type.
                typeComponentMap = new Dictionary<Type, object>();
                s_GameObjectComponentMap.Add(gameObject, typeComponentMap);
            }

            // Find the component reference and cache the results.
            var targetComponent = gameObject.GetComponent<T>();
            typeComponentMap.Add(typeof(T), targetComponent);
            return targetComponent;
        }

        /// <summary>
        /// Returns a cached parent component reference for the specified type.
        /// </summary>
        /// <param name="gameObject">The GameObject (or child GameObject) to get the component reference of.</param>
        /// <returns>The cached component reference.</returns>
        public static T GetCachedParentComponent<T>(this GameObject gameObject)
        {
            Dictionary<Type, object> typeComponentMap;
            // Return the cached component if it exists.
            if (s_GameObjectParentComponentMap.TryGetValue(gameObject, out typeComponentMap)) {
                object targetObject;
                if (typeComponentMap.TryGetValue(typeof(T), out targetObject)) {
                    return (T)targetObject;
                }
            } else {
                // The cached component doesn't exist for the specified type.
                typeComponentMap = new Dictionary<Type, object>();
                s_GameObjectParentComponentMap.Add(gameObject, typeComponentMap);
            }

            // Find the component reference and cache the results.
            var targetComponent = gameObject.GetComponentInParent<T>();
            typeComponentMap.Add(typeof(T), targetComponent);
            return targetComponent;
        }

        /// <summary>
        /// Returns a cached component reference for the specified type.
        /// </summary>
        /// <param name="gameObject">The GameObject (or child GameObject) to get the component reference of.</param>
        /// <returns>The cached components reference.</returns>
        public static T[] GetCachedComponentsInChildren<T>(this GameObject gameObject)
        {
            Dictionary<Type, object[]> typeComponentsMap;
            // Return the cached component if it exists.
            if (s_GameObjectComponentsMap.TryGetValue(gameObject, out typeComponentsMap)) {
                object[] targetObjects;
                if (typeComponentsMap.TryGetValue(typeof(T), out targetObjects)) {
                    return targetObjects as T[];
                }
            } else {
                // The cached component doesn't exist for the specified type.
                typeComponentsMap = new Dictionary<Type, object[]>();
                s_GameObjectComponentsMap.Add(gameObject, typeComponentsMap);
            }

            // Find the component reference and cache the results.
            var targetComponents = gameObject.GetComponentsInChildren<T>();
            typeComponentsMap.Add(typeof(T), targetComponents as object[]);
            return targetComponents;
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearCache()
        {
            s_GameObjectComponentMap.Clear();
            s_GameObjectParentComponentMap.Clear();
            s_GameObjectComponentsMap.Clear();
        }
    }
}