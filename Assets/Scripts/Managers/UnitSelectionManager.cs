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
                UpdateSelectionBox(dragStartPosition, currentPosition);
            }
        }

        private void OnClickStarted(InputAction.CallbackContext context)
        {
            Vector2 mousePosition = positionAction.action.ReadValue<Vector2>();

            // Try single click selection first
            if (TrySingleSelection(mousePosition))
            {
                isDragging = false;
                return;
            }

            // Start drag selection
            dragStartPosition = mousePosition;
            isDragging = true;

            if (selectionBoxUI != null)
            {
                selectionBoxUI.gameObject.SetActive(true);
            }
        }

        private void OnClickReleased(InputAction.CallbackContext context)
        {
            if (isDragging)
            {
                Vector2 mousePosition = positionAction.action.ReadValue<Vector2>();
                PerformDragSelection(dragStartPosition, mousePosition);
                
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

            Vector2 boxStart = start;
            Vector2 boxEnd = end;

            Vector2 center = (boxStart + boxEnd) / 2f;
            selectionBoxRect.position = center;

            Vector2 size = new Vector2(
                Mathf.Abs(boxStart.x - boxEnd.x),
                Mathf.Abs(boxStart.y - boxEnd.y)
            );
            selectionBoxRect.sizeDelta = size;

            Debug.Log($"Box Pos: {selectionBoxRect.position}, Size: {selectionBoxRect.sizeDelta}");

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
