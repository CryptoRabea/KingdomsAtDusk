using UnityEngine;

namespace RTS.Core
{
    /// <summary>
    /// Defines the playable area for the game. This single component controls bounds for:
    /// - Minimap camera view
    /// - Main camera movement limits
    /// - Fog of War grid positioning
    ///
    /// Use the red gizmo in the Scene view to visually scale and position the play area.
    /// </summary>
    [ExecuteAlways]
    public class PlayAreaBounds : MonoBehaviour
    {
        [Header("Play Area Size")]
        [Tooltip("Size of the play area in world units (X = width, Y = depth/Z)")]
        [SerializeField] private Vector2 size = new Vector2(200f, 200f);

        [Header("Gizmo Settings")]
        [SerializeField] private Color gizmoColor = new Color(1f, 0f, 0f, 0.5f);
        [SerializeField] private Color gizmoWireColor = Color.red;
        [SerializeField] private bool showGizmo = true;
        [SerializeField] private float gizmoHeight = 50f;

        // Singleton for easy access
        private static PlayAreaBounds _instance;
        public static PlayAreaBounds Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PlayAreaBounds>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (Application.isPlaying)
            {
                _instance = this;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Center of the play area in world space (Y is always 0)
        /// </summary>
        public Vector3 Center => transform.position;

        /// <summary>
        /// Size of the play area (X = width, Y = depth/Z in world space)
        /// </summary>
        public Vector2 Size => size;

        /// <summary>
        /// Half size for bounds calculations
        /// </summary>
        public Vector2 HalfSize => size * 0.5f;

        /// <summary>
        /// Minimum world coordinates (X, Z)
        /// </summary>
        public Vector2 WorldMin => new Vector2(
            transform.position.x - size.x * 0.5f,
            transform.position.z - size.y * 0.5f
        );

        /// <summary>
        /// Maximum world coordinates (X, Z)
        /// </summary>
        public Vector2 WorldMax => new Vector2(
            transform.position.x + size.x * 0.5f,
            transform.position.z + size.y * 0.5f
        );

        /// <summary>
        /// Get bounds as Unity Bounds object
        /// </summary>
        public Bounds GetBounds()
        {
            return new Bounds(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(size.x, 0, size.y)
            );
        }

        /// <summary>
        /// Check if a world position is within the play area
        /// </summary>
        public bool ContainsPoint(Vector3 worldPosition)
        {
            Vector2 min = WorldMin;
            Vector2 max = WorldMax;
            return worldPosition.x >= min.x && worldPosition.x <= max.x &&
                   worldPosition.z >= min.y && worldPosition.z <= max.y;
        }

        /// <summary>
        /// Clamp a world position to within the play area
        /// </summary>
        public Vector3 ClampPosition(Vector3 worldPosition)
        {
            Vector2 min = WorldMin;
            Vector2 max = WorldMax;
            return new Vector3(
                Mathf.Clamp(worldPosition.x, min.x, max.x),
                worldPosition.y,
                Mathf.Clamp(worldPosition.z, min.y, max.y)
            );
        }

        /// <summary>
        /// Convert world position to normalized (0-1) position within play area
        /// </summary>
        public Vector2 WorldToNormalized(Vector3 worldPosition)
        {
            Vector2 min = WorldMin;
            Vector2 max = WorldMax;
            return new Vector2(
                Mathf.InverseLerp(min.x, max.x, worldPosition.x),
                Mathf.InverseLerp(min.y, max.y, worldPosition.z)
            );
        }

        /// <summary>
        /// Convert normalized (0-1) position to world position
        /// </summary>
        public Vector3 NormalizedToWorld(Vector2 normalizedPosition, float y = 0f)
        {
            Vector2 min = WorldMin;
            Vector2 max = WorldMax;
            return new Vector3(
                Mathf.Lerp(min.x, max.x, normalizedPosition.x),
                y,
                Mathf.Lerp(min.y, max.y, normalizedPosition.y)
            );
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showGizmo) return;
            DrawPlayAreaGizmo(false);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmo) return;
            DrawPlayAreaGizmo(true);
        }

        private void DrawPlayAreaGizmo(bool selected)
        {
            Vector3 center = new Vector3(transform.position.x, transform.position.y + gizmoHeight * 0.5f, transform.position.z);
            Vector3 gizmoSize = new Vector3(size.x, gizmoHeight, size.y);

            // Draw solid box (semi-transparent)
            Gizmos.color = selected ? new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoColor.a * 0.3f) : gizmoColor;
            Gizmos.DrawCube(center, gizmoSize);

            // Draw wire frame
            Gizmos.color = selected ? Color.yellow : gizmoWireColor;
            Gizmos.DrawWireCube(center, gizmoSize);

            // Draw corner posts for better visibility
            float postHeight = gizmoHeight;
            Vector3[] corners = new Vector3[]
            {
                new Vector3(transform.position.x - size.x * 0.5f, transform.position.y, transform.position.z - size.y * 0.5f),
                new Vector3(transform.position.x + size.x * 0.5f, transform.position.y, transform.position.z - size.y * 0.5f),
                new Vector3(transform.position.x + size.x * 0.5f, transform.position.y, transform.position.z + size.y * 0.5f),
                new Vector3(transform.position.x - size.x * 0.5f, transform.position.y, transform.position.z + size.y * 0.5f),
            };

            Gizmos.color = gizmoWireColor;
            foreach (var corner in corners)
            {
                Gizmos.DrawLine(corner, corner + Vector3.up * postHeight);
            }

            // Draw ground plane outline
            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]);
            Gizmos.DrawLine(corners[3], corners[0]);

            // Draw center cross on ground
            float crossSize = Mathf.Min(size.x, size.y) * 0.1f;
            Vector3 groundCenter = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(groundCenter + Vector3.left * crossSize, groundCenter + Vector3.right * crossSize);
            Gizmos.DrawLine(groundCenter + Vector3.back * crossSize, groundCenter + Vector3.forward * crossSize);

            // Draw label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                center + Vector3.up * gizmoHeight * 0.5f,
                $"Play Area\n{size.x}x{size.y}"
            );
            #endif
        }
#endif
    }
}
