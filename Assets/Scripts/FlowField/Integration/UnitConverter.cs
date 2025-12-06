using UnityEngine;
using UnityEngine.AI;
using FlowField.Movement;
using Debug = UnityEngine.Debug;

namespace FlowField.Integration
{
    /// <summary>
    /// Converts existing NavMesh-based units to Flow Field units
    /// Can be run at runtime or in editor
    /// </summary>
    public class UnitConverter : MonoBehaviour
    {
        [Header("Conversion Settings")]
        [SerializeField] private bool convertOnStart = false;
        [SerializeField] private bool disableNavMeshAgent = true;
        [SerializeField] private bool removeNavMeshAgent = false;

        [Header("Flow Field Settings")]
        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float turnSpeed = 720f;
        [SerializeField] private float avoidanceRadius = 2f;

        [Header("Target Selection")]
        [SerializeField] private bool convertAllUnitsInScene = false;
        [SerializeField] private string unitTag = "Unit";
        [SerializeField] private LayerMask unitLayer;

        private void Start()
        {
            if (convertOnStart)
            {
                ConvertAllUnits();
            }
        }

        /// <summary>
        /// Convert all units in scene from NavMesh to Flow Field
        /// </summary>
        [ContextMenu("Convert All Units")]
        public void ConvertAllUnits()
        {
            GameObject[] allUnits;

            if (convertAllUnitsInScene)
            {
                allUnits = GameObject.FindGameObjectsWithTag(unitTag);
            }
            else
            {
                // Find all objects with NavMeshAgent
                var agents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
                allUnits = new GameObject[agents.Length];
                for (int i = 0; i < agents.Length; i++)
                {
                    allUnits[i] = agents[i].gameObject;
                }
            }

            int convertedCount = 0;

            foreach (var unit in allUnits)
            {
                if (ConvertUnit(unit))
                {
                    convertedCount++;
                }
            }

            UnityEngine.Debug.Log($"Converted {convertedCount} units to Flow Field movement");
        }

        /// <summary>
        /// Convert a single unit
        /// </summary>
        public bool ConvertUnit(GameObject unit)
        {
            if (unit == null)
                return false;

            // Check if already has FlowFieldFollower
            if (unit.GetComponent<FlowFieldFollower>() != null)
            {
                UnityEngine.Debug.LogWarning($"Unit {unit.name} already has FlowFieldFollower");
                return false;
            }

            NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
            // Units.Components.UnitMovement unitMovement = unit.GetComponent<Units.Components.UnitMovement>();
            var unitMovement = unit.GetComponent("UnitMovement") as MonoBehaviour;

            // Transfer settings from NavMeshAgent
            float speed = maxSpeed;
            float radius = 0.5f;

            if (agent != null)
            {
                speed = agent.speed;
                radius = agent.radius;

                if (disableNavMeshAgent)
                {
                    agent.enabled = false;
                }

                if (removeNavMeshAgent)
                {
                    Destroy(agent);
                }
            }

            // Disable old UnitMovement script
            if (unitMovement != null)
            {
                unitMovement.enabled = false;
            }

            // Add FlowFieldFollower
            FlowFieldFollower follower = unit.AddComponent<FlowFieldFollower>();

            // Configure using reflection to set private serialized fields
            var followerType = typeof(FlowFieldFollower);

            SetPrivateField(follower, "maxSpeed", speed);
            SetPrivateField(follower, "acceleration", acceleration);
            SetPrivateField(follower, "turnSpeed", turnSpeed);
            SetPrivateField(follower, "avoidanceRadius", avoidanceRadius);
            SetPrivateField(follower, "unitRadius", radius);

            // Ensure Rigidbody exists
            Rigidbody rb = unit.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = unit.AddComponent<Rigidbody>();
            }

            // Configure Rigidbody
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX |
                            RigidbodyConstraints.FreezeRotationZ;
            rb.mass = 1f;

            UnityEngine.Debug.Log($"Converted {unit.name} to Flow Field movement (Speed: {speed})");

            return true;
        }

        /// <summary>
        /// Helper to set private serialized fields
        /// </summary>
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }

        /// <summary>
        /// Revert unit back to NavMesh movement
        /// </summary>
        public void RevertUnit(GameObject unit)
        {
            if (unit == null)
                return;

            FlowFieldFollower follower = unit.GetComponent<FlowFieldFollower>();
            if (follower != null)
            {
                Destroy(follower);
            }

            NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
            }

            // Units.Components.UnitMovement unitMovement = unit.GetComponent<Units.Components.UnitMovement>();
            var unitMovement = unit.GetComponent("UnitMovement") as MonoBehaviour;
            if (unitMovement != null)
            {
                unitMovement.enabled = true;
            }

            UnityEngine.Debug.Log($"Reverted {unit.name} to NavMesh movement");
        }
    }
}
