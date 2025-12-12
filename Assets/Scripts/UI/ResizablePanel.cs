using UnityEngine;
using UnityEngine.EventSystems;

namespace RTS.UI
{
    /// <summary>
    /// Makes a UI panel resizable by dragging corners/edges (Windows style)
    /// </summary>
    public class ResizablePanel : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [Header("Resize Settings")]
        [SerializeField] private RectTransform panelRectTransform;
        [SerializeField] private Vector2 minSize = new Vector2(400, 400);
        [SerializeField] private Vector2 maxSize = new Vector2(1200, 900);
        [SerializeField] private float resizeHandleSize = 20f; // Size of corner/edge detection area

        private Vector2 originalSizeDelta;
        private Vector2 originalLocalPointerPosition;
        private Vector3 originalPosition;
        private ResizeDirection currentResizeDirection = ResizeDirection.None;

        private enum ResizeDirection
        {
            None,
            TopLeft,
            Top,
            TopRight,
            Right,
            BottomRight,
            Bottom,
            BottomLeft,
            Left
        }

        private void Awake()
        {
            if (panelRectTransform == null)
                panelRectTransform = GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            originalSizeDelta = panelRectTransform.sizeDelta;
            originalPosition = panelRectTransform.position;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelRectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out originalLocalPointerPosition);

            currentResizeDirection = GetResizeDirection(originalLocalPointerPosition);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (currentResizeDirection == ResizeDirection.None)
                return;

            Vector2 localPointerPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelRectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPointerPosition);

            Vector2 delta = localPointerPosition - originalLocalPointerPosition;
            ResizePanel(delta);
        }

        private void ResizePanel(Vector2 delta)
        {
            Vector2 newSizeDelta = originalSizeDelta;
            Vector3 newPosition = originalPosition;

            switch (currentResizeDirection)
            {
                case ResizeDirection.Right:
                    newSizeDelta.x = Mathf.Clamp(originalSizeDelta.x + delta.x, minSize.x, maxSize.x);
                    break;

                case ResizeDirection.Left:
                    float widthChange = -delta.x;
                    newSizeDelta.x = Mathf.Clamp(originalSizeDelta.x + widthChange, minSize.x, maxSize.x);
                    float actualWidthChange = newSizeDelta.x - originalSizeDelta.x;
                    newPosition.x = originalPosition.x - actualWidthChange * 0.5f;
                    break;

                case ResizeDirection.Top:
                    newSizeDelta.y = Mathf.Clamp(originalSizeDelta.y + delta.y, minSize.y, maxSize.y);
                    break;

                case ResizeDirection.Bottom:
                    float heightChange = -delta.y;
                    newSizeDelta.y = Mathf.Clamp(originalSizeDelta.y + heightChange, minSize.y, maxSize.y);
                    float actualHeightChange = newSizeDelta.y - originalSizeDelta.y;
                    newPosition.y = originalPosition.y - actualHeightChange * 0.5f;
                    break;

                case ResizeDirection.TopRight:
                    newSizeDelta.x = Mathf.Clamp(originalSizeDelta.x + delta.x, minSize.x, maxSize.x);
                    newSizeDelta.y = Mathf.Clamp(originalSizeDelta.y + delta.y, minSize.y, maxSize.y);
                    break;

                case ResizeDirection.TopLeft:
                    widthChange = -delta.x;
                    newSizeDelta.x = Mathf.Clamp(originalSizeDelta.x + widthChange, minSize.x, maxSize.x);
                    actualWidthChange = newSizeDelta.x - originalSizeDelta.x;
                    newPosition.x = originalPosition.x - actualWidthChange * 0.5f;

                    newSizeDelta.y = Mathf.Clamp(originalSizeDelta.y + delta.y, minSize.y, maxSize.y);
                    break;

                case ResizeDirection.BottomRight:
                    newSizeDelta.x = Mathf.Clamp(originalSizeDelta.x + delta.x, minSize.x, maxSize.x);

                    heightChange = -delta.y;
                    newSizeDelta.y = Mathf.Clamp(originalSizeDelta.y + heightChange, minSize.y, maxSize.y);
                    actualHeightChange = newSizeDelta.y - originalSizeDelta.y;
                    newPosition.y = originalPosition.y - actualHeightChange * 0.5f;
                    break;

                case ResizeDirection.BottomLeft:
                    widthChange = -delta.x;
                    newSizeDelta.x = Mathf.Clamp(originalSizeDelta.x + widthChange, minSize.x, maxSize.x);
                    actualWidthChange = newSizeDelta.x - originalSizeDelta.x;
                    newPosition.x = originalPosition.x - actualWidthChange * 0.5f;

                    heightChange = -delta.y;
                    newSizeDelta.y = Mathf.Clamp(originalSizeDelta.y + heightChange, minSize.y, maxSize.y);
                    actualHeightChange = newSizeDelta.y - originalSizeDelta.y;
                    newPosition.y = originalPosition.y - actualHeightChange * 0.5f;
                    break;
            }

            panelRectTransform.sizeDelta = newSizeDelta;
            panelRectTransform.position = newPosition;
        }

        private ResizeDirection GetResizeDirection(Vector2 localPoint)
        {
            Rect rect = panelRectTransform.rect;
            float halfWidth = rect.width / 2f;
            float halfHeight = rect.height / 2f;

            bool isLeft = localPoint.x < -halfWidth + resizeHandleSize;
            bool isRight = localPoint.x > halfWidth - resizeHandleSize;
            bool isTop = localPoint.y > halfHeight - resizeHandleSize;
            bool isBottom = localPoint.y < -halfHeight + resizeHandleSize;

            // Corners have priority
            if (isTop && isRight) return ResizeDirection.TopRight;
            if (isTop && isLeft) return ResizeDirection.TopLeft;
            if (isBottom && isRight) return ResizeDirection.BottomRight;
            if (isBottom && isLeft) return ResizeDirection.BottomLeft;

            // Edges
            if (isTop) return ResizeDirection.Top;
            if (isBottom) return ResizeDirection.Bottom;
            if (isLeft) return ResizeDirection.Left;
            if (isRight) return ResizeDirection.Right;

            return ResizeDirection.None;
        }

        private void Update()
        {
            UpdateCursor();
        }

        private void UpdateCursor()
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(panelRectTransform, Input.mousePosition, null))
                return;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelRectTransform,
                Input.mousePosition,
                null,
                out localPoint);

            ResizeDirection direction = GetResizeDirection(localPoint);

            // Note: Unity doesn't support custom system cursors directly
            // You'll need to use Cursor.SetCursor with custom textures for proper resize cursors
            // For now, this just detects the resize zones
        }
    }
}
