/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Sensors
{
    using Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters;

    /// <summary>
    /// A sensor that detects the luminance (brightness) at the agent's position. Can be used for light-based detection.
    /// </summary>
    public class Luminance : Sensor, IFloatSensor
    {
        /// <summary>
        /// Returns the luminance value at the agent's current position.
        /// </summary>
        /// <returns>The luminance value at the agent's position.</returns>
        public float GetDetectedAmount()
        {
            return LuminanceManager.Instance.GetLuminance(m_GameObject);
        }
    }
}