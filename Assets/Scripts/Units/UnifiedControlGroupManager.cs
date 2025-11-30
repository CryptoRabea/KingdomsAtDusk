using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using RTS.Core.Events;
using RTS.Units;
using RTS.Buildings;

namespace RTS.Core
{
    /// <summary>
    /// Unified control group manager that handles BOTH units and buildings.
    /// Each group slot (0-9) can contain EITHER units OR a building, not both.
    /// Assigning one type to a group automatically clears the other type.
    /// 
    /// Usage:
    /// - Select units -> Ctrl+1 -> Group 1 now contains units
    /// - Select building -> Ctrl+1 -> Group 1 now contains building (units cleared!)
    /// - Press 1 -> Recalls whatever is in group 1 (units or building)
    /// 
    /// ADD THIS TO ONE GAMEOBJECT IN YOUR SCENE (like "GameManager").
    /// REPLACES: UnitGroupManager and BuildingGroupManager (remove those if present)
    /// </summary>
    public class UnifiedControlGroupManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnitSelectionManager unitSelectionManager;
        [SerializeField] private BuildingSelectionManager buildingSelectionManager;
        [SerializeField] private Camera mainCamera;

        [Header("Settings")]
        [SerializeField] private int numberOfGroups = 10; // 0-9
        [SerializeField] private bool enableDoubleTapCenter = true;
        [SerializeField] private float doubleTapTime = 0.3f;
        [SerializeField] private bool clearDeadUnitsAndBuildings = true;
        [SerializeField] private int xOffset,yOffset,zOffset;


        [Header("Camera Settings")]
        [SerializeField] private float cameraDistance = 20f;
        [SerializeField] private float cameraHeight = 15f;
        [SerializeField] private float cameraMoveSpeed = 5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugMessages = true;

        // Group storage - each group can have EITHER units OR a building
        private Dictionary<int, ControlGroup> groups = new Dictionary<int, ControlGroup>();

        // For double-tap detection
        private int lastPressedGroup = -1;
        private float lastGroupPressTime = 0f;


        /// <summary>
        /// Represents a control group that can contain either units or a building
        /// </summary>
        private class ControlGroup
        {
            public ControlGroupType Type { get; set; }
            public List<UnitSelectable> Units { get; set; }
            public BuildingSelectable Building { get; set; }

            public ControlGroup()
            {
                Type = ControlGroupType.Empty;
                Units = new List<UnitSelectable>();
                Building = null;
            }

            public bool IsEmpty()
            {
                return Type == ControlGroupType.Empty;
            }

            public void Clear()
            {
                Type = ControlGroupType.Empty;
                Units.Clear();
                Building = null;
            }
        }

        private enum ControlGroupType
        {
            Empty,
            Units,
            Building
        }

