/// ---------------------------------------------
/// Shared Add-On for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.Shared.Demo
{
    using UnityEngine;

    /// <summary>
    /// Provides an interface for common pathfinding functions.
    /// </summary>
    public interface IPathfindingAgent
    {
        /// <summary>
        /// Warps the pathfinding implementation.
        /// </summary>
        /// <param name="position">The warp position.</param>
        void Warp(Vector3 position);

        /// <summary>
        /// Sets the target destination.
        /// </summary>
        /// <param name="position">The position that should be set.</param>
        void SetDestination(Vector3 position);
    }
}