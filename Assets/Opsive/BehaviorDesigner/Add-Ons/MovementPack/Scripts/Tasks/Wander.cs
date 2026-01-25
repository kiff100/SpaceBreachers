/// ---------------------------------------------
/// Movement Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.MovementPack.Runtime.Tasks
{
    using Opsive.BehaviorDesigner.Runtime.Tasks;
    using Opsive.BehaviorDesigner.Runtime.Utility;
    using Opsive.GraphDesigner.Runtime;
    using Opsive.GraphDesigner.Runtime.Variables;
    using Unity.Entities;
    using UnityEngine;

    [Opsive.Shared.Utility.Description("Wanders around the pathfinding area. The task can wander within a specified bounds or in random directions.")]
    [NodeIcon("f900ccca7c66371459b52036efeb8778", "cfd0e78235c50db46bc12d1751492ecf")]
    public class Wander : MovementBase
    {
        /// <summary>
        /// Specifies the bounds that the agent can wander in.
        /// </summary>
        public enum WanderBounds
        {
            None,   // The agent will wander in an unrestricted area.
            Sphere, // The agent will wander within a sphere.
            Box     // The agent will wander within a box.
        }

        [Tooltip("Specifies the bounds that the agent can wander in.")]
        [SerializeField] protected SharedVariable<WanderBounds> m_Bounds;
        [Tooltip("Specifies the center of the sphere or box bounds.")]
        [SerializeField] protected SharedVariable<Vector3> m_BoundsCenter;
        [Tooltip("Specifies the radius of the bounding sphere.")]
        [SerializeField] protected SharedVariable<float> m_BoundsRadius = 10;
        [Tooltip("Specifies the size of the bounding box.")]
        [SerializeField] protected SharedVariable<Vector3> m_BoundsSize = new Vector3(10, 10, 10);

        [Tooltip("Specifies the distance ahead of the current position to look ahead for a destination.")]
        [SerializeField] protected SharedVariable<RangeFloat> m_WanderDistance = new RangeFloat(5, 10);
        [Tooltip("Specifies the number of degrees that the agent can turn when wandering.")]
        [SerializeField] protected SharedVariable<RangeFloat> m_WanderDegrees = new RangeFloat(-30, 30);
        [Tooltip("Amount to decrease the distance if the wander position can't be found.")]
        [SerializeField] protected SharedVariable<float> m_WanderDistanceDecrease = 1;
        [Tooltip("Amount to increase the degrees if the wander position can't be found.")]
        [SerializeField] protected SharedVariable<float> m_WanderDegreesIncrease = 5;

        [Tooltip("Specifies the amount of time the agent should wait at the destination before selecting a new wander destination.")]
        [SerializeField] protected SharedVariable<RangeFloat> m_WaitAtDestinationDuration;
        [Tooltip("Specifies the remaining distance when the agent should select a new destination.")]
        [SerializeField] protected SharedVariable<RangeFloat> m_DestinationSelectionDistance = new RangeFloat(1, 3);
        [Tooltip("The maximum number of retries per tick (set higher if using a slow tick time)")]
        [SerializeField] protected SharedVariable<int> m_DestinationRetries = 1;
#if UNITY_EDITOR
        [Tooltip("Should a ray be drawn at the invalid destination location?")]
        [SerializeField] protected bool m_DrawInvalidDestinationRay;
#endif

        public SharedVariable<WanderBounds> Bounds { get => m_Bounds; set => m_Bounds = value; }

        private float m_WaitTime = -1;
        private float m_DestinationReachedTime = -1;

        /// <summary>
        /// Starts the task.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();

            if (m_DestinationReachedTime == -1) {
                TrySetDestination();
            }
        }

        /// <summary>
        /// Returns the target destination.
        /// </summary>
        /// <returns>The target destination.</returns>
        public override TaskStatus OnUpdate()
        {
            // Select a new destination if the agent has arrived or the agent should selected a new destination before reaching the original destination.
            var destinationSelectionDistance = m_DestinationSelectionDistance.Value.RandomValue;
            if (HasArrived() || RemainingDistance < destinationSelectionDistance) {
                // The agent can wait at the destination for a period of time.
                if (m_WaitTime == -1) {
                    var waitDuration = m_WaitAtDestinationDuration.Value.RandomValue;
                    if (waitDuration > 0) {
                        m_DestinationReachedTime = Time.time;
                        m_WaitTime = waitDuration;
                    }
                } else if (m_DestinationReachedTime + m_WaitTime <= Time.time) {
                    // Reset the WaitTime so the destination is set.
                    m_DestinationReachedTime = -1;
                    m_WaitTime = -1;
                }

                // If WaitTime is still -1 then the agent is either not waiting or is done waiting.
                if (m_WaitTime == -1) {
                    TrySetDestination();
                    return TaskStatus.Running;
                }
            }

            return TaskStatus.Running;
        }

        /// <summary>
        /// Tries to set the destination based on the bounds type.
        /// </summary>
        /// <returns>True if the destination was set.</returns>
        private bool TrySetDestination()
        {
            var direction = transform.forward;
            var attempts = 0;
            Vector3 destination;
            while (attempts < m_DestinationRetries.Value) {
                direction = Quaternion.Euler(0, Mathf.Clamp(m_WanderDegrees.Value.RandomValue + m_WanderDegreesIncrease.Value * attempts, -360, 360), 0) * direction;
                destination = transform.position + direction.normalized * Mathf.Max(0, m_WanderDistance.Value.RandomValue - (m_WanderDistanceDecrease.Value * attempts));

                // The destination should stay within the bounds of the sphere or box.
                var validDestination = true;
                if (m_Bounds.Value == WanderBounds.Sphere) {
                    var destinationRadius = (destination - m_BoundsCenter.Value).magnitude;
                    if (destinationRadius > m_BoundsRadius.Value) {
                        validDestination = false;
#if UNITY_EDITOR
                        if (m_DrawInvalidDestinationRay) {
                            Debug.DrawRay(destination, Vector3.up, Color.red, 1);
                        }
#endif
                    }
                } else if (m_Bounds.Value == WanderBounds.Box) {
                    var destinationDirection = (destination - m_BoundsCenter.Value);
                    if (Mathf.Abs(destinationDirection.x) > m_BoundsSize.Value.x ||
                        Mathf.Abs(destinationDirection.y) > m_BoundsSize.Value.y ||
                        Mathf.Abs(destinationDirection.z) > m_BoundsSize.Value.z) {
                        validDestination = false;
#if UNITY_EDITOR
                        if (m_DrawInvalidDestinationRay) {
                            Debug.DrawRay(destination, Vector3.up, Color.red, 1);
                        }
#endif
                    }
                }

                if (validDestination && SamplePosition(ref destination)) {
                    SetDestination(destination);
                    return true;
                }
                attempts++;
            }
            return false;
        }

        /// <summary>
        /// The task has ended.
        /// </summary>
        public override void OnEnd()
        {
            base.OnEnd();

            m_WaitTime = -1;
            m_DestinationReachedTime = -1;
        }

        /// <summary>
        /// Returns the current task state.
        /// </summary>
        /// <param name="world">The DOTS world.</param>
        /// <param name="entity">The DOTS entity.</param>
        /// <returns>The current task state.</returns>
        public override object Save(World world, Entity entity)
        {
            var saveData = new object[3];
            saveData[0] = base.Save(world, entity);
            saveData[1] = m_WaitTime;
            saveData[2] = Time.time - m_DestinationReachedTime;
            return saveData;
        }

        /// <summary>
        /// Loads the previous task state.
        /// </summary>
        /// <param name="saveData">The previous task state.</param>
        /// <param name="world">The DOTS world.</param>
        /// <param name="entity">The DOTS entity.</param>
        public override void Load(object saveData, World world, Entity entity)
        {
            var saveDataArray = (object[])saveData;
            base.Load(saveDataArray[0], world, entity);
            m_WaitTime = (float)saveDataArray[1];
            m_DestinationReachedTime = Time.time - (float)saveDataArray[2];
        }

        /// <summary>
        /// Resets the task values back to their default.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            m_Bounds = WanderBounds.None;
            m_BoundsCenter = Vector3.zero;
            m_BoundsRadius = 10;
            m_BoundsSize = new Vector3(10, 10, 10);
            m_WanderDistance = new RangeFloat(5, 10);
            m_WanderDegrees = new RangeFloat(-30, 30);
            m_WanderDegreesIncrease = 5;
            m_WaitAtDestinationDuration = new RangeFloat();
            m_DestinationSelectionDistance= new RangeFloat(1, 3);
            m_DestinationRetries = 1;
#if UNITY_EDITOR
            m_DrawInvalidDestinationRay = false;
#endif
        }
    }
}