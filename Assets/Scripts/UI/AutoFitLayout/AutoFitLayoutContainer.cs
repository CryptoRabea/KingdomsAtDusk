using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RTS.UI.AutoFit
{
    /// <summary>
    /// Universal auto-fit layout container that ensures contents never overflow.
    /// Works with any layout type (Grid, Horizontal, Vertical) and any shape (Square, Rectangle, Circle, Triangle).
    /// Can be reused across any project.
    ///
    /// Usage:
    /// 1. Add this component to a container GameObject
    /// 2. Set the container shape and max size
    /// 3. Put your UI elements inside
    /// 4. The tool automatically scales them to fit!
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

        [Header("Container Settings")]
        [SerializeField] private ContainerShape shape = ContainerShape.Square;

        [Tooltip("Maximum width of the container in pixels")]
        [SerializeField] private float maxWidth = 300f;

        [Tooltip("Maximum height of the container in pixels")]
        [SerializeField] private float maxHeight = 300f;

        [Tooltip("Padding inside the container")]
        [SerializeField] private float padding = 10f;

        [Header("Content Scaling")]
        [SerializeField] private float minContentSize = 16f;
        [SerializeField] private float maxContentSize = 128f;

        [Tooltip("Auto-detect layout component (Grid, Horizontal, Vertical)")]
        [SerializeField] private bool autoDetectLayout = true;

        [Header("Shape Specific Settings")]
        [Tooltip("For Circle: How much to reduce usable area (0-1)")]
        [SerializeField, Range(0f, 1f)] private float circleInscribeFactor = 0.707f; // √2/2 for inscribed square

        [Tooltip("For Triangle: Aspect ratio adjustment")]
        [SerializeField, Range(0.5f, 2f)] private float triangleAspectRatio = 0.866f; // Equilateral triangle

        [Header("Advanced")]
        [SerializeField] private bool updateInEditMode = true;
        [SerializeField] private bool debugMode = false;

        private RectTransform rectTransform;
        private GridLayoutGroup gridLayout;
        private HorizontalLayoutGroup horizontalLayout;
        private VerticalLayoutGroup verticalLayout;
        private LayoutElement layoutElement;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            CacheLayoutComponents();
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

        private void OnRectTransformDimensionsChange()
        {
            UpdateLayout();
        }

        /// <summary>
        /// Main method that updates the container and content to fit within the shape.
        /// </summary>
        public void UpdateLayout()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (autoDetectLayout)
            {
                CacheLayoutComponents();
            }

            // Calculate container size based on shape
            Vector2 containerSize = CalculateContainerSize();

            // Apply container size
            rectTransform.sizeDelta = containerSize;

            // Calculate and apply content sizing
            UpdateContentSizing(containerSize);

            if (debugMode)
            {
                Debug.Log($"[AutoFitLayout] Shape: {shape}, Container: {containerSize}, Content updated");
            }
        }

        /// <summary>
        /// Calculates the optimal container size based on the selected shape.
        /// </summary>
        private Vector2 CalculateContainerSize()
        {
            switch (shape)
            {
                case ContainerShape.Square:
                    float squareSize = Mathf.Min(maxWidth, maxHeight);
                    return new Vector2(squareSize, squareSize);

                case ContainerShape.Rectangle:
                    return new Vector2(maxWidth, maxHeight);

                case ContainerShape.Circle:
                    float circleSize = Mathf.Min(maxWidth, maxHeight);
                    return new Vector2(circleSize, circleSize);

                case ContainerShape.Triangle:
                    // Equilateral triangle in a bounding box
                    float triWidth = Mathf.Min(maxWidth, maxHeight);
                    float triHeight = triWidth * triangleAspectRatio;
                    return new Vector2(triWidth, triHeight);

                case ContainerShape.Custom:
                    return new Vector2(maxWidth, maxHeight);

                default:
                    return new Vector2(maxWidth, maxHeight);
            }
        }

        /// <summary>
        /// Updates content sizing based on container size and layout type.
        /// </summary>
        private void UpdateContentSizing(Vector2 containerSize)
        {
            // Calculate usable area based on shape
            Vector2 usableArea = CalculateUsableArea(containerSize);

            // Count children
            int childCount = CountActiveChildren();
            if (childCount == 0) return;

            // Update based on layout type
            if (gridLayout != null)
            {
                UpdateGridLayout(usableArea, childCount);
            }
            else if (horizontalLayout != null)
            {
                UpdateHorizontalLayout(usableArea, childCount);
            }
            else if (verticalLayout != null)
            {
                UpdateVerticalLayout(usableArea, childCount);
            }
            else
            {
                // No layout group - try to fit children manually
                UpdateManualLayout(usableArea, childCount);
            }
        }

        /// <summary>
        /// Calculates the usable area within the container based on shape.
        /// </summary>
        private Vector2 CalculateUsableArea(Vector2 containerSize)
        {
            Vector2 usableSize = containerSize - new Vector2(padding * 2, padding * 2);

            switch (shape)
            {
                case ContainerShape.Circle:
                    // Inscribed square in circle
                    float inscribedSize = usableSize.x * circleInscribeFactor;
                    return new Vector2(inscribedSize, inscribedSize);

                case ContainerShape.Triangle:
                    // Reduce usable area for triangle shape
                    return usableSize * 0.8f; // 80% of bounding box

                default:
                    return usableSize;
            }
        }

        /// <summary>
        /// Updates GridLayoutGroup to fit within the container.
        /// </summary>
        private void UpdateGridLayout(Vector2 usableArea, int childCount)
        {
            if (gridLayout == null) return;

            // Calculate optimal grid dimensions
            int columns = Mathf.CeilToInt(Mathf.Sqrt(childCount));
            int rows = Mathf.CeilToInt((float)childCount / columns);

            // Calculate cell size to fit within usable area
            float cellWidth = (usableArea.x - (gridLayout.spacing.x * (columns - 1))) / columns;
            float cellHeight = (usableArea.y - (gridLayout.spacing.y * (rows - 1))) / rows;

            // For square cells, use the smaller dimension
            float cellSize = Mathf.Min(cellWidth, cellHeight);
            cellSize = Mathf.Clamp(cellSize, minContentSize, maxContentSize);

            // Update grid layout
            gridLayout.cellSize = new Vector2(cellSize, cellSize);

            if (gridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            {
                gridLayout.constraintCount = columns;
            }
            else if (gridLayout.constraint == GridLayoutGroup.Constraint.FixedRowCount)
            {
                gridLayout.constraintCount = rows;
            }

            if (debugMode)
            {
                Debug.Log($"[AutoFitLayout] Grid: {columns}x{rows}, Cell: {cellSize}px");
            }
        }

        /// <summary>
        /// Updates HorizontalLayoutGroup to fit within the container.
        /// </summary>
        private void UpdateHorizontalLayout(Vector2 usableArea, int childCount)
        {
            if (horizontalLayout == null) return;

            // Calculate item width
            float totalSpacing = horizontalLayout.spacing * (childCount - 1);
            float itemWidth = (usableArea.x - totalSpacing) / childCount;
            itemWidth = Mathf.Clamp(itemWidth, minContentSize, maxContentSize);

            // Apply to children with LayoutElement
            foreach (RectTransform child in transform)
            {
                if (!child.gameObject.activeSelf) continue;

                LayoutElement element = child.GetComponent<LayoutElement>();
                if (element == null)
                {
                    element = child.gameObject.AddComponent<LayoutElement>();
                }

                element.preferredWidth = itemWidth;
                element.preferredHeight = Mathf.Min(usableArea.y, maxContentSize);
            }

            if (debugMode)
            {
                Debug.Log($"[AutoFitLayout] Horizontal: {childCount} items, Width: {itemWidth}px");
            }
        }

        /// <summary>
        /// Updates VerticalLayoutGroup to fit within the container.
        /// </summary>
        private void UpdateVerticalLayout(Vector2 usableArea, int childCount)
        {
            if (verticalLayout == null) return;

            // Calculate item height
            float totalSpacing = verticalLayout.spacing * (childCount - 1);
            float itemHeight = (usableArea.y - totalSpacing) / childCount;
            itemHeight = Mathf.Clamp(itemHeight, minContentSize, maxContentSize);

            // Apply to children with LayoutElement
            foreach (RectTransform child in transform)
            {
                if (!child.gameObject.activeSelf) continue;

                LayoutElement element = child.GetComponent<LayoutElement>();
                if (element == null)
                {
                    element = child.gameObject.AddComponent<LayoutElement>();
                }

                element.preferredHeight = itemHeight;
                element.preferredWidth = Mathf.Min(usableArea.x, maxContentSize);
            }

            if (debugMode)
            {
                Debug.Log($"[AutoFitLayout] Vertical: {childCount} items, Height: {itemHeight}px");
            }
        }

        /// <summary>
        /// Manual layout when no layout group is present.
        /// </summary>
        private void UpdateManualLayout(Vector2 usableArea, int childCount)
        {
            // Simple grid-like arrangement
            int columns = Mathf.CeilToInt(Mathf.Sqrt(childCount));
            int rows = Mathf.CeilToInt((float)childCount / columns);

            float cellWidth = usableArea.x / columns;
            float cellHeight = usableArea.y / rows;
            float cellSize = Mathf.Min(cellWidth, cellHeight);
            cellSize = Mathf.Clamp(cellSize, minContentSize, maxContentSize);

            int index = 0;
            foreach (RectTransform child in transform)
            {
                if (!child.gameObject.activeSelf) continue;

                int col = index % columns;
                int row = index / columns;

                child.sizeDelta = new Vector2(cellSize, cellSize);
                child.anchoredPosition = new Vector2(
                    padding + col * cellWidth + cellSize * 0.5f,
                    -(padding + row * cellHeight + cellSize * 0.5f)
                );

                index++;
            }
        }

        /// <summary>
        /// Caches layout components for performance.
        /// </summary>
        private void CacheLayoutComponents()
        {
            gridLayout = GetComponent<GridLayoutGroup>();
            horizontalLayout = GetComponent<HorizontalLayoutGroup>();
            verticalLayout = GetComponent<VerticalLayoutGroup>();
            layoutElement = GetComponent<LayoutElement>();
        }

        /// <summary>
        /// Counts active children in the container.
        /// </summary>
        private int CountActiveChildren()
        {
            int count = 0;
            foreach (RectTransform child in transform)
            {
                if (child.gameObject.activeSelf)
                {
                    count++;
                }
            }
            return count;
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
        /// Sets the container shape programmatically.
        /// </summary>
        public void SetShape(ContainerShape newShape)
        {
            shape = newShape;
            UpdateLayout();
        }

        /// <summary>
        /// Sets the maximum size programmatically.
        /// </summary>
        public void SetMaxSize(float width, float height)
        {
            maxWidth = width;
            maxHeight = height;
            UpdateLayout();
        }

        /// <summary>
        /// Sets the padding programmatically.
        /// </summary>
        public void SetPadding(float newPadding)
        {
            padding = newPadding;
            UpdateLayout();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            Gizmos.color = Color.yellow;

            Vector3 center = rectTransform.position;
            Vector3 size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 0);

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
