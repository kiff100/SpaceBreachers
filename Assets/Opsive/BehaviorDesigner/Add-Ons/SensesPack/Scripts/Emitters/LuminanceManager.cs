/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Manages all luminance emitters in the scene and provides methods to calculate total luminance at any point.
    /// </summary>
    public class LuminanceManager : MonoBehaviour
    {
        private static LuminanceManager s_Instance;
        public static LuminanceManager Instance {
            get {
                if (s_Instance == null) {
                    s_Instance = new GameObject("LuminanceManager").AddComponent<LuminanceManager>();
                }
                return s_Instance;
            }
        }

        [Tooltip("Should the ambient light be used in the luminance calculation?")]
        [SerializeField] protected bool m_UseAmbientLight = true;

        /// <summary>
        /// List of all active luminance emitters in the scene.
        /// </summary>
        private List<LuminanceEmitter> m_LuminanceEmitters = new List<LuminanceEmitter>();

        /// <summary>
        /// Called when the object is enabled. Sets up the singleton instance.
        /// </summary>
        private void OnEnable()
        {
            s_Instance = this;
        }

        /// <summary>
        /// Registers a new luminance emitter with the manager.
        /// </summary>
        /// <param name="emitter">The emitter to register.</param>
        public void Register(LuminanceEmitter emitter)
        {
            if (emitter == null) {
                return;
            }

            m_LuminanceEmitters.Add(emitter);
        }

        /// <summary>
        /// Calculates the total luminance at a target's position from all registered emitters.
        /// </summary>
        /// <param name="target">The GameObject to calculate luminance for.</param>
        /// <returns>The total luminance value at the target's position.</returns>
        public float GetLuminance(GameObject target)
        {
            var luminance = m_UseAmbientLight ? RenderSettings.ambientIntensity : 0;
            for (int i = 0; i < m_LuminanceEmitters.Count; ++i) {
                luminance += m_LuminanceEmitters[i].GetLuminance(target);
            }
            return luminance;
        }

        /// <summary>
        /// Unregisters a luminance emitter from the manager.
        /// </summary>
        /// <param name="emitter">The emitter to unregister.</param>
        public static void Unregister(LuminanceEmitter emitter)
        {
            if (s_Instance == null) {
                return;
            }
            Instance.UnregisterInternal(emitter);
        }

        /// <summary>
        /// Unregisters a luminance emitter from the manager.
        /// </summary>
        /// <param name="emitter">The emitter to unregister.</param>
        private void UnregisterInternal(LuminanceEmitter emitter)
        {
            m_LuminanceEmitters.Remove(emitter);
        }

        /// <summary>
        /// Called when the object is disabled. Cleans up the singleton instance.
        /// </summary>
        private void OnDisable()
        {
            s_Instance = null;
        }

        /// <summary>
        /// Reset the static variables for domain reloading.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void DomainReset()
        {
            s_Instance = null;
        }
    }
}