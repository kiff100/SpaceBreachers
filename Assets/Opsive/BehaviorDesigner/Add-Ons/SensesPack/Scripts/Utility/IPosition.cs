/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Utility
{
    using Unity.Mathematics;

    /// <summary>
    /// Interface for objects that have a position in 3D space.
    /// </summary>
    public interface IPosition
    {
        /// <summary>
        /// Gets the position of the object in 3D space.
        /// </summary>
        float3 Position { get; }
    }
}