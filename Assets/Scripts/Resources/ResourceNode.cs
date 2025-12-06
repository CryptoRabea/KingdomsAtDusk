using UnityEngine;
using KingdomsAtDusk.Core;
using RTS.Core.Services;

namespace KingdomsAtDusk.Resources
{
    /// <summary>
    /// Represents a resource node that workers can gather from.
    /// Examples: trees, farms, mines, quarries, fishing spots, etc.
    /// </summary>
    public class ResourceNode : MonoBehaviour
    {
        [Header("Resource Configuration")]
        [Tooltip("Type of resource this node provides")]
        public ResourceType resourceType = ResourceType.Wood;

        [Tooltip("How many resources this node contains (-1 for infinite)")]
        public int resourceAmount = 100;

        [Tooltip("Is this an infinite resource node?")]
        public bool isInfinite = false;

        [Header("Gathering Settings")]
        [Tooltip("Maximum number of workers that can gather from this node simultaneously")]
        [Range(1, 10)]
        public int maxWorkers = 3;

        [Tooltip("Gathering positions around the node where workers will stand")]
        public Transform[] gatheringPositions;

        [Header("Visual Feedback")]
        [Tooltip("Optional: Visual effect to play when being gathered from")]
        public GameObject gatheringEffect;

        [Tooltip("Optional: Show resource depletion visually")]
        public bool showDepletionVisuals = false;

        [Tooltip("Optional: Replace with depleted prefab when empty")]
        public GameObject depletedPrefab;

        // Runtime tracking
        private int currentWorkers = 0;
        private bool isDepleted = false;

        /// <summary>
        /// Check if a worker can gather from this node.
        /// </summary>
        public bool CanGatherFrom()
        {
            if (isDepleted) return false;
            if (currentWorkers >= maxWorkers) return false;
            if (!isInfinite && resourceAmount <= 0) return false;
            return true;
        }

        /// <summary>
        /// Register a worker as gathering from this node.
        /// </summary>
        public bool RegisterWorker()
        {
            if (!CanGatherFrom()) return false;
            currentWorkers++;
            return true;
        }

        /// <summary>
        /// Unregister a worker from this node.
        /// </summary>
        public void UnregisterWorker()
        {
            currentWorkers = Mathf.Max(0, currentWorkers - 1);
        }

        /// <summary>
        /// Gather resources from this node.
        /// </summary>
        /// <param name="amount">Amount to gather</param>
        /// <returns>Actual amount gathered</returns>
        public int GatherResources(int amount)
        {
            if (isDepleted) return 0;

            if (isInfinite)
            {
                return amount;
            }

            int actualAmount = Mathf.Min(amount, resourceAmount);
            resourceAmount -= actualAmount;

            if (resourceAmount <= 0)
            {
                Deplete();
            }

            return actualAmount;
        }

        /// <summary>
        /// Get a free gathering position for a worker.
        /// </summary>
        public Vector3 GetGatheringPosition()
        {
            // If we have defined positions, use them
            if (gatheringPositions != null && gatheringPositions.Length > 0)
            {
                // Simple round-robin assignment
                int index = currentWorkers % gatheringPositions.Length;
                return gatheringPositions[index].position;
            }

            // Otherwise, generate a position around the node
            float angle = (360f / maxWorkers) * currentWorkers;
            float radius = 2f; // 2 units from center
            Vector3 offset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius
            );

            return transform.position + offset;
        }

        /// <summary>
        /// Mark this node as depleted.
        /// </summary>
        private void Deplete()
        {
            isDepleted = true;
            currentWorkers = 0;

            if (depletedPrefab != null)
            {
                // Spawn depleted version
                Instantiate(depletedPrefab, transform.position, transform.rotation);
                Destroy(gameObject);
            }
            else if (showDepletionVisuals)
            {
                // Just hide the visual
                var renderers = GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.enabled = false;
                }
            }
        }

        // Visualization in editor
        private void OnDrawGizmos()
        {
            GameConfigSO config = UnityEngine.Resources.Load<GameConfigSO>("GameConfig");
            if (config != null && !config.showDebugGizmos) return;

            // Draw gathering radius
            Gizmos.color = GetResourceColor();
            Gizmos.DrawWireSphere(transform.position, 2f);

            // Draw gathering positions
            if (gatheringPositions != null)
            {
                foreach (var pos in gatheringPositions)
                {
                    if (pos != null)
                    {
                        Gizmos.DrawWireCube(pos.position, Vector3.one * 0.5f);
                    }
                }
            }
        }

        private Color GetResourceColor()
        {
            return resourceType switch
            {
                ResourceType.Wood => new Color(0.6f, 0.4f, 0.2f), // Brown
                ResourceType.Food => new Color(1f, 1f, 0f),      // Yellow
                ResourceType.Gold => new Color(1f, 0.84f, 0f),   // Gold
                ResourceType.Stone => new Color(0.5f, 0.5f, 0.5f), // Gray
                _ => Color.white
            };
        }

        // Get current info for debugging
        public string GetNodeInfo()
        {
            return $"{resourceType} Node: {resourceAmount}/{(isInfinite ? "∞" : resourceAmount.ToString())} " +
                   $"| Workers: {currentWorkers}/{maxWorkers}";
        }
    }
}
