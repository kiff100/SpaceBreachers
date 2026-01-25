/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Sensors
{
    using UnityEngine;

    /// <summary>
    /// Interface for sensors that detect and return GameObjects. Used by sensors that need to identify specific objects in the environment.
    /// </summary>
    public interface IGameObjectSensor
    {
        /// <summary>
        /// Returns the GameObject that was detected by the sensor.
        /// </summary>
        /// <returns>The detected GameObject (can be null).</returns>
        public GameObject GetDetectedObject();
    }

    /// <summary>
    /// Interface for sensors that detect and return float values. Used by sensors that measure continuous values like distance, temperature, or light intensity.
    /// </summary>
    public interface IFloatSensor
    {
        /// <summary>
        /// Returns the float amount that was detected by the sensor.
        /// </summary>
        /// <returns>The detected float amount. Each sensor implementation will specify a default amount if no value was detected.</returns>
        public float GetDetectedAmount();
    }
}