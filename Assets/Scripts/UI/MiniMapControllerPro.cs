using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using RTS.Core.Events;
using RTS.UI.Minimap;
using System.Collections;

namespace RTS.UI
{
    /// <summary>
    /// Professional high-performance minimap controller with modular architecture.
    /// Features: object pooling, batched updates, configurable performance, click-to-world positioning.
    ///
    /// Improvements over MiniMapController:
    /// - Object pooling for markers (reduced GC pressure)
    /// - Modular marker management (separate building/unit managers)
    /// - Configurable update intervals (performance tuning)
    /// - Batched marker updates (handle 1000+ units)
    /// - Marker culling (hide off-screen markers)
    /// - ScriptableObject configuration (easy tweaking)
    /// - Enhanced click-to-world conversion with validation
    /// </summary>
    public class MiniMapControllerPro : MonoBehaviour, IPointerClickHandler
    {
        [Header("Configuration")]
        [SerializeField] private MinimapConfig config;

        [Header("UI References")]
        [SerializeField] private RectTransform miniMapRect;
        [SerializeField] private RawImage miniMapImage;

        [Header("Camera References")]
        [SerializeField] private Camera miniMapCamera;
        [SerializeField] private RTSCameraController cameraController;

        [Header("Viewport Indicator")]
        [SerializeField] private RectTransform viewportIndicator;

        [Header("Marker Containers")]
        [SerializeField] private RectTransform buildingMarkersContainer;
        [SerializeField] private RectTransform unitMarkersContainer;

        [Header("Optional Marker Prefabs")]
        [SerializeField] private GameObject buildingMarkerPrefab;
        [SerializeField] private GameObject unitMarkerPrefab;

        [Header("Performance Monitoring")]
        [SerializeField] private bool showDebugStats = false;

        [Header("Entity Detection")]
        [Tooltip("Method to detect entity ownership (friendly/enemy). Auto tries Component > Tag > Layer")]
        [SerializeField] private MinimapEntityDetector.DetectionMethod detectionMethod = MinimapEntityDetector.DetectionMethod.Auto;

        // Marker managers
        private MinimapBuildingMarkerManager buildingMarkerManager;
        private MinimapUnitMarkerManager unitMarkerManager;

        // Rendering
        private RenderTexture miniMapRenderTexture;
        private Coroutine cameraMoveCoroutine;

        // Performance tracking
        private int frameCounter = 0;
        private float lastUpdateTime = 0f;

        #region Initialization

        private void Awake()
        {
            // Validate configuration
            if (config == null)
            {
                Debug.LogError("MiniMapControllerPro: MinimapConfig is not assigned! Please create and assign a MinimapConfig ScriptableObject.");
                enabled = false;
                return;
            }

            // Auto-find camera controller if not set
            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<RTSCameraController>();
            }

            // Create containers if they don't exist
            CreateContainersIfNeeded();

            // Setup minimap camera and render texture
            if (config.renderWorldMap)
            {
                SetupMiniMapCamera();
            }

            // Setup viewport indicator
            SetupViewportIndicator();

            // Initialize marker managers
            InitializeMarkerManagers();
        }

        private void Start()
        {
            // Register existing units and buildings in the scene
            RegisterExistingUnits();
            RegisterExistingBuildings();
        }

