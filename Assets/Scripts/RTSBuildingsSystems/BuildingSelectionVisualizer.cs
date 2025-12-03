using UnityEngine;

namespace KAD.RTSBuildingsSystems
{
    /// <summary>
    /// Simple sprite-based visualizer for building selection.
    /// Shows optional square and circle sprites when building is selected.
    /// </summary>
    public class BuildingSelectionVisualizer : MonoBehaviour
    {
        [Header("Selection Indicator Settings")]
        [Tooltip("Square sprite GameObject to show when selected (assign from building prefab)")]
        [SerializeField] private GameObject squareIndicator;

        [Tooltip("Circle sprite GameObject to show when selected (assign from building prefab)")]
        [SerializeField] private GameObject circleIndicator;

        [Header("Square Settings")]
        [Tooltip("Enable square indicator")]
        [SerializeField] private bool showSquare = true;

        [Tooltip("Color of the square indicator")]
        [SerializeField] private Color squareColor = new Color(0f, 1f, 1f, 0.6f); // Cyan

        [Header("Circle Settings")]
        [Tooltip("Enable circle indicator")]
        [SerializeField] private bool showCircle = true;

        [Tooltip("Color of the circle indicator")]
        [SerializeField] private Color circleColor = new Color(0f, 1f, 1f, 0.5f); // Cyan

        [Header("Optional Material Highlight")]
        [Tooltip("Enable material color highlight when selected (OPTIONAL)")]
        [SerializeField] private bool enableMaterialHighlight = false;

        [Tooltip("Highlight color to apply to materials")]
        [SerializeField] private Color highlightColor = new Color(0f, 1f, 1f, 1f); // Cyan

        // Internal state
        private SpriteRenderer squareSpriteRenderer;
        private SpriteRenderer circleSpriteRenderer;
        private Renderer[] buildingRenderers;
        private MaterialPropertyBlock propertyBlock;
        private bool isSelected = false;

        private void Awake()
        {
            // Get sprite renderers
            if (squareIndicator != null)
            {
                squareSpriteRenderer = squareIndicator.GetComponent<SpriteRenderer>();
                if (squareSpriteRenderer == null)
                {
                    Debug.LogWarning("BuildingSelectionVisualizer: Square indicator doesn't have a SpriteRenderer component!");
                }
            }

            if (circleIndicator != null)
            {
                circleSpriteRenderer = circleIndicator.GetComponent<SpriteRenderer>();
                if (circleSpriteRenderer == null)
                {
                    Debug.LogWarning("BuildingSelectionVisualizer: Circle indicator doesn't have a SpriteRenderer component!");
                }
            }

            // Setup material highlighting if enabled
            if (enableMaterialHighlight)
            {
                buildingRenderers = GetComponentsInChildren<Renderer>();
                propertyBlock = new MaterialPropertyBlock();
            }

            // Hide by default
            HideSelection();
        }

        /// <summary>
        /// Shows selection indicators
        /// </summary>
        public void ShowSelection()
        {
            isSelected = true;

            // Show and color square indicator
            if (showSquare && squareIndicator != null)
            {
                squareIndicator.SetActive(true);
                if (squareSpriteRenderer != null)
                {
                    squareSpriteRenderer.color = squareColor;
                }
            }

            // Show and color circle indicator
            if (showCircle && circleIndicator != null)
            {
                circleIndicator.SetActive(true);
                if (circleSpriteRenderer != null)
                {
                    circleSpriteRenderer.color = circleColor;
                }
            }

            // Apply material highlight if enabled
            if (enableMaterialHighlight)
            {
                ApplyMaterialHighlight();
            }
        }

        /// <summary>
        /// Hides selection indicators
        /// </summary>
        public void HideSelection()
        {
            isSelected = false;

            // Hide square indicator
            if (squareIndicator != null)
            {
                squareIndicator.SetActive(false);
            }

            // Hide circle indicator
            if (circleIndicator != null)
            {
                circleIndicator.SetActive(false);
            }

            // Remove material highlight if enabled
            if (enableMaterialHighlight)
            {
                RemoveMaterialHighlight();
            }
        }

        private void ApplyMaterialHighlight()
        {
            if (buildingRenderers == null || propertyBlock == null)
            {
                return;
            }

            foreach (var renderer in buildingRenderers)
            {
                if (renderer != null && !(renderer is SpriteRenderer)) // Skip sprite renderers
                {
                    renderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetColor("_Color", highlightColor);

                    // Try BaseColor for URP materials
                    if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
                    {
                        propertyBlock.SetColor("_BaseColor", highlightColor);
                    }

                    renderer.SetPropertyBlock(propertyBlock);
                }
            }
        }

        private void RemoveMaterialHighlight()
        {
            if (buildingRenderers == null || propertyBlock == null)
            {
                return;
            }

            foreach (var renderer in buildingRenderers)
            {
                if (renderer != null)
                {
                    // Clear property block to restore original material colors
                    propertyBlock.Clear();
                    renderer.SetPropertyBlock(propertyBlock);
                }
            }
        }

        /// <summary>
        /// Sets the square indicator sprite reference (can be called at runtime)
        /// </summary>
        public void SetSquareIndicator(GameObject indicator)
        {
            squareIndicator = indicator;
            if (indicator != null)
            {
                squareSpriteRenderer = indicator.GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// Sets the circle indicator sprite reference (can be called at runtime)
        /// </summary>
        public void SetCircleIndicator(GameObject indicator)
        {
            circleIndicator = indicator;
            if (indicator != null)
            {
                circleSpriteRenderer = indicator.GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// Sets whether the square indicator is shown
        /// </summary>
        public void SetShowSquare(bool show)
        {
            showSquare = show;
            if (squareIndicator != null)
            {
                squareIndicator.SetActive(show && isSelected);
            }
        }

        /// <summary>
        /// Sets whether the circle indicator is shown
        /// </summary>
        public void SetShowCircle(bool show)
        {
            showCircle = show;
            if (circleIndicator != null)
            {
                circleIndicator.SetActive(show && isSelected);
            }
        }

        /// <summary>
        /// Sets the square color
        /// </summary>
        public void SetSquareColor(Color color)
        {
            squareColor = color;
            if (isSelected && squareSpriteRenderer != null)
            {
                squareSpriteRenderer.color = color;
            }
        }

        /// <summary>
        /// Sets the circle color
        /// </summary>
        public void SetCircleColor(Color color)
        {
            circleColor = color;
            if (isSelected && circleSpriteRenderer != null)
            {
                circleSpriteRenderer.color = color;
            }
        }

        private void OnDestroy()
        {
            // Clean up material highlights
            if (enableMaterialHighlight && buildingRenderers != null && propertyBlock != null)
            {
                foreach (var renderer in buildingRenderers)
                {
                    if (renderer != null)
                    {
                        propertyBlock.Clear();
                        renderer.SetPropertyBlock(propertyBlock);
                    }
                }
            }
        }
    }
}