        private void Awake()
        {
            if (unitSelectionManager == null)
            {
                unitSelectionManager = FindFirstObjectByType<UnitSelectionManager>();
            }

            if (buildingSelectionManager == null)
            {
                buildingSelectionManager = FindFirstObjectByType<BuildingSelectionManager>();
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // Initialize groups
            for (int i = 0; i < numberOfGroups; i++)
            {
                groups[i] = new ControlGroup();
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
                        // Save current selection to group
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
        /// Saves current selection (units OR building) to a group.
        /// OVERWRITES whatever was in that group before.
        /// </summary>
        public void SaveGroup(int groupNumber)
        {
            if (groupNumber < 0 || groupNumber >= numberOfGroups)
            {
                Debug.LogWarning($"Invalid group number: {groupNumber}");
                return;
            }

            // Check what's currently selected
            bool hasUnitsSelected = unitSelectionManager != null && unitSelectionManager.SelectionCount > 0;
            bool hasBuildingSelected = buildingSelectionManager != null && buildingSelectionManager.CurrentlySelectedBuilding != null;

            if (!hasUnitsSelected && !hasBuildingSelected)
            {
                if (showDebugMessages)
                    Debug.Log($"Nothing selected to save to group {groupNumber}");
                return;
            }

            // Clear the group first (this is the key - overwrites previous assignment)
            groups[groupNumber].Clear();

            // Save units if units are selected
            if (hasUnitsSelected)
            {
                groups[groupNumber].Type = ControlGroupType.Units;
                groups[groupNumber].Units.Clear();

                foreach (var unit in unitSelectionManager.SelectedUnits)
                {
                    if (unit != null)
                    {
                        groups[groupNumber].Units.Add(unit);
                    }
                }

                if (showDebugMessages)
                    Debug.Log($"[SWORD] Saved {groups[groupNumber].Units.Count} units to group {groupNumber} (overwrote previous assignment)");

                EventBus.Publish(new UnitGroupSavedEvent(groupNumber, groups[groupNumber].Units.Count));
            }
            // Save building if building is selected
            else if (hasBuildingSelected)
            {
                groups[groupNumber].Type = ControlGroupType.Building;
                groups[groupNumber].Building = buildingSelectionManager.CurrentlySelectedBuilding;

                if (showDebugMessages)
                    Debug.Log($"[BUILDING] Saved building '{groups[groupNumber].Building.gameObject.name}' to group {groupNumber} (overwrote previous assignment)");

                EventBus.Publish(new BuildingGroupSavedEvent(groupNumber, groups[groupNumber].Building.gameObject.name));
            }
        }

        /// <summary>
        /// Recalls a group (units or building).
        /// Double-tap to center camera.
        /// </summary>
        public void RecallGroup(int groupNumber)
        {
            if (groupNumber < 0 || groupNumber >= numberOfGroups)
            {
                Debug.LogWarning($"Invalid group number: {groupNumber}");
                return;
            }

            // Clean up dead/null objects if enabled
            if (clearDeadUnitsAndBuildings)
            {
                CleanupGroup(groupNumber);
            }

            if (groups[groupNumber].IsEmpty())
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

            // Recall based on type
            if (groups[groupNumber].Type == ControlGroupType.Units)
            {
                RecallUnitGroup(groupNumber, isDoubleTap);
            }
            else if (groups[groupNumber].Type == ControlGroupType.Building)
            {
                RecallBuildingGroup(groupNumber, isDoubleTap);
            }
        }

        /// <summary>
        /// Recalls a unit group
        /// </summary>
        private void RecallUnitGroup(int groupNumber, bool isDoubleTap)
        {
            var group = groups[groupNumber];

            if (group.Units.Count == 0)
            {
                if (showDebugMessages)
                    Debug.Log($"Unit group {groupNumber} is empty");
                return;
            }

            // Deselect all currently selected units
            if (unitSelectionManager != null)
            {
                foreach (var unit in unitSelectionManager.SelectedUnits.ToList())
                {
                    unit?.Deselect();
                }
            }

            // Select units from the group
            List<UnitSelectable> validUnits = new List<UnitSelectable>();
            foreach (var unit in group.Units)
            {
                if (unit != null)
                {
                    unit.Select();
                    validUnits.Add(unit);
                }
            }

            if (showDebugMessages)
                Debug.Log($"[SWORD] Recalled unit group {groupNumber}: {validUnits.Count} units");

            // Center camera on group if double-tapped
            if (isDoubleTap && validUnits.Count > 0)
            {
                CenterCameraOnUnits(validUnits);
            }

            // Publish event
            EventBus.Publish(new UnitGroupRecalledEvent(groupNumber, validUnits.Count, isDoubleTap));
        }

        /// <summary>
        /// Recalls a building group
        /// </summary>
        private void RecallBuildingGroup(int groupNumber, bool isDoubleTap)
        {
            var group = groups[groupNumber];

            if (group.Building == null)
            {
                if (showDebugMessages)
                    Debug.Log($"Building group {groupNumber} is empty");
                return;
            }

            // Deselect current building
            if (buildingSelectionManager != null && buildingSelectionManager.CurrentlySelectedBuilding != null)
            {
                buildingSelectionManager.CurrentlySelectedBuilding.Deselect();
            }

            // Select the building from the group
            group.Building.Select();

            if (showDebugMessages)
                Debug.Log($"[BUILDING] Recalled building group {groupNumber}: {group.Building.gameObject.name}");

            // Center camera on building if double-tapped
            if (isDoubleTap)
            {
                CenterCameraOnBuilding(group.Building);
            }

            // Publish event
            EventBus.Publish(new BuildingGroupRecalledEvent(groupNumber, group.Building.gameObject.name, isDoubleTap));
        }

        /// <summary>
        /// Cleans up dead/null units and buildings from a group
        /// </summary>
        private void CleanupGroup(int groupNumber)
        {
            var group = groups[groupNumber];

            if (group.Type == ControlGroupType.Units)
            {
                // Remove null/dead units
                group.Units.RemoveAll(unit => unit == null);

                // If no units left, clear the group
                if (group.Units.Count == 0)
                {
                    group.Clear();
                }
            }
            else if (group.Type == ControlGroupType.Building)
            {
                // Check if building is destroyed
                if (group.Building == null || group.Building.gameObject == null)
                {
                    group.Clear();
                }
            }
        }

        /// <summary>
        /// Centers camera on a list of units
        /// </summary>
        private void CenterCameraOnUnits(List<UnitSelectable> units)
        {
            if (mainCamera == null || units.Count == 0)
                return;

            // Calculate center position
            Vector3 center = Vector3.zero;
            foreach (var unit in units)
            {
                center += unit.transform.position;
            }
            center /= units.Count;

            // Calculate camera position
            Vector3 targetPosition = center + new Vector3(0, cameraHeight, -cameraDistance+zOffset);

            // Move camera
            if (cameraMoveSpeed > 0)
            {
                StartCoroutine(SmoothCameraMove(targetPosition, center));
            }
            else
            {
                mainCamera.transform.position = targetPosition;
                mainCamera.transform.LookAt(center);
            }

            if (showDebugMessages)
                Debug.Log($"[CAMERA] Centered camera on {units.Count} units");
        }

        /// <summary>
        /// Centers camera on a building
        /// </summary>
        private void CenterCameraOnBuilding(BuildingSelectable building)
        {
            if (mainCamera == null || building == null)
                return;

            Vector3 buildingPosition = building.transform.position;
            Vector3 targetPosition = buildingPosition + new Vector3(0, cameraHeight, -cameraDistance);

            if (cameraMoveSpeed > 0)
            {
                StartCoroutine(SmoothCameraMove(targetPosition, buildingPosition));
            }
            else
            {
                mainCamera.transform.position = targetPosition;
                mainCamera.transform.LookAt(buildingPosition);
            }

            if (showDebugMessages)
                Debug.Log($"[CAMERA] Centered camera on building '{building.gameObject.name}'");
        }

        /// <summary>
        /// Smoothly moves camera to target position
        /// </summary>
        private System.Collections.IEnumerator SmoothCameraMove(Vector3 targetPosition, Vector3 lookAtPosition)
        {
            float elapsed = 0f;
            Vector3 startPosition = mainCamera.transform.position;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * cameraMoveSpeed;
                mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed);
                yield return null;
            }

            mainCamera.transform.position = targetPosition;
        }

        /// <summary>
        /// Gets info about what's in a group (for UI or debugging)
        /// </summary>
        public string GetGroupInfo(int groupNumber)
        {
            if (groupNumber < 0 || groupNumber >= numberOfGroups)
                return "Invalid group";

            var group = groups[groupNumber];

            if (group.IsEmpty())
                return "Empty";

            if (group.Type == ControlGroupType.Units)
                return $"{group.Units.Count} units";
            else
                return group.Building != null ? group.Building.gameObject.name : "Empty";
        }

        /// <summary>
        /// Clears a specific group
        /// </summary>
        public void ClearGroup(int groupNumber)
        {
            if (groupNumber >= 0 && groupNumber < numberOfGroups)
            {
                groups[groupNumber].Clear();
                if (showDebugMessages)
                    Debug.Log($"Cleared group {groupNumber}");
            }
        }

        /// <summary>
        /// Clears all groups
        /// </summary>
        public void ClearAllGroups()
        {
            foreach (var group in groups.Values)
            {
                group.Clear();
            }
            if (showDebugMessages)
                Debug.Log("Cleared all groups");
        }

        /// <summary>
        /// Gets the Unity Key for a number (0-9)
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
            Debug.Log("=== Control Group Status ===");
            for (int i = 0; i < numberOfGroups; i++)
            {
                string info = GetGroupInfo(i);
                Debug.Log($"Group {i}: {info}");
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