        private void CreateContainersIfNeeded()
        {
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

        private void SetupViewportIndicator()
        {
            if (viewportIndicator != null)
            {
                Image img = viewportIndicator.GetComponent<Image>();
                if (img != null)
                {
                    img.color = config.viewportColor;
                }
            }
        }

        private void InitializeMarkerManagers()
        {
            buildingMarkerManager = new MinimapBuildingMarkerManager(
                config,
                miniMapRect,
                buildingMarkersContainer,
                buildingMarkerPrefab
            );

            unitMarkerManager = new MinimapUnitMarkerManager(
                config,
                miniMapRect,
                unitMarkersContainer,
                unitMarkerPrefab
            );

            Debug.Log($"MiniMapControllerPro initialized with object pooling. " +
                      $"Building pool: {config.buildingMarkerPoolSize}, Unit pool: {config.unitMarkerPoolSize}");
        }

        /// <summary>
        /// Register existing units in the scene (units placed before runtime)
        /// </summary>
        private void RegisterExistingUnits()
        {
            // Find all UnitAIController components in the scene
            var unitControllers = FindObjectsByType<RTS.Units.UnitAIController>(FindObjectsSortMode.None);

            int registeredCount = 0;
            foreach (var unitController in unitControllers)
            {
                if (unitController == null) continue;

                GameObject unit = unitController.gameObject;
                bool isEnemy = MinimapEntityDetector.IsEnemy(unit, detectionMethod);

                unitMarkerManager.AddMarker(unit, unit.transform.position, isEnemy);
                registeredCount++;
            }

            Debug.Log($"MiniMapControllerPro: Registered {registeredCount} existing units");
        }

        /// <summary>
        /// Register existing buildings in the scene (buildings placed before runtime)
        /// </summary>
        private void RegisterExistingBuildings()
        {
            // Find all Building components in the scene
            var buildings = FindObjectsByType<RTSBuildingsSystems.Building>(FindObjectsSortMode.None);

            int registeredCount = 0;
            foreach (var building in buildings)
            {
                if (building == null) continue;

                GameObject buildingObj = building.gameObject;
                bool isEnemy = MinimapEntityDetector.IsEnemy(buildingObj, detectionMethod);

                buildingMarkerManager.AddMarker(buildingObj, buildingObj.transform.position, isEnemy);
                registeredCount++;
            }

            Debug.Log($"MiniMapControllerPro: Registered {registeredCount} existing buildings");
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

        private void OnDestroy()
        {
            // Clean up managers
            buildingMarkerManager?.Dispose();
            unitMarkerManager?.Dispose();

            // Clean up render texture
            if (miniMapRenderTexture != null)
            {
                miniMapRenderTexture.Release();
                Destroy(miniMapRenderTexture);
            }
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            frameCounter++;

            // Update viewport indicator every N frames
            if (frameCounter % config.viewportUpdateInterval == 0)
            {
                UpdateCameraViewportIndicator();
            }

            // Update markers every N frames
            if (frameCounter % config.markerUpdateInterval == 0)
            {
                buildingMarkerManager?.UpdateMarkers();
                unitMarkerManager?.UpdateMarkers();
            }

            // Show debug stats
            if (showDebugStats && Time.time - lastUpdateTime > 1f)
            {
                LogDebugStats();
                lastUpdateTime = Time.time;
            }
        }

        #endregion

        #region Mini-Map Camera Setup

        private void SetupMiniMapCamera()
        {
            // Create render texture
            miniMapRenderTexture = new RenderTexture(
                config.renderTextureSize,
                config.renderTextureSize,
                24,
                RenderTextureFormat.ARGB32
            );
            miniMapRenderTexture.name = "MiniMapRenderTexture";
            miniMapRenderTexture.antiAliasing = 1;
            miniMapRenderTexture.autoGenerateMips = false;
            miniMapRenderTexture.useMipMap = false;
            miniMapRenderTexture.Create();

            // Assign to raw image
            if (miniMapImage != null)
            {
                miniMapImage.texture = miniMapRenderTexture;
                miniMapImage.uvRect = new Rect(0, 0, 1, 1);
            }
            else
            {
                Debug.LogError("MiniMapControllerPro: MiniMap RawImage is null!");
                return;
            }

            // Setup camera
            if (miniMapCamera == null)
            {
                GameObject camObj = new GameObject("MiniMapCamera");
                miniMapCamera = camObj.AddComponent<Camera>();
            }

            // Configure camera
            miniMapCamera.targetTexture = miniMapRenderTexture;
            miniMapCamera.orthographic = true;
            miniMapCamera.orthographicSize = config.WorldSize.y / 2f;

            // Position camera above world center
            Vector3 worldCenter = config.WorldCenter;
            worldCenter.y = config.minimapCameraHeight;
            miniMapCamera.transform.position = worldCenter;
            miniMapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Set render settings
            miniMapCamera.cullingMask = config.minimapLayers;
            miniMapCamera.clearFlags = CameraClearFlags.SolidColor;
            miniMapCamera.backgroundColor = config.backgroundColor;
            miniMapCamera.depth = -10;

            // URP setup
            var cameraData = miniMapCamera.GetUniversalAdditionalCameraData();
            if (cameraData != null)
            {
                cameraData.renderType = CameraRenderType.Base;
                cameraData.requiresColorOption = CameraOverrideOption.On;
                cameraData.requiresDepthOption = CameraOverrideOption.On;
            }

            // Remove audio listener
            AudioListener listener = miniMapCamera.GetComponent<AudioListener>();
            if (listener != null)
            {
                Destroy(listener);
            }

            Debug.Log($"MiniMapControllerPro: Camera setup complete. Rendering {config.WorldSize.x}x{config.WorldSize.y} world area at {config.renderTextureSize}x{config.renderTextureSize}.");
        }

        #endregion

        #region Click Handling (Click-to-World Position)

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!config.enableClickToMove || cameraController == null) return;

            Vector3 worldPos = ScreenToWorldPosition(eventData.position, eventData.pressEventCamera);

            if (worldPos != Vector3.zero)
            {
                Debug.Log($"MiniMapControllerPro: Click at screen {eventData.position} -> World {worldPos}");
                MoveCameraToPosition(worldPos);
            }
        }

