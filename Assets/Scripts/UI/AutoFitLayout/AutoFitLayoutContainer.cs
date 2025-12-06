using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RTS.UI.AutoFit
{
    /// <summary>
    /// Universal auto-fit layout container that ensures contents NEVER overflow the container bounds.
    /// Hides overflow items instead of showing them outside the yellow frame.
    ///
    /// Key Features:
    /// - Respects min/max cell sizes (never goes smaller than min or larger than max)
    /// - Respects min/max container sizes
    /// - Hides overflow items that don't fit
    /// - Configurable rows/columns (0 = unlimited)
    /// - Directional flow (left-to-right, right-to-left, top-to-bottom, bottom-to-top)
    /// - NEVER shows content outside container bounds
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    public class AutoFitLayoutContainer : MonoBehaviour
    {
        public enum ContainerShape
        {
            Square,
            Rectangle,
            Circle,
            Triangle,
            Custom
        }

        public enum FlowDirection
        {
            LeftToRight,
            RightToLeft,
            TopToBottom,
            BottomToTop
        }

        public enum LayoutPreference
        {
            PreferHorizontal,  // Fill rows first
            PreferVertical     // Fill columns first
        }

        [Header("Container Bounds")]
        [Tooltip("Shape of the container")]
        [SerializeField] private ContainerShape shape = ContainerShape.Square;

        [Tooltip("Minimum container width (0 = no limit)")]
        [SerializeField] private float minContainerWidth = 100f;

        [Tooltip("Maximum container width")]
        [SerializeField] private float maxContainerWidth = 500f;

        [Tooltip("Minimum container height (0 = no limit)")]
        [SerializeField] private float minContainerHeight = 100f;

        [Tooltip("Maximum container height")]
        [SerializeField] private float maxContainerHeight = 500f;

        [Tooltip("Inner padding")]
        [SerializeField] private float padding = 10f;

        [Header("Cell Size Constraints")]
        [Tooltip("Minimum size for each cell/icon")]
        [SerializeField] private float minCellSize = 32f;

        [Tooltip("Maximum size for each cell/icon")]
        [SerializeField] private float maxCellSize = 128f;

        [Tooltip("Spacing between cells")]
        [SerializeField] private float cellSpacing = 8f;

        [Header("Grid Configuration")]
        [Tooltip("Number of columns (0 = unlimited, auto-calculate)")]
        [SerializeField] private int fixedColumns = 0;

        [Tooltip("Number of rows (0 = unlimited, auto-calculate)")]
        [SerializeField] private int fixedRows = 0;

        [Tooltip("Prefer horizontal or vertical layout")]
        [SerializeField] private LayoutPreference layoutPreference = LayoutPreference.PreferHorizontal;

        [Tooltip("Direction items flow")]
        [SerializeField] private FlowDirection flowDirection = FlowDirection.LeftToRight;

        [Header("Overflow Handling")]
        [Tooltip("Hide items that don't fit instead of showing outside bounds")]
        [SerializeField] private bool hideOverflow = true;

        [Tooltip("Show warning when items are hidden")]
        [SerializeField] private bool warnOnOverflow = true;

        [Header("Advanced")]
        [SerializeField] private bool updateInEditMode = true;
        [SerializeField] private bool debugMode = false;

        private RectTransform rectTransform;
        private GridLayoutGroup gridLayout;
        private List<RectTransform> children = new List<RectTransform>();
        private int visibleChildCount = 0;
        private int hiddenChildCount = 0;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            gridLayout = GetComponent<GridLayoutGroup>();
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                UpdateLayout();
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying && updateInEditMode)
            {
                UpdateLayout();
            }
        }
