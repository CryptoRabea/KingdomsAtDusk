using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RTS.UI
{
    /// <summary>
    /// Represents a single cell in the formation grid.
    /// Each cell has a square background with a circle in the middle that can be toggled.
    /// </summary>
    public class FormationGridCell : MonoBehaviour, IPointerClickHandler
    {
        [Header("Visual Settings")]
        [SerializeField] private Image squareBackground;
        [SerializeField] private Image circle;
        [SerializeField] private Color squareColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color emptyCircleColor = new Color(0.4f, 0.4f, 0.4f, 0.3f);
        [SerializeField] private Color filledCircleColor = new Color(0.3f, 0.6f, 1f, 1f);

        // State
        private bool isFilled = false;
        private Vector2Int gridPosition;
        private FormationBuilderUI builderUI;

        public bool IsFilled => isFilled;
        public Vector2Int GridPosition => gridPosition;

        /// <summary>
        /// Initialize the cell
        /// </summary>
        public void Initialize(FormationBuilderUI builder, Vector2Int position)
        {
            builderUI = builder;
            gridPosition = position;

            // Setup visuals if not assigned
            if (squareBackground == null)
            {
                squareBackground = GetComponent<Image>();
                if (squareBackground == null)
                {
                    squareBackground = gameObject.AddComponent<Image>();
                }
            }

            if (circle == null)
            {
                // Create circle as child
                GameObject circleObj = new GameObject("Circle");
                circleObj.transform.SetParent(transform, false);
                circle = circleObj.AddComponent<Image>();

                // Make circle smaller than the square
                RectTransform circleRect = circleObj.GetComponent<RectTransform>();
                circleRect.anchorMin = new Vector2(0.5f, 0.5f);
                circleRect.anchorMax = new Vector2(0.5f, 0.5f);
                circleRect.pivot = new Vector2(0.5f, 0.5f);
                circleRect.sizeDelta = new Vector2(8f, 8f); // Small circle
                circleRect.anchoredPosition = Vector2.zero;

                // Make it circular
                circle.sprite = CreateCircleSprite();
            }

            UpdateVisual();
        }

        /// <summary>
        /// Toggle the filled state of the cell
        /// </summary>
        public void Toggle()
        {
            isFilled = !isFilled;
            UpdateVisual();
            builderUI?.OnCellToggled(this);
        }

        /// <summary>
        /// Set the filled state
        /// </summary>
        public void SetFilled(bool filled)
        {
            isFilled = filled;
            UpdateVisual();
        }

        /// <summary>
        /// Update the visual appearance based on state
        /// </summary>
        private void UpdateVisual()
        {
            if (squareBackground != null)
            {
                squareBackground.color = squareColor;
            }

            if (circle != null)
            {
                circle.color = isFilled ? filledCircleColor : emptyCircleColor;
            }
        }

        /// <summary>
        /// Handle click events
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            Toggle();
        }

        /// <summary>
        /// Create a circle sprite
        /// </summary>
        private Sprite CreateCircleSprite()
        {
            // Create a simple circle texture
            int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    Color color = distance <= radius ? Color.white : Color.clear;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
