using RTS.Core.Events;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RTS.Buildings
{
    /// <summary>
    /// Manages building selection using modern Input System.
    /// ADD THIS TO ONE GAMEOBJECT IN YOUR SCENE (like a "GameManager").
    /// Allows players to click on buildings to select them and show training UI.
    /// NEW: Supports multi-select with shift/ctrl and double/triple-click
    /// </summary>
    public class BuildingSelectionManager : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private InputActionReference clickAction;
        [SerializeField] private InputActionReference rightClickAction;
        [SerializeField] private InputActionReference positionAction;

        [Header("Selection Settings")]
        [SerializeField] private LayerMask buildingLayer;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Camera mainCamera;

        [Header("Multi-Select Settings")]
        [SerializeField] private bool enableMultiSelect = true;
        [SerializeField] private bool enableDoubleClick = true;
        [SerializeField] private float doubleClickTime = 0.3f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // Multi-select support
        private List<BuildingSelectable> selectedBuildings = new List<BuildingSelectable>();
        private bool isSpawnPointMode = false;
        private RTS.Managers.BuildingManager buildingManager;

        // Click tracking for double/triple click
        private int clickCount = 0;
        private float lastClickTimestamp = 0f;

        public BuildingSelectable CurrentlySelectedBuilding => selectedBuildings.Count > 0 ? selectedBuildings[0] : null;
        public IReadOnlyList<BuildingSelectable> SelectedBuildings => selectedBuildings;
        public int SelectionCount => selectedBuildings.Count;

        //  Cache these to avoid GC allocations
        private PointerEventData cachedPointerEventData;
        private List<RaycastResult> cachedRaycastResults = new List<RaycastResult>();

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            //  Initialize cached pointer data
            if (EventSystem.current != null)
            {
                cachedPointerEventData = new PointerEventData(EventSystem.current);
            }

            // Find BuildingManager to check if in placement mode
            buildingManager = Object.FindAnyObjectByType<RTS.Managers.BuildingManager>();
        }
        private bool IsMouseOverUI()
        {
            if (EventSystem.current == null)
                return false;

            // Initialize if needed (in case EventSystem wasn't ready at Awake)
            if (cachedPointerEventData == null)
            {
                cachedPointerEventData = new PointerEventData(EventSystem.current);
            }

            // Update position
            cachedPointerEventData.position = Mouse.current.position.ReadValue();

            // Clear previous results and raycast
            cachedRaycastResults.Clear();
            EventSystem.current.RaycastAll(cachedPointerEventData, cachedRaycastResults);

            return cachedRaycastResults.Count > 0;
        }
        private void OnEnable()
        {
            if (clickAction != null)
            {
                clickAction.action.Enable();
                clickAction.action.performed += OnClick;
            }

            if (rightClickAction != null)
            {
                rightClickAction.action.Enable();
                rightClickAction.action.performed += OnRightClick;
            }

            if (positionAction != null)
            {
                positionAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (clickAction != null)
            {
                clickAction.action.Disable();
                clickAction.action.performed -= OnClick;
            }

            if (rightClickAction != null)
            {
                rightClickAction.action.Disable();
                rightClickAction.action.performed -= OnRightClick;
            }

            if (positionAction != null)
            {
                positionAction.action.Disable();
            }
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            if (positionAction == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("BuildingSelectionManager: positionAction is null!");
                return;
            }

            //  CALL IT HERE
            if (IsMouseOverUI())
            {
                if (enableDebugLogs)
                    Debug.Log("BuildingSelectionManager: Click was over UI, ignoring");
                return;
            }

            // Don't process selection clicks if currently placing a building
            if (buildingManager != null && buildingManager.IsPlacingBuilding)
            {
                if (enableDebugLogs)
                    Debug.Log("BuildingSelectionManager: Ignoring click - currently placing building");
                return;
            }

            Vector2 mousePosition = positionAction.action.ReadValue<Vector2>();

            if (enableDebugLogs)
                Debug.Log($"BuildingSelectionManager: Click detected at {mousePosition}");

            if (isSpawnPointMode)
            {
                TrySetRallyPoint(mousePosition);
                isSpawnPointMode = false;
            }
            else
            {
                HandleClickLogic(mousePosition);
            }
        }

        private void HandleClickLogic(Vector2 screenPosition)
        {
            if (!enableDoubleClick)
            {
                // If double-click handling disabled, always treat as single
                clickCount = 1;
                lastClickTimestamp = Time.time;
                TrySelectBuilding(screenPosition);
                return;
            }

            float now = Time.time;
            float delta = now - lastClickTimestamp;

            if (delta <= doubleClickTime)
            {
                clickCount++;
            }
            else
            {
                clickCount = 1;
            }

            lastClickTimestamp = now;

            // Clamp to 3 (we don't need more)
            if (clickCount > 3) clickCount = 3;

            if (clickCount == 1)
            {
                TrySelectBuilding(screenPosition);
            }
            else if (clickCount == 2)
            {
                HandleDoubleClick(screenPosition);
            }
            else if (clickCount == 3)
            {
                HandleTripleClick(screenPosition);
                // reset after triple-click so next click starts fresh
                clickCount = 0;
                lastClickTimestamp = 0f;
            }
        }

        private void HandleDoubleClick(Vector2 screenPosition)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, buildingLayer))
            {
                if (hit.collider.TryGetComponent<BuildingSelectable>(out var clickedBuilding))
                {
                    // Get the building data
                    var building = clickedBuilding.GetComponent<Building>();
                    if (building != null && building.Data != null)
                    {
                        SelectAllVisibleBuildingsOfType(building.Data);
                        Debug.Log($"Double-clicked building: {building.Data.buildingName}. Selected all visible buildings of this type.");
                        return;
                    }
                }
            }

            // Double-click on empty space = select all visible buildings
            SelectAllVisibleBuildings();
        }

        private void HandleTripleClick(Vector2 screenPosition)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, buildingLayer))
            {
                if (hit.collider.TryGetComponent<BuildingSelectable>(out var clickedBuilding))
                {
                    // Get the building data
                    var building = clickedBuilding.GetComponent<Building>();
                    if (building != null && building.Data != null)
                    {
                        SelectAllBuildingsOfTypeInScene(building.Data);
                        Debug.Log($"Triple-click on building: {building.Data.buildingName}. Selected all buildings of this type in entire scene.");
                        return;
                    }
                }
            }

            // Triple-click on empty space = select ALL buildings in scene
            SelectAllBuildingsSceneWide();
            Debug.Log($"Triple-click on empty space: Selected all buildings in entire scene. Total: {selectedBuildings.Count}");
        }

        private void TrySelectBuilding(Vector2 screenPosition)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            if (enableDebugLogs)
                Debug.Log($"BuildingSelectionManager: Raycasting with layer mask {buildingLayer.value}");

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, buildingLayer))
            {
                if (enableDebugLogs)
                    Debug.Log($"BuildingSelectionManager: Hit {hit.collider.gameObject.name}");

                if (hit.collider.TryGetComponent<BuildingSelectable>(out var selectable))
                {
                    if (enableDebugLogs)
                        Debug.Log($" BuildingSelectable found on {hit.collider.gameObject.name}");

                    // Check for shift/ctrl for additive selection
                    bool shift = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
                    bool ctrl = Keyboard.current != null && Keyboard.current.ctrlKey.isPressed;
                    bool additive = enableMultiSelect && (shift || ctrl);
                    if (!additive)
                    {
                        ClearSelection();
                    }

                    SelectBuilding(selectable);
                    return;
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($"Hit building {hit.collider.gameObject.name} but no BuildingSelectable component!");
                }
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log("BuildingSelectionManager: No building hit, deselecting");
            }

            // Clicked empty space - deselect all buildings
            ClearSelection();
        }

        private void SelectBuilding(BuildingSelectable building)
        {
            // Add to selection if not already selected
            if (!selectedBuildings.Contains(building))
            {
                selectedBuildings.Add(building);
                building.Select();

                if (enableDebugLogs)
                    Debug.Log($"🏰 Selecting building: {building.gameObject.name}. Total selected: {selectedBuildings.Count}");
            }
        }

        private void ClearSelection()
        {
            foreach (var building in selectedBuildings)
            {
                if (building != null)
                {
                    building.Deselect();
                }
            }

            selectedBuildings.Clear();

            if (enableDebugLogs)
                Debug.Log("Deselected all buildings");
        }

        public void DeselectBuilding()
        {
            ClearSelection();
        }

        private void OnRightClick(InputAction.CallbackContext context)
        {
            // Only process right-clicks if a building is selected
            if (CurrentlySelectedBuilding == null)
            {
                if (enableDebugLogs)
                    Debug.Log("BuildingSelectionManager: Right-click but no building selected");
                return;
            }

            // Don't process if clicking on UI
            if (IsMouseOverUI())
            {
                if (enableDebugLogs)
                    Debug.Log("BuildingSelectionManager: Right-click was over UI, ignoring");
                return;
            }

            if (positionAction == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("BuildingSelectionManager: positionAction is null!");
                return;
            }

            Vector2 mousePosition = positionAction.action.ReadValue<Vector2>();
            TrySetRallyPoint(mousePosition);
        }

        private void TrySetRallyPoint(Vector2 screenPosition)
        {
            var building = CurrentlySelectedBuilding;
            if (building == null)
            {
                if (enableDebugLogs)
                    Debug.Log("BuildingSelectionManager: No building selected, cannot set rally point");
                return;
            }

            // Don't process if clicking on UI
            if (IsMouseOverUI())
            {
                if (enableDebugLogs)
                    Debug.Log("BuildingSelectionManager: Click was over UI, ignoring");
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            if (enableDebugLogs)
                Debug.Log($"🎯 Attempting to set rally point from screen position {screenPosition} for building {building.gameObject.name}");

            // Try to hit ground layer
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                if (enableDebugLogs)
                    Debug.Log($" Ground hit at world position {hit.point} on object {hit.collider.gameObject.name}");

                // Get UnitTrainingQueue component
                if (building.TryGetComponent<UnitTrainingQueue>(out UnitTrainingQueue trainingQueue))
                {
                    trainingQueue.SetRallyPointPosition(hit.point);

                    // Update flag position if present
                    if (building.TryGetComponent<RallyPointFlag>(out var rallyFlag))
                    {
                        if (enableDebugLogs)
                            Debug.Log($"🚩 BuildingSelectionManager: Found RallyPointFlag component on {building.gameObject.name}, updating position and showing flag...");

                        rallyFlag.SetRallyPointPosition(hit.point);
                        rallyFlag.ShowFlag(); // Show flag when rally point is set
                    }
                    else
                    {
                        if (enableDebugLogs)
                            Debug.LogWarning($"⚠️ BuildingSelectionManager: Building {building.gameObject.name} has no RallyPointFlag component - flag will not be shown. This is optional.");
                    }

                    if (enableDebugLogs)
                        Debug.Log($" Rally point successfully set for {building.gameObject.name} at {hit.point}");
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($" Building {building.gameObject.name} has no UnitTrainingQueue component - cannot set rally point");
                }
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning($" Click did not hit ground layer (mask: {groundLayer.value}). Make sure ground has correct layer assigned.");
            }
        }

        #region Multi-Select Helper Methods

        /// <summary>
        /// Select all visible buildings
        /// </summary>
        private void SelectAllVisibleBuildings()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            ClearSelection();

            BuildingSelectable[] allBuildings = FindObjectsByType<BuildingSelectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<BuildingSelectable> visibleBuildings = new List<BuildingSelectable>();

            foreach (var buildingSelectable in allBuildings)
            {
                if (buildingSelectable == null)
                    continue;

                // Check if building is visible to camera
                Vector3 screenPos = mainCamera.WorldToScreenPoint(buildingSelectable.transform.position);

                if (screenPos.z > 0 &&
                    screenPos.x >= 0 && screenPos.x <= Screen.width &&
                    screenPos.y >= 0 && screenPos.y <= Screen.height)
                {
                    visibleBuildings.Add(buildingSelectable);
                }
            }

            // Select all visible buildings
            foreach (var building in visibleBuildings)
            {
                SelectBuilding(building);
            }

            Debug.Log($"Selected all visible buildings: {selectedBuildings.Count}");
        }

        /// <summary>
        /// Select all visible buildings of a specific type (by BuildingDataSO reference)
        /// </summary>
        private void SelectAllVisibleBuildingsOfType(BuildingDataSO targetData)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null || targetData == null)
                return;

            ClearSelection();

            BuildingSelectable[] allBuildings = FindObjectsByType<BuildingSelectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<BuildingSelectable> matchingBuildings = new List<BuildingSelectable>();

            foreach (var buildingSelectable in allBuildings)
            {
                if (buildingSelectable == null)
                    continue;

                // Check if building data matches by ScriptableObject reference
                var buildingComponent = buildingSelectable.GetComponent<Building>();
                if (buildingComponent == null || buildingComponent.Data != targetData)
                    continue;

                // Check if building is visible to camera
                Vector3 screenPos = mainCamera.WorldToScreenPoint(buildingSelectable.transform.position);

                if (screenPos.z > 0 &&
                    screenPos.x >= 0 && screenPos.x <= Screen.width &&
                    screenPos.y >= 0 && screenPos.y <= Screen.height)
                {
                    matchingBuildings.Add(buildingSelectable);
                }
            }

            // Select all matching buildings
            foreach (var building in matchingBuildings)
            {
                SelectBuilding(building);
            }

            Debug.Log($"Selected {selectedBuildings.Count} visible buildings of type: {targetData.buildingName}");
        }

        /// <summary>
        /// Select all buildings of a specific type in entire scene (by BuildingDataSO reference)
        /// </summary>
        private void SelectAllBuildingsOfTypeInScene(BuildingDataSO targetData)
        {
            if (targetData == null)
                return;

            ClearSelection();

            BuildingSelectable[] allBuildings = FindObjectsByType<BuildingSelectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<BuildingSelectable> matchingBuildings = new List<BuildingSelectable>();

            foreach (var buildingSelectable in allBuildings)
            {
                if (buildingSelectable == null)
                    continue;

                // Check if building data matches by ScriptableObject reference
                var buildingComponent = buildingSelectable.GetComponent<Building>();
                if (buildingComponent == null || buildingComponent.Data != targetData)
                    continue;

                matchingBuildings.Add(buildingSelectable);
            }

            // Select all matching buildings
            foreach (var building in matchingBuildings)
            {
                SelectBuilding(building);
            }

            Debug.Log($"Selected {selectedBuildings.Count} buildings of type: {targetData.buildingName} in entire scene");
        }

        /// <summary>
        /// Select all buildings in entire scene
        /// </summary>
        private void SelectAllBuildingsSceneWide()
        {
            ClearSelection();

            BuildingSelectable[] allBuildings = FindObjectsByType<BuildingSelectable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var building in allBuildings)
            {
                if (building == null)
                    continue;

                SelectBuilding(building);
            }

            Debug.Log($"Selected all buildings in scene: {selectedBuildings.Count}");
        }

        #endregion

        /// <summary>
        /// Enable or disable spawn point setting mode.
        /// When enabled, left-clicking on ground sets spawn point instead of selecting buildings.
        /// </summary>
        public void SetSpawnPointMode(bool enabled)
        {
            isSpawnPointMode = enabled;

            if (enableDebugLogs)
            {
                Debug.Log($"Spawn point mode: {(enabled ? "ENABLED" : "DISABLED")}");
            }
        }

        /// <summary>
        /// Check if currently in spawn point setting mode
        /// </summary>
        public bool IsSpawnPointMode()
        {
            return isSpawnPointMode;
        }
    }
}