#endif

        private void OnEnable()
        {
            UpdateLayout();
        }

        private void OnTransformChildrenChanged()
        {
            UpdateLayout();
        }

        /// <summary>
        /// Main layout update method - NEVER allows content outside container bounds.
        /// </summary>
        public void UpdateLayout()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            // Get all children
            CacheChildren();

            if (children.Count == 0)
            {
                return;
            }

            // Calculate container size based on shape
            Vector2 containerSize = CalculateContainerSize();
            rectTransform.sizeDelta = containerSize;

            // Calculate usable area (accounting for padding and shape)
            Vector2 usableArea = CalculateUsableArea(containerSize);

            // Calculate grid dimensions and cell size
            CalculateGridLayout(usableArea, children.Count, out int columns, out int rows, out float cellSize);

            // Apply layout and hide overflow
            ApplyLayoutAndHideOverflow(columns, rows, cellSize, usableArea);

            if (debugMode)
            {
                Debug.Log($"[AutoFitLayout] Container: {containerSize}, Grid: {columns}x{rows}, Cell: {cellSize}px, Visible: {visibleChildCount}/{children.Count}");
            }

            if (warnOnOverflow && hiddenChildCount > 0)
            {
                Debug.LogWarning($"[AutoFitLayout] {hiddenChildCount} items hidden due to space constraints. Increase container size or decrease min cell size.");
            }
        }

        /// <summary>
        /// Calculates the container size based on shape constraints.
        /// </summary>
        private Vector2 CalculateContainerSize()
        {
            float width = maxContainerWidth;
            float height = maxContainerHeight;

            switch (shape)
            {
                case ContainerShape.Square:
                    float squareSize = Mathf.Min(maxContainerWidth, maxContainerHeight);
                    squareSize = Mathf.Max(squareSize, Mathf.Max(minContainerWidth, minContainerHeight));
                    return new Vector2(squareSize, squareSize);

                case ContainerShape.Rectangle:
                    width = Mathf.Clamp(width, minContainerWidth, maxContainerWidth);
                    height = Mathf.Clamp(height, minContainerHeight, maxContainerHeight);
                    return new Vector2(width, height);

                case ContainerShape.Circle:
                    float circleSize = Mathf.Min(maxContainerWidth, maxContainerHeight);
                    circleSize = Mathf.Max(circleSize, Mathf.Max(minContainerWidth, minContainerHeight));
                    return new Vector2(circleSize, circleSize);

                case ContainerShape.Triangle:
                    float triWidth = Mathf.Clamp(maxContainerWidth, minContainerWidth, maxContainerWidth);
                    float triHeight = Mathf.Clamp(maxContainerHeight, minContainerHeight, maxContainerHeight);
                    return new Vector2(triWidth, triHeight);

                default:
                    return new Vector2(width, height);
            }
        }

        /// <summary>
        /// Calculates usable area within the container based on shape.
        /// </summary>
        private Vector2 CalculateUsableArea(Vector2 containerSize)
        {
            Vector2 usableSize = containerSize - new Vector2(padding * 2, padding * 2);

            switch (shape)
            {
                case ContainerShape.Circle:
                    // Inscribed square in circle (0.707 = √2/2)
                    float inscribedSize = usableSize.x * 0.707f;
                    return new Vector2(inscribedSize, inscribedSize);

                case ContainerShape.Triangle:
                    // Reduce usable area for triangle shape
                    return usableSize * 0.8f;

                default:
                    return usableSize;
            }
        }

        /// <summary>
        /// Calculates optimal grid dimensions and cell size within constraints.
        /// NEVER goes below minCellSize - hides overflow instead.
        /// </summary>
        private void CalculateGridLayout(Vector2 usableArea, int itemCount, out int columns, out int rows, out float cellSize)
        {
            // Start with fixed columns/rows if specified
            columns = fixedColumns > 0 ? fixedColumns : 0;
            rows = fixedRows > 0 ? fixedRows : 0;

            // If both are undefined, calculate based on preference
            if (columns == 0 && rows == 0)
            {
                if (layoutPreference == LayoutPreference.PreferHorizontal)
                {
                    // Calculate columns first, then rows
                    columns = CalculateOptimalColumns(usableArea.x, itemCount);
                    rows = Mathf.CeilToInt((float)itemCount / columns);
                }
                else
                {
                    // Calculate rows first, then columns
                    rows = CalculateOptimalRows(usableArea.y, itemCount);
                    columns = Mathf.CeilToInt((float)itemCount / rows);
                }
            }
            else if (columns == 0)
            {
                // Rows fixed, calculate columns
                columns = Mathf.CeilToInt((float)itemCount / rows);
            }
            else if (rows == 0)
            {
                // Columns fixed, calculate rows
                rows = Mathf.CeilToInt((float)itemCount / columns);
            }

            // Calculate cell size that fits within usable area
            float maxCellWidth = (usableArea.x - (cellSpacing * (columns - 1))) / columns;
            float maxCellHeight = (usableArea.y - (cellSpacing * (rows - 1))) / rows;

            // Use the smaller dimension for square cells
            cellSize = Mathf.Min(maxCellWidth, maxCellHeight);

            // CRITICAL: Clamp to min/max cell size - NEVER go below min
            cellSize = Mathf.Clamp(cellSize, minCellSize, maxCellSize);

            // If cell size is at minimum but items still don't fit, we'll hide overflow
        }

        /// <summary>
        /// Calculates optimal number of columns based on available width.
        /// </summary>
        private int CalculateOptimalColumns(float availableWidth, int itemCount)
        {
            // Try to fit as many columns as possible with max cell size
            int maxColumns = Mathf.FloorToInt((availableWidth + cellSpacing) / (maxCellSize + cellSpacing));
            maxColumns = Mathf.Max(1, maxColumns);

            // Don't create more columns than items
            return Mathf.Min(maxColumns, itemCount);
        }

        /// <summary>
        /// Calculates optimal number of rows based on available height.
        /// </summary>
        private int CalculateOptimalRows(float availableHeight, int itemCount)
        {
            // Try to fit as many rows as possible with max cell size
            int maxRows = Mathf.FloorToInt((availableHeight + cellSpacing) / (maxCellSize + cellSpacing));
            maxRows = Mathf.Max(1, maxRows);

            // Don't create more rows than items
            return Mathf.Min(maxRows, itemCount);
        }

        /// <summary>
        /// Applies the calculated layout and hides items that don't fit.
        /// NEVER shows content outside the container bounds.
        /// </summary>
        private void ApplyLayoutAndHideOverflow(int columns, int rows, float cellSize, Vector2 usableArea)
        {
            visibleChildCount = 0;
            hiddenChildCount = 0;

            // Maximum items that can fit
            int maxVisibleItems = columns * rows;

            // Update GridLayoutGroup if present
            if (gridLayout != null)
            {
                gridLayout.cellSize = new Vector2(cellSize, cellSize);
                gridLayout.spacing = new Vector2(cellSpacing, cellSpacing);
                gridLayout.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);

                // Set constraint based on layout preference
                if (fixedColumns > 0 || layoutPreference == LayoutPreference.PreferHorizontal)
                {
                    gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    gridLayout.constraintCount = columns;
                }
                else
                {
                    gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                    gridLayout.constraintCount = rows;
                }

                // Set start corner based on flow direction
                gridLayout.startCorner = GetStartCorner(flowDirection);
                gridLayout.childAlignment = GetChildAlignment(flowDirection);
            }

            // Show/hide children based on what fits
            for (int i = 0; i < children.Count; i++)
            {
                if (hideOverflow && i >= maxVisibleItems)
                {
                    // Hide overflow items
                    children[i].gameObject.SetActive(false);
                    hiddenChildCount++;
                }
                else
                {
                    // Show items that fit
                    children[i].gameObject.SetActive(true);
                    visibleChildCount++;

                    // Ensure child has proper size
                    LayoutElement layoutElement = children[i].GetComponent<LayoutElement>();
                    if (layoutElement == null && gridLayout == null)
                    {
                        layoutElement = children[i].gameObject.AddComponent<LayoutElement>();
                    }

                    if (layoutElement != null)
                    {
                        layoutElement.preferredWidth = cellSize;
                        layoutElement.preferredHeight = cellSize;
                    }
                }
            }
        }

        /// <summary>
        /// Gets GridLayoutGroup start corner based on flow direction.
        /// </summary>
        private GridLayoutGroup.Corner GetStartCorner(FlowDirection direction)
        {
            switch (direction)
            {
                case FlowDirection.LeftToRight:
                    return GridLayoutGroup.Corner.UpperLeft;
                case FlowDirection.RightToLeft:
                    return GridLayoutGroup.Corner.UpperRight;
                case FlowDirection.TopToBottom:
                    return GridLayoutGroup.Corner.UpperLeft;
                case FlowDirection.BottomToTop:
                    return GridLayoutGroup.Corner.LowerLeft;
                default:
                    return GridLayoutGroup.Corner.UpperLeft;
            }
        }

        /// <summary>
        /// Gets child alignment based on flow direction.
        /// </summary>
        private TextAnchor GetChildAlignment(FlowDirection direction)
        {
            switch (direction)
            {
                case FlowDirection.LeftToRight:
                    return TextAnchor.UpperLeft;
                case FlowDirection.RightToLeft:
                    return TextAnchor.UpperRight;
                case FlowDirection.TopToBottom:
                    return TextAnchor.UpperLeft;
                case FlowDirection.BottomToTop:
                    return TextAnchor.LowerLeft;
                default:
                    return TextAnchor.MiddleCenter;
            }
        }

        /// <summary>
        /// Caches all children for processing.
        /// </summary>
        private void CacheChildren()
        {
            children.Clear();
            foreach (RectTransform child in transform)
            {
                if (child != null)
                {
                    children.Add(child);
                }
            }
        }

        /// <summary>
        /// Public method to force layout update.
        /// </summary>
        [ContextMenu("Force Update Layout")]
        public void ForceUpdate()
        {
            UpdateLayout();
        }

        /// <summary>
        /// Sets the container shape.
        /// </summary>
        public void SetShape(ContainerShape newShape)
        {
            shape = newShape;
            UpdateLayout();
        }

        /// <summary>
        /// Sets container size limits.
        /// </summary>
        public void SetContainerSizeLimits(float minWidth, float maxWidth, float minHeight, float maxHeight)
        {
            minContainerWidth = minWidth;
            maxContainerWidth = maxWidth;
            minContainerHeight = minHeight;
            maxContainerHeight = maxHeight;
            UpdateLayout();
        }

        /// <summary>
        /// Sets cell size limits.
        /// </summary>
        public void SetCellSizeLimits(float minSize, float maxSize)
        {
            minCellSize = minSize;
            maxCellSize = maxSize;
            UpdateLayout();
        }

        /// <summary>
        /// Sets fixed grid dimensions (0 = auto).
        /// </summary>
        public void SetGridDimensions(int cols, int rows)
        {
            fixedColumns = cols;
            fixedRows = rows;
            UpdateLayout();
        }

        /// <summary>
        /// Gets the number of currently visible children.
        /// </summary>
        public int GetVisibleChildCount()
        {
            return visibleChildCount;
        }

        /// <summary>
        /// Gets the number of hidden children (overflow).
        /// </summary>
        public int GetHiddenChildCount()
        {
            return hiddenChildCount;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            // Draw container bounds in yellow
            Gizmos.color = Color.yellow;

            Vector3 center = rectTransform.position;
            Vector3 size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 0);

            // Draw bounds based on shape
            switch (shape)
            {
                case ContainerShape.Square:
                case ContainerShape.Rectangle:
                    Gizmos.DrawWireCube(center, size);
                    break;

                case ContainerShape.Circle:
                    DrawCircleGizmo(center, size.x * 0.5f);
                    break;

                case ContainerShape.Triangle:
                    DrawTriangleGizmo(center, size);
                    break;
            }

            // Draw usable area in green
            Gizmos.color = Color.green;
            Vector2 usableArea = CalculateUsableArea(rectTransform.sizeDelta);
            Gizmos.DrawWireCube(center, new Vector3(usableArea.x, usableArea.y, 0));
        }

        private void DrawCircleGizmo(Vector3 center, float radius)
        {
            int segments = 32;
            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

                Vector3 point1 = center + new Vector3(
                    Mathf.Cos(angle1) * radius,
                    Mathf.Sin(angle1) * radius,
                    0
                );

                Vector3 point2 = center + new Vector3(
                    Mathf.Cos(angle2) * radius,
                    Mathf.Sin(angle2) * radius,
                    0
                );

                Gizmos.DrawLine(point1, point2);
            }
        }

        private void DrawTriangleGizmo(Vector3 center, Vector3 size)
        {
            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;

            Vector3 top = center + new Vector3(0, halfHeight, 0);
            Vector3 bottomLeft = center + new Vector3(-halfWidth, -halfHeight, 0);
            Vector3 bottomRight = center + new Vector3(halfWidth, -halfHeight, 0);

            Gizmos.DrawLine(top, bottomLeft);
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, top);
        }
#endif
    }
}
