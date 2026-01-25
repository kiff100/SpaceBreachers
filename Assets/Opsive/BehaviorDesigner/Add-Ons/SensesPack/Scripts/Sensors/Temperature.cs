/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Sensors
{
    using Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters;
    using Opsive.BehaviorDesigner.Runtime;
    using UnityEngine;

    /// <summary>
    /// A sensor that detects the temperature at the agent's position. Can be used for temperature-based detection.
    /// </summary>
    public class Temperature : Sensor, IFloatSensor
    {
        [Tooltip("The maximum number of temperature volumes that can affect the agent at once.")]
        [SerializeField] protected int m_MaxTriggers = 3;

        private TemperatureVolume[] m_Volumes;
        private int m_Count;

        /// <summary>
        /// Initializes the temperature sensor with the specified GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject that the sensor is attached to.</param>
        public override void Initialize(GameObject gameObject)
        {
            base.Initialize(gameObject);

            m_Volumes = new TemperatureVolume[m_MaxTriggers];

            var behaviorTree = gameObject.GetComponent<BehaviorTree>();
            behaviorTree.OnBehaviorTreeTriggerEnter += OnTriggerEnter;
            behaviorTree.OnBehaviorTreeTriggerExit += OnTriggerExit;
            behaviorTree.OnBehaviorTreeTriggerEnter2D += OnTriggerEnter2D;
            behaviorTree.OnBehaviorTreeTriggerExit2D += OnTriggerExit2D;
        }

        /// <summary>
        /// Callback when a trigger collider enters the agent's collider.
        /// </summary>
        /// <param name="other">The collider that entered the trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            AddRemoveVolume(other.gameObject, true);
        }

        /// <summary>
        /// Callback when a trigger collider exits the agent's collider.
        /// </summary>
        /// <param name="other">The collider that exited the trigger.</param>
        private void OnTriggerExit(Collider other)
        {
            AddRemoveVolume(other.gameObject, false);
        }

        /// <summary>
        /// Callback when a 2D trigger collider enters the agent's collider.
        /// </summary>
        /// <param name="other">The 2D collider that entered the trigger.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            AddRemoveVolume(other.gameObject, true);
        }

        /// <summary>
        /// Callback when a 2D trigger collider exits the agent's collider.
        /// </summary>
        /// <param name="other">The 2D collider that exited the trigger.</param>
        private void OnTriggerExit2D(Collider2D other)
        {
            AddRemoveVolume(other.gameObject, false);
        }

        /// <summary>
        /// Adds or removes a temperature volume from the list of affecting volumes.
        /// </summary>
        /// <param name="obj">The GameObject containing the temperature volume.</param>
        /// <param name="add">True if the volume should be added, false if it should be removed.</param>
        private void AddRemoveVolume(GameObject obj, bool add)
        {
            var temperatureVolume = obj.GetComponent<TemperatureVolume>();
            if (temperatureVolume == null) {
                return;
            }

            if (add) {
                if (m_Count >= m_MaxTriggers) {
                    return;
                }
                for (int i = 0; i < m_Count; ++i) {
                    // Don't add the volume multiple times.
                    if (m_Volumes[i] == temperatureVolume) {
                        return;
                    }
                }

                m_Volumes[m_Count] = temperatureVolume;
                m_Count++;
            } else {
                var found = false;
                for (int i = 0; i < m_Count; ++i) {
                    if (m_Volumes[i] == temperatureVolume) {
                        found = true;
                    }
                    // Shift the elements to ensure there is always a valid element for the count.
                    if (found && i + 1 < m_Volumes.Length) {
                        m_Volumes[i] = m_Volumes[i + 1];
                    }
                }
                m_Volumes[m_Count - 1] = null;
                m_Count--;
            }
        }

        /// <summary>
        /// Returns the combined temperature value from all affecting temperature volumes and the scene temperature.
        /// </summary>
        /// <returns>The combined temperature value at the agent's position.</returns>
        public float GetDetectedAmount()
        {
            var temperature = 0f;
            if (m_Count > 0) {
                for (int i = 0; i < m_Count; ++i) {
                    if (m_Volumes[i].Absolute) {
                        return m_Volumes[i].Value;
                    }
                    temperature += m_Volumes[i].Value;
                }
            }
            if (SceneTemperature.Instance != null) {
                temperature += SceneTemperature.Instance.Evaluate();
            }
            return temperature;
        }
    }
}