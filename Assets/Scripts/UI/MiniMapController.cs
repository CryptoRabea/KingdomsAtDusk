using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using RTS.Core.Events;
using System.Collections;
using System.Collections.Generic;

namespace RTS.UI
{
    /// <summary>
    /// Mini-map controller that displays the entire playable area.
    /// Click on the mini-map to smoothly move the camera to that position.
    /// </summary>
    public class MiniMapController : MonoBehaviour, IPointerClickHandler
    {
        [Header("Mini-Map Setup")]
        [SerializeField] private RectTransform miniMapRect;
        [SerializeField] private RawImage miniMapImage;

        [Header("Mini-Map Camera (World Rendering)")]
        [SerializeField] private bool renderWorldMap = true;
        [SerializeField] private Camera miniMapCamera;
        [SerializeField] private int renderTextureSize = 512;
        [SerializeField] private float miniMapCameraHeight = 500f;
        [SerializeField] private LayerMask miniMapLayers = -1;

        [Header("Camera Reference")]
        [SerializeField] private RTSCameraController cameraController;

        [Header("World Bounds")]
        [SerializeField] private Vector2 worldMin = new Vector2(-1000f, -1000f);
        [SerializeField] private Vector2 worldMax = new Vector2(1000f, 1000f);

        [Header("Camera Movement")]
        [SerializeField] private float cameraMoveSpeed = 2f;
        [SerializeField] private bool useSmoothing = true;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Camera Viewport Indicator")]
        [SerializeField] private RectTransform viewportIndicator;
        [SerializeField] private Color viewportColor = new Color(1f, 1f, 1f, 0.3f);

        [Header("Building Markers")]
        [SerializeField] private GameObject buildingMarkerPrefab;
        [SerializeField] private RectTransform buildingMarkersContainer;
        [SerializeField] private Color friendlyBuildingColor = Color.blue;
        [SerializeField] private float buildingMarkerSize = 5f;

        [Header("Unit Markers")]
        [SerializeField] private GameObject unitMarkerPrefab;
        [SerializeField] private RectTransform unitMarkersContainer;
        [SerializeField] private Color friendlyUnitColor = Color.green;
        [SerializeField] private Color enemyUnitColor = Color.red;
        [SerializeField] private float unitMarkerSize = 3f;

        // Internal tracking
        private Dictionary<GameObject, RectTransform> buildingMarkers = new Dictionary<GameObject, RectTransform>();
        private Dictionary<GameObject, RectTransform> unitMarkers = new Dictionary<GameObject, RectTransform>();
        private Coroutine cameraMoveCoroutine;
        private Vector2 worldSize;
        private RenderTexture miniMapRenderTexture;

        private void Awake()
        {
            // Calculate world size
            worldSize = worldMax - worldMin;

            // Auto-find camera controller if not set
            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<RTSCameraController>();
            }

            // Setup mini-map camera and render texture
            if (renderWorldMap)
            {
                SetupMiniMapCamera();
            }

            // Setup viewport indicator
            if (viewportIndicator != null)
            {
                Image img = viewportIndicator.GetComponent<Image>();
                if (img != null)
                {
                    img.color = viewportColor;
                }
            }

            // Create containers if they don't exist
            if (buildingMarkersContainer == null)
            {
                GameObject container = new GameObject("BuildingMarkers");
                buildingMarkersContainer = container.AddComponent<RectTransform>();
                buildingMarkersContainer.SetParent(miniMapRect, false);
                buildingMarkersContainer.anchorMin = Vector2.zero;
                buildingMarkersContainer.anchorMax = Vector2.one;
                buildingMarkersContainer.offsetMin = Vector2.zero;
                buildingMarkersContainer.offsetMax = Vector2.zero;
            }

            if (unitMarkersContainer == null)
            {
                GameObject container = new GameObject("UnitMarkers");
                unitMarkersContainer = container.AddComponent<RectTransform>();
                unitMarkersContainer.SetParent(miniMapRect, false);
                unitMarkersContainer.anchorMin = Vector2.zero;
                unitMarkersContainer.anchorMax = Vector2.one;
                unitMarkersContainer.offsetMin = Vector2.zero;
                unitMarkersContainer.offsetMax = Vector2.zero;
            }
        }

