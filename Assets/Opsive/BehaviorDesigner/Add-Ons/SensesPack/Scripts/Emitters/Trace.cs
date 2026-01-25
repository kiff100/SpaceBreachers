/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters
{
    using Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Utility;
    using System;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Represents a trace or scent that can be detected by agents in the environment.
    /// The trace has an intensity that dissipates over time.
    /// </summary>
    public struct Trace : IPosition, IEquatable<Trace>
    {
        [Tooltip("The maximum intensity of the trace.")]
        public float Intensity;
        [Tooltip("The position in 3D space where the trace is located.")]
        public float3 Position { get; }

        private float StartTime;
        private float DissipationRate;

        /// <summary>
        /// Initializes a new instance of the Trace struct.
        /// </summary>
        /// <param name="position">The position where the trace is created.</param>
        public Trace(Vector3 position)
        {
            Intensity = 1;
            Position = position;
            StartTime = Time.time;
            DissipationRate = Intensity / 10; // Default to a dissipation time of 10 seconds.
        }

        /// <summary>
        /// Initializes a new instance of the Trace struct.
        /// </summary>
        /// <param name="position">The position where the trace is created.</param>
        /// <param name="intensity">The maximum intensity of the trace.</param>
        /// <param name="dissipationTime">The amount of time in seconds before the trace is fully dissipated.</param>
        public Trace(Vector3 position, float intensity, float dissipationTime)
        {
            Position = position;
            Intensity = intensity;
            StartTime = Time.time;
            if (dissipationTime > 0) {
                DissipationRate = Intensity / dissipationTime;
            } else {
                DissipationRate = 0;
            }
        }

        /// <summary>
        /// Calculates the current intensity of the trace based on time elapsed since creation.
        /// </summary>
        /// <param name="time">The current time.</param>
        /// <returns>The current intensity of the trace.</returns>
        public float GetIntensity(float time)
        {
            return Intensity - (time - StartTime) * DissipationRate;
        }

        /// <summary>
        /// Determines whether this trace is equal to another trace.
        /// </summary>
        /// <param name="other">The trace to compare with.</param>
        /// <returns>True if the traces are at the same position.</returns>
        public bool Equals(Trace other)
        {
            return Position.x == other.Position.x && Position.y == other.Position.y && Position.z == other.Position.z;
        }
    }
}