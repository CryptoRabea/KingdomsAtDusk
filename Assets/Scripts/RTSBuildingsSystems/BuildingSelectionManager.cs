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

        [Header("Audio")]
        [SerializeField] private AudioClip rallyPointSetSFX;
        [SerializeField] private float rallyPointSFXVolume = 1f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // Multi-select support
        private List<BuildingSelectable> selectedBuildings = new List<BuildingSelectable>();
        private bool isSpawnPointMode = false;
        private RTS.Managers.BuildingManager buildingManager;
        private WallPlacementController wallPlacementController;

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
            wallPlacementController = Object.FindAnyObjectByType<WallPlacementController>();
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
                return;
            }

            //  CALL IT HERE
            if (IsMouseOverUI())
            {
                if (enableDebugLogs)
                return;
            }

            // Don't process selection clicks if currently placing a building or wall
            if (buildingManager != null && buildingManager.IsPlacingBuilding)
            {
                if (enableDebugLogs)
                return;
            }

            if (wallPlacementController != null && wallPlacementController.IsPlacingWalls)
            {
                if (enableDebugLogs)
                return;
            }

            Vector2 mousePosition = positionAction.action.ReadValue<Vector2>();

            if (enableDebugLogs)

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
                    if (clickedBuilding.TryGetComponent<Building>(out var building) && building.Data != null)
                    {
                        SelectAllVisibleBuildingsOfType(building.Data);
                        return;
                    }
                }
            }

            // Double-click on empty space = do nothing (don't select buildings unless clicking ON a building)
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
                    if (clickedBuilding.TryGetComponent<Building>(out var building) && building.Data != null)
                    {
                        SelectAllBuildingsOfTypeInScene(building.Data);
                        return;
                    }
                }
            }

            // Triple-click on empty space = do nothing (don't select buildings unless clicking ON a building)
        }

        private void TrySelectBuilding(Vector2 screenPosition)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, buildingLayer))
            {
                if (hit.collider != null)
                {
                    if (hit.collider.TryGetComponent<BuildingSelectable>(out var selectable))
                    {
                        // Multi-select support: shift/ctrl keys
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
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log("BuildingSelectionManager: No building hit, deselecting all.");
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

        }

        public void DeselectBuilding()
        {
            ClearSelection();
        }

        private void OnRightClick(InputAction.CallbackContext context)
        {
            // Don't process right-clicks if currently placing a building or wall
            // (right-click should cancel placement instead)
            if (buildingManager != null && buildingManager.IsPlacingBuilding)
            {
                if (enableDebugLogs)
                return;
            }

            if (wallPlacementController != null && wallPlacementController.IsPlacingWalls)
            {
                if (enableDebugLogs)
                return;
            }

            // Only process right-clicks if a building is selected
            if (CurrentlySelectedBuilding == null)
            {
                if (enableDebugLogs)
                return;
            }

            // Don't process if clicking on UI
            if (IsMouseOverUI())
            {
                if (enableDebugLogs)
                return;
            }

            if (positionAction == null)
            {
                if (enableDebugLogs)
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
                return;
            }

            // Don't process if clicking on UI
            if (IsMouseOverUI())
            {
                if (enableDebugLogs)
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            if (enableDebugLogs)

            // Try to hit ground layer
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                if (enableDebugLogs)

                // Get UnitTrainingQueue component
                if (building.TryGetComponent<UnitTrainingQueue>(out UnitTrainingQueue trainingQueue))
                {
                    trainingQueue.SetRallyPointPosition(hit.point);

                    // Update flag position if present
                    if (building.TryGetComponent<RallyPointFlag>(out var rallyFlag))
                    {
                        if (enableDebugLogs)

                        rallyFlag.SetRallyPointPosition(hit.point);
                        rallyFlag.ShowFlag(); // Show flag when rally point is set
                    }

                    // Play rally point set SFX
                    if (rallyPointSetSFX != null)
                    {
                        AudioSource.PlayClipAtPoint(rallyPointSetSFX, hit.point, rallyPointSFXVolume);
                    }

                }
               
            }
            
        }

        #region Multi-Select Helper Methods

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
                if (buildingSelectable.TryGetComponent<Building>(out var buildingComponent))
                {
                }
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
                if (buildingSelectable.TryGetComponent<Building>(out var buildingComponent))
                {
                }
                if (buildingComponent == null || buildingComponent.Data != targetData)
                    continue;

                matchingBuildings.Add(buildingSelectable);
            }

            // Select all matching buildings
            foreach (var building in matchingBuildings)
            {
                SelectBuilding(building);
            }

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
