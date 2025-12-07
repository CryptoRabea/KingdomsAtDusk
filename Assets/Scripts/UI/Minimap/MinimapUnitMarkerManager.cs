using UnityEngine;
using UnityEngine.UI;

namespace RTS.UI.Minimap
{
    /// <summary>
    /// Manages unit markers on the minimap with object pooling and circle sprites.
    /// Optimized for handling large numbers of units efficiently.
    /// </summary>
    public class MinimapUnitMarkerManager : MinimapMarkerManager
    {
        private readonly MinimapMarkerPool friendlyPool;
        private readonly MinimapMarkerPool enemyPool;
        private readonly GameObject markerPrefab;
        private readonly RectTransform container;
        private Sprite circleSprite;

        public MinimapUnitMarkerManager(
            MinimapConfig config,
            RectTransform minimapRect,
            RectTransform container,
            GameObject markerPrefab = null)
            : base(config, minimapRect)
        {
            this.container = container;
            this.markerPrefab = markerPrefab;

            // Create circle sprite for unit markers
            circleSprite = CreateCircleSprite();

            // Create object pools for friendly and enemy units
            friendlyPool = new MinimapMarkerPool(
                markerPrefab,
                container,
                config.unitMarkerPoolSize / 2,
                config.unitMarkerSize,
                config.friendlyUnitColor
            );

            enemyPool = new MinimapMarkerPool(
                markerPrefab,
                container,
                config.unitMarkerPoolSize / 2,
                config.unitMarkerSize,
                config.enemyUnitColor
            );
        }

        public override void AddMarker(GameObject unit, Vector3 worldPosition, bool isEnemy = false)
        {
            if (unit == null) return;
            if (markers.ContainsKey(unit)) return;

            // Get marker from appropriate pool
            MinimapMarkerPool pool = isEnemy ? enemyPool : friendlyPool;
            RectTransform marker = pool.Get();

            // Ensure marker has correct visual appearance
            if (marker.TryGetComponent<Image>(out var img))
            {
                img.color = isEnemy ? config.enemyUnitColor : config.friendlyUnitColor;
                img.sprite = circleSprite;
            }

            // Set size
            marker.sizeDelta = new Vector2(config.unitMarkerSize, config.unitMarkerSize);

            // Store marker reference
            markers[unit] = marker;

            // Update position
            UpdateMarkerPosition(marker, worldPosition);

            SetDirty();
        }

        public override void RemoveMarker(GameObject unit)
        {
            if (unit == null) return;
            if (!markers.TryGetValue(unit, out RectTransform marker)) return;

            if (marker != null)
            {
                // Determine which pool to return to based on color
                if (marker.TryGetComponent<Image>(out var img))
                {
                }
                bool isEnemy = img != null && img.color == config.enemyUnitColor;

                MinimapMarkerPool pool = isEnemy ? enemyPool : friendlyPool;
                pool.Return(marker);
            }

            markers.Remove(unit);
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
                    bool isEnemy = img != null && img.color == config.enemyUnitColor;

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

            if (circleSprite != null && circleSprite.texture != null)
            {
                Object.Destroy(circleSprite.texture);
            }
        }

        /// <summary>
        /// Get pool statistics for debugging.
        /// </summary>
        public string GetPoolStats()
        {
            return $"Unit Markers - Friendly Pool: {friendlyPool.ActiveCount}/{friendlyPool.TotalCount}, " +
                   $"Enemy Pool: {enemyPool.ActiveCount}/{enemyPool.TotalCount}";
        }

        /// <summary>
        /// Create a circular sprite for unit markers.
        /// </summary>
        private Sprite CreateCircleSprite()
        {
            int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = dist <= radius ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;

            return Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }
    }
}
