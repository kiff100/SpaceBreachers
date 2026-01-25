/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters
{
    using UnityEngine;

    /// <summary>
    /// A component that defines a temperature volume within a trigger.
    /// </summary>
    public class TemperatureVolume : MonoBehaviour
    {
        [Tooltip("The temperature value for this volume, including any fluctuations.")]
        [SerializeField] protected float m_Value;
        [Tooltip("Is the temperature an absolute temperature? If false then the temperature is relative to the scene temperature.")]
        [SerializeField] protected bool m_Absolute;

        public float Value { get => m_Value; set => m_Value = value; }
        public bool Absolute { get => m_Absolute; set => m_Absolute = value; }
    }
}