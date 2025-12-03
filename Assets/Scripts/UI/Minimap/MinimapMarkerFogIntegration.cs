using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using KingdomsAtDusk.FogOfWar;

namespace RTS.UI.Minimap
{
    /// <summary>
    /// Integrates minimap markers with the FogOfWarManager system.
    /// Hides enemy unit/building markers based on fog of war visibility.
    /// Friendly units always show. Enemy units only show in visible (not explored) areas.
    ///
    /// Attach this to the same GameObject as MiniMapControllerPro.
    /// </summary>
    [RequireComponent(typeof(MiniMapControllerPro))]
    public class MinimapMarkerFogIntegration : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool hideInExplored = true;
        [Tooltip("Hide enemy markers in explored (but not currently visible) areas. Standard RTS behavior.")]

        [SerializeField] private int updateInterval = 2;
        [Tooltip("Update fog visibility every N frames (higher = better performance)")]
        [Range(1, 10)]

        [Header("Enemy Detection")]
        [SerializeField] private float enemyColorThreshold = 0.2f;
        [Tooltip("Color threshold for detecting enemy markers (markers with R > G+threshold are considered enemies)")]
        [Range(0f, 1f)]

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;
        [SerializeField] private bool showVisibilityStats = false;

        private MiniMapControllerPro minimapController;
        private MinimapConfig minimapConfig;
        private int frameCounter = 0;

        // Cache of marker visibility states
        private Dictionary<RectTransform, bool> markerVisibilityCache = new Dictionary<RectTransform, bool>();

        // World bounds - calculated from minimap config
        private Vector2 worldMin;
        private Vector2 worldMax;

        #region Initialization

