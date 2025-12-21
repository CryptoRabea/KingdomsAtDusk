using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RTS.Core.Events;
using RTS.Core;
using RTS.UI.Minimap;
using RTS.CameraControl;
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

        [Header("Play Area")]
        [Tooltip("Reference to PlayAreaBounds. If not set, will use config world bounds.")]
        [SerializeField] private PlayAreaBounds playAreaBounds;

        [Header("UI References")]
        [SerializeField] private RectTransform miniMapRect;
        [SerializeField] private RawImage miniMapImage;

        [Header("Camera References")]
        [Tooltip("Assign your pre-configured minimap camera here. If left empty, one will be created.")]
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

        #region Initialization

        private void Awake()
        {
            // Validate configuration
            if (config == null)
            {
                enabled = false;
                return;
            }

            // Find PlayAreaBounds if not assigned
            if (playAreaBounds == null)
            {
                playAreaBounds = PlayAreaBounds.Instance;
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

            // Update marker managers with camera view bounds after camera setup
            if (config.renderWorldMap && miniMapCamera != null)
            {
                Bounds cameraBounds = GetCameraViewBounds();
                buildingMarkerManager?.SetCameraViewBounds(cameraBounds);
                unitMarkerManager?.SetCameraViewBounds(cameraBounds);
            }
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
                if (viewportIndicator.TryGetComponent<Image>(out var img))
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


        }

        /// <summary>
        /// Register existing units in the scene (units placed before runtime)
        /// </summary>
        private void RegisterExistingUnits()
        {
            // Find all UnitAIController components in the scene
            var unitControllers = FindObjectsByType<RTS.Units.AI.UnitAIController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            int registeredCount = 0;
            foreach (var unitController in unitControllers)
            {
                if (unitController == null) continue;

                GameObject unit = unitController.gameObject;
                bool isEnemy = MinimapEntityDetector.IsEnemy(unit, detectionMethod);

                unitMarkerManager.AddMarker(unit, unit.transform.position, isEnemy);
                registeredCount++;
            }

        }

        /// <summary>
        /// Register existing buildings in the scene (buildings placed before runtime)
        /// </summary>
        private void RegisterExistingBuildings()
        {
            // Find all Building components in the scene
            var buildings = FindObjectsByType<RTS.Buildings.Building>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            int registeredCount = 0;
            foreach (var building in buildings)
            {
                if (building == null) continue;

                GameObject buildingObj = building.gameObject;
                bool isEnemy = MinimapEntityDetector.IsEnemy(buildingObj, detectionMethod);

                buildingMarkerManager.AddMarker(buildingObj, buildingObj.transform.position, isEnemy);
                registeredCount++;
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
                return;
            }

            // Get world bounds from PlayAreaBounds or fall back to config
            Vector2 worldSize;
            Vector3 worldCenter;

            if (playAreaBounds != null)
            {
                worldSize = playAreaBounds.Size;
                worldCenter = playAreaBounds.Center;
            }
            else
            {
                worldSize = config.WorldSize;
                worldCenter = config.WorldCenter;
            }

            // Only create a new camera if one wasn't assigned
            bool cameraWasAssigned = miniMapCamera != null;
            if (!cameraWasAssigned)
            {
                GameObject camObj = new GameObject("MiniMapCamera");
                miniMapCamera = camObj.AddComponent<Camera>();
            }

            // Configure camera render target
            miniMapCamera.targetTexture = miniMapRenderTexture;
            miniMapCamera.orthographic = true;

            // Calculate orthographic size to fit entire play area
            float worldWidth = worldSize.x;
            float worldDepth = worldSize.y;

            // Use the larger dimension to ensure entire world is visible
            miniMapCamera.orthographicSize = Mathf.Max(worldWidth, worldDepth) / 2f;

            // Position camera above world center (only if we created the camera or it needs repositioning)
            if (!cameraWasAssigned)
            {
                Vector3 cameraPosition = worldCenter;
                cameraPosition.y = config.minimapCameraHeight;
                miniMapCamera.transform.SetPositionAndRotation(cameraPosition, Quaternion.Euler(90f, 0f, 0f));
            }

            // Set render settings
            miniMapCamera.cullingMask = config.minimapLayers;
            miniMapCamera.clearFlags = CameraClearFlags.SolidColor;
            miniMapCamera.backgroundColor = config.backgroundColor;
            miniMapCamera.depth = -10;

            // Remove audio listener if present
            if (miniMapCamera.TryGetComponent<AudioListener>(out var listener))
            {
                Destroy(listener);
            }
        }

        #endregion

        #region Click Handling (Click-to-World Position)

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!config.enableClickToMove || cameraController == null) return;

            Vector3 worldPos = ScreenToWorldPosition(eventData.position, eventData.pressEventCamera);

            if (worldPos != Vector3.zero)
            {
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

            // FIX: Account for camera's actual view bounds vs configured world bounds
            // Get the actual camera view bounds
            Bounds cameraBounds = GetCameraViewBounds();

            // Convert to world position using camera's actual view
            Vector3 worldPos = new Vector3(
                Mathf.Lerp(cameraBounds.min.x, cameraBounds.max.x, normalizedPos.x),
                cameraController != null ? cameraController.transform.position.y : 0f,
                Mathf.Lerp(cameraBounds.min.z, cameraBounds.max.z, normalizedPos.y)
            );

            return worldPos;
        }

        /// <summary>
        /// Convert world position to minimap screen position.
        /// </summary>
        public Vector2 WorldToMinimapScreen(Vector3 worldPosition)
        {
            // FIX: Use camera's actual view bounds for accurate positioning
            Bounds cameraBounds = GetCameraViewBounds();

            Vector2 normalizedPos = new Vector2(
                Mathf.InverseLerp(cameraBounds.min.x, cameraBounds.max.x, worldPosition.x),
                Mathf.InverseLerp(cameraBounds.min.z, cameraBounds.max.z, worldPosition.z)
            );

            Vector2 localPos = new Vector2(
                (normalizedPos.x - 0.5f) * miniMapRect.rect.width,
                (normalizedPos.y - 0.5f) * miniMapRect.rect.height
            );

            return localPos;
        }

        /// <summary>
        /// Get the actual world bounds visible by the minimap camera.
        /// This accounts for orthographic size and aspect ratio.
        /// </summary>
        private Bounds GetCameraViewBounds()
        {
            // Get world center from PlayAreaBounds or config
            Vector3 worldCenter;
            Vector2 worldSize;

            if (playAreaBounds != null)
            {
                worldCenter = playAreaBounds.Center;
                worldSize = playAreaBounds.Size;
            }
            else
            {
                worldCenter = config.WorldCenter;
                worldSize = config.WorldSize;
            }

            if (miniMapCamera == null)
            {
                // Fallback to bounds if camera not set up yet
                return new Bounds(
                    worldCenter,
                    new Vector3(worldSize.x, 0, worldSize.y)
                );
            }

            // For orthographic camera looking down (90 degrees on X axis):
            // - Camera's vertical extent (in world Z) = orthographicSize * 2
            // - Camera's horizontal extent (in world X) = orthographicSize * aspect * 2
            // - For square render texture, aspect = 1.0

            float orthoSize = miniMapCamera.orthographicSize;
            float aspect = 1.0f; // Square render texture

            float viewWidth = orthoSize * aspect * 2f;  // World X extent
            float viewDepth = orthoSize * 2f;            // World Z extent

            Vector3 size = new Vector3(viewWidth, 0, viewDepth);

            return new Bounds(worldCenter, size);
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
      

        #endregion
    }
}
