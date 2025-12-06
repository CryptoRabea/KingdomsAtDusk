using System.Collections.Generic;
using UnityEngine;

namespace RTS.UI.Minimap
{
    /// <summary>
    /// Base class for managing minimap markers with performance optimizations.
    /// Implements object pooling, batched updates, and marker culling.
    /// </summary>
    public abstract class MinimapMarkerManager
    {
        protected readonly MinimapConfig config;
        protected readonly RectTransform minimapRect;
        protected readonly Dictionary<GameObject, RectTransform> markers = new Dictionary<GameObject, RectTransform>();

        private readonly List<GameObject> pendingRemoval = new List<GameObject>();
        private int updateIndex = 0;
        private bool isDirty = false;

        // FIX: Cache for camera view bounds to sync with actual camera view
        private Bounds? cachedCameraBounds = null;

        public int MarkerCount => markers.Count;

        protected MinimapMarkerManager(MinimapConfig config, RectTransform minimapRect)
        {
            this.config = config;
            this.minimapRect = minimapRect;
        }

        /// <summary>
        /// Set the actual camera view bounds for accurate marker positioning.
        /// Call this when the minimap camera is set up or when bounds change.
        /// </summary>
        public void SetCameraViewBounds(Bounds bounds)
        {
            cachedCameraBounds = bounds;
        }

        /// <summary>
        /// Add a marker for a game object at the specified world position.
        /// </summary>
        public abstract void AddMarker(GameObject obj, Vector3 worldPosition, bool isEnemy = false);

        /// <summary>
        /// Remove a marker for a game object.
        /// </summary>
        public abstract void RemoveMarker(GameObject obj);

        /// <summary>
        /// Update all marker positions. Call this every frame or at intervals.
        /// </summary>
        public virtual void UpdateMarkers()
        {
            if (markers.Count == 0) return;

            // Clean up null references
            pendingRemoval.Clear();
            foreach (var kvp in markers)
            {
                if (kvp.Key == null)
                {
                    pendingRemoval.Add(kvp.Key);
                }
            }

            foreach (var key in pendingRemoval)
            {
                RemoveMarker(key);
            }

            // Batch update positions
            if (config.maxMarkersPerFrame > 0)
            {
                UpdateMarkersBatched();
            }
            else
            {
                UpdateMarkersAll();
            }
        }

        /// <summary>
        /// Clear all markers.
        /// </summary>
        public abstract void ClearAll();

        /// <summary>
        /// Convert a world position to minimap local position.
        /// </summary>
        protected Vector2 WorldToMinimapPosition(Vector3 worldPosition)
        {
            // FIX: Use camera view bounds if available, otherwise fall back to config bounds
            float minX, maxX, minZ, maxZ;

            if (cachedCameraBounds.HasValue)
            {
                // Use actual camera view bounds for accurate positioning
                Bounds bounds = cachedCameraBounds.Value;
                minX = bounds.min.x;
                maxX = bounds.max.x;
                minZ = bounds.min.z;
                maxZ = bounds.max.z;
            }
            else
            {
                // Fallback to config bounds
                minX = config.worldMin.x;
                maxX = config.worldMax.x;
                minZ = config.worldMin.y;  // worldMin.y represents Z in 2D
                maxZ = config.worldMax.y;  // worldMax.y represents Z in 2D
            }

            // Convert world position to normalized position (0-1)
            Vector2 normalizedPos = new Vector2(
                Mathf.InverseLerp(minX, maxX, worldPosition.x),
                Mathf.InverseLerp(minZ, maxZ, worldPosition.z)
            );

            // Convert to local minimap coordinates
            Vector2 localPos = new Vector2(
                (normalizedPos.x - 0.5f) * minimapRect.rect.width,
                (normalizedPos.y - 0.5f) * minimapRect.rect.height
            );

            return localPos;
        }

        /// <summary>
        /// Check if a marker is within the visible minimap area (for culling).
        /// </summary>
        protected bool IsMarkerVisible(Vector2 normalizedPosition)
        {
            if (!config.enableMarkerCulling) return true;

            float margin = config.cullingMargin;
            return normalizedPosition.x >= -margin && normalizedPosition.x <= 1f + margin &&
                   normalizedPosition.y >= -margin && normalizedPosition.y <= 1f + margin;
        }

        /// <summary>
        /// Update marker position in world space.
        /// </summary>
        protected void UpdateMarkerPosition(RectTransform marker, Vector3 worldPosition)
        {
            if (marker == null) return;

            Vector2 localPos = WorldToMinimapPosition(worldPosition);
            marker.anchoredPosition = localPos;

            // Optional: Cull markers outside visible area
            if (config.enableMarkerCulling)
            {
                Vector2 normalizedPos = new Vector2(
                    Mathf.InverseLerp(config.worldMin.x, config.worldMax.x, worldPosition.x),
                    Mathf.InverseLerp(config.worldMin.y, config.worldMax.y, worldPosition.z)
                );

                bool isVisible = IsMarkerVisible(normalizedPos);
                if (marker.gameObject.activeSelf != isVisible)
                {
                    marker.gameObject.SetActive(isVisible);
                }
            }
        }

        private void UpdateMarkersAll()
        {
            foreach (var kvp in markers)
            {
                if (kvp.Key != null && kvp.Value != null)
                {
                    UpdateMarkerPosition(kvp.Value, kvp.Key.transform.position);
                }
            }
        }

        private void UpdateMarkersBatched()
        {
            int markersUpdated = 0;
            int maxPerFrame = config.maxMarkersPerFrame;

            // Create a list of markers for indexed access
            var markersList = new List<KeyValuePair<GameObject, RectTransform>>(markers);

            while (markersUpdated < maxPerFrame && updateIndex < markersList.Count)
            {
                var kvp = markersList[updateIndex];

                if (kvp.Key != null && kvp.Value != null)
                {
                    UpdateMarkerPosition(kvp.Value, kvp.Key.transform.position);
                }

                updateIndex++;
                markersUpdated++;
            }

            // Reset index when we've updated all markers
            if (updateIndex >= markersList.Count)
            {
                updateIndex = 0;
            }
        }

        /// <summary>
        /// Mark the manager as needing an update.
        /// </summary>
        protected void SetDirty()
        {
            isDirty = true;
        }

        /// <summary>
        /// Check if the manager needs an update.
        /// </summary>
        public bool IsDirty()
        {
            return isDirty;
        }

        /// <summary>
        /// Clear the dirty flag.
        /// </summary>
        protected void ClearDirty()
        {
            isDirty = false;
        }
    }
}
