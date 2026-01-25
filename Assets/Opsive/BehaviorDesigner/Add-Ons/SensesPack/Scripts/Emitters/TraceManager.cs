/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Emitters
{
    using Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Utility;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Mathematics;
    using Unity.Jobs;
    using UnityEngine;

    /// <summary>
    /// Manages the creation, tracking, and querying of traces (like scent trails or blood splatter) in the game world.
    /// Uses an octree data structure for efficient spatial queries of traces.
    /// </summary>
    [BurstCompile]
    public class TraceManager : MonoBehaviour
    {
        private static TraceManager s_Instance;
        public static TraceManager Instance { 
            get {
                if (s_Instance == null) {
                    s_Instance = new GameObject("TraceManager").AddComponent<TraceManager>();
                }
                return s_Instance;
            }
        }

        [Tooltip("The bounds of the world space where traces can exist.")]
        [SerializeField] protected Bounds m_WorldBounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000));

        private OctreeNode<Trace> m_Octree;
        private NativeList<Trace> m_AllTraces;
        private NativeList<int> m_TraceIndiciesToRemove;
        private JobHandle m_UpdateJobHandle;

        /// <summary>
        /// Called when the object is enabled. Sets up the singleton instance and initializes native collections.
        /// </summary>
        private void OnEnable()
        {
            s_Instance = this;
            m_Octree = new OctreeNode<Trace>(m_WorldBounds);
            m_AllTraces = new NativeList<Trace>(Allocator.Persistent);
            m_TraceIndiciesToRemove = new NativeList<int>(Allocator.Persistent);
        }

        /// <summary>
        /// Adds a new trace to the system.
        /// </summary>
        /// <param name="trace">The trace to add.</param>
        public void Add(Trace trace)
        {
            m_UpdateJobHandle.Complete();

            m_Octree = m_Octree.Insert(trace);
            m_AllTraces.Add(trace);
        }

        /// <summary>
        /// Updates the trace system by removing traces that have faded out.
        /// </summary>
        private void Update()
        {
            m_UpdateJobHandle.Complete();
            // Remove traces that have faded.
            for (int i = m_TraceIndiciesToRemove.Length - 1; i >= 0; --i) {
                m_Octree = m_Octree.Remove(m_AllTraces[m_TraceIndiciesToRemove[i]]);
                m_AllTraces.RemoveAtSwapBack(m_TraceIndiciesToRemove[i]);
            }
            m_TraceIndiciesToRemove.Clear();

            var updateJob = new UpdateTracesJob
            {
                Time = Time.time,
                Traces = m_AllTraces,
                TraceIndiciesToRemove = m_TraceIndiciesToRemove
            };
            m_UpdateJobHandle = updateJob.Schedule();
        }

        /// <summary>
        /// Gets the combined intensity of all traces at a given position.
        /// </summary>
        /// <param name="position">The position to check for traces.</param>
        /// <param name="position">The maximum range at which traces can be detected.</param>
        /// <returns>The total intensity of all traces at the given position, weighted by distance.</returns>
        [BurstCompile]
        public float GetIntensityAt(Vector3 position, float range)
        {
            var nearbyTraces = new NativeList<Trace>(Allocator.Temp);
            var traceCount = m_Octree.Query(new Bounds(position, Vector3.one * range), nearbyTraces);
            var totalIntensity = 0f;
            for (int i = 0; i < traceCount; ++i) {
                var distance = math.length((float3)position - nearbyTraces[i].Position);
                totalIntensity += nearbyTraces[i].GetIntensity(Time.time) * (1f - distance / range);
            }
            nearbyTraces.Dispose();
            return totalIntensity;
        }

        /// <summary>
        /// Gets the combined intensity of all traces at a given position and the trace with the largest intensity.
        /// </summary>
        /// <param name="position">The position to check for traces.</param>
        /// <param name="position">The maximum range at which traces can be detected.</param>
        /// <returns>A tuple with the total intensity of all traces at the given position (weighted by distance) and the trace with the largest intensity.</returns>
        [BurstCompile]
        public (float, Vector3) GetIntensityPositionAt(Vector3 position, float range)
        {
            var nearbyTraces = new NativeList<Trace>(Allocator.Temp);
            var traceCount = m_Octree.Query(new Bounds(position, Vector3.one * range), nearbyTraces);
            var totalIntensity = 0f;
            var largestIntensity = 0f;
            var closestPosition = position;
            for (int i = 0; i < traceCount; ++i) {
                var distance = math.length((float3)position - nearbyTraces[i].Position);
                var intensity = nearbyTraces[i].GetIntensity(Time.time) * (1f - distance / range);
                totalIntensity += intensity;
                if (intensity > largestIntensity) {
                    largestIntensity = intensity;
                    closestPosition = nearbyTraces[i].Position;
                }
            }
            nearbyTraces.Dispose();
            return (totalIntensity, closestPosition);
        }

        /// <summary>
        /// Job that identifies traces that have faded out and need to be removed.
        /// </summary>
        [BurstCompile]
        private struct UpdateTracesJob : IJob
        {
            [Tooltip("The current time.")]
            public float Time;
            [Tooltip("The list of all traces in the system.")]
            public NativeList<Trace> Traces;
            [Tooltip("The list that will store the indices of traces that need to be removed.")]
            public NativeList<int> TraceIndiciesToRemove;

            /// <summary>
            /// Executes the job by checking each trace's intensity and marking it for removal if it has faded out.
            /// </summary>
            [BurstCompile]
            public void Execute()
            {
                for (int i = 0; i < Traces.Length; ++i) {
                    if (Traces[i].GetIntensity(Time) <= 0) {
                        TraceIndiciesToRemove.Add(i);
                    }
                }
            }
        }

        /// <summary>
        /// Called when the object is disabled. Cleans up native collections.
        /// </summary>
        private void OnDisable()
        {
            m_UpdateJobHandle.Complete();
            m_Octree.Dispose();
            m_AllTraces.Dispose();
            m_TraceIndiciesToRemove.Dispose();

            s_Instance = null;
        }

        /// <summary>
        /// Reset the static variables for domain reloading.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void DomainReset()
        {
            s_Instance = null;
        }

        /// <summary>
        /// Draws the octree when the component is selected in the editor.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            var originalColor = Gizmos.color;
            Gizmos.color = Editor.BehaviorDesignerSettings.Instance.DefaultGizmosColor;
            DrawOctreeNode(m_Octree);
            Gizmos.color = originalColor;
#endif
        }

        /// <summary>
        /// The radius of the octree node sphere visualization.
        /// </summary>
        private const float c_NodeRadius = 0.1f;

        /// <summary>
        /// Draws the octree node to the scene view.
        /// </summary>
        /// <param name="node">The node that should be drawn.</param>
        private void DrawOctreeNode(OctreeNode<Trace> node)
        {
            Gizmos.DrawWireCube(node.NodeBounds.center, node.NodeBounds.size);
            if (node.NodeObjects.IsCreated) {
                for (int i = 0; i < node.NodeObjects.Length; i++) {
                    Gizmos.DrawSphere(node.NodeObjects[i].Position, node.NodeObjects[i].Intensity * c_NodeRadius);
                }
            }

            if (node.NodeChildren.IsCreated) {
                for (int i = 0; i < node.NodeChildren.Length; i++) {
                    DrawOctreeNode(node.NodeChildren[i]);
                }
            }
        }
    }
}