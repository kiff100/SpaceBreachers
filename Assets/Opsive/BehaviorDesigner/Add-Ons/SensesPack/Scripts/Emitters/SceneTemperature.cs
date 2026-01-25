using UnityEngine;

/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters
{
    /// <summary>
    /// Represents a temperature value with optional fluctuation over time.
    /// </summary>
    [System.Serializable]
    public class TempeatureValue
    {
        [Tooltip("The base temperature value.")]
        public float Value;
        [Tooltip("An animation curve that defines how the temperature fluctuates over time.")]
        public AnimationCurve Fluctuation = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0.75f, 0, 0.333f), new Keyframe(0.5f, 1.25f), new Keyframe(1, 0.75f) });

        /// <summary>
        /// Evaluates the temperature at a given time, including any fluctuations.
        /// </summary>
        /// <param name="time">The time to evaluate the temperature at.</param>
        /// <returns>The calculated temperature value.</returns>
        public float Evaluate(float time)
        {
            return Value * Fluctuation.Evaluate(time);
        }
    }

    /// <summary>
    /// Manages the global scene temperature, which can vary based on both seasonal (day of year) and daily (time of day) cycles.
    /// </summary>
    public class SceneTemperature : MonoBehaviour
    {
        private static SceneTemperature s_Instance;
        public static SceneTemperature Instance { 
            get {
                if (s_Instance == null) {
                    s_Instance = new GameObject("SceneTemperature").AddComponent<SceneTemperature>();
                }
                return s_Instance;
            }
        }

        [Tooltip("The temperature for the season (0-365). Use the Fluctuation curve to define seasonal temperature variations.")]
        [SerializeField] protected TempeatureValue m_DayTemperature;
        [Tooltip("The temperature for the time of day (0-24). Use the Fluctuation curve to define daily temperature variations.")]
        [SerializeField] protected TempeatureValue m_TimeTemperature;

        [Tooltip("The current day of year (0-365).")]
        [SerializeField] protected float m_CurrentDay = 0;
        [Tooltip("The current time of day (0-24).")]
        [SerializeField] protected float m_CurrentTime = 12;
        [Tooltip("The speed at which the day progresses (days per second). Set to 0 to keep the day static.")]
        [SerializeField] protected float m_DayProgressionSpeed = 0;
        [Tooltip("The speed at which the time of day progresses (hours per second). Set to 0 to keep the time static.")]
        [SerializeField] protected float m_TimeProgressionSpeed = 0;

        public float CurrentDay { get => m_CurrentDay; set => m_CurrentDay = value; }
        public float CurrentTime { get => m_CurrentTime; set => m_CurrentTime = value; }

        /// <summary>
        /// The object has been enabled.
        /// </summary>
        private void OnEnable()
        {
            s_Instance = this;
        }

        /// <summary>
        /// Updates the current day and time based on the progression speeds.
        /// </summary>
        private void Update()
        {
            m_CurrentDay += m_DayProgressionSpeed * Time.deltaTime;
            m_CurrentTime += m_TimeProgressionSpeed * Time.deltaTime;

            m_CurrentDay = Mathf.Repeat(m_CurrentDay, 365);
            m_CurrentTime = Mathf.Repeat(m_CurrentTime, 24);
        }

        /// <summary>
        /// Evaluates the current scene temperature by combining the seasonal and daily temperature values.
        /// </summary>
        /// <returns>The combined temperature value based on the current day and time.</returns>
        public float Evaluate()
        {
            return m_DayTemperature.Evaluate(m_CurrentDay / 365) + m_TimeTemperature.Evaluate(m_CurrentTime / 24);
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