using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using RTS.Core.Events;
using RTS.RTSCamera;

namespace RTS.Units
{
    /// <summary>
    /// Manages unit groups/squads for quick access.
    /// Press Ctrl+Number to save current selection to a group.
    /// Press Number to recall a group.
    /// Double-press Number to recall and center camera on group.
    /// </summary>
    public class UnitGroupManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnitSelectionManager selectionManager;
        [SerializeField] private Camera mainCamera;

        [Header("Settings")]
        [SerializeField] private int numberOfGroups = 10; // 0-9
        [SerializeField] private bool enableDoubleTapCenter = true;
        [SerializeField] private float doubleTapTime = 0.3f;
        [SerializeField] private bool clearEmptyGroups = true;
        [SerializeField] private int xOffset, yOffset, zOffset;
        [Header("Visual Feedback")]
        [SerializeField] private bool showDebugMessages = true;

        // Group storage: Key = group number, Value = list of units
        private Dictionary<int, List<UnitSelectable>> groups = new Dictionary<int, List<UnitSelectable>>();

        // For double-tap detection
        private int lastPressedGroup = -1;
        private float lastGroupPressTime = 0f;

        private void Awake()
        {
            if (selectionManager == null)
            {
                selectionManager = FindFirstObjectByType<UnitSelectionManager>();
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // Initialize groups
            for (int i = 0; i < numberOfGroups; i++)
            {
                groups[i] = new List<UnitSelectable>();
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
        /// Saves the current selection to a group.
        /// </summary>
        public void SaveGroup(int groupNumber)
        {
            if (groupNumber < 0 || groupNumber >= numberOfGroups)
            {
                return;
            }

            if (selectionManager == null || selectionManager.SelectionCount == 0)
            {
                if (showDebugMessages)
                return;
            }

            // Clear existing group
            groups[groupNumber].Clear();

            // Add current selection to group
            foreach (var unit in selectionManager.SelectedUnits)
            {
                if (unit != null)
                {
                    groups[groupNumber].Add(unit);
                }
            }

            if (showDebugMessages)

            // Publish event
            EventBus.Publish(new UnitGroupSavedEvent(groupNumber, groups[groupNumber].Count));
        }

        /// <summary>
        /// Recalls a saved group and selects those units.
        /// </summary>
        public void RecallGroup(int groupNumber)
        {
            if (groupNumber < 0 || groupNumber >= numberOfGroups)
            {
                return;
            }

            if (selectionManager == null)
                return;

            // Clean up dead/null units if enabled
            if (clearEmptyGroups)
            {
                groups[groupNumber].RemoveAll(unit => unit == null);
            }

            if (groups[groupNumber].Count == 0)
            {
                if (showDebugMessages)
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

            // Select units in the group
            // Note: We need to manually select since UnitSelectionManager doesn't have a public method for this
            // We'll clear current selection and select each unit
            foreach (var unit in selectionManager.SelectedUnits.ToList())
            {
                unit?.Deselect();
            }

            List<UnitSelectable> validUnits = new List<UnitSelectable>();
            foreach (var unit in groups[groupNumber])
            {
                if (unit != null)
                {
                    unit.Select();
                    validUnits.Add(unit);
                }
            }

            if (showDebugMessages)

            // Center camera on group if double-tapped
            if (isDoubleTap && validUnits.Count > 0)
            {
                CenterCameraOnUnits(validUnits);
            }

            // Publish event
            EventBus.Publish(new UnitGroupRecalledEvent(groupNumber, validUnits.Count, isDoubleTap));
        }

        /// <summary>
        /// Centers the camera on a group of units.
        /// </summary>
        private void CenterCameraOnUnits(List<UnitSelectable> units)
        {
            if (units.Count == 0 || mainCamera == null)
                return;

            // Calculate center position
            Vector3 centerPos = Vector3.zero;
            foreach (var unit in units)
            {
                if (unit != null)
                {
                    centerPos += unit.transform.position;
                }
            }
            centerPos /= units.Count;

            // Move camera to center (keeping same height)
            Vector3 targetPos = new Vector3(centerPos.x+xOffset, mainCamera.transform.position.y+yOffset, centerPos.z+zOffset);

            // Use the RTS camera controller if available
            if (mainCamera.TryGetComponent<RTSCameraController>(out var cameraController))
            {
                // Just set position directly - the camera controller handles the rest
                mainCamera.transform.position = targetPos;
            }
            else
            {
                // Fallback to direct positioning
                mainCamera.transform.position = targetPos;
            }

        }

        /// <summary>
        /// Gets the Key enum for a number (0-9).
        /// </summary>
        private Key GetNumberKey(int number)
        {
            switch (number)
            {
                case 0: return Key.Digit0;
                case 1: return Key.Digit1;
                case 2: return Key.Digit2;
                case 3: return Key.Digit3;
                case 4: return Key.Digit4;
                case 5: return Key.Digit5;
                case 6: return Key.Digit6;
                case 7: return Key.Digit7;
                case 8: return Key.Digit8;
                case 9: return Key.Digit9;
                default: return Key.Digit0;
            }
        }

        /// <summary>
        /// Gets the units in a specific group.
        /// </summary>
        public IReadOnlyList<UnitSelectable> GetGroup(int groupNumber)
        {
            if (groupNumber >= 0 && groupNumber < numberOfGroups)
            {
                return groups[groupNumber].AsReadOnly();
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
                groups[groupNumber].Clear();
            }
        }

        /// <summary>
        /// Clears all groups.
        /// </summary>
        public void ClearAllGroups()
        {
            foreach (var group in groups.Values)
            {
                group.Clear();
            }
        }

        #region Debug Helpers

        [ContextMenu("Debug - Print All Groups")]
        private void DebugPrintAllGroups()
        {
            for (int i = 0; i < numberOfGroups; i++)
            {
                if (groups[i].Count > 0)
                {
                }
            }
        }

        #endregion
    }
}
