/// ---------------------------------------------
/// Movement Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.MovementPack.Runtime.Tasks
{
    using Opsive.BehaviorDesigner.Runtime.Tasks;
    using Opsive.GraphDesigner.Runtime;
    using Opsive.GraphDesigner.Runtime.Variables;
    using UnityEngine;

    [Opsive.Shared.Utility.Description("Moves towards a 3D cover point. The agent can look at the cover point after the agent has arrived.")]
    [NodeIcon("54759f53c1dc79a489ef7702de195ade", "1565ea2bb117e0643b8943c3dac0de81")]
    public class Cover : MovementBase
    {
        [Tooltip("Specifies the maximum distance away from the agent of a valid cover location.")]
        [SerializeField] protected SharedVariable<float> m_CoverSearchDistance = 20;
        [Tooltip("The offset relative to the agent's transform that the cover search should originate from.")]
        [SerializeField] protected SharedVariable<Vector3> m_CoverSearchOffset = new Vector3(0, 1, 0);
        [Tooltip("The layermask of valid cover locations.")]
        [SerializeField] protected SharedVariable<LayerMask> m_CoverLayers;
        [Tooltip("The maximum number of raycasts that should be invoked before the agent gives up looking for a location to find cover.")]
        [SerializeField] protected SharedVariable<int> m_MaxRaycasts = 90;
        [Tooltip("The step size betwen raycasts.")]
        [SerializeField] protected SharedVariable<float> m_RayStep = 4;
        [Tooltip("Specifies the distance away from the cover point that the agent should move to.")]
        [SerializeField] protected SharedVariable<float> m_CoverOffset = 1;
        [Tooltip("Should an OverlapSphere check be performed at the cover location in order to determine if it is occupied?")]
        [SerializeField] protected SharedVariable<bool> m_CheckForOccupency = true;
        [Tooltip("The radius of the OverlapSphere check if checking for occupency.")]
        [SerializeField] protected SharedVariable<float> m_OccupencyOverlapRadius = 0.2f;
        [Tooltip("The layermask of the layers that can cause overlap.")]
        [SerializeField] protected SharedVariable<LayerMask> m_OccupencyLayerMask;
        [Tooltip("Should the agent look at the cover point after it has arrived?")]
        [SerializeField] protected SharedVariable<bool> m_LookAtCoverPoint;
        [Tooltip("The maximum number of angles the agent can rotate in a single tick (in degrees).")]
        [SerializeField] protected SharedVariable<float> m_MaxRotationDelta = 1;
        [Tooltip("Specifies the angle when the agent has arrived at the target rotation (in degrees).")]
        [SerializeField] protected SharedVariable<float> m_ArrivedAngle = 0.5f;

        private Vector3 m_CoverLocation;
        private Vector3 m_CoverLocationOffset;
        private Collider[] m_OccupencyObjects;
        private bool m_FoundCover;

        /// <summary>
        /// The task has started.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();

            m_FoundCover = false;
            RaycastHit hit;
            var position = transform.TransformPoint(m_CoverSearchOffset.Value);
            var direction = transform.forward;
            var count = 0;
            var step = 0f;
            // Keep searching for cover until too many rays have been fired.
            while (count < m_MaxRaycasts.Value) {
                var ray = new Ray(position, direction);
                if (Physics.Raycast(ray, out hit, m_CoverSearchDistance.Value, m_CoverLayers.Value.value)) {
                    // A suitable location has been found. Find the opposite side of that location by casting a ray in the opposite direction from the point.
                    if (hit.collider.Raycast(new Ray(hit.point - hit.normal * m_CoverSearchDistance.Value, hit.normal), out hit, Mathf.Infinity)) {
                        m_CoverLocation = hit.point;
                        m_CoverLocationOffset = m_CoverLocation + hit.normal * m_CoverOffset.Value;

                        // If checking for occupency ensure no objects are within an OverlapSphere.
                        if (!m_CheckForOccupency.Value || Physics.OverlapSphereNonAlloc(m_CoverLocationOffset, m_OccupencyOverlapRadius.Value, m_OccupencyObjects, m_OccupencyLayerMask.Value, QueryTriggerInteraction.Ignore) == 0) {
                            SetDestination(m_CoverLocationOffset);
                            m_FoundCover = true;
                            break;
                        }
                    }
                }
                // Keep sweeiping along the y axis
                step += m_RayStep.Value;
                direction = Quaternion.Euler(0, transform.eulerAngles.y + step, 0) * Vector3.forward;
                count++;
            }
        }

        /// <summary>
        /// Moves to the cover location.
        /// </summary>
        /// <returns>Success as soon as the destination is reached or the agent is looking at the cover location.</returns>
        public override TaskStatus OnUpdate()
        {
            if (!m_FoundCover) {
                return TaskStatus.Failure;
            }
            if (HasArrived()) {
                var rotation = Quaternion.LookRotation(m_CoverLocation - transform.position);
                // Return success if the agent isn't going to look at the cover point or is looking at the cover point.
                if (!m_LookAtCoverPoint.Value || Quaternion.Angle(transform.rotation, rotation) < m_ArrivedAngle.Value) {
                    return TaskStatus.Success;
                } else {
                    // Rotates towards the target.
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, m_MaxRotationDelta.Value);
                }
            }

            return TaskStatus.Running;
        }

        /// <summary>
        /// Resets the task values back to their default.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            m_CoverSearchDistance = 20;
            m_CoverSearchOffset = new Vector3(0, 1, 0);
            m_CoverLayers = new LayerMask();
            m_MaxRaycasts = 90;
            m_RayStep = 4;
            m_CoverOffset = 1;
            m_CheckForOccupency = true;
            m_OccupencyOverlapRadius = 0.2f;
            m_OccupencyLayerMask = new LayerMask();
            m_LookAtCoverPoint = false;
            m_MaxRotationDelta = 1;
            m_ArrivedAngle = 0.5f;
        }
    }
}