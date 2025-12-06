using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RTS.UI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    /// <summary>
    /// Represents a draggable piece in the formation builder
    /// </summary>
    public class FormationPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("Visual Settings")]
        [SerializeField] private Image pieceImage;
        [SerializeField] private Color normalColor = new Color(0.3f, 0.6f, 1f, 1f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.8f, 0f, 1f);
        [SerializeField] private Color draggingColor = new Color(1f, 1f, 1f, 0.7f);

        [Header("References")]
        private RectTransform rectTransform;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private FormationBuilderUI builderUI;

        // State
        private Vector2 originalPosition;
        private bool isDragging = false;
        private bool isSelected = false;

        // Index in the formation
        public int PieceIndex { get; set; }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();

            // Add CanvasGroup if not present
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Setup image if not assigned
            if (pieceImage == null)
            {
                pieceImage = GetComponent<Image>();
                if (pieceImage == null)
                {
                    pieceImage = gameObject.AddComponent<Image>();
                }
            }

            UpdateVisual();
        }

        public void Initialize(FormationBuilderUI builder, int index)
        {
            builderUI = builder;
            PieceIndex = index;
            UpdateVisual();
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateVisual();
        }

        public bool IsSelected()
        {
            return isSelected;
        }

        private void UpdateVisual()
        {
            if (pieceImage == null) return;

            if (isDragging)
            {
                pieceImage.color = draggingColor;
            }
            else if (isSelected)
            {
                pieceImage.color = selectedColor;
            }
            else
            {
                pieceImage.color = normalColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Left click to select/deselect
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                builderUI?.OnPieceClicked(this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            isDragging = true;
            originalPosition = rectTransform.anchoredPosition;

            // Make piece semi-transparent during drag
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;

            UpdateVisual();

            builderUI?.OnPieceDragStart(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (canvas != null)
            {
                // Convert screen position to local position in the canvas
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint);

                rectTransform.anchoredPosition = localPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            isDragging = false;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            UpdateVisual();

            builderUI?.OnPieceDragEnd(this, eventData);
        }

        public Vector2 GetPosition()
        {
            return rectTransform.anchoredPosition;
        }

        public void SetPosition(Vector2 position)
        {
            rectTransform.anchoredPosition = position;
        }

        public Vector2 GetOriginalPosition()
        {
            return originalPosition;
        }

        public void RestoreOriginalPosition()
        {
            rectTransform.anchoredPosition = originalPosition;
        }

        /// <summary>
        /// Delete this piece
        /// </summary>
        public void DeletePiece()
        {
            builderUI?.RemovePiece(this);
            Destroy(gameObject);
        }

        private void Update()
        {
            // Handle delete key when selected
            if (isSelected && Input.GetKeyDown(KeyCode.Delete))
            {
                DeletePiece();
            }
        }
    }
}
