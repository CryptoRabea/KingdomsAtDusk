using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTS.UI.Minimap
{
    /// <summary>
    /// High-performance object pool for minimap markers.
    /// Reduces GC pressure by reusing marker GameObjects instead of creating/destroying them.
    /// </summary>
    public class MinimapMarkerPool
    {
        private readonly GameObject markerPrefab;
        private readonly RectTransform container;
        private readonly Queue<RectTransform> availableMarkers = new Queue<RectTransform>();
        private readonly List<RectTransform> activeMarkers = new List<RectTransform>();
        private readonly float markerSize;
        private readonly Color markerColor;

        public int ActiveCount => activeMarkers.Count;
        public int AvailableCount => availableMarkers.Count;
        public int TotalCount => ActiveCount + AvailableCount;

        public MinimapMarkerPool(GameObject prefab, RectTransform parent, int initialSize, float size, Color color)
        {
            markerPrefab = prefab;
            container = parent;
            markerSize = size;
            markerColor = color;

            // Pre-allocate markers
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewMarker();
            }
        }

        /// <summary>
        /// Get a marker from the pool, creating a new one if necessary.
        /// </summary>
        public RectTransform Get()
        {
            RectTransform marker;

            if (availableMarkers.Count > 0)
            {
                marker = availableMarkers.Dequeue();
            }
            else
            {
                marker = CreateNewMarker();
            }

            marker.gameObject.SetActive(true);
            activeMarkers.Add(marker);

            return marker;
        }

        /// <summary>
        /// Return a marker to the pool for reuse.
        /// </summary>
        public void Return(RectTransform marker)
        {
            if (marker == null) return;

            marker.gameObject.SetActive(false);
            activeMarkers.Remove(marker);
            availableMarkers.Enqueue(marker);
        }

        /// <summary>
        /// Return all active markers to the pool.
        /// </summary>
        public void ReturnAll()
        {
            for (int i = activeMarkers.Count - 1; i >= 0; i--)
            {
                RectTransform marker = activeMarkers[i];
                marker.gameObject.SetActive(false);
                availableMarkers.Enqueue(marker);
            }
            activeMarkers.Clear();
        }

        /// <summary>
        /// Clear the entire pool and destroy all markers.
        /// </summary>
        public void Clear()
        {
            // Destroy active markers
            foreach (var marker in activeMarkers)
            {
                if (marker != null)
                {
                    Object.Destroy(marker.gameObject);
                }
            }
            activeMarkers.Clear();

            // Destroy available markers
            while (availableMarkers.Count > 0)
            {
                RectTransform marker = availableMarkers.Dequeue();
                if (marker != null)
                {
                    Object.Destroy(marker.gameObject);
                }
            }
        }

        private RectTransform CreateNewMarker()
        {
            GameObject markerObj;

            if (markerPrefab != null)
            {
                markerObj = Object.Instantiate(markerPrefab, container);
            }
            else
            {
                // Create default marker
                markerObj = new GameObject("MinimapMarker");
                markerObj.transform.SetParent(container, false);

                Image img = markerObj.AddComponent<Image>();
                img.color = markerColor;
            }

            RectTransform rect = markerObj.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = markerObj.AddComponent<RectTransform>();
            }

            rect.sizeDelta = new Vector2(markerSize, markerSize);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            markerObj.SetActive(false);
            availableMarkers.Enqueue(rect);

            return rect;
        }
    }
}