        private void OnEnable()
        {
            // Subscribe to events
            EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
            EventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);
            EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Unsubscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
            EventBus.Unsubscribe<UnitSpawnedEvent>(OnUnitSpawned);
            EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        }

        private void Update()
        {
            UpdateCameraViewportIndicator();
            UpdateBuildingMarkers();
            UpdateUnitMarkers();
        }

        private void OnDestroy()
        {
            // Clean up render texture
            if (miniMapRenderTexture != null)
            {
                miniMapRenderTexture.Release();
                Destroy(miniMapRenderTexture);
            }
        }

        #region Mini-Map Camera Setup

        private void SetupMiniMapCamera()
        {
            // Create render texture
            miniMapRenderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 16);
            miniMapRenderTexture.name = "MiniMapRenderTexture";

            // Assign to raw image
            if (miniMapImage != null)
            {
                miniMapImage.texture = miniMapRenderTexture;
                // Flip the texture vertically to match Unity's RenderTexture orientation
                // This ensures clicks on the minimap correspond to the correct world positions
                miniMapImage.uvRect = new Rect(0, 1, 1, -1);
            }

            // Setup camera
            if (miniMapCamera == null)
            {
                // Create new camera GameObject
                GameObject camObj = new GameObject("MiniMapCamera");
                miniMapCamera = camObj.AddComponent<Camera>();

                // Don't destroy on load if mini-map persists
                // DontDestroyOnLoad(camObj); // Uncomment if needed
            }

            // Configure camera
            miniMapCamera.targetTexture = miniMapRenderTexture;
            miniMapCamera.orthographic = true;
            miniMapCamera.orthographicSize = worldSize.y / 2f; // Cover entire world height

            // Position camera above world center
            Vector3 worldCenter = new Vector3(
                (worldMin.x + worldMax.x) / 2f,
                miniMapCameraHeight,
                (worldMin.y + worldMax.y) / 2f
            );
            miniMapCamera.transform.position = worldCenter;
            miniMapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Look straight down

            // Set render settings
            miniMapCamera.cullingMask = miniMapLayers;
            miniMapCamera.clearFlags = CameraClearFlags.SolidColor;
            miniMapCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark background
            miniMapCamera.depth = -10; // Render before main camera

            // URP-specific setup: Add UniversalAdditionalCameraData component
            var cameraData = miniMapCamera.GetUniversalAdditionalCameraData();
            if (cameraData != null)
            {
                cameraData.renderType = CameraRenderType.Base; // This camera renders independently
                cameraData.requiresColorOption = CameraOverrideOption.On;
                cameraData.requiresDepthOption = CameraOverrideOption.On;
            }

            // Disable audio listener if it has one
            AudioListener listener = miniMapCamera.GetComponent<AudioListener>();
            if (listener != null)
            {
                Destroy(listener);
            }

            Debug.Log($"Mini-map camera setup complete. Rendering {worldSize.x}x{worldSize.y} world area.");
        }

        #endregion

        #region Click Handling

        public void OnPointerClick(PointerEventData eventData)
        {
            if (cameraController == null) return;

            // Convert click position to world position
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                miniMapRect,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint))
            {
                // Convert local mini-map position to normalized position (0-1)
                Vector2 normalizedPos = new Vector2(
                    (localPoint.x + miniMapRect.rect.width * 0.5f) / miniMapRect.rect.width,
                    (localPoint.y + miniMapRect.rect.height * 0.5f) / miniMapRect.rect.height
                );

                // Convert normalized position to world position
                Vector3 worldPos = new Vector3(
                    Mathf.Lerp(worldMin.x, worldMax.x, normalizedPos.x),
                    cameraController.transform.position.y,
                    Mathf.Lerp(worldMin.y, worldMax.y, normalizedPos.y)
                );

                // Move camera to world position
                MoveCameraToPosition(worldPos);
            }
        }

        private void MoveCameraToPosition(Vector3 targetPosition)
        {
            if (cameraMoveCoroutine != null)
            {
                StopCoroutine(cameraMoveCoroutine);
            }

            if (useSmoothing)
            {
                cameraMoveCoroutine = StartCoroutine(SmoothCameraMove(targetPosition));
            }
            else
            {
                cameraController.transform.position = targetPosition;
            }
        }

        private IEnumerator SmoothCameraMove(Vector3 targetPosition)
        {
            Vector3 startPosition = cameraController.transform.position;
            float distance = Vector3.Distance(startPosition, targetPosition);
            float duration = distance / (cameraMoveSpeed * 10f); // Adjust speed
            duration = Mathf.Clamp(duration, 0.3f, 2f); // Clamp between 0.3 and 2 seconds

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curveValue = movementCurve.Evaluate(t);

                cameraController.transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);

                yield return null;
            }

            cameraController.transform.position = targetPosition;
            cameraMoveCoroutine = null;
        }

        #endregion

        #region Camera Viewport Indicator

        private void UpdateCameraViewportIndicator()
        {
            if (viewportIndicator == null || cameraController == null) return;

            // Get camera position in world space
            Vector3 camPos = cameraController.transform.position;

            // Convert camera position to mini-map position
            Vector2 normalizedPos = new Vector2(
                Mathf.InverseLerp(worldMin.x, worldMax.x, camPos.x),
                Mathf.InverseLerp(worldMin.y, worldMax.y, camPos.z)
            );

            // Convert to local mini-map coordinates
            Vector2 localPos = new Vector2(
                (normalizedPos.x - 0.5f) * miniMapRect.rect.width,
                (normalizedPos.y - 0.5f) * miniMapRect.rect.height
            );

            viewportIndicator.anchoredPosition = localPos;

            // Optional: Scale viewport indicator based on camera zoom
            // This would require access to camera's field of view or orthographic size
        }

        #endregion

        #region Building Markers

        private void OnBuildingPlaced(BuildingPlacedEvent evt)
        {
            if (evt.Building == null) return;

            CreateBuildingMarker(evt.Building, evt.Position);
        }

        private void OnBuildingDestroyed(BuildingDestroyedEvent evt)
        {
            if (evt.Building == null) return;

            RemoveBuildingMarker(evt.Building);
        }

        private void CreateBuildingMarker(GameObject building, Vector3 position)
        {
            if (buildingMarkers.ContainsKey(building)) return;

            GameObject marker;

            if (buildingMarkerPrefab != null)
            {
                marker = Instantiate(buildingMarkerPrefab, buildingMarkersContainer);
            }
            else
            {
                // Create simple square marker
                marker = new GameObject($"BuildingMarker_{building.name}");
                Image img = marker.AddComponent<Image>();
                img.color = friendlyBuildingColor;
                // Image component automatically adds RectTransform
            }

            RectTransform markerRect = marker.GetComponent<RectTransform>();
            markerRect.SetParent(buildingMarkersContainer, false);

            markerRect.sizeDelta = new Vector2(buildingMarkerSize, buildingMarkerSize);

            buildingMarkers[building] = markerRect;

            UpdateMarkerPosition(markerRect, position);
        }

        private void RemoveBuildingMarker(GameObject building)
        {
            if (buildingMarkers.TryGetValue(building, out RectTransform marker))
            {
                if (marker != null)
                {
                    Destroy(marker.gameObject);
                }
                buildingMarkers.Remove(building);
            }
        }

        private void UpdateBuildingMarkers()
        {
            // Update positions of building markers
            List<GameObject> toRemove = new List<GameObject>();

            foreach (var kvp in buildingMarkers)
            {
                if (kvp.Key == null)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                UpdateMarkerPosition(kvp.Value, kvp.Key.transform.position);
            }

            // Clean up null references
            foreach (var key in toRemove)
            {
                if (buildingMarkers[key] != null)
                {
                    Destroy(buildingMarkers[key].gameObject);
                }
                buildingMarkers.Remove(key);
            }
        }

        #endregion

        #region Unit Markers

        private void OnUnitSpawned(UnitSpawnedEvent evt)
        {
            if (evt.Unit == null) return;

            // Determine if unit is enemy
            bool isEnemy = evt.Unit.layer == LayerMask.NameToLayer("Enemy");
            CreateUnitMarker(evt.Unit, evt.Position, isEnemy);
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            if (evt.Unit == null) return;

            RemoveUnitMarker(evt.Unit);
        }

        private void CreateUnitMarker(GameObject unit, Vector3 position, bool isEnemy)
        {
            if (unitMarkers.ContainsKey(unit)) return;

            GameObject marker;

            if (unitMarkerPrefab != null)
            {
                marker = Instantiate(unitMarkerPrefab, unitMarkersContainer);
            }
            else
            {
                // Create simple circle marker
                marker = new GameObject($"UnitMarker_{unit.name}");
                Image img = marker.AddComponent<Image>();
                img.color = isEnemy ? enemyUnitColor : friendlyUnitColor;

                // Make it circular
                img.sprite = CreateCircleSprite();
                // Image component automatically adds RectTransform
            }

            RectTransform markerRect = marker.GetComponent<RectTransform>();
            markerRect.SetParent(unitMarkersContainer, false);

            markerRect.sizeDelta = new Vector2(unitMarkerSize, unitMarkerSize);

            unitMarkers[unit] = markerRect;

            UpdateMarkerPosition(markerRect, position);
        }

        private void RemoveUnitMarker(GameObject unit)
        {
            if (unitMarkers.TryGetValue(unit, out RectTransform marker))
            {
                if (marker != null)
                {
                    Destroy(marker.gameObject);
                }
                unitMarkers.Remove(unit);
            }
        }

        private void UpdateUnitMarkers()
        {
            // Update positions of unit markers
            List<GameObject> toRemove = new List<GameObject>();

            foreach (var kvp in unitMarkers)
            {
                if (kvp.Key == null)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                UpdateMarkerPosition(kvp.Value, kvp.Key.transform.position);
            }

            // Clean up null references
            foreach (var key in toRemove)
            {
                if (unitMarkers[key] != null)
                {
                    Destroy(unitMarkers[key].gameObject);
                }
                unitMarkers.Remove(key);
            }
        }

        #endregion

        #region Helper Methods

        private void UpdateMarkerPosition(RectTransform marker, Vector3 worldPosition)
        {
            if (marker == null) return;

            // Convert world position to normalized position (0-1)
            Vector2 normalizedPos = new Vector2(
                Mathf.InverseLerp(worldMin.x, worldMax.x, worldPosition.x),
                Mathf.InverseLerp(worldMin.y, worldMax.y, worldPosition.z)
            );

            // Convert to local mini-map coordinates
            Vector2 localPos = new Vector2(
                (normalizedPos.x - 0.5f) * miniMapRect.rect.width,
                (normalizedPos.y - 0.5f) * miniMapRect.rect.height
            );

            marker.anchoredPosition = localPos;
        }

        private Sprite CreateCircleSprite()
        {
            // Create a simple white circle texture
            int size = 32;
            Texture2D texture = new Texture2D(size, size);
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

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually move camera to a specific world position.
        /// </summary>
        public void MoveCameraTo(Vector3 worldPosition)
        {
            MoveCameraToPosition(worldPosition);
        }

        /// <summary>
        /// Clear all markers from the mini-map.
        /// </summary>
        public void ClearAllMarkers()
        {
            foreach (var marker in buildingMarkers.Values)
            {
                if (marker != null)
                    Destroy(marker.gameObject);
            }
            buildingMarkers.Clear();

            foreach (var marker in unitMarkers.Values)
            {
                if (marker != null)
                    Destroy(marker.gameObject);
            }
            unitMarkers.Clear();
        }

        #endregion
    }
}
