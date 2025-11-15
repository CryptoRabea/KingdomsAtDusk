using Unity.AI.Navigation;
using UnityEngine;

namespace RTS.Buildings
{
    /// <summary>
    /// Handles stairs/ramps for walls using NavMeshLink.
    /// Allows units to traverse from ground to wall top and vice versa.
    /// </summary>
    [RequireComponent(typeof(NavMeshLink))]
    public class WallStairs : MonoBehaviour
    {
        [Header("Stair Settings")]
        [SerializeField] private float wallHeight = 3f;
        [SerializeField] private float stairWidth = 2f;
        [SerializeField] private float stairDepth = 3f;
        [SerializeField] private bool bidirectional = true;
        [SerializeField] private int areaMask = -1; // All areas

        [Header("Visual Settings")]
        [SerializeField] private GameObject stairMeshPrefab;
        [SerializeField] private bool createDefaultVisual = true;

        private NavMeshLink navMeshLink;
        private GameObject visualMesh;

        private void Awake()
        {
            SetupNavMeshLink();
            SetupVisual();
        }

        private void SetupNavMeshLink()
        {
            navMeshLink = GetComponent<NavMeshLink>();
            if (navMeshLink == null)
            {
                navMeshLink = gameObject.AddComponent<NavMeshLink>();
            }

            // Configure the NavMeshLink
            navMeshLink.bidirectional = bidirectional;
            navMeshLink.area = areaMask;
            navMeshLink.autoUpdate = false;

            // Set start point (ground level)
            navMeshLink.startPoint = Vector3.zero;

            // Set end point (top of wall)
            navMeshLink.endPoint = new Vector3(0, wallHeight, stairDepth);

            // Set width
            navMeshLink.width = stairWidth;

            Debug.Log($"WallStairs NavMeshLink configured: {gameObject.name}");
        }

        private void SetupVisual()
        {
            if (stairMeshPrefab != null)
            {
                visualMesh = Instantiate(stairMeshPrefab, transform);
                visualMesh.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            else if (createDefaultVisual)
            {
                CreateDefaultStairVisual();
            }
        }

        private void CreateDefaultStairVisual()
        {
            // Create a simple ramp as default visual
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.transform.SetParent(transform);
            ramp.name = "StairRamp";

            // Position and scale the ramp
            ramp.transform.localPosition = new Vector3(0, wallHeight * 0.5f, stairDepth * 0.5f);
            ramp.transform.localScale = new Vector3(stairWidth, 0.2f, stairDepth);

            // Rotate to create ramp angle
            float angle = Mathf.Atan2(wallHeight, stairDepth) * Mathf.Rad2Deg;
            ramp.transform.localRotation = Quaternion.Euler(-angle, 0, 0);

            // Remove collider (NavMeshLink handles navigation)
            if (ramp.TryGetComponent<Collider>(out var collider))
            {
                Destroy(collider);
            }

            visualMesh = ramp;

            Debug.Log($"Created default stair visual for {gameObject.name}");
        }

        /// <summary>
        /// Update the stair configuration (useful for dynamic walls)
        /// </summary>
        public void UpdateStairConfiguration(float newWallHeight, float newDepth)
        {
            wallHeight = newWallHeight;
            stairDepth = newDepth;

            if (navMeshLink != null)
            {
                navMeshLink.endPoint = new Vector3(0, wallHeight, stairDepth);
            }

            // Update visual if needed
            if (visualMesh != null && createDefaultVisual)
            {
                Destroy(visualMesh);
                CreateDefaultStairVisual();
            }
        }

        /// <summary>
        /// Set custom start and end points for the NavMeshLink
        /// </summary>
        public void SetCustomPoints(Vector3 startPoint, Vector3 endPoint)
        {
            if (navMeshLink != null)
            {
                navMeshLink.startPoint = startPoint;
                navMeshLink.endPoint = endPoint;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (navMeshLink == null)
                navMeshLink = GetComponent<NavMeshLink>();

            if (navMeshLink != null)
            {
                Vector3 start = transform.position + navMeshLink.startPoint;
                Vector3 end = transform.position + navMeshLink.endPoint;

                // Draw the link
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(start, end);
                Gizmos.DrawWireSphere(start, 0.3f);
                Gizmos.DrawWireSphere(end, 0.3f);

                // Draw width indicator
                Gizmos.color = Color.blue;
                Vector3 widthOffset = Vector3.right * (stairWidth * 0.5f);
                Gizmos.DrawLine(start - widthOffset, start + widthOffset);
                Gizmos.DrawLine(end - widthOffset, end + widthOffset);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw detailed info when selected
            if (navMeshLink != null)
            {
                Vector3 start = transform.position + navMeshLink.startPoint;
                Vector3 end = transform.position + navMeshLink.endPoint;

                Gizmos.color = new Color(0, 1, 1, 0.3f);

                // Draw traversable area
                Vector3[] corners = new Vector3[4];
                Vector3 widthOffset = Vector3.right * (stairWidth * 0.5f);
                corners[0] = start - widthOffset;
                corners[1] = start + widthOffset;
                corners[2] = end + widthOffset;
                corners[3] = end - widthOffset;

                for (int i = 0; i < 4; i++)
                {
                    Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
                }
            }
        }
#endif
    }
}