        private void Awake()
        {
            minimapController = GetComponent<MiniMapControllerPro>();

            if (minimapController == null)
            {
                Debug.LogError("[MinimapMarkerFogIntegration] MiniMapControllerPro component required!");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            // Get minimap config from the controller
            // Note: We need to access this via reflection or make it public
            // For now, we'll use default world bounds

            // Check if fog of war is available
            if (FogOfWarManager.Instance == null)
            {
                Debug.LogWarning("[MinimapMarkerFogIntegration] No FogOfWarManager found in scene. Disabling fog integration.");
                enabled = false;
                return;
            }

            // Use fog of war world bounds from GameBoundary
            Bounds bounds = FogOfWarManager.Instance.Boundary.Bounds;
            worldMin = new Vector2(bounds.min.x, bounds.min.z);
            worldMax = new Vector2(bounds.max.x, bounds.max.z);

            if (showDebugLogs)
                Debug.Log($"[MinimapMarkerFogIntegration] Initialized with world bounds: {worldMin} to {worldMax}");
        }

        #endregion

        #region Update Loop

        private void LateUpdate()
        {
            if (FogOfWarManager.Instance == null || minimapController == null) return;

            // Update at intervals for performance
            frameCounter++;
            if (frameCounter < updateInterval)
                return;

            frameCounter = 0;

            // Update marker visibility based on fog
            UpdateMarkerVisibility();

            if (showVisibilityStats && Time.frameCount % 60 == 0)
            {
                int hiddenCount = 0;
                foreach (var visibility in markerVisibilityCache.Values)
                {
                    if (!visibility) hiddenCount++;
                }
                Debug.Log($"[MinimapMarkerFogIntegration] Markers - Visible: {markerVisibilityCache.Count - hiddenCount}, Hidden: {hiddenCount}");
            }
        }

        #endregion

        #region Visibility Management

        private void UpdateMarkerVisibility()
        {
            // Find marker containers
            Transform buildingContainer = null;
            Transform unitContainer = null;

            // Try to find containers as children of the minimap controller
            foreach (Transform child in minimapController.transform)
            {
                if (child.name.Contains("Building") && child.name.Contains("Marker"))
                {
                    buildingContainer = child;
                }
                else if (child.name.Contains("Unit") && child.name.Contains("Marker"))
                {
                    unitContainer = child;
                }
            }

            // Also check the minimap panel itself
            var minimapPanel = minimapController.transform.Find("MiniMap");
            if (minimapPanel != null)
            {
                if (buildingContainer == null)
                    buildingContainer = minimapPanel.Find("BuildingMarkers");
                if (unitContainer == null)
                    unitContainer = minimapPanel.Find("UnitMarkers");
            }

            if (buildingContainer != null)
            {
                UpdateContainerMarkers(buildingContainer);
            }

            if (unitContainer != null)
            {
                UpdateContainerMarkers(unitContainer);
            }

            if (buildingContainer == null && unitContainer == null && showDebugLogs)
            {
                Debug.LogWarning("[MinimapMarkerFogIntegration] Could not find marker containers. Check minimap hierarchy.");
            }
        }

        private void UpdateContainerMarkers(Transform container)
        {
            // Iterate through all active markers in the container
            for (int i = 0; i < container.childCount; i++)
            {
                var markerTransform = container.GetChild(i);

                // Skip inactive markers (pooled)
                if (!markerTransform.gameObject.activeSelf)
                    continue;

                var rectTransform = markerTransform as RectTransform;
                if (rectTransform == null) continue;

                // Get marker Image component to check color (enemy vs friendly)
                if (!rectTransform.TryGetComponent<Image>(out var markerImage)) continue;

                // Determine if this is an enemy marker (red/enemy color)
                bool isEnemyMarker = IsEnemyColor(markerImage.color);

                // Friendly markers always visible
                if (!isEnemyMarker)
                {
                    SetMarkerVisibility(rectTransform, true);
                    continue;
                }

                // For enemy markers, check fog visibility
                Vector3 worldPosition = MinimapToWorldPosition(rectTransform);
                bool isVisible = CheckFogVisibility(worldPosition);

                SetMarkerVisibility(rectTransform, isVisible);
            }
        }

        private bool IsEnemyColor(Color markerColor)
        {
            // Enemy markers are typically red-ish
            // Check if marker is significantly more red than green/blue
            bool isRedish = markerColor.r > markerColor.g + enemyColorThreshold &&
                           markerColor.r > markerColor.b + enemyColorThreshold;

            return isRedish;
        }

        private Vector3 MinimapToWorldPosition(RectTransform marker)
        {
            // Convert minimap local position to world position
            Vector2 localPos = marker.anchoredPosition;

            // Get minimap dimensions
            RectTransform minimapRect = minimapController.GetComponent<RectTransform>();
            Vector2 minimapSize = minimapRect.rect.size;

            // Normalize position (0-1)
            float normalizedX = (localPos.x + minimapSize.x / 2f) / minimapSize.x;
            float normalizedY = (localPos.y + minimapSize.y / 2f) / minimapSize.y;

            // Convert to world position
            float worldX = Mathf.Lerp(worldMin.x, worldMax.x, normalizedX);
            float worldZ = Mathf.Lerp(worldMin.y, worldMax.y, normalizedY);

            return new Vector3(worldX, 0, worldZ);
        }

        private bool CheckFogVisibility(Vector3 worldPosition)
        {
            if (FogOfWarManager.Instance == null || FogOfWarManager.Instance.Grid == null)
            {
                // No fog of war - show everything
                return true;
            }

            // Get vision state at this position
            VisionState state = FogOfWarManager.Instance.GetVisionState(worldPosition);

            // Only show in visible areas
            if (state == VisionState.Visible)
            {
                return true;
            }

            // If hideInExplored is false, also show in explored areas
            if (!hideInExplored && state == VisionState.Explored)
            {
                return true;
            }

            // Hidden in unexplored or (if hideInExplored) explored areas
            return false;
        }

        private void SetMarkerVisibility(RectTransform marker, bool visible)
        {
            // Cache the visibility state
            bool wasVisible = markerVisibilityCache.ContainsKey(marker) ? markerVisibilityCache[marker] : true;
            markerVisibilityCache[marker] = visible;

            // Only update if visibility changed
            if (wasVisible != visible)
            {
                marker.gameObject.SetActive(visible);

                if (showDebugLogs)
                    Debug.Log($"[MinimapMarkerFogIntegration] Marker at {marker.anchoredPosition} visibility changed: {visible}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set whether to hide enemy markers in explored areas.
        /// </summary>
        public void SetHideInExplored(bool hide)
        {
            if (hideInExplored != hide)
            {
                hideInExplored = hide;
                ForceUpdateVisibility();
            }
        }

        /// <summary>
        /// Force an immediate update of all marker visibility.
        /// </summary>
        public void ForceUpdateVisibility()
        {
            frameCounter = updateInterval; // Force update on next frame
        }

        /// <summary>
        /// Clear the visibility cache (call when minimap markers are reset).
        /// </summary>
        public void ClearVisibilityCache()
        {
            markerVisibilityCache.Clear();
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        [ContextMenu("Force Update Visibility")]
        private void DebugForceUpdate()
        {
            UpdateMarkerVisibility();
            Debug.Log("[MinimapMarkerFogIntegration] Forced visibility update");
        }

        [ContextMenu("Print Visibility Stats")]
        private void DebugPrintStats()
        {
            int visible = 0;
            int hidden = 0;

            foreach (var vis in markerVisibilityCache.Values)
            {
                if (vis) visible++;
                else hidden++;
            }

            Debug.Log($"=== Minimap Fog Visibility Stats ===");
            Debug.Log($"Total Markers: {markerVisibilityCache.Count}");
            Debug.Log($"Visible: {visible}");
            Debug.Log($"Hidden: {hidden}");
            Debug.Log($"Hide In Explored: {hideInExplored}");
        }

        [ContextMenu("Toggle Hide In Explored")]
        private void DebugToggleHideInExplored()
        {
            SetHideInExplored(!hideInExplored);
            Debug.Log($"[MinimapMarkerFogIntegration] Hide In Explored: {hideInExplored}");
        }
#endif

        #endregion
    }
}
