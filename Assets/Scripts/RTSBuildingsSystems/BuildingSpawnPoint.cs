using UnityEngine;

namespace RTSBuildingsSystems
{
    /// <summary>
    /// Marks the spawn point location for units trained in buildings.
    /// This component should be attached to a child object of the building prefab.
    /// </summary>
    public class BuildingSpawnPoint : MonoBehaviour
    {
        [Header("Spawn Point Settings")]
        [SerializeField] private Color gizmoColor = Color.cyan;
        [SerializeField] private float gizmoRadius = 0.5f;

        /// <summary>
        /// Gets the world position where units should spawn.
        /// </summary>
        public Vector3 Position => transform.position;

        /// <summary>
        /// Gets the spawn point transform.
        /// </summary>
        public Transform Transform => transform;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Draw spawn point indicator in editor
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);

            // Draw direction arrow if this has a parent
            if (transform.parent != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.parent.position, transform.position);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw more prominent indicator when selected
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, gizmoRadius * 0.5f);
        }
#endif
    }
}
