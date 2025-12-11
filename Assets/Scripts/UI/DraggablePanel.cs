using UnityEngine;
using UnityEngine.EventSystems;

namespace RTS.UI
{
    /// <summary>
    /// Makes a UI panel draggable by clicking and dragging the title bar (Windows style)
    /// </summary>
    public class DraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [Header("Drag Settings")]
        [SerializeField] private RectTransform panelRectTransform;
        [SerializeField] private RectTransform dragHandleRect; // The title bar area
        [SerializeField] private Canvas canvas;

        private Vector2 originalLocalPointerPosition;
        private Vector3 originalPanelLocalPosition;

        private void Awake()
        {
            if (panelRectTransform == null)
                panelRectTransform = GetComponent<RectTransform>();

            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            originalPanelLocalPosition = panelRectTransform.localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelRectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out originalLocalPointerPosition);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (panelRectTransform == null || canvas == null)
                return;

            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelRectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPointerPosition))
            {
                Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;
                panelRectTransform.localPosition = originalPanelLocalPosition + offsetToOriginal;
            }
        }
    }
}
