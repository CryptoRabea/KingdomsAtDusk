using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using RTS.Core.Events;

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
        private bool isRallyPointMode = false;

        public BuildingSelectable CurrentlySelectedBuilding => currentlySelected;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
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

            Vector2 mousePosition = positionAction.action.ReadValue<Vector2>();

            if (enableDebugLogs)
                Debug.Log($"BuildingSelectionManager: Click detected at {mousePosition}");

            // If in rally point mode, set rally point on left click
            if (isRallyPointMode)
            {
                TrySetRallyPoint(mousePosition);
                // Auto-exit rally point mode after setting
                isRallyPointMode = false;
            }
            else
            {
                TrySelectBuilding(mousePosition);
            }
        }

        private void TrySelectBuilding(Vector2 screenPosition)
        {
            // Don't select if clicking on UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (enableDebugLogs)
                    Debug.Log("BuildingSelectionManager: Click was over UI, ignoring");
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            if (enableDebugLogs)
                Debug.Log($"BuildingSelectionManager: Raycasting with layer mask {buildingLayer.value}");

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, buildingLayer))
            {
                if (enableDebugLogs)
                    Debug.Log($"BuildingSelectionManager: Hit {hit.collider.gameObject.name}");

                var selectable = hit.collider.GetComponent<BuildingSelectable>();
                if (selectable != null)
                {
                    if (enableDebugLogs)
                        Debug.Log($"✅ BuildingSelectable found on {hit.collider.gameObject.name}");

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
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
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
            if (currentlySelected == null) return;

            // Don't process if clicking on UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (enableDebugLogs)
                    Debug.Log("BuildingSelectionManager: Click was over UI, ignoring");
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            // Try to hit ground layer
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                if (enableDebugLogs)
                    Debug.Log($"BuildingSelectionManager: Setting rally point at {hit.point}");

                // Get UnitTrainingQueue component
                var trainingQueue = currentlySelected.GetComponent<UnitTrainingQueue>();
                if (trainingQueue != null)
                {
                    trainingQueue.SetRallyPointPosition(hit.point);

                    // Update flag position if present
                    var rallyFlag = currentlySelected.GetComponent<RallyPointFlag>();
                    if (rallyFlag != null)
                    {
                        rallyFlag.SetRallyPointPosition(hit.point);
                    }

                    if (enableDebugLogs)
                        Debug.Log($"✅ Rally point set for {currentlySelected.gameObject.name}");
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($"Building {currentlySelected.gameObject.name} has no UnitTrainingQueue component");
                }
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log("BuildingSelectionManager: Click did not hit ground");
            }
        }

        /// <summary>
        /// Enable or disable rally point setting mode.
        /// When enabled, left-clicking on ground sets rally point instead of selecting buildings.
        /// </summary>
        public void SetRallyPointMode(bool enabled)
        {
            isRallyPointMode = enabled;

            if (enableDebugLogs)
            {
                Debug.Log($"Rally point mode: {(enabled ? "ENABLED" : "DISABLED")}");
            }
        }

        /// <summary>
        /// Check if currently in rally point setting mode
        /// </summary>
        public bool IsRallyPointMode()
        {
            return isRallyPointMode;
        }
    }
}
