/// ---------------------------------------------
/// Tactical Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.Shared.Demo
{
    using Opsive.BehaviorDesigner.AddOns.TacticalPack.Demo;
    using Opsive.GraphDesigner.Runtime.Variables;
    using UnityEngine;

    /// <summary>
    /// Extends the Scenario Selector allowing for the correct target to be set.
    /// </summary>
    public class TacticalScenarioSelector : ScenarioSelector
    {
        [Tooltip("The name of the target SharedVariable.")]
        [SerializeField] protected string m_TargetsVariableName = "Targets";
        [Tooltip("The name of the defend SharedVariable.")]
        [SerializeField] protected string m_DefendVariableName = "Defend";
        [Tooltip("The name of the attack SharedVariable.")]
        [SerializeField] protected string m_InitialAttackVariableName = "InitialAttack";
        [Tooltip("The name of the reforcements SharedVariable.")]
        [SerializeField] protected string m_ReforcementsVariableName = "Reinforcements";

        [Tooltip("The enemy targets for the main scenario.")]
        [SerializeField] protected GameObject[] m_Targets;
        [Tooltip("The targets for the ambush scenario.")]
        [SerializeField] protected GameObject[] m_AmbushTargets;
        [Tooltip("The friendly unit to defend in the defense scenario.")]
        [SerializeField] protected GameObject m_DefendFriendly;
        [Tooltip("The targets to defend against in the defense scenario.")]
        [SerializeField] protected GameObject[] m_DefendTargets;
        [Tooltip("The spawn location for reinforcements in the reinforcement scenario.")]
        [SerializeField] protected GameObject m_ReforcementsAttackerLocation;
        [Tooltip("The targets in the moving targets scenario.")]
        [SerializeField] protected GameObject[] m_MovingTargets;

        // Cache of initial positions and rotations for scenario reset.
        private Vector3[] m_AmbushTargetsStartPositions;
        private Quaternion[] m_AmbushTargetsStartRotations;
        private Vector3[] m_DefendTargetsStartPositions;
        private Quaternion[] m_DefendTargetsStartRotations;
        private Vector3[] m_MovingTargetsStartPositions;
        private Quaternion[] m_MovingTargetsStartRotations;
        // Cache of health components for all targets.
        private Health[] m_TargetHealths;

        /// <summary>
        /// Initializes the scenario by setting up target variables and caching initial positions and health components.
        /// </summary>
        protected override void Initialized()
        {
            // Set target variables for all behavior trees.
            for (int i = 0; i < m_AgentBehaviorTrees.Length; ++i) {
                SetVariableValue(m_TargetsVariableName, m_Targets, SharedVariable.SharingScope.GameObject);
            }

            m_AmbushTargetsStartPositions = new Vector3[m_AmbushTargets.Length];
            m_AmbushTargetsStartRotations = new Quaternion[m_AmbushTargets.Length];
            for (int i = 0; i < m_AmbushTargets.Length; ++i) {
                m_AmbushTargetsStartPositions[i] = m_AmbushTargets[i].transform.position;
                m_AmbushTargetsStartRotations[i] = m_AmbushTargets[i].transform.rotation;
            }
            m_DefendTargetsStartPositions = new Vector3[m_DefendTargets.Length];
            m_DefendTargetsStartRotations = new Quaternion[m_DefendTargets.Length];
            for (int i = 0; i < m_DefendTargets.Length; ++i) {
                m_DefendTargetsStartPositions[i] = m_DefendTargets[i].transform.position;
                m_DefendTargetsStartRotations[i] = m_DefendTargets[i].transform.rotation;
            }
            m_MovingTargetsStartPositions = new Vector3[m_MovingTargets.Length];
            m_MovingTargetsStartRotations = new Quaternion[m_MovingTargets.Length];
            for (int i = 0; i < m_MovingTargets.Length; ++i) {
                m_MovingTargetsStartPositions[i] = m_MovingTargets[i].transform.position;
                m_MovingTargetsStartRotations[i] = m_MovingTargets[i].transform.rotation;
            }

            // Cache health components for all targets.
            m_TargetHealths = new Health[m_Targets.Length + m_AmbushTargets.Length + m_DefendTargets.Length + m_MovingTargets.Length];
            for (int i = 0; i < m_Targets.Length; ++i) {
                m_TargetHealths[i] = m_Targets[i].GetComponent<Health>();
            }
            var offset = m_Targets.Length;
            for (int i = 0; i < m_AmbushTargets.Length; ++i) {
                m_TargetHealths[i + offset] = m_AmbushTargets[i].GetComponent<Health>();
            }
            offset += m_AmbushTargets.Length;
            for (int i = 0; i < m_DefendTargets.Length; ++i) {
                m_TargetHealths[i + offset] = m_DefendTargets[i].GetComponent<Health>();
            }
            offset += m_DefendTargets.Length;
            for (int i = 0; i < m_MovingTargets.Length; ++i) {
                m_TargetHealths[i + offset] = m_MovingTargets[i].GetComponent<Health>();
            }
        }

        /// <summary>
        /// Enables the scenario at the specified index and resets all target health.
        /// </summary>
        /// <param name="index">The index of the scenario to enable.</param>
        protected override void EnableScenario(int index)
        {
            for (int i = 0; i < m_TargetHealths.Length; ++i) {
                m_TargetHealths[i].ResetHealth();
            }

            base.EnableScenario(index);
        }

        /// <summary>
        /// Starts the ambush scenario.
        /// </summary>
        public void StartAmbushScenario()
        {
            // Reset positions and rotations of ambush targets.
            for (int i = 0; i < m_AmbushTargets.Length; ++i) {
                m_AmbushTargets[i].transform.SetPositionAndRotation(m_AmbushTargetsStartPositions[i], m_AmbushTargetsStartRotations[i]);
                m_AmbushTargets[i].GetComponent<NavMeshPathfindingAgent>().Warp(m_AmbushTargets[i].transform.position);
            }
            SetVariableValue(m_TargetsVariableName, m_AmbushTargets, SharedVariable.SharingScope.GameObject);
        }

        /// <summary>
        /// Stops the ambush scenario.
        /// </summary>
        public void StopAmbushScenario()
        {
            SetVariableValue(m_TargetsVariableName, m_Targets, SharedVariable.SharingScope.GameObject);
        }

        /// <summary>
        /// Starts the defense scenario.
        /// </summary>
        public void StartDefendScenario()
        {
            // Reset positions and rotations of defend targets.
            for (int i = 0; i < m_DefendTargets.Length; ++i) {
                m_DefendTargets[i].transform.SetPositionAndRotation(m_DefendTargetsStartPositions[i], m_DefendTargetsStartRotations[i]);
                m_DefendTargets[i].GetComponent<NavMeshPathfindingAgent>().Warp(m_DefendTargets[i].transform.position);
            }
            SetVariableValue(m_TargetsVariableName, m_DefendTargets, SharedVariable.SharingScope.GameObject);
            SetVariableValue(m_DefendVariableName, m_DefendFriendly, SharedVariable.SharingScope.GameObject);
        }

        /// <summary>
        /// Stops the defense scenario.
        /// </summary>
        public void StopDefendScenario()
        {
            SetVariableValue(m_TargetsVariableName, m_Targets, SharedVariable.SharingScope.GameObject);
        }

        /// <summary>
        /// Starts the reinforcements scenario.
        /// </summary>
        public void StartReinforcementsScenario()
        {
            // Position the attacker at the reinforcement location
            m_AgentBehaviorTrees[0][m_ActiveIndex].transform.SetPositionAndRotation(m_ReforcementsAttackerLocation.transform.position, m_ReforcementsAttackerLocation.transform.rotation);
            m_AgentBehaviorTrees[0][m_ActiveIndex].GetVariable<bool>(m_InitialAttackVariableName).Value = true;
            
            // Set up reinforcement agents
            var agentList = new GameObject[m_AgentBehaviorTrees.Length - 1];
            for (int i = 0; i < agentList.Length; ++i) {
                agentList[i] = m_AgentBehaviorTrees[i + 1][m_ActiveIndex].gameObject;
            }
            SetVariableValue(m_ReforcementsVariableName, agentList, SharedVariable.SharingScope.Graph);
        }

        /// <summary>
        /// Starts the moving target scenario.
        /// </summary>
        public void StartMovingTargetScenario()
        {
            // Reset positions and rotations of defend targets.
            for (int i = 0; i < m_MovingTargets.Length; ++i) {
                m_MovingTargets[i].transform.SetPositionAndRotation(m_MovingTargetsStartPositions[i], m_MovingTargetsStartRotations[i]);
                m_MovingTargets[i].GetComponent<NavMeshPathfindingAgent>().Warp(m_MovingTargets[i].transform.position);
            }
            SetVariableValue(m_TargetsVariableName, m_MovingTargets, SharedVariable.SharingScope.GameObject);
        }

        /// <summary>
        /// Stops the moving target scenario.
        /// </summary>
        public void StopMovingTargetScenario()
        {
            SetVariableValue(m_TargetsVariableName, m_Targets, SharedVariable.SharingScope.GameObject);
        }

        /// <summary>
        /// Helper method to set variable values across all behavior trees.
        /// </summary>
        /// <typeparam name="T">The type of the variable value.</typeparam>
        /// <param name="variableName">The name of the variable to set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="scope">The sharing scope of the variable.</param>
        private void SetVariableValue<T>(string variableName, T value, SharedVariable.SharingScope scope)
        {
            for (int i = 0; i < m_AgentBehaviorTrees.Length; ++i) {
                m_AgentBehaviorTrees[i][m_ActiveIndex].GetVariable<T>(variableName, scope).Value = value;
            }
        }
    }
} 