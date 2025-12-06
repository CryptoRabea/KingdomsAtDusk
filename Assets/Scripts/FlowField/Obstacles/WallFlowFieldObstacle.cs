using UnityEngine;
using FlowField.Core;

namespace FlowField.Obstacles
{
    /// <summary>
    /// Marks wall segments as obstacles in the FlowField cost grid
    /// Replaces NavMeshObstacle functionality for FlowField pathfinding
    /// Automatically updates the cost field when walls are placed/destroyed
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WallFlowFieldObstacle : MonoBehaviour
    {
        [Header("Obstacle Settings")]
        [SerializeField] private float expansionPadding = 0.2f; // Extra space around wall

        [Header("Cost Settings")]
        [SerializeField] private byte obstacleCost = GridCell.UNWALKABLE_COST;
        [SerializeField] private bool updateOnEnable = true;

        private FlowFieldManager flowFieldManager;
        private Collider wallCollider;
        private Bounds obstacleBounds;
        private bool isRegistered = false;

        private void Awake()
        {
            wallCollider = GetComponent<Collider>();
            if (wallCollider == null)
            {
                UnityEngine. Debug.LogError($"WallFlowFieldObstacle: No collider found on {gameObject.name}!");
                return;
            }

            CalculateBounds();
        }

        private void Start()
        {
            flowFieldManager = FlowFieldManager.Instance;

            if (flowFieldManager == null)
            {
                UnityEngine.Debug.LogWarning("FlowFieldManager not found! Wall obstacle will not affect pathfinding.");
                return;
            }

            if (updateOnEnable)
            {
                RegisterObstacle();
            }
        }

        private void OnEnable()
        {
            if (flowFieldManager != null && updateOnEnable && isRegistered)
            {
                UpdateCostField();
            }
        }

        private void OnDisable()
        {
            if (flowFieldManager != null && isRegistered)
            {
                // When disabled, mark area as walkable again
                UnregisterObstacle();
            }
        }

        private void OnDestroy()
        {
            if (flowFieldManager != null && isRegistered)
            {
                UnregisterObstacle();
            }
        }

        /// <summary>
        /// Calculate obstacle bounds from collider
        /// </summary>
        private void CalculateBounds()
        {
            if (wallCollider == null)
                return;

            obstacleBounds = wallCollider.bounds;

            // Add padding
            obstacleBounds.Expand(expansionPadding * 2f);
        }

        /// <summary>
        /// Register this obstacle and update the cost field
        /// </summary>
        public void RegisterObstacle()
        {
            if (isRegistered || flowFieldManager == null)
                return;

            CalculateBounds();
            UpdateCostField();
            isRegistered = true;

            UnityEngine.Debug.Log($"WallFlowFieldObstacle registered for {gameObject.name} at {transform.position}");
        }

        /// <summary>
        /// Unregister this obstacle and restore walkability
        /// </summary>
        public void UnregisterObstacle()
        {
            if (!isRegistered || flowFieldManager == null)
                return;

            // Update cost field to restore walkability
            if (flowFieldManager.Grid != null)
            {
                flowFieldManager.UpdateCostField(obstacleBounds);
            }

            isRegistered = false;

            UnityEngine.Debug.Log($"WallFlowFieldObstacle unregistered for {gameObject.name}");
        }

        /// <summary>
        /// Update the cost field to mark this area as unwalkable
        /// </summary>
        private void UpdateCostField()
        {
            if (flowFieldManager == null || flowFieldManager.Grid == null)
                return;

            // Notify FlowFieldManager that this region needs updating
            flowFieldManager.UpdateCostField(obstacleBounds);
        }

        /// <summary>
        /// Get the obstacle bounds
        /// </summary>
        public Bounds GetBounds()
        {
            return obstacleBounds;
        }

        /// <summary>
        /// Manually trigger a cost field update
        /// </summary>
        public void RefreshCostField()
        {
            CalculateBounds();
            UpdateCostField();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (wallCollider == null)
                wallCollider = GetComponent<Collider>();

            if (obstacleBounds.size == Vector3.zero && wallCollider != null)
            {
                CalculateBounds();
            }

            Gizmos.color = new Color(1f, 0f, 0f, 0.6f); // Red for walls
            Gizmos.DrawWireCube(obstacleBounds.center, obstacleBounds.size);

            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawCube(obstacleBounds.center, obstacleBounds.size);
        }

        private void OnValidate()
        {
            if (wallCollider != null)
            {
                CalculateBounds();
            }
        }
#endif
    }
}
