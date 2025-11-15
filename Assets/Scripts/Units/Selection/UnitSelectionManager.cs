using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using RTS.Core.Events;

namespace RTS.Units
{
    /// <summary>
    /// Manages unit selection using modern Input System and events.
    /// ADD THIS TO ONE GAMEOBJECT IN YOUR SCENE (like a "SelectionManager").
    /// DO NOT ADD TO UNITS!
    /// </summary>
    public class UnitSelectionManager : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private InputActionReference clickAction;
        [SerializeField] private InputActionReference positionAction;

        [Header("Selection Settings")]
        [SerializeField] private LayerMask selectableLayer;
        [SerializeField] private Camera mainCamera;

        [Header("Drag Selection")]
        [SerializeField] private Image selectionBoxUI;
        [SerializeField] private Color selectionBoxColor = new Color(0, 1, 0, 0.2f);
        [SerializeField] private float dragThreshold = 5f; // Minimum pixels to count as drag

        private List<UnitSelectable> selectedUnits = new List<UnitSelectable>();
        private Vector2 dragStartPosition;
        private bool isDragging = false;
        private RectTransform selectionBoxRect;

        public IReadOnlyList<UnitSelectable> SelectedUnits => selectedUnits;
        public int SelectionCount => selectedUnits.Count;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (selectionBoxUI != null)
            {
                selectionBoxRect = selectionBoxUI.GetComponent<RectTransform>();
                selectionBoxUI.color = selectionBoxColor;
                selectionBoxUI.gameObject.SetActive(false);

                // Ensure pivot and anchors are at bottom-left (0, 0) for proper corner-to-corner scaling
                selectionBoxRect.pivot = new Vector2(0, 0);
                selectionBoxRect.anchorMin = new Vector2(0, 0);
                selectionBoxRect.anchorMax = new Vector2(0, 0);
            }
        }

        private void OnEnable()
        {
            if (clickAction != null)
            {
                clickAction.action.Enable();
                clickAction.action.started += OnClickStarted;
                clickAction.action.canceled += OnClickReleased;
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
                clickAction.action.started -= OnClickStarted;
                clickAction.action.canceled -= OnClickReleased;
            }

            if (positionAction != null)
            {
                positionAction.action.Disable();
            }
        }

        private void Update()
        {
            if (isDragging && positionAction != null)
            {
                Vector2 currentPosition = positionAction.action.ReadValue<Vector2>();
                float dragDistance = Vector2.Distance(dragStartPosition, currentPosition);

                // Only show selection box if we've dragged past the threshold
                if (dragDistance > dragThreshold)
                {
                    if (selectionBoxUI != null && !selectionBoxUI.gameObject.activeSelf)
                    {
                        selectionBoxUI.gameObject.SetActive(true);
                    }
                    UpdateSelectionBox(dragStartPosition, currentPosition);
                }
            }
        }

        private void OnClickStarted(InputAction.CallbackContext context)
        {
            Vector2 mousePosition = positionAction.action.ReadValue<Vector2>();

            // Start tracking for potential drag or click
            dragStartPosition = mousePosition;
            isDragging = true;

            // Don't show selection box yet - wait for drag threshold in Update
        }

        private void OnClickReleased(InputAction.CallbackContext context)
        {
            if (isDragging)
            {
                Vector2 mousePosition = positionAction.action.ReadValue<Vector2>();
                float dragDistance = Vector2.Distance(dragStartPosition, mousePosition);

                // Determine if this was a drag or a single click
                if (dragDistance > dragThreshold)
                {
                    // Was a drag - perform drag selection
                    PerformDragSelection(dragStartPosition, mousePosition);
                }
                else
                {
                    // Was a click - perform single selection
                    TrySingleSelection(mousePosition);
                }

                isDragging = false;
                if (selectionBoxUI != null)
                {
                    selectionBoxUI.gameObject.SetActive(false);
                }
            }
        }

        private bool TrySingleSelection(Vector2 screenPosition)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, selectableLayer))
            {
                var selectable = hit.collider.GetComponent<UnitSelectable>();
                if (selectable != null)
                {
                    // Add to selection or replace (based on shift key)
                    bool additive = Keyboard.current?.shiftKey.isPressed ?? false;
                    
                    if (!additive)
                    {
                        ClearSelection();
                    }

                    SelectUnit(selectable);
                    return true;
                }
            }

            // Clicked empty space - clear selection
            ClearSelection();
            return false;
        }

        private void PerformDragSelection(Vector2 start, Vector2 end)
        {
            ClearSelection();

            Rect selectionRect = GetScreenRect(start, end);
            
            // Find all selectables in scene
            UnitSelectable[] allSelectables = FindObjectsByType<UnitSelectable>(FindObjectsSortMode.None);

            foreach (var selectable in allSelectables)
            {
                if (selectable == null) continue;

                Vector3 screenPos = mainCamera.WorldToScreenPoint(selectable.transform.position);
                
                // Check if within selection rectangle
                if (selectionRect.Contains(screenPos))
                {
                    SelectUnit(selectable);
                }
            }
        }

        private void UpdateSelectionBox(Vector2 start, Vector2 end)
        {
            if (selectionBoxRect == null) return;

            // Calculate the difference between start and end
            Vector2 diff = end - start;

            // Determine the actual starting corner based on drag direction
            // We want the box to always start from the drag start point
            Vector2 boxStart = new Vector2(
                diff.x < 0 ? start.x + diff.x : start.x,  // If dragging left, start from end.x
                diff.y < 0 ? start.y + diff.y : start.y   // If dragging down, start from end.y
            );

            // Size is always positive (absolute values)
            Vector2 boxSize = new Vector2(
                Mathf.Abs(diff.x),
                Mathf.Abs(diff.y)
            );

            // Set position to the calculated starting corner
            selectionBoxRect.anchoredPosition = boxStart;

            // Set size (always positive)
            selectionBoxRect.sizeDelta = boxSize;
        }

        private Rect GetScreenRect(Vector2 start, Vector2 end)
        {
            Vector2 min = Vector2.Min(start, end);
            Vector2 max = Vector2.Max(start, end);
            return new Rect(min, max - min);
        }

        private void SelectUnit(UnitSelectable unit)
        {
            if (!selectedUnits.Contains(unit))
            {
                selectedUnits.Add(unit);
                unit.Select();
            }

            EventBus.Publish(new SelectionChangedEvent(selectedUnits.Count));
        }

        private void ClearSelection()
        {
            foreach (var unit in selectedUnits)
            {
                if (unit != null)
                {
                    unit.Deselect();
                }
            }

            selectedUnits.Clear();
            EventBus.Publish(new SelectionChangedEvent(0));
        }

        #region Command Methods (for RTS controls)

        public void MoveSelectedUnits(Vector3 destination)
        {
            foreach (var unit in selectedUnits)
            {
                if (unit == null) continue;

                var movement = unit.GetComponent<UnitMovement>();
                movement?.SetDestination(destination);
            }
        }

        public void AttackMoveSelectedUnits(Vector3 destination)
        {
            // Implement attack-move logic
            MoveSelectedUnits(destination);
        }

        #endregion
    }
}
