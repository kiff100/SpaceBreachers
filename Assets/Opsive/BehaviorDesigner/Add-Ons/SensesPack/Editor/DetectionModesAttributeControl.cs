/// ---------------------------------------------
/// Movement Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Editor
{
    using Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Sensors;
    using Opsive.BehaviorDesigner.Editor;
    using Opsive.Shared.Editor.UIElements;
    using Opsive.Shared.Editor.UIElements.Controls;
    using Opsive.Shared.Editor.UIElements.Controls.Attributes;
    using Opsive.Shared.Utility;
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// Implements AttributeControlBase for the DetectionModeListAttribute type.
    /// </summary>
    [ControlType(typeof(DetectionModeListAttribute))]
    public class DetectionModesAttributeControl : AttributeControlBase
    {
        /// <summary>
        /// The DetectionModeView displays the contents of the DetectionMode.
        /// </summary>
        public class DetectionModeView : VisualElement
        {
            protected UnityEngine.Object m_UnityObject;
            protected DetectionMode[] m_DetectionModes;
            protected Func<object, bool> m_OnChangeEvent;

            private List<Type> m_DetectionModeTypes = new List<Type>();
            private List<string> m_DetectionModeTypeNames = new List<string>();

            private ReorderableList m_ReorderableList;

            private int m_SelectedIndex = 0;
            private VisualElement m_DetectionModeContainer;

            /// <summary>
            /// DetectionModeView constructor.
            /// </summary>
            /// <param name="unityObject">A reference to the owning Unity Object.</param>
            /// <param name="detectionModes">The DetectionModes being drawn.</param>
            /// <param name="onChangeEvent">An event that is sent when the value changes. Returns false if the control cannot be changed.</param>
            public DetectionModeView(UnityEngine.Object unityObject, DetectionMode[] detectionModes, Func<object, bool> onChangeEvent)
            {
                m_UnityObject = unityObject;
                m_DetectionModes = detectionModes;
                m_OnChangeEvent = onChangeEvent;

                var graphWindow = BehaviorDesignerWindow.Instance;
                m_ReorderableList = new ReorderableList(m_DetectionModes, (VisualElement parent, int index) =>
                {
                    parent.Add(new Label());
                }, (VisualElement parent, int index) =>
                {
                    (parent.ElementAt(0) as Label).text = m_DetectionModes[index].GetType().Name;
                }, (VisualElement parent) =>
                {
                    parent.Add(new Label("Detection Modes"));
                }, (int index) =>
                {
                    ShowSelectedDetectionMode(index);
                }, () => // Add.
                {
                    FilterWindow.ShowFilterWindow(graphWindow, new SearchWindowContext(graphWindow.position.position + m_ReorderableList.AddButton.worldBound.position),
                                                  new Type[] { typeof(DetectionMode) }, FilterWindow.FilterType.Class, "Detection Mode",
                                                  false, null, OnAdd, (Type type) => { return typeof(DetectionMode).IsAssignableFrom(type); });
                }, (int index) => // Remove.
                {
                    ArrayUtility.RemoveAt(ref m_DetectionModes, index);
                    m_ReorderableList.EnableRemove = m_DetectionModes.Length > 1 && !Application.isPlaying;
                    m_ReorderableList.Refresh(m_DetectionModes);
                    m_OnChangeEvent(m_DetectionModes);
                    m_SelectedIndex = Mathf.Clamp(m_SelectedIndex - 1, 0, m_DetectionModes.Length - 1);
                    ShowSelectedDetectionMode(m_SelectedIndex);
                }, (int fromIndex, int toIndex) => // Reorder.
                {
                    m_OnChangeEvent(m_DetectionModes);
                    if (m_SelectedIndex == fromIndex) {
                        ShowSelectedDetectionMode(toIndex);
                    }
                });
                m_ReorderableList.EnableAdd = m_ReorderableList.EnableReorder = !Application.isPlaying;
                m_ReorderableList.EnableRemove = (m_DetectionModes != null && m_DetectionModes.Length > 1) && !Application.isPlaying;
                Add(m_ReorderableList);

                // Show any detection mode fields.
                m_DetectionModeContainer = new VisualElement();
                m_DetectionModeContainer.style.marginBottom = 4;
                m_DetectionModeContainer.style.display = DisplayStyle.Flex;
                Add(m_DetectionModeContainer);
                ShowSelectedDetectionMode(m_SelectedIndex);

                if (m_DetectionModes != null && m_SelectedIndex < m_DetectionModes.Length) {
                    m_ReorderableList.SelectedIndex = m_SelectedIndex;
                }
            }

            /// <summary>
            /// Adds a new detection mode to the array.
            /// </summary>
            /// <param name="selectedObject">The selected task type.</param>
            /// <param name="baseType">The base type that was used to find the element.</param>
            /// <param name="windowPosition">The position of the FilterWindow.</param>
            private void OnAdd(object selectedObject, Type baseType, Vector2 windowPosition)
            {
                if (selectedObject == null) {
                    return;
                }

                if (m_DetectionModes == null) {
                    m_DetectionModes = new DetectionMode[1];
                } else {
                    Array.Resize(ref m_DetectionModes, m_DetectionModes.Length + 1);
                }

                m_DetectionModes[m_DetectionModes.Length - 1] = Activator.CreateInstance((Type)selectedObject) as DetectionMode;
                m_ReorderableList.Refresh(m_DetectionModes);
                m_ReorderableList.SelectedIndex = m_DetectionModes.Length - 1;
                m_ReorderableList.EnableRemove = m_DetectionModes.Length > 1 && !Application.isPlaying;
                m_OnChangeEvent(m_DetectionModes);
            }

            /// <summary>
            /// Shows the selected detection mode.
            /// </summary>
            /// <param name="index">The index of the detection mode within the detection mode array.</param>
            private void ShowSelectedDetectionMode(int index)
            {
                m_SelectedIndex = index;
                m_DetectionModeContainer.Clear();

                if (m_DetectionModes == null || m_DetectionModes.Length <= m_SelectedIndex) {
                    return;
                }

                var label = new Label();
                label.enableRichText = true;
                label.style.marginLeft = 3;
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                label.text = $"<font-weight=700><size=+2>{m_DetectionModes[index].GetType().Name}</size></font-weight>";
                label.tooltip = m_DetectionModes[index].GetType().FullName;
                m_DetectionModeContainer.Add(label);

                FieldInspectorView.AddFields(m_UnityObject, m_DetectionModes[index], MemberVisibility.Public, m_DetectionModeContainer, (object obj) =>
                {
                    m_DetectionModes[index] = obj as DetectionMode;
                    m_OnChangeEvent?.Invoke(m_DetectionModes);
                });
            }
        }

        /// <summary>
        /// Does the control use a label?
        /// </summary>
        public override bool UseLabel { get { return false; } }

        /// <summary>
        /// Does the attribute override the type control?
        /// </summary>
        public override bool OverrideTypeControl => true;

        /// <summary>
        /// Returns the control that should be used for the specified AttributeControlType.
        /// </summary>
        /// <param name="input">The input to the control.</param>
        /// <returns>The created control.</returns>
        protected override VisualElement GetControl(AttributeControlInput input)
        {
            return new DetectionModeView(input.UnityObject, input.Value as DetectionMode[], input.OnChangeEvent);
        }
    }
}