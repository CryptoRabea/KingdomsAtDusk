using UnityEngine;
using UnityEngine.AI;

namespace RTS.Buildings
{
    /// <summary>
    /// Automatically adds and configures NavMeshObstacle for wall segments.
    /// This ensures walls block unit navigation properly.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WallNavMeshObstacle : MonoBehaviour
    {
        [Header("NavMesh Settings")]
        [SerializeField] private bool carveNavMesh = true;
        [SerializeField] private bool moveThreshold = true;
        [SerializeField] private float carvingMoveThreshold = 0.1f;
        [SerializeField] private float carvingTimeToStationary = 0.5f;

        private NavMeshObstacle obstacle;
        private Collider wallCollider;

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

            // Get the collider to match its bounds
            wallCollider = GetComponent<Collider>();
            if (wallCollider == null)
            {
                Debug.LogError($"WallNavMeshObstacle: No collider found on {gameObject.name}!");
                return;
            }

            // Configure NavMeshObstacle to match collider bounds
            ConfigureObstacle();
        }

        private void ConfigureObstacle()
        {
            if (obstacle == null || wallCollider == null)
                return;

            // Use box shape for walls
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.carving = carveNavMesh;

            // Match the collider bounds
            Bounds bounds = wallCollider.bounds;
            Vector3 size = bounds.size;

            // Set the obstacle size
            obstacle.size = size;

            // Center the obstacle
            obstacle.center = wallCollider.bounds.center - transform.position;

            // Configure carving settings
            if (carveNavMesh)
            {
                obstacle.carveOnlyStationary = moveThreshold;
                obstacle.carvingMoveThreshold = carvingMoveThreshold;
                obstacle.carvingTimeToStationary = carvingTimeToStationary;
            }

            Debug.Log($"WallNavMeshObstacle configured for {gameObject.name} with size {size}");
        }

        private void OnValidate()
        {
            // Update settings when changed in inspector
            if (Application.isPlaying && obstacle != null)
            {
                obstacle.carving = carveNavMesh;
                obstacle.carveOnlyStationary = moveThreshold;
                obstacle.carvingMoveThreshold = carvingMoveThreshold;
                obstacle.carvingTimeToStationary = carvingTimeToStationary;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (obstacle != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position + obstacle.center, obstacle.size);
            }
        }
#endif
    }
}
