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
    /// Implements AttributeControlBase for the CastDetectionModeListAttribute type.
    /// </summary>
    [ControlType(typeof(CastDetectionModeListAttribute))]
    public class CastDetectionModesAttributeControl : AttributeControlBase
    {
        /// <summary>
        /// The CastDetectionModeView displays the contents of the CastDetectionMode.
        /// </summary>
        public class CastDetectionModeView : VisualElement
        {
            protected UnityEngine.Object m_UnityObject;
            protected CastDetectionMode[] m_CastDetectionModes;
            protected Func<object, bool> m_OnChangeEvent;

            private ReorderableList m_ReorderableList;

            private int m_SelectedIndex = 0;
            private VisualElement m_CastDetectionModeContainer;

            /// <summary>
            /// CastDetectionModeView constructor.
            /// </summary>
            /// <param name="unityObject">A reference to the owning Unity Object.</param>
            /// <param name="castDetectionModes">The CastDetectionModes being drawn.</param>
            /// <param name="onChangeEvent">An event that is sent when the value changes. Returns false if the control cannot be changed.</param>
            public CastDetectionModeView(UnityEngine.Object unityObject, CastDetectionMode[] castDetectionModes, Func<object, bool> onChangeEvent)
            {
                m_UnityObject = unityObject;
                m_CastDetectionModes = castDetectionModes;
                m_OnChangeEvent = onChangeEvent;

                var graphWindow = BehaviorDesignerWindow.Instance;
                m_ReorderableList = new ReorderableList(m_CastDetectionModes, (VisualElement parent, int index) =>
                {
                    parent.Add(new Label());
                }, (VisualElement parent, int index) =>
                {
                    (parent.ElementAt(0) as Label).text = m_CastDetectionModes[index].GetType().Name;
                }, (VisualElement parent) =>
                {
                    parent.Add(new Label("Cast Detection Modes"));
                }, (int index) =>
                {
                    ShowSelectedCastDetectionMode(index);
                }, () => // Add.
                {
                    FilterWindow.ShowFilterWindow(graphWindow, new SearchWindowContext(graphWindow.position.position + m_ReorderableList.AddButton.worldBound.position),
                                                  new Type[] { typeof(CastDetectionMode) }, FilterWindow.FilterType.Class, "Detection Mode",
                                                  false, null, OnAdd, (Type type) => { return typeof(CastDetectionMode).IsAssignableFrom(type); });
                }, (int index) => // Remove.
                {
                    ArrayUtility.RemoveAt(ref m_CastDetectionModes, index);
                    m_ReorderableList.EnableRemove = m_CastDetectionModes.Length > 1 && !Application.isPlaying;
                    m_ReorderableList.Refresh(m_CastDetectionModes);
                    m_OnChangeEvent(m_CastDetectionModes);
                    m_SelectedIndex = Mathf.Clamp(m_SelectedIndex - 1, 0, m_CastDetectionModes.Length - 1);
                    ShowSelectedCastDetectionMode(m_SelectedIndex);
                }, (int fromIndex, int toIndex) => // Reorder.
                {
                    m_OnChangeEvent(m_CastDetectionModes);
                    if (m_SelectedIndex == fromIndex) {
                        ShowSelectedCastDetectionMode(toIndex);
                    }
                });
                m_ReorderableList.EnableAdd = m_ReorderableList.EnableReorder = !Application.isPlaying;
                m_ReorderableList.EnableRemove = (m_CastDetectionModes != null && m_CastDetectionModes.Length > 1) && !Application.isPlaying;
                Add(m_ReorderableList);

                // Show any detection mode fields.
                m_CastDetectionModeContainer = new VisualElement();
                m_CastDetectionModeContainer.style.marginBottom = 4;
                m_CastDetectionModeContainer.style.display = DisplayStyle.Flex;
                Add(m_CastDetectionModeContainer);
                ShowSelectedCastDetectionMode(m_SelectedIndex);

                if (m_CastDetectionModes != null && m_SelectedIndex < m_CastDetectionModes.Length) {
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

                if (m_CastDetectionModes == null) {
                    m_CastDetectionModes = new CastDetectionMode[1];
                } else {
                    Array.Resize(ref m_CastDetectionModes, m_CastDetectionModes.Length + 1);
                }

                m_CastDetectionModes[m_CastDetectionModes.Length - 1] = Activator.CreateInstance((Type)selectedObject) as CastDetectionMode;
                m_ReorderableList.Refresh(m_CastDetectionModes);
                m_ReorderableList.SelectedIndex = m_CastDetectionModes.Length - 1;
                m_ReorderableList.EnableRemove = m_CastDetectionModes.Length > 1 && !Application.isPlaying;
                m_OnChangeEvent(m_CastDetectionModes);
            }

            /// <summary>
            /// Shows the selected detection mode.
            /// </summary>
            /// <param name="index">The index of the detection mode within the detection mode array.</param>
            private void ShowSelectedCastDetectionMode(int index)
            {
                m_SelectedIndex = index;
                m_CastDetectionModeContainer.Clear();

                if (m_CastDetectionModes == null || m_CastDetectionModes.Length <= m_SelectedIndex) {
                    return;
                }

                var label = new Label();
                label.enableRichText = true;
                label.style.marginLeft = 3;
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                label.text = $"<font-weight=700><size=+2>{m_CastDetectionModes[index].GetType().Name}</size></font-weight>";
                label.tooltip = m_CastDetectionModes[index].GetType().FullName;
                m_CastDetectionModeContainer.Add(label);

                FieldInspectorView.AddFields(m_UnityObject, m_CastDetectionModes[index], MemberVisibility.Public, m_CastDetectionModeContainer, (object obj) =>
                {
                    m_CastDetectionModes[index] = obj as CastDetectionMode;
                    m_OnChangeEvent?.Invoke(m_CastDetectionModes);
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
            return new CastDetectionModeView(input.UnityObject, input.Value as CastDetectionMode[], input.OnChangeEvent);
        }
    }
}