        /// <summary>
        /// Convert screen position to world position with enhanced accuracy and validation.
        /// </summary>
        public Vector3 ScreenToWorldPosition(Vector2 screenPosition, Camera eventCamera)
        {
            Vector2 localPoint;

            // Convert screen point to local rect position
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                miniMapRect,
                screenPosition,
                eventCamera,
                out localPoint))
            {
                return Vector3.zero;
            }

            // Convert local position to normalized (0-1)
            Vector2 normalizedPos = new Vector2(
                (localPoint.x + miniMapRect.rect.width * 0.5f) / miniMapRect.rect.width,
                (localPoint.y + miniMapRect.rect.height * 0.5f) / miniMapRect.rect.height
            );

            // Validate normalized position
            if (config.clampClickPositions)
            {
                normalizedPos.x = Mathf.Clamp01(normalizedPos.x);
                normalizedPos.y = Mathf.Clamp01(normalizedPos.y);
            }
            else if (normalizedPos.x < 0f || normalizedPos.x > 1f || normalizedPos.y < 0f || normalizedPos.y > 1f)
            {
                // Click outside minimap bounds
                return Vector3.zero;
            }

            // Convert to world position
            Vector3 worldPos = new Vector3(
                Mathf.Lerp(config.worldMin.x, config.worldMax.x, normalizedPos.x),
                cameraController.transform.position.y,
                Mathf.Lerp(config.worldMin.y, config.worldMax.y, normalizedPos.y)
            );

            return worldPos;
        }

        /// <summary>
        /// Convert world position to minimap screen position.
        /// </summary>
        public Vector2 WorldToMinimapScreen(Vector3 worldPosition)
        {
            Vector2 normalizedPos = new Vector2(
                Mathf.InverseLerp(config.worldMin.x, config.worldMax.x, worldPosition.x),
                Mathf.InverseLerp(config.worldMin.y, config.worldMax.y, worldPosition.z)
            );

            Vector2 localPos = new Vector2(
                (normalizedPos.x - 0.5f) * miniMapRect.rect.width,
                (normalizedPos.y - 0.5f) * miniMapRect.rect.height
            );

            return localPos;
        }

        private void MoveCameraToPosition(Vector3 targetPosition)
        {
            if (cameraMoveCoroutine != null)
            {
                StopCoroutine(cameraMoveCoroutine);
            }

            if (config.useSmoothing)
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
            float duration = Mathf.Lerp(
                config.minMoveDuration,
                config.maxMoveDuration,
                Mathf.Clamp01(distance / 1000f)
            );

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curveValue = config.movementCurve.Evaluate(t);

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

            Vector3 camPos = cameraController.transform.position;
            Vector2 localPos = WorldToMinimapScreen(camPos);

            viewportIndicator.anchoredPosition = localPos;
        }

        #endregion

        #region Event Handlers

        private void OnBuildingPlaced(BuildingPlacedEvent evt)
        {
            if (evt.Building == null) return;

            // Determine if enemy using flexible detection system
            bool isEnemy = MinimapEntityDetector.IsEnemy(evt.Building, detectionMethod);

            buildingMarkerManager.AddMarker(evt.Building, evt.Position, isEnemy);
        }

        private void OnBuildingDestroyed(BuildingDestroyedEvent evt)
        {
            if (evt.Building == null) return;

            buildingMarkerManager.RemoveMarker(evt.Building);
        }

        private void OnUnitSpawned(UnitSpawnedEvent evt)
        {
            if (evt.Unit == null) return;

            // Determine if enemy using flexible detection system
            bool isEnemy = MinimapEntityDetector.IsEnemy(evt.Unit, detectionMethod);

            unitMarkerManager.AddMarker(evt.Unit, evt.Position, isEnemy);
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            if (evt.Unit == null) return;

            unitMarkerManager.RemoveMarker(evt.Unit);
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
        /// Clear all markers from the minimap.
        /// </summary>
        public void ClearAllMarkers()
        {
            buildingMarkerManager?.ClearAll();
            unitMarkerManager?.ClearAll();
        }

        /// <summary>
        /// Get performance statistics.
        /// </summary>
        public string GetPerformanceStats()
        {
            return $"Buildings: {buildingMarkerManager?.MarkerCount ?? 0}, " +
                   $"Units: {unitMarkerManager?.MarkerCount ?? 0}\n" +
                   $"{buildingMarkerManager?.GetPoolStats()}\n" +
                   $"{unitMarkerManager?.GetPoolStats()}";
        }

        #endregion

        #region Debug

        private void LogDebugStats()
        {
            Debug.Log($"=== MiniMapControllerPro Stats ===\n{GetPerformanceStats()}");
        }

        [ContextMenu("Verify Setup")]
        public void VerifySetup()
        {
            Debug.Log("=== MiniMapControllerPro Setup Verification ===");
            Debug.Log($"Config: {(config != null ? "OK" : "MISSING")}");
            Debug.Log($"MiniMap RawImage: {(miniMapImage != null ? "OK" : "MISSING")}");
            Debug.Log($"RenderTexture: {(miniMapRenderTexture != null ? $"OK ({miniMapRenderTexture.width}x{miniMapRenderTexture.height})" : "MISSING")}");
            Debug.Log($"MiniMap Camera: {(miniMapCamera != null ? "OK" : "MISSING")}");
            Debug.Log($"Building Manager: {(buildingMarkerManager != null ? "OK" : "MISSING")}");
            Debug.Log($"Unit Manager: {(unitMarkerManager != null ? "OK" : "MISSING")}");
            Debug.Log(GetPerformanceStats());
            Debug.Log("===========================================");
        }

        #endregion
    }
}
