using UnityEngine;
using UnityEngine.EventSystems;

namespace RTS.UI
{
    /// <summary>
    /// Makes a UI panel resizable like Windows panels using corners & edges.
    /// Does NOT move children – only changes RectTransform size.
    /// </summary>
    public class ResizablePanel : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [Header("Resize Settings")]
        [SerializeField] private RectTransform panelRectTransform;
        [SerializeField] private float resizeHandleSize = 16f;
        [SerializeField] private Vector2 minSize = new Vector2(400, 400);
        [SerializeField] private Vector2 maxSize = new Vector2(1200, 900);

        private Vector2 pointerStartLocalPos;
        private Vector2 panelStartSize;

        private ResizeDirection resizeDirection = ResizeDirection.None;

        private enum ResizeDirection
        {
            None,
            Left, Right, Top, Bottom,
            TopLeft, TopRight, BottomLeft, BottomRight
        }

        private void Awake()
        {
            if (panelRectTransform == null)
                panelRectTransform = GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelRectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out pointerStartLocalPos
            );

            resizeDirection = GetResizeDirection(pointerStartLocalPos);
            panelStartSize = panelRectTransform.sizeDelta;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (resizeDirection == ResizeDirection.None)
                return;

            Vector2 localPointerPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelRectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPointerPos
            );

            Vector2 delta = localPointerPos - pointerStartLocalPos;

            Resize(delta);
        }

        private void Resize(Vector2 delta)
        {
            var size = panelStartSize;
            var offMin = panelRectTransform.offsetMin;
            var offMax = panelRectTransform.offsetMax;

            // Horizontal
            if (resizeDirection == ResizeDirection.Left ||
                resizeDirection == ResizeDirection.TopLeft ||
                resizeDirection == ResizeDirection.BottomLeft)
            {
                float newWidth = Mathf.Clamp(size.x - delta.x, minSize.x, maxSize.x);
                float widthOffset = size.x - newWidth;
                offMin.x += widthOffset;
            }
            else if (resizeDirection == ResizeDirection.Right ||
                     resizeDirection == ResizeDirection.TopRight ||
                     resizeDirection == ResizeDirection.BottomRight)
            {
                float newWidth = Mathf.Clamp(size.x + delta.x, minSize.x, maxSize.x);
                float widthOffset = newWidth - size.x;
                offMax.x += widthOffset;
            }

            // Vertical
            if (resizeDirection == ResizeDirection.Bottom ||
                resizeDirection == ResizeDirection.BottomLeft ||
                resizeDirection == ResizeDirection.BottomRight)
            {
                float newHeight = Mathf.Clamp(size.y - delta.y, minSize.y, maxSize.y);
                float heightOffset = size.y - newHeight;
                offMin.y += heightOffset;
            }
            else if (resizeDirection == ResizeDirection.Top ||
                     resizeDirection == ResizeDirection.TopLeft ||
                     resizeDirection == ResizeDirection.TopRight)
            {
                float newHeight = Mathf.Clamp(size.y + delta.y, minSize.y, maxSize.y);
                float heightOffset = newHeight - size.y;
                offMax.y += heightOffset;
            }

            panelRectTransform.offsetMin = offMin;
            panelRectTransform.offsetMax = offMax;
        }

        private ResizeDirection GetResizeDirection(Vector2 localPoint)
        {
            Rect r = panelRectTransform.rect;

            bool left = localPoint.x < r.xMin + resizeHandleSize;
            bool right = localPoint.x > r.xMax - resizeHandleSize;
            bool top = localPoint.y > r.yMax - resizeHandleSize;
            bool bottom = localPoint.y < r.yMin + resizeHandleSize;

            if (left && top) return ResizeDirection.TopLeft;
            if (right && top) return ResizeDirection.TopRight;
            if (left && bottom) return ResizeDirection.BottomLeft;
            if (right && bottom) return ResizeDirection.BottomRight;

            if (left) return ResizeDirection.Left;
            if (right) return ResizeDirection.Right;
            if (top) return ResizeDirection.Top;
            if (bottom) return ResizeDirection.Bottom;

            return ResizeDirection.None;
        }
    }
}
