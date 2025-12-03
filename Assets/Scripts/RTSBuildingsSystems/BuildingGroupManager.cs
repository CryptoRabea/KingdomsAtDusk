using RTS.Core.Events;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RTS.Buildings
{
    /// <summary>
    /// Manages building groups for quick access.
    /// Press Ctrl+Number to save current selected building to a group.
    /// Press Number to recall a group and select that building.
    /// Double-press Number to recall and center camera on building.
    /// Note: If you assign a new building to an occupied group, it REPLACES the old one.
    /// </summary>
    public class BuildingGroupManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BuildingSelectionManager selectionManager;
        [SerializeField] private Camera mainCamera;

        [Header("Settings")]
        [SerializeField] private int numberOfGroups = 10; // 0-9
        [SerializeField] private bool enableDoubleTapCenter = true;
        [SerializeField] private float doubleTapTime = 0.3f;
        [SerializeField] private bool clearDestroyedBuildings = true;

        [Header("Camera Settings")]
        [SerializeField] private float cameraDistance = 20f;
        [SerializeField] private float cameraHeight = 15f;
        [SerializeField] private float cameraMoveSpeed = 5f;

        [Header("Visual Feedback")]
        [SerializeField] private bool showDebugMessages = true;
        [SerializeField] private bool showOnScreenFeedback = true;

        // Group storage: Key = group number, Value = building
        private Dictionary<int, BuildingSelectable> groups = new Dictionary<int, BuildingSelectable>();

        // For double-tap detection
        private int lastPressedGroup = -1;
        private float lastGroupPressTime = 0f;

        private void Awake()
        {
            if (selectionManager == null)
            {
                selectionManager = FindFirstObjectByType<BuildingSelectionManager>();
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // Initialize groups (all null by default)
            for (int i = 0; i < numberOfGroups; i++)
            {
                groups[i] = null;
            }
        }

        private void Update()
        {
            // Check for Ctrl+Number to save groups
            bool ctrlPressed = Keyboard.current != null &&
                             (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);

            // Check number keys (0-9)
            for (int i = 0; i < numberOfGroups; i++)
            {
                Key numberKey = GetNumberKey(i);

                if (Keyboard.current != null && Keyboard.current[numberKey].wasPressedThisFrame)
                {
                    if (ctrlPressed)
                    {
                        // Save current selection to group (REPLACES old assignment)
                        SaveGroup(i);
                    }
                    else
                    {
                        // Recall group
                        RecallGroup(i);
                    }
                }
            }
        }

        /// <summary>
        /// Saves the currently selected building to a group.
        /// REPLACES any previous building in that group.
        /// </summary>
        public void SaveGroup(int groupNumber)
        {
            if (groupNumber < 0 || groupNumber >= numberOfGroups)
            {
                Debug.LogWarning($"Invalid group number: {groupNumber}");
                return;
            }

            if (selectionManager == null || selectionManager.CurrentlySelectedBuilding == null)
            {
                if (showDebugMessages)
                    Debug.Log($"No building selected to save to group {groupNumber}");
                return;
            }

            // Get currently selected building
            BuildingSelectable selectedBuilding = selectionManager.CurrentlySelectedBuilding;

            // Check if replacing an existing building
            if (groups[groupNumber] != null)
            {
                if (showDebugMessages)
                    Debug.Log($"Replacing building in group {groupNumber}: {groups[groupNumber].gameObject.name} → {selectedBuilding.gameObject.name}");
            }

            // REPLACE (not add) - assign the new building
            groups[groupNumber] = selectedBuilding;

            if (showDebugMessages)
                Debug.Log($"🏰 Saved {selectedBuilding.gameObject.name} to group {groupNumber}");

            // Show on-screen feedback
            if (showOnScreenFeedback)
            {
                ShowFeedback($"Building assigned to group {groupNumber}");
            }

            // Publish event
            EventBus.Publish(new BuildingGroupSavedEvent(groupNumber, selectedBuilding.gameObject.name));
        }

        /// <summary>
        /// Recalls a saved group and selects that building.
        /// Double-tap to center camera on the building.
        /// </summary>
        public void RecallGroup(int groupNumber)
        {
            if (groupNumber < 0 || groupNumber >= numberOfGroups)
            {
                Debug.LogWarning($"Invalid group number: {groupNumber}");
                return;
            }

            if (selectionManager == null)
                return;

            // Clean up destroyed buildings if enabled
            if (clearDestroyedBuildings && groups[groupNumber] != null)
            {
                if (groups[groupNumber] == null || groups[groupNumber].gameObject == null)
                {
                    groups[groupNumber] = null;
                }
            }

            if (groups[groupNumber] == null)
            {
                if (showDebugMessages)
                    Debug.Log($"Group {groupNumber} is empty");
                return;
            }

            // Check for double-tap
            bool isDoubleTap = false;
            if (enableDoubleTapCenter)
            {
                float timeSinceLastPress = Time.time - lastGroupPressTime;
                if (lastPressedGroup == groupNumber && timeSinceLastPress < doubleTapTime)
                {
                    isDoubleTap = true;
                }

                lastPressedGroup = groupNumber;
                lastGroupPressTime = Time.time;
            }

            // Get the building
            BuildingSelectable building = groups[groupNumber];

            // Deselect current building
            if (selectionManager.CurrentlySelectedBuilding != null)
            {
                selectionManager.CurrentlySelectedBuilding.Deselect();
            }

            // Select the building from the group
            building.Select();

            if (showDebugMessages)
                Debug.Log($"🏰 Recalled group {groupNumber}: {building.gameObject.name}");

            // Show on-screen feedback
            if (showOnScreenFeedback)
            {
                ShowFeedback($"Selected: {building.gameObject.name}");
            }

            // Center camera on building if double-tapped
            if (isDoubleTap)
            {
                CenterCameraOnBuilding(building);
            }

            // Publish event
            EventBus.Publish(new BuildingGroupRecalledEvent(groupNumber, building.gameObject.name, isDoubleTap));
        }

        /// <summary>
        /// Centers the camera on a building.
        /// </summary>
        private void CenterCameraOnBuilding(BuildingSelectable building)
        {
            if (mainCamera == null || building == null)
                return;

            Vector3 buildingPosition = building.transform.position;

            // Calculate camera position (behind and above the building)
            Vector3 targetPosition = buildingPosition + new Vector3(0, cameraHeight, -cameraDistance);

            // Smooth camera movement (or instant if you prefer)
            if (cameraMoveSpeed > 0)
            {
                StartCoroutine(SmoothCameraMove(targetPosition, buildingPosition));
            }
            else
            {
                // Instant move
                mainCamera.transform.position = targetPosition;
                mainCamera.transform.LookAt(buildingPosition);
            }

            if (showDebugMessages)
                Debug.Log($"📷 Centered camera on {building.gameObject.name}");
        }

        private System.Collections.IEnumerator SmoothCameraMove(Vector3 targetPosition, Vector3 lookAtPosition)
        {
            float elapsed = 0f;
            Vector3 startPosition = mainCamera.transform.position;
            Quaternion startRotation = mainCamera.transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(lookAtPosition - targetPosition);

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * cameraMoveSpeed;
                mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed);
                mainCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed);
                yield return null;
            }

            mainCamera.transform.position = targetPosition;
            mainCamera.transform.rotation = targetRotation;
        }

        /// <summary>
        /// Gets the building in a specific group.
        /// </summary>
        public BuildingSelectable GetGroup(int groupNumber)
        {
            if (groupNumber >= 0 && groupNumber < numberOfGroups)
            {
                return groups[groupNumber];
            }
            return null;
        }

        /// <summary>
        /// Clears a specific group.
        /// </summary>
        public void ClearGroup(int groupNumber)
        {
            if (groupNumber >= 0 && groupNumber < numberOfGroups)
            {
                groups[groupNumber] = null;
                if (showDebugMessages)
                    Debug.Log($"Cleared group {groupNumber}");
            }
        }

        /// <summary>
        /// Clears all groups.
        /// </summary>
        public void ClearAllGroups()
        {
            for (int i = 0; i < numberOfGroups; i++)
            {
                groups[i] = null;
            }
            if (showDebugMessages)
                Debug.Log("Cleared all building groups");
        }

        /// <summary>
        /// Shows on-screen feedback message (you can implement a UI system for this)
        /// </summary>
        private void ShowFeedback(string message)
        {
            // TODO: Hook this up to your UI system
            // For now, just log it
            Debug.Log($"[Feedback] {message}");
        }

        /// <summary>
        /// Gets the Unity Key for a number (0-9).
        /// </summary>
        private Key GetNumberKey(int number)
        {
            return number switch
            {
                0 => Key.Digit0,
                1 => Key.Digit1,
                2 => Key.Digit2,
                3 => Key.Digit3,
                4 => Key.Digit4,
                5 => Key.Digit5,
                6 => Key.Digit6,
                7 => Key.Digit7,
                8 => Key.Digit8,
                9 => Key.Digit9,
                _ => Key.Digit0
            };
        }

        #region Debug Helpers

        [ContextMenu("Debug - Print All Groups")]
        private void DebugPrintAllGroups()
        {
            for (int i = 0; i < numberOfGroups; i++)
            {
                if (groups[i] != null)
                {
                    Debug.Log($"Group {i}: {groups[i].gameObject.name}");
                }
                else
                {
                    Debug.Log($"Group {i}: <empty>");
                }
            }
        }

        [ContextMenu("Debug - Clear All Groups")]
        private void DebugClearAllGroups()
        {
            ClearAllGroups();
        }

        #endregion
    }
}