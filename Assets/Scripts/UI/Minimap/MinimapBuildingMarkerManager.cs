using UnityEngine;
using UnityEngine.UI;

namespace RTS.UI.Minimap
{
    /// <summary>
    /// Manages building markers on the minimap with object pooling.
    /// Handles friendly and enemy buildings with different colors.
    /// </summary>
    public class MinimapBuildingMarkerManager : MinimapMarkerManager
    {
        private readonly MinimapMarkerPool friendlyPool;
        private readonly MinimapMarkerPool enemyPool;
        private readonly GameObject markerPrefab;
        private readonly RectTransform container;

        public MinimapBuildingMarkerManager(
            MinimapConfig config,
            RectTransform minimapRect,
            RectTransform container,
            GameObject markerPrefab = null)
            : base(config, minimapRect)
        {
            this.container = container;
            this.markerPrefab = markerPrefab;

            // Create object pools for friendly and enemy buildings
            friendlyPool = new MinimapMarkerPool(
                markerPrefab,
                container,
                config.buildingMarkerPoolSize / 2,
                config.buildingMarkerSize,
                config.friendlyBuildingColor
            );

            enemyPool = new MinimapMarkerPool(
                markerPrefab,
                container,
                config.buildingMarkerPoolSize / 2,
                config.buildingMarkerSize,
                config.enemyBuildingColor
            );
        }

        public override void AddMarker(GameObject building, Vector3 worldPosition, bool isEnemy = false)
        {
            if (building == null) return;
            if (markers.ContainsKey(building)) return;

            // Get marker from appropriate pool
            MinimapMarkerPool pool = isEnemy ? enemyPool : friendlyPool;
            RectTransform marker = pool.Get();

            // Ensure marker has correct color
            if (marker.TryGetComponent<Image>(out var img))
            {
                img.color = isEnemy ? config.enemyBuildingColor : config.friendlyBuildingColor;
            }

            // Set size
            marker.sizeDelta = new Vector2(config.buildingMarkerSize, config.buildingMarkerSize);

            // Store marker reference
            markers[building] = marker;

            // Update position
            UpdateMarkerPosition(marker, worldPosition);

            SetDirty();
        }

        public override void RemoveMarker(GameObject building)
        {
            if (building == null) return;
            if (!markers.TryGetValue(building, out RectTransform marker)) return;

            if (marker != null)
            {
                // Determine which pool to return to based on color
                if (marker.TryGetComponent<Image>(out var img))
                {
                }
                bool isEnemy = img != null && img.color == config.enemyBuildingColor;

                MinimapMarkerPool pool = isEnemy ? enemyPool : friendlyPool;
                pool.Return(marker);
            }

            markers.Remove(building);
            SetDirty();
        }

        public override void ClearAll()
        {
            // Return all markers to pools
            foreach (var kvp in markers)
            {
                if (kvp.Value != null)
                {
                    if (kvp.Value.TryGetComponent<Image>(out var img))
                    {
                    }
                    bool isEnemy = img != null && img.color == config.enemyBuildingColor;

                    MinimapMarkerPool pool = isEnemy ? enemyPool : friendlyPool;
                    pool.Return(kvp.Value);
                }
            }

            markers.Clear();
            SetDirty();
        }

        /// <summary>
        /// Clean up and destroy all pooled markers.
        /// </summary>
        public void Dispose()
        {
            ClearAll();
            friendlyPool.Clear();
            enemyPool.Clear();
        }

        /// <summary>
        /// Get pool statistics for debugging.
        /// </summary>
        public string GetPoolStats()
        {
            return $"Building Markers - Friendly Pool: {friendlyPool.ActiveCount}/{friendlyPool.TotalCount}, " +
                   $"Enemy Pool: {enemyPool.ActiveCount}/{enemyPool.TotalCount}";
        }
    }
}
