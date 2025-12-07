using UnityEngine;
using FlowField.Core;

namespace FlowField.Obstacles
{
    /// <summary>
    /// Marks buildings as obstacles in the FlowField cost grid
    /// Replaces NavMeshObstacle functionality for FlowField pathfinding
    /// Automatically updates the cost field when buildings are placed/destroyed
    /// </summary>
    public class BuildingFlowFieldObstacle : MonoBehaviour
    {
        [Header("Obstacle Settings")]
        [SerializeField] private bool autoDetectSize = true;
        [SerializeField] private Vector3 manualSize = Vector3.one * 5f;
        [SerializeField] private Vector3 manualCenter = Vector3.zero;
        [SerializeField] private float expansionPadding = 0.5f; // Extra space around building

        [Header("Cost Settings")]
        [SerializeField] private byte obstacleCost = GridCell.UNWALKABLE_COST;
        [SerializeField] private bool updateOnEnable = true;

        private FlowFieldManager flowFieldManager;
        private Bounds obstacleBounds;
        private bool isRegistered = false;

        private void Awake()
        {
            CalculateBounds();
        }

        private void Start()
        {
            flowFieldManager = FlowFieldManager.Instance;

            if (flowFieldManager == null)
            {
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
        /// Calculate obstacle bounds from colliders or renderers
        /// </summary>
        private void CalculateBounds()
        {
            if (autoDetectSize)
            {
                DetectAndSetSize();
            }
            else
            {
                obstacleBounds = new Bounds(
                    transform.position + manualCenter,
                    manualSize
                );
            }

            // Add padding
            obstacleBounds.Expand(expansionPadding * 2f);
        }

        /// <summary>
        /// Auto-detect size from colliders or renderers
        /// </summary>
        private void DetectAndSetSize()
        {
            // Try to get bounds from collider
            Collider buildingCollider = GetComponent<Collider>();
            if (buildingCollider != null)
            {
                obstacleBounds = buildingCollider.bounds;
                return;
            }

            // Try to get bounds from all child colliders
            Collider[] childColliders = GetComponentsInChildren<Collider>();
            if (childColliders.Length > 0)
            {
                Bounds combinedBounds = childColliders[0].bounds;
                for (int i = 1; i < childColliders.Length; i++)
                {
                    combinedBounds.Encapsulate(childColliders[i].bounds);
                }
                obstacleBounds = combinedBounds;
                return;
            }

            // Try to get bounds from renderers
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
                obstacleBounds = combinedBounds;
                return;
            }

            // Fallback to default size
            obstacleBounds = new Bounds(transform.position, Vector3.one * 5f);
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
        /// Manually trigger a cost field update (e.g., if building moves)
        /// </summary>
        public void RefreshCostField()
        {
            CalculateBounds();
            UpdateCostField();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (obstacleBounds.size == Vector3.zero)
            {
                CalculateBounds();
            }

            Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f); // Orange for buildings
            Gizmos.DrawWireCube(obstacleBounds.center, obstacleBounds.size);

            Gizmos.color = new Color(1f, 0.3f, 0f, 0.2f);
            Gizmos.DrawCube(obstacleBounds.center, obstacleBounds.size);
        }

        private void OnValidate()
        {
            // Recalculate bounds when settings change in inspector
            CalculateBounds();
        }
#endif
    }
}
