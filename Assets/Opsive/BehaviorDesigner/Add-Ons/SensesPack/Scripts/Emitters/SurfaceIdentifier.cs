/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters
{
    using UnityEngine;

    /// <summary>
    /// A component that identifies the type of surface on a GameObject.
    /// Used by the SurfaceManager to determine surface properties for behavior tree decisions.
    /// </summary>
    public class SurfaceIdentifier : MonoBehaviour
    {
        /// <summary>
        /// The type of surface that this GameObject represents.
        /// </summary>
        [SerializeField] protected SurfaceType m_SurfaceType;

        /// <summary>
        /// Gets the surface type of this GameObject.
        /// </summary>
        public SurfaceType SurfaceType { get { return m_SurfaceType; } }
    }
}