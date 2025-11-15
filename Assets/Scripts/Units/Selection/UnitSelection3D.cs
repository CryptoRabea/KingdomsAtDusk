using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using RTS.Core.Events;

namespace RTS.Units
{
    /// <summary>
    /// 3D world-space box selection system.
    /// Uses a 3D cube in world space for selection instead of screen-space rectangle.
    /// Supports collision-based and bounds-based detection.
    /// </summary>
    public class UnitSelection3D : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private InputActionReference clickAction;
        [SerializeField] private InputActionReference positionAction;

        [Header("Selection Settings")]
        [SerializeField] private LayerMask selectableLayer;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask groundLayer; // For raycasting to ground

        [Header("3D Selection Box")]
        [SerializeField] private GameObject selectionBoxPrefab; // Visual representation of the box
        [SerializeField] private float boxHeight = 50f; // Height of the selection box
        [SerializeField] private Color boxColor = new Color(0, 1, 0, 0.3f);

        [Header("Detection Method")]
        [SerializeField] private DetectionType detectionType = DetectionType.Overlap;

        [Header("Selection Behavior")]
        [SerializeField] private bool enableMaxSelection = false;
        [SerializeField] private int maxSelectionCount = 50;
        [SerializeField] private bool sortByDistance = true;

        private List<UnitSelectable> selectedUnits = new List<UnitSelectable>();
        private Vector3 dragStartWorldPos;
        private bool isDragging = false;
        private GameObject selectionBoxInstance;
        private LineRenderer lineRenderer;

        public IReadOnlyList<UnitSelectable> SelectedUnits => selectedUnits;
        public int SelectionCount => selectedUnits.Count;

