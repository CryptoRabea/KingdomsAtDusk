/*
 * MinimapFogOfWarIntegration.cs
 * Integrates Fog of War with Minimap system
 * Hides enemy markers that are not in revealed fog areas
 * 
 * IMPORTANT: Attach to the same GameObject as MiniMapControllerPro
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using FischlWorks_FogWar;
using RTS.UI;

namespace RTS.FogOfWar
{
    /// <summary>
    /// Filters minimap markers based on fog of war visibility.
    /// Enemy units/buildings only show on minimap if they're in revealed fog.
    /// Friendly units/buildings always show.
    /// </summary>
    [RequireComponent(typeof(MiniMapControllerPro))]
    public class MinimapFogOfWarIntegration : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private csFogWar fogWarSystem;
        [Tooltip("If null, will search for csFogWar in scene")]

        [Header("World Bounds (Optional - Auto-detects from Fog System)")]
        [SerializeField] private Vector2 customWorldMin = Vector2.zero;
        [Tooltip("Leave at 0 to auto-calculate from fog system")]
        
        [SerializeField] private Vector2 customWorldMax = Vector2.zero;
        [Tooltip("Leave at 0 to auto-calculate from fog system")]

        [Header("Visibility Settings")]
        [SerializeField] private int visibilityCheckRadius = 0;
        [Tooltip("Additional radius around marker position to check (0 = exact position only)")]

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
        private int frameCounter = 0;

        // Cache of marker visibility states
        private Dictionary<RectTransform, bool> markerVisibilityCache = new Dictionary<RectTransform, bool>();

        #region Initialization

        private void Awake()
        {
            minimapController = GetComponent<MiniMapControllerPro>();

            if (minimapController == null)
            {
                Debug.LogError("[MinimapFogOfWarIntegration] MiniMapControllerPro component required!");
                enabled = false;
                return;
            }

            // Find fog war system if not assigned
            if (fogWarSystem == null)
            {
                fogWarSystem = FindFirstObjectByType<csFogWar>();

                if (fogWarSystem == null)
                {
                    Debug.LogError("[MinimapFogOfWarIntegration] No csFogWar component found in scene!");
                    enabled = false;
                    return;
                }

                if (showDebugLogs)
                    Debug.Log("[MinimapFogOfWarIntegration] Found csFogWar component automatically");
            }
        }

        #endregion

        #region Update Loop

        private void LateUpdate()
        {
            if (fogWarSystem == null || minimapController == null) return;

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
                Debug.Log($"[MinimapFogOfWarIntegration] Markers - Visible: {markerVisibilityCache.Count - hiddenCount}, Hidden: {hiddenCount}");
            }
        }

        #endregion

        #region Visibility Management

        private void UpdateMarkerVisibility()
        {
            // Try to get marker containers through serialized fields
            // This requires the containers to be public or we find them by name
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
                Debug.LogWarning("[MinimapFogOfWarIntegration] Could not find marker containers. Check minimap hierarchy.");
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
            // Compare with enemy colors from config
            // Enemy markers are typically red-ish
            // This heuristic can be adjusted via enemyColorThreshold
            
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

            // Get world bounds
            float worldMinX, worldMaxX, worldMinZ, worldMaxZ;

            if (customWorldMin != Vector2.zero || customWorldMax != Vector2.zero)
            {
                // Use custom bounds if specified
                worldMinX = customWorldMin.x;
                worldMaxX = customWorldMax.x;
                worldMinZ = customWorldMin.y;
                worldMaxZ = customWorldMax.y;
            }
            else
            {
                // Auto-calculate from fog war system
                worldMinX = fogWarSystem._LevelMidPoint.position.x - (fogWarSystem._UnitScale * fogWarSystem.levelData.levelDimensionX / 2f);
                worldMaxX = fogWarSystem._LevelMidPoint.position.x + (fogWarSystem._UnitScale * fogWarSystem.levelData.levelDimensionX / 2f);
                worldMinZ = fogWarSystem._LevelMidPoint.position.z - (fogWarSystem._UnitScale * fogWarSystem.levelData.levelDimensionY / 2f);
                worldMaxZ = fogWarSystem._LevelMidPoint.position.z + (fogWarSystem._UnitScale * fogWarSystem.levelData.levelDimensionY / 2f);
            }

            // Convert to world position
            float worldX = Mathf.Lerp(worldMinX, worldMaxX, normalizedX);
            float worldZ = Mathf.Lerp(worldMinZ, worldMaxZ, normalizedY);

            return new Vector3(worldX, 0, worldZ);
        }

        private bool CheckFogVisibility(Vector3 worldPosition)
        {
            if (fogWarSystem == null) return true; // Fallback: show if no fog system

            // Check if position is within fog grid range
            if (!fogWarSystem.CheckWorldGridRange(worldPosition))
            {
                // Position outside fog grid - treat as not visible
                return false;
            }

            // Use fog war system's CheckVisibility method
            // This checks if the position is currently revealed (not explored, but actively visible)
            bool isVisible = fogWarSystem.CheckVisibility(worldPosition, visibilityCheckRadius);

            return isVisible;
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
                    Debug.Log($"[MinimapFogOfWarIntegration] Marker at {marker.anchoredPosition} visibility changed: {visible}");
            }
        }

        #endregion

        #region Public API

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
            Debug.Log("[MinimapFogOfWarIntegration] Forced visibility update");
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
        }
#endif

        #endregion
    }
}
