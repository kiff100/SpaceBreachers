/// ---------------------------------------------
/// Movement Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Editor
{
    using Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Sensors;
    using Opsive.Shared.Editor.UIElements;
    using Opsive.Shared.Editor.UIElements.Controls;
    using Opsive.Shared.Editor.UIElements.Controls.Types;
    using Opsive.Shared.Utility;
    using System;
    using System.Collections.Generic;
    using UnityEngine.UIElements;

    /// <summary>
    /// Implements TypeControlBase for the Sensor type.
    /// </summary>
    [ControlType(typeof(Sensor))]
    [ControlType(typeof(IGameObjectSensor))]
    [ControlType(typeof(IFloatSensor))]
    public class SensorTypeControl : TypeControlBase
    {
        /// <summary>
        /// The SensorView displays the contents of the Sensor.
        /// </summary>
        public class SensorView : VisualElement
        {
            protected UnityEngine.Object m_UnityObject;
            protected Sensor m_Sensor;
            protected Func<object, bool> m_OnChangeEvent;

            private List<Type> m_SensorTypes = new List<Type>();
            private List<string> m_SensorTypeNames = new List<string>();

            private int m_SensorIndex = 0;
            private VisualElement m_SensorContainer;

            /// <summary>
            /// SensorView constructor.
            /// </summary>
            /// <param name="unityObject">A reference to the owning Unity Object.</param>
            /// <param name="fieldType">The type of the field.</param>
            /// <param name="sensor">The Sensor being drawn.</param>
            /// <param name="onChangeEvent">An event that is sent when the value changes. Returns false if the control cannot be changed.</param>
            public SensorView(UnityEngine.Object unityObject, Type fieldType, Sensor sensor, Func<object, bool> onChangeEvent)
            {
                m_UnityObject = unityObject;
                m_Sensor = sensor;
                m_OnChangeEvent = onChangeEvent;

                PopulateSensorTypes(fieldType);

                // Set the first sensor if the task doesn't already have one set.
                if (m_Sensor == null) {
                    m_Sensor = Activator.CreateInstance(m_SensorTypes[m_SensorIndex]) as Sensor;
                    onChangeEvent?.Invoke(m_Sensor);
                }

                // Add a popup with the different sensor choices.
                var sensorTypePopup = new PopupField<string>();
                sensorTypePopup.label = "Sensor";
                sensorTypePopup.tooltip = "Specifies the sensor implementation that should be used.";
                sensorTypePopup.choices = m_SensorTypeNames;
                sensorTypePopup.value = m_SensorTypeNames[m_SensorIndex];
                sensorTypePopup.RegisterValueChangedCallback(c =>
                {
                    m_SensorIndex = sensorTypePopup.index;
                    m_Sensor = Activator.CreateInstance(m_SensorTypes[m_SensorIndex]) as Sensor;
                    
                    m_SensorContainer.Clear();
                    FieldInspectorView.AddFields(m_UnityObject, m_Sensor, MemberVisibility.Public, m_SensorContainer, (object obj) => { onChangeEvent?.Invoke(obj); });

                    onChangeEvent?.Invoke(m_Sensor);
                });
                Add(sensorTypePopup);

                // Show any sensor fields.
                m_SensorContainer = new VisualElement();
                m_SensorContainer.style.marginBottom = 4;
                FieldInspectorView.AddFields(m_UnityObject, m_Sensor, MemberVisibility.Public, m_SensorContainer, (object obj) => { onChangeEvent?.Invoke(obj); });
                Add(m_SensorContainer);
            }

            /// <summary>
            /// Gets all of the sensor implementations available.
            /// <param name="fieldType"/>The type of sensors that should be displayed</param>
            /// </summary>
            private void PopulateSensorTypes(Type fieldType)
            {
                m_SensorTypes.Clear();
                m_SensorTypeNames.Clear();
                var types = UnitOptions.GetAllTypes();
                for (int i = 0; i < types.Length; ++i) {
                    if (fieldType.IsAssignableFrom(types[i]) && !types[i].IsAbstract && !types[i].IsInterface) {
                        m_SensorTypes.Add(types[i]);
                        m_SensorTypeNames.Add(types[i].Name);

                        if (m_Sensor != null && m_Sensor.GetType() == types[i]) {
                            m_SensorIndex = m_SensorTypeNames.Count - 1;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Does the control use a label?
        /// </summary>
        public override bool UseLabel { get { return false; } }

        /// <summary>
        /// Returns the control that should be used for the specified ControlType.
        /// </summary>
        /// <param name="input">The input to the control.</param>
        /// <returns>The created control.</returns>
        protected override VisualElement GetControl(TypeControlInput input)
        {
            return new SensorView(input.UnityObject, input.Field.FieldType, input.Value as Sensor, input.OnChangeEvent);
        }
    }
}