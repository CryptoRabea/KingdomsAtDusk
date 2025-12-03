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

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private BuildingSelectable currentlySelected;
        private bool isSpawnPointMode = false;
        private RTS.Managers.BuildingManager buildingManager;

        public BuildingSelectable CurrentlySelectedBuilding => currentlySelected;


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
                TrySelectBuilding(mousePosition);
            }
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

            // Clicked empty space - deselect current building
            DeselectCurrentBuilding();
        }

        private void SelectBuilding(BuildingSelectable building)
        {
            // Deselect previous building if different
            if (currentlySelected != null && currentlySelected != building)
            {
                if (enableDebugLogs)
                    Debug.Log($"Deselecting previous building: {currentlySelected.gameObject.name}");
                currentlySelected.Deselect();
            }

            currentlySelected = building;

            if (enableDebugLogs)
                Debug.Log($"🏰 Selecting building: {building.gameObject.name}");

            building.Select();
        }

        private void DeselectCurrentBuilding()
        {
            if (currentlySelected != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"Deselecting building: {currentlySelected.gameObject.name}");

                currentlySelected.Deselect();
                currentlySelected = null;
            }
        }

        public void DeselectBuilding()
        {
            DeselectCurrentBuilding();
        }

        private void OnRightClick(InputAction.CallbackContext context)
        {
            // Only process right-clicks if a building is selected
            if (currentlySelected == null)
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
            if (currentlySelected == null)
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
                Debug.Log($"🎯 Attempting to set rally point from screen position {screenPosition} for building {currentlySelected.gameObject.name}");

            // Try to hit ground layer
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                if (enableDebugLogs)
                    Debug.Log($" Ground hit at world position {hit.point} on object {hit.collider.gameObject.name}");

                // Get UnitTrainingQueue component
                if (currentlySelected.TryGetComponent<UnitTrainingQueue>(out UnitTrainingQueue trainingQueue))
                {
                    trainingQueue.SetRallyPointPosition(hit.point);

                    // Update flag position if present
                    if (currentlySelected.TryGetComponent<RallyPointFlag>(out var rallyFlag))
                    {
                        if (enableDebugLogs)
                            Debug.Log($"🚩 BuildingSelectionManager: Found RallyPointFlag component on {currentlySelected.gameObject.name}, updating position and showing flag...");

                        rallyFlag.SetRallyPointPosition(hit.point);
                        rallyFlag.ShowFlag(); // Show flag when rally point is set
                    }
                    else
                    {
                        if (enableDebugLogs)
                            Debug.LogWarning($"⚠️ BuildingSelectionManager: Building {currentlySelected.gameObject.name} has no RallyPointFlag component - flag will not be shown. This is optional.");
                    }

                    if (enableDebugLogs)
                        Debug.Log($" Rally point successfully set for {currentlySelected.gameObject.name} at {hit.point}");
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($" Building {currentlySelected.gameObject.name} has no UnitTrainingQueue component - cannot set rally point");
                }
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning($" Click did not hit ground layer (mask: {groundLayer.value}). Make sure ground has correct layer assigned.");
            }
        }

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
