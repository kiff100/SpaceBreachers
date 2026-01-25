/// ---------------------------------------------
/// Shared Add-On for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.Shared.Demo
{
    using Opsive.BehaviorDesigner.Runtime;
    using System;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Events;

    /// <summary>
    /// Manages the scenario selections for the Behavior Designer Pro Add-Ons.
    /// </summary>
    public class ScenarioSelector : MonoBehaviour
    {
        /// <summary>
        /// Details about the scenario.
        /// </summary>
        [System.Serializable]
        protected struct Scenario
        {
            [Tooltip("The start location of the agent.")]
            public Transform StartLocation;
            [Tooltip("The start location of the destination marker.")]
            public Transform DestinationLocation;
            [Tooltip("The parent transform of the camera. If empty the camera's starting location will be used.")]
            public Transform CameraParent;
            [Tooltip("The desription of the scenario.")]
            [Multiline] public string Description;
            [Tooltip("The index of the behavior tree that should be enabled on the destination marker. Set to 0 to disable.")]
            public int DestinationTreeIndex;
            [Tooltip("Specifies any GameObjects that should be activated.")]
            public GameObject[] ActiveTargets;
            [Tooltip("Specifies a component that should be enabled.")]
            public MonoBehaviour[] EnableTargets;
            [Tooltip("Should the player be enabled?")]
            public bool ShowPlayer;
            [Tooltip("The start location of the player. If empty the player's starting location will be used.")]
            public Transform PlayerStartLocation;
            [Tooltip("Event that is invoked when the scenario starts.")]
            public UnityEvent OnScenarioStarted;
            [Tooltip("Event that is invoked when the scenario ends.")]
            public UnityEvent OnScenarioEnded;
        }

        [Tooltip("A reference to the behavior tree agent.")]
        [SerializeField] protected GameObject m_Agent;
        [Tooltip("The number of agents that should be spawned.")]
        [SerializeField] protected int m_SpawnCount;
        [Tooltip("The radius around the spawn center where agents can be spawned.")]
        [ShowIfPositive("m_SpawnCount")]
        [SerializeField] protected float m_SpawnRadius;
        [Tooltip("The maximum number of attempts to find a valid spawn position for each agent.")]
        [ShowIfPositive("m_SpawnCount")]
        [SerializeField] protected int m_MaxSpawnAttempts = 10;
        [Tooltip("The minimum distance that must be maintained between spawned agents.")]
        [ShowIfPositive("m_SpawnCount")]
        [SerializeField] protected float m_MinSpawnDistance = 1.0f;
        [Tooltip("A reference to the destination marker.")]
        [SerializeField] protected GameObject m_Destination;
        [Tooltip("A reference to the player.")]
        [SerializeField] protected GameObject m_Player;
        [Tooltip("A reference to the UI title text.")]
        [SerializeField] protected Text m_TitleText;
        [Tooltip("A reference to the UI description text.")]
        [SerializeField] protected Text m_DescriptionText;
        [Tooltip("The scenario index.")]
        [SerializeField] protected int m_ActiveIndex;
        [Tooltip("Specifies details about each scenario.")]
        [SerializeField] protected Scenario[] m_Scenarios;

        public int ActiveIndex => m_ActiveIndex;

        private GameObject[] m_Agents;
        private IPathfindingAgent[] m_PathfindingAgents;
        private Transform[] m_AgentTransforms;
        protected BehaviorTree[][] m_AgentBehaviorTrees;
        private Transform m_DestinationTransform;
        private BehaviorTree[] m_DestinationBehaviorTrees;

        private Transform m_PlayerTransform;
        private Vector3 m_PlayerStartPosition;
        private Quaternion m_PlayerStartRotation;
        private Transform m_CameraTransform;
        private Vector3 m_CameraStartPosition;
        private Quaternion m_CameraStartRotation;

        public BehaviorTree[][] AgentBehaviorTrees => m_AgentBehaviorTrees;

        /// <summary>
        /// Initializes the default values.
        /// </summary>
        protected virtual void Awake()
        {
            // Spawn multiple agents if the count is greater than 0.
            m_Agents = new GameObject[Mathf.Max(1, m_SpawnCount)];
            m_AgentTransforms = new Transform[m_Agents.Length];
            m_PathfindingAgents = new IPathfindingAgent[m_Agents.Length];
            m_AgentBehaviorTrees = new BehaviorTree[m_Agents.Length][];
            if (m_SpawnCount > 0) {
                for (int i = 0; i < m_SpawnCount; ++i) {
                    var spawnedAgent = Instantiate(m_Agent, Vector3.zero, Quaternion.identity);
                    spawnedAgent.name = string.Format("{0} {1}", m_Agent.name, i);
                    m_Agents[i] = spawnedAgent;
                    m_AgentTransforms[i] = spawnedAgent.transform;
                    m_PathfindingAgents[i] = spawnedAgent.GetComponent<IPathfindingAgent>();
                }
            } else {
                m_Agents[0] = m_Agent;
                m_AgentTransforms[0] = m_Agent.transform;
                m_PathfindingAgents[0] = m_Agent.GetComponent<IPathfindingAgent>();
            }
            for (int i = 0; i < m_Agents.Length; ++i) {
                m_AgentBehaviorTrees[i] = m_Agents[i].GetComponents<BehaviorTree>();
                Array.Sort(m_AgentBehaviorTrees[i], (a, b) =>
                {
                    return a.Index.CompareTo(b.Index);
                });
                for (int j = 0; j < m_AgentBehaviorTrees[i].Length; ++j) {
                    m_AgentBehaviorTrees[i][j].enabled = false;
                }
            }

            if (m_Destination != null) {
                m_DestinationTransform = m_Destination.transform;
                m_DestinationBehaviorTrees = m_Destination.GetComponents<BehaviorTree>();
            }
            if (m_Player != null) {
                m_PlayerTransform = m_Player.transform;
                m_PlayerStartPosition = m_PlayerTransform.position;
                m_PlayerStartRotation = m_PlayerTransform.rotation;
            }
            m_CameraTransform = Camera.main.transform;
            m_CameraStartPosition = m_CameraTransform.position;
            m_CameraStartRotation = m_CameraTransform.rotation;
            for (int i = 0; i < m_Scenarios.Length; ++i) {
                if (m_Scenarios[i].ActiveTargets != null) {
                    for (int j = 0; j < m_Scenarios[i].ActiveTargets.Length; ++j) {
                        if (m_Scenarios[i].ActiveTargets[j] == null) {
                            continue;
                        }
                        m_Scenarios[i].ActiveTargets[j].SetActive(false);
                    }
                }
            }

            Initialized();

            EnableScenario(m_ActiveIndex);
        }

        /// <summary>
        /// The selector has been initialized.
        /// </summary>
        protected virtual void Initialized() { }

        /// <summary>
        /// Enables the sceario at the specified index.
        /// </summary>
        /// <param name="index">The index of the scenario.</param>
        protected virtual void EnableScenario(int index)
        {
            // Disable all behavior trees for all agents.
            for (int i = 0; i < m_AgentBehaviorTrees.Length; ++i) {
                for (int j = 0; j < m_AgentBehaviorTrees[i].Length; ++j) {
                    m_AgentBehaviorTrees[i][j].enabled = false;
                }
            }

            if (m_Scenarios[m_ActiveIndex].ActiveTargets != null) {
                for (int i = 0; i < m_Scenarios[m_ActiveIndex].ActiveTargets.Length; ++i) {
                    if (m_Scenarios[m_ActiveIndex].ActiveTargets[i] == null) {
                        continue;
                    }

                    m_Scenarios[m_ActiveIndex].ActiveTargets[i].SetActive(false);
                }
            }
            if (m_Scenarios[m_ActiveIndex].EnableTargets != null) {
                for (int i = 0; i < m_Scenarios[m_ActiveIndex].EnableTargets.Length; ++i) {
                    if (m_Scenarios[m_ActiveIndex].EnableTargets[i] == null) {
                        continue;
                    }

                    m_Scenarios[m_ActiveIndex].EnableTargets[i].enabled = false;
                }
            }
            if (m_Scenarios[m_ActiveIndex].ShowPlayer) {
                m_Player.SetActive(false);
            }
            EnableDisableDestinationTree(m_Scenarios[m_ActiveIndex].DestinationTreeIndex, false);
            m_Scenarios[m_ActiveIndex].OnScenarioEnded?.Invoke();

            m_ActiveIndex = index;
            // Reset the objects for the new scenario.
            var scenario = m_Scenarios[m_ActiveIndex];

            // Position all agents.
            for (int i = 0; i < m_Agents.Length; ++i) {
                Vector3 spawnPosition;
                int attempts = 0;

                // Skip random positioning for the first agent
                if (i == 0) {
                    spawnPosition = scenario.StartLocation.position;
                } else {
                    do {
                        var randomCircle = UnityEngine.Random.insideUnitCircle * m_SpawnRadius;
                        spawnPosition = scenario.StartLocation.position + new Vector3(randomCircle.x, 0, randomCircle.y);
                        attempts++;
                    } while (attempts < m_MaxSpawnAttempts && IsTooCloseToOtherAgents(spawnPosition, i, m_MinSpawnDistance));
                }

                m_PathfindingAgents[i].Warp(spawnPosition);
                m_AgentTransforms[i].SetPositionAndRotation(spawnPosition, scenario.StartLocation.rotation);
            }
            
            if (m_Destination != null) {
                m_Destination.SetActive(scenario.DestinationLocation != null);
                if (scenario.DestinationLocation != null) {
                    m_Destination.transform.SetPositionAndRotation(scenario.DestinationLocation.position, scenario.DestinationLocation.rotation);
                }
            }
            if (scenario.ActiveTargets != null) {
                for (int i = 0; i < scenario.ActiveTargets.Length; ++i) {
                    if (scenario.ActiveTargets[i] == null) {
                        continue;
                    }

                    m_Scenarios[m_ActiveIndex].ActiveTargets[i].SetActive(true);
                }
            }
            if (m_Player != null && scenario.ShowPlayer) {
                if (scenario.PlayerStartLocation == null) {
                    m_PlayerTransform.SetPositionAndRotation(m_PlayerStartPosition, m_PlayerStartRotation);
                } else {
                    m_PlayerTransform.SetPositionAndRotation(scenario.PlayerStartLocation.position, scenario.PlayerStartLocation.rotation);
                }
                m_Player.SetActive(true);
            }
            if (scenario.CameraParent != null) {
                m_CameraTransform.parent = scenario.CameraParent.transform;
                m_CameraTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            } else if (m_CameraTransform != null) {
                m_CameraTransform.parent = null;
                m_CameraTransform.SetPositionAndRotation(m_CameraStartPosition, m_CameraStartRotation);
            }
            if (scenario.EnableTargets != null) {
                for (int i = 0; i < scenario.EnableTargets.Length; ++i) {
                    if (scenario.EnableTargets[i] == null) {
                        continue;
                    }

                    scenario.EnableTargets[i].enabled = true;
                }
            }
            m_TitleText.text = m_AgentBehaviorTrees[0][m_ActiveIndex].Name;
            m_DescriptionText.text = scenario.Description;
            EnableDisableDestinationTree(scenario.DestinationTreeIndex, true);
            for (int i = 0; i < m_AgentBehaviorTrees.Length; ++i) {
                m_AgentBehaviorTrees[i][m_ActiveIndex].enabled = true;
            }
            scenario.OnScenarioStarted?.Invoke();
        }

        /// <summary>
        /// Resets the current scenario.
        /// </summary>
        public void ResetScenario()
        {
            EnableScenario(m_ActiveIndex);
        }

        /// <summary>
        /// Changes to the next or previous scenario.
        /// </summary>
        /// <param name="next">Should the next scenario be selected? If false the previous scenario will be selected.</param>
        public void ChangeScenario(bool next)
        {
            var nextIndex = (m_ActiveIndex + (next ? 1 : -1)) % m_AgentBehaviorTrees[0].Length;
            if (nextIndex < 0) nextIndex = m_AgentBehaviorTrees[0].Length - 1;
            EnableScenario(nextIndex);
        }

        /// <summary>
        /// Enables or disables the destination tree at the specified index.
        /// </summary>
        /// <param name="index">The index of the destination tree.</param>
        /// <param name="enable">Should the destination tree be enabled?</param>
        private void EnableDisableDestinationTree(int index, bool enable)
        {
            // All destination trees will have a positive value.
            if (index == 0) {
                return;
            }

            BehaviorTree destinationTree = null;
            for (int i = 0; i < m_DestinationBehaviorTrees.Length; ++i) {
                if (m_DestinationBehaviorTrees[i].Index == index) {
                    destinationTree = m_DestinationBehaviorTrees[i];
                    break;
                }
            }
            if (destinationTree == null) {
                return;
            }
            destinationTree.enabled = enable;
        }

        /// <summary>
        /// Checks if the proposed spawn position is too close to any other agents.
        /// </summary>
        /// <param name="position">The proposed spawn position.</param>
        /// <param name="currentAgentIndex">The index of the current agent being spawned.</param>
        /// <param name="minDistance">The minimum allowed distance between agents.</param>
        /// <returns>True if the position is too close to another agent.</returns>
        private bool IsTooCloseToOtherAgents(Vector3 position, int currentAgentIndex, float minDistance)
        {
            for (int i = 0; i < currentAgentIndex; i++) {
                if (Vector3.Distance(position, m_AgentTransforms[i].position) < minDistance) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Attribute that allows conditional showing of inspector fields based on another field having a positive value.
        /// </summary>
        public class ShowIfPositiveAttribute : PropertyAttribute
        {
            private string m_ConditionalSourceField;

            public string ConditionalSourceField => m_ConditionalSourceField;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="conditionalSourceField">The name of the conditional field.</param>
            public ShowIfPositiveAttribute(string conditionalSourceField)
            {
                m_ConditionalSourceField = conditionalSourceField;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draws the property field in the Unity Inspector if the conditional source field has a positive value.
        /// </summary>
        [UnityEditor.CustomPropertyDrawer(typeof(ShowIfPositiveAttribute))]
        public class ShowIfPositiveDrawer : UnityEditor.PropertyDrawer
        {
            /// <summary>
            /// Draws the property field.
            /// </summary>
            /// <param name="position">The position and size of the property field in the Inspector.</param>
            /// <param name="property">The SerializedProperty to draw.</param>
            /// <param name="label">The label for the property field.</param>
            public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
            {
                var showIfAttribute = (ShowIfPositiveAttribute)attribute;
                var sourcePropertyValue = property.serializedObject.FindProperty(showIfAttribute.ConditionalSourceField);
                if (sourcePropertyValue != null && sourcePropertyValue.intValue > 0) {
                    UnityEditor.EditorGUI.PropertyField(position, property, label, true);
                }
            }

            /// <summary>
            /// Calculates the height of the property field in the Inspector.
            /// </summary>
            /// <param name="property">The SerializedProperty to calculate height for.</param>
            /// <param name="label">The label for the property field.</param>
            /// <returns>The height of the property field.</returns>
            public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
            {
                var showIfAttribute = (ShowIfPositiveAttribute)attribute;
                var sourcePropertyValue = property.serializedObject.FindProperty(showIfAttribute.ConditionalSourceField);
                if (sourcePropertyValue != null && sourcePropertyValue.intValue > 0) {
                    return UnityEditor.EditorGUI.GetPropertyHeight(property, label);
                }
                return 0;
            }
        }
#endif
    }
}