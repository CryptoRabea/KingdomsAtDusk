using UnityEngine;
using UnityEngine.AI;

namespace RTS.Buildings
{
    /// <summary>
    /// Automatically adds and configures NavMeshObstacle for buildings.
    /// This ensures buildings block unit navigation properly.
    /// Can be added to building prefabs or attached dynamically.
    /// </summary>
    public class BuildingNavMeshObstacle : MonoBehaviour
    {
        [Header("NavMesh Settings")]
        [SerializeField] private bool carveNavMesh = true;
        [SerializeField] private bool carveOnlyWhenStationary = true;
        [SerializeField] private float carvingMoveThreshold = 0.1f;
        [SerializeField] private float carvingTimeToStationary = 0.5f;

        [Header("Obstacle Shape")]
        [SerializeField] private bool autoDetectSize = true;
        [SerializeField] private Vector3 manualSize = Vector3.one;
        [SerializeField] private Vector3 manualCenter = Vector3.zero;

        private NavMeshObstacle obstacle;
        private Building building;

        private void Awake()
        {
            SetupNavMeshObstacle();
        }

        private void SetupNavMeshObstacle()
        {
            // Get or add NavMeshObstacle component
            obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle == null)
            {
                obstacle = gameObject.AddComponent<NavMeshObstacle>();
            }

            building = GetComponent<Building>();

            ConfigureObstacle();
        }

        private void ConfigureObstacle()
        {
            if (obstacle == null)
                return;

            // Use box shape for buildings
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.carving = carveNavMesh;

            if (autoDetectSize)
            {
                DetectAndSetSize();
            }
            else
            {
                obstacle.size = manualSize;
                obstacle.center = manualCenter;
            }

            // Configure carving settings
            if (carveNavMesh)
            {
                obstacle.carveOnlyStationary = carveOnlyWhenStationary;
                obstacle.carvingMoveThreshold = carvingMoveThreshold;
                obstacle.carvingTimeToStationary = carvingTimeToStationary;
            }

            Debug.Log($"BuildingNavMeshObstacle configured for {gameObject.name} with size {obstacle.size}");
        }

        private void DetectAndSetSize()
        {
            // Try to get bounds from collider
            Collider buildingCollider = GetComponent<Collider>();
            if (buildingCollider != null)
            {
                Bounds bounds = buildingCollider.bounds;
                obstacle.size = bounds.size;
                obstacle.center = bounds.center - transform.position;
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

                obstacle.size = combinedBounds.size;
                obstacle.center = combinedBounds.center - transform.position;
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

                obstacle.size = combinedBounds.size;
                obstacle.center = combinedBounds.center - transform.position;
                return;
            }

            // Fallback to default size
            Debug.LogWarning($"BuildingNavMeshObstacle: Could not auto-detect size for {gameObject.name}, using default");
            obstacle.size = Vector3.one * 5f; // Default building size
            obstacle.center = Vector3.zero;
        }

        private void OnValidate()
        {
            // Update settings when changed in inspector
            if (Application.isPlaying && obstacle != null)
            {
                obstacle.carving = carveNavMesh;
                obstacle.carveOnlyStationary = carveOnlyWhenStationary;
                obstacle.carvingMoveThreshold = carvingMoveThreshold;
                obstacle.carvingTimeToStationary = carvingTimeToStationary;

                if (!autoDetectSize)
                {
                    obstacle.size = manualSize;
                    obstacle.center = manualCenter;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (obstacle != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange for buildings
                Gizmos.DrawWireCube(transform.position + obstacle.center, obstacle.size);
            }
        }
#endif
    }
}
