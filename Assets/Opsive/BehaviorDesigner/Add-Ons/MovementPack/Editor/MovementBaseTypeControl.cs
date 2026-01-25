/// ---------------------------------------------
/// Movement Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.MovementPack.Editor
{
    using Opsive.BehaviorDesigner.AddOns.MovementPack.Runtime.Tasks;
    using Opsive.BehaviorDesigner.AddOns.Shared.Editor;
    using Opsive.Shared.Editor.UIElements;
    using Opsive.Shared.Editor.UIElements.Controls;
    using Opsive.Shared.Utility;
    using System;
    using UnityEngine.UIElements;

    /// <summary>
    /// Implements TypeControlBase for the MovementBase type.
    /// </summary>
    [ControlType(typeof(MovementBase))]
    public class MovementBaseTypeControl : PathfinderTypeControlBase
    {
        /// <summary>
        /// The MovementBaseView displays the contents of the MovementBase.
        /// </summary>
        public class MovementBaseView : PathfinderView
        {
            protected MovementBase m_MovementBase;

            /// <summary>
            /// MovementBaseView constructor.
            /// </summary>
            /// <param name="unityObject">A reference to the owning Unity Object.</param>
            /// <param name="movementBase">The MovementBase task being drawn.</param>
            /// <param name="onChangeEvent">An event that is sent when the value changes. Returns false if the control cannot be changed.</param>
            public MovementBaseView(UnityEngine.Object unityObject, MovementBase movementBase, Func<object, bool> onChangeEvent) : base(unityObject, onChangeEvent, MovementBase.PathfinderTypeKey)
            {
                m_MovementBase = movementBase;

                InitializePathfinder(
                    () => m_MovementBase.Pathfinder,
                    (pathfinder) => m_MovementBase.Pathfinder = pathfinder
                );

                AddTitleLabel(m_MovementBase.GetType().Name, m_MovementBase.GetType().FullName);
                AddTaskFields();
            }

            /// <summary>
            /// Adds the remaining task fields.
            /// </summary>
            protected virtual void AddTaskFields()
            {
                FieldInspectorView.AddFields(m_UnityObject, m_MovementBase, MemberVisibility.Public, this, (object obj) => { m_OnChangeEvent?.Invoke(obj); });
            }
        }

        /// <summary>
        /// Returns the control that should be used for the specified ControlType.
        /// </summary>
        /// <param name="input">The input to the control.</param>
        /// <returns>The created control.</returns>
        protected override VisualElement GetControl(TypeControlInput input)
        {
            return new MovementBaseView(input.UnityObject, input.Value as MovementBase, input.OnChangeEvent);
        }
    }
}