        public enum DetectionType
        {
            Overlap,        // Use Physics.OverlapBox
            Bounds,         // Check if renderer bounds intersect with box
            Position        // Check if unit position is within box
        }

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            // Create selection box visual if prefab not provided
            if (selectionBoxPrefab == null)
            {
                CreateDefaultSelectionBox();
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
                Vector2 currentScreenPos = positionAction.action.ReadValue<Vector2>();
                Vector3 currentWorldPos = GetWorldPosition(currentScreenPos);

                if (currentWorldPos != Vector3.zero)
                {
                    UpdateSelectionBox3D(dragStartWorldPos, currentWorldPos);
                }
            }
        }

        private void OnClickStarted(InputAction.CallbackContext context)
        {
            Vector2 mousePosition = positionAction.action.ReadValue<Vector2>();
            Vector3 worldPos = GetWorldPosition(mousePosition);

            if (worldPos != Vector3.zero)
            {
                dragStartWorldPos = worldPos;
                isDragging = true;

                // Create selection box instance
                if (selectionBoxInstance == null)
                {
                    if (selectionBoxPrefab != null)
                    {
                        selectionBoxInstance = Instantiate(selectionBoxPrefab);
                    }
                    else
                    {
                        CreateDefaultSelectionBox();
                    }
                }

                if (selectionBoxInstance != null)
                {
                    selectionBoxInstance.SetActive(true);
                }
            }
        }

        private void OnClickReleased(InputAction.CallbackContext context)
        {
            if (isDragging)
            {
                Vector2 mousePosition = positionAction.action.ReadValue<Vector2>();
                Vector3 worldPos = GetWorldPosition(mousePosition);

                if (worldPos != Vector3.zero)
                {
                    float distance = Vector3.Distance(dragStartWorldPos, worldPos);

                    if (distance > 0.5f) // Threshold to differentiate drag from click
                    {
                        PerformDragSelection3D(dragStartWorldPos, worldPos);
                    }
                    else
                    {
                        TrySingleSelection(mousePosition);
                    }
                }

                isDragging = false;
                if (selectionBoxInstance != null)
                {
                    selectionBoxInstance.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Gets world position from screen position by raycasting to ground.
        /// </summary>
        private Vector3 GetWorldPosition(Vector2 screenPos)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                return hit.point;
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Updates the 3D selection box visual.
        /// </summary>
        private void UpdateSelectionBox3D(Vector3 start, Vector3 end)
        {
            if (selectionBoxInstance == null)
                return;

            // Calculate box center and size
            Vector3 center = (start + end) / 2f;
            center.y = boxHeight / 2f; // Set height to half box height

            Vector3 size = new Vector3(
                Mathf.Abs(end.x - start.x),
                boxHeight,
                Mathf.Abs(end.z - start.z)
            );

            // Update position and scale
            selectionBoxInstance.transform.position = center;
            selectionBoxInstance.transform.localScale = size;

            // Update line renderer if present
            if (lineRenderer != null)
            {
                DrawBoxOutline(center, size);
            }
        }

        /// <summary>
        /// Performs 3D box selection.
        /// </summary>
        private void PerformDragSelection3D(Vector3 start, Vector3 end)
        {
            ClearSelection();

            // Calculate box center and size
            Vector3 center = (start + end) / 2f;
            center.y = boxHeight / 2f;

            Vector3 size = new Vector3(
                Mathf.Abs(end.x - start.x),
                boxHeight,
                Mathf.Abs(end.z - start.z)
            );

            List<UnitSelectable> unitsInBox = new List<UnitSelectable>();

            switch (detectionType)
            {
                case DetectionType.Overlap:
                    unitsInBox = GetUnitsViaOverlap(center, size);
                    break;

                case DetectionType.Bounds:
                    unitsInBox = GetUnitsViaBounds(center, size);
                    break;

                case DetectionType.Position:
                    unitsInBox = GetUnitsViaPosition(center, size);
                    break;
            }

            // Apply max selection limit and distance sorting
            if (enableMaxSelection && unitsInBox.Count > maxSelectionCount)
            {
                if (sortByDistance)
                {
                    unitsInBox.Sort((a, b) =>
                    {
                        float distA = Vector3.Distance(a.transform.position, start);
                        float distB = Vector3.Distance(b.transform.position, start);
                        return distA.CompareTo(distB);
                    });
                }

                unitsInBox = unitsInBox.GetRange(0, maxSelectionCount);
            }

            // Select all units
            foreach (var unit in unitsInBox)
            {
                SelectUnit(unit);
            }
        }

        /// <summary>
        /// Gets units using Physics.OverlapBox.
        /// </summary>
        private List<UnitSelectable> GetUnitsViaOverlap(Vector3 center, Vector3 size)
        {
            List<UnitSelectable> units = new List<UnitSelectable>();
            Collider[] colliders = Physics.OverlapBox(center, size / 2f, Quaternion.identity, selectableLayer);

            foreach (var collider in colliders)
            {
                var selectable = collider.GetComponent<UnitSelectable>();
                if (selectable != null)
                {
                    units.Add(selectable);
                }
            }

            return units;
        }

        /// <summary>
        /// Gets units by checking renderer bounds intersection.
        /// </summary>
        private List<UnitSelectable> GetUnitsViaBounds(Vector3 center, Vector3 size)
        {
            List<UnitSelectable> units = new List<UnitSelectable>();
            UnitSelectable[] allSelectables = FindObjectsByType<UnitSelectable>(FindObjectsSortMode.None);

            Bounds selectionBounds = new Bounds(center, size);

            foreach (var selectable in allSelectables)
            {
                if (selectable == null)
                    continue;

                var renderer = selectable.GetComponent<Renderer>();
                if (renderer != null && selectionBounds.Intersects(renderer.bounds))
                {
                    units.Add(selectable);
                }
            }

            return units;
        }

        /// <summary>
        /// Gets units by checking if their position is within the box.
        /// </summary>
        private List<UnitSelectable> GetUnitsViaPosition(Vector3 center, Vector3 size)
        {
            List<UnitSelectable> units = new List<UnitSelectable>();
            UnitSelectable[] allSelectables = FindObjectsByType<UnitSelectable>(FindObjectsSortMode.None);

            Bounds selectionBounds = new Bounds(center, size);

            foreach (var selectable in allSelectables)
            {
                if (selectable != null && selectionBounds.Contains(selectable.transform.position))
                {
                    units.Add(selectable);
                }
            }

            return units;
        }

        /// <summary>
        /// Single unit selection via raycast.
        /// </summary>
        private bool TrySingleSelection(Vector2 screenPosition)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, selectableLayer))
            {
                var selectable = hit.collider.GetComponent<UnitSelectable>();
                if (selectable != null)
                {
                    bool additive = Keyboard.current?.shiftKey.isPressed ?? false;

                    if (!additive)
                    {
                        ClearSelection();
                    }

                    SelectUnit(selectable);
                    return true;
                }
            }

            ClearSelection();
            return false;
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

        /// <summary>
        /// Creates a default selection box visual using LineRenderer.
        /// </summary>
        private void CreateDefaultSelectionBox()
        {
            selectionBoxInstance = new GameObject("SelectionBox3D");
            selectionBoxInstance.transform.SetParent(transform);

            // Add LineRenderer for box outline
            lineRenderer = selectionBoxInstance.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 16; // 12 edges of a box, with some reused for continuous lines
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = boxColor;
            lineRenderer.endColor = boxColor;
            lineRenderer.useWorldSpace = true;

            selectionBoxInstance.SetActive(false);
        }

        /// <summary>
        /// Draws box outline using LineRenderer.
        /// </summary>
        private void DrawBoxOutline(Vector3 center, Vector3 size)
        {
            Vector3 halfSize = size / 2f;

            // Define 8 corners of the box
            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            corners[1] = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            corners[2] = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            corners[3] = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            corners[4] = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            corners[5] = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            corners[6] = center + new Vector3(halfSize.x, halfSize.y, halfSize.z);
            corners[7] = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);

            // Draw the 12 edges of the box
            Vector3[] positions = new Vector3[16];

            // Bottom face
            positions[0] = corners[0];
            positions[1] = corners[1];
            positions[2] = corners[2];
            positions[3] = corners[3];
            positions[4] = corners[0];

            // Vertical edges
            positions[5] = corners[4];
            positions[6] = corners[5];
            positions[7] = corners[1];
            positions[8] = corners[5];
            positions[9] = corners[6];
            positions[10] = corners[2];
            positions[11] = corners[6];
            positions[12] = corners[7];
            positions[13] = corners[3];
            positions[14] = corners[7];
            positions[15] = corners[4];

            lineRenderer.SetPositions(positions);
        }

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (isDragging && selectionBoxInstance != null)
            {
                Gizmos.color = boxColor;
                Gizmos.DrawWireCube(selectionBoxInstance.transform.position, selectionBoxInstance.transform.localScale);
            }
        }

        #endregion
    }
}
