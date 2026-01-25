/// ---------------------------------------------
/// Movement Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.MovementPack.Editor
{
    using Opsive.BehaviorDesigner.AddOns.MovementPack.Runtime.Tasks;
    using Opsive.Shared.Editor.UIElements;
    using Opsive.Shared.Editor.UIElements.Controls;
    using System;
    using UnityEngine.UIElements;

    /// <summary>
    /// Implements TypeControlBase for the Wander type.
    /// </summary>
    [ControlType(typeof(Wander))]
    public class WanderTypeControl : MovementBaseTypeControl
    {
        /// <summary>
        /// The WanderView displays the contents of the Wander task.
        /// </summary>
        private class WanderView : MovementBaseView
        {
            /// <summary>
            /// WanderView constructor.
            /// </summary>
            /// <param name="unityObject">A reference to the owning Unity Object.</param>
            /// <param name="movementBase">The MovementBase task being drawn.</param>
            /// <param name="onChangeEvent">An event that is sent when the value changes. Returns false if the control cannot be changed.</param>
            public WanderView(UnityEngine.Object unityObject, MovementBase movementBase, Func<object, bool> onChangeEvent) : base(unityObject, movementBase, onChangeEvent) { }

            /// <summary>
            /// Adds the remaining task fields.
            /// </summary>
            protected override void AddTaskFields()
            {
                FieldInspectorView.AddField(m_UnityObject, m_MovementBase, "m_StopOnTaskEnd", this, (object obj) =>
                {
                    m_OnChangeEvent(obj);
                });
                var boundsContainer = new VisualElement();
                FieldInspectorView.AddField(m_UnityObject, m_MovementBase, "m_Bounds", this, (object obj) =>
                {
                    m_OnChangeEvent(obj);
                    UpdateBounds(boundsContainer);
                });
                
                Add(boundsContainer);
                UpdateBounds(boundsContainer);

                FieldInspectorView.AddField(m_UnityObject, m_MovementBase, "m_WanderDistance", this, (object obj) =>
                {
                    m_OnChangeEvent(obj);
                });
                FieldInspectorView.AddField(m_UnityObject, m_MovementBase, "m_WanderDegrees", this, (object obj) =>
                {
                    m_OnChangeEvent(obj);
                });
                FieldInspectorView.AddField(m_UnityObject, m_MovementBase, "m_WanderDegreesIncrease", this, (object obj) =>
                {
                    m_OnChangeEvent(obj);
                });
                FieldInspectorView.AddField(m_UnityObject, m_MovementBase, "m_WaitAtDestinationDuration", this, (object obj) =>
                {
                    m_OnChangeEvent(obj);
                });
                FieldInspectorView.AddField(m_UnityObject, m_MovementBase, "m_DestinationSelectionDistance", this, (object obj) =>
                {
                    m_OnChangeEvent(obj);
                });
                FieldInspectorView.AddField(m_UnityObject, m_MovementBase, "m_DestinationRetries", this, (object obj) =>
                {
                    m_OnChangeEvent(obj);
                });
                FieldInspectorView.AddField(m_UnityObject, m_MovementBase, "m_DrawInvalidDestinationRay", this, (object obj) =>
                {
                    m_OnChangeEvent(obj);
                });
            }

            /// <summary>
            /// Updates the bounds fields.
            /// </summary>
            /// <param name="container">The parent container.</param>
            private void UpdateBounds(VisualElement container)
            {
                container.Clear();

                var wanderTask = m_MovementBase as Wander;
                if (wanderTask.Bounds == null || wanderTask.Bounds.Value == Wander.WanderBounds.None) {
                    return;
                }
                
                FieldInspectorView.AddField(m_UnityObject, m_MovementBase, "m_BoundsCenter", container, (object obj) =>
                {
                    m_OnChangeEvent(obj);
                });
                if (wanderTask.Bounds.Value == Wander.WanderBounds.Sphere) {
                    FieldInspectorView.AddField(m_UnityObject, m_MovementBase, "m_BoundsRadius", container, (object obj) =>
                    {
                        m_OnChangeEvent(obj);
                    });
                }
                if (wanderTask.Bounds.Value == Wander.WanderBounds.Box) {
                    FieldInspectorView.AddField(m_UnityObject, m_MovementBase, "m_BoundsSize", container, (object obj) =>
                    {
                        m_OnChangeEvent(obj);
                    });
                }
            }
        }

        /// <summary>
        /// Returns the control that should be used for the specified ControlType.
        /// </summary>
        /// <param name="input">The input to the control.</param>
        /// <returns>The created control.</returns>
        protected override VisualElement GetControl(TypeControlInput input)
        {
            return new WanderView(input.UnityObject, input.Value as MovementBase, input.OnChangeEvent);
        }
    }
}