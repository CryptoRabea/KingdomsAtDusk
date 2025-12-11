using UnityEngine;

namespace KAD.RTSBuildingsSystems
{
    /// <summary>
    /// Simple sprite-based visualizer for building placement.
    /// Shows a square sprite that changes color based on placement validity.
    /// </summary>
    public class BuildingPlacementGridVisualizer : MonoBehaviour
    {
        [Header("Placement Indicator Settings")]
        [Tooltip("Square sprite GameObject to show during placement (assign from building prefab)")]
        [SerializeField] private GameObject squareIndicator;

        [Tooltip("Enable/disable the placement indicator")]
        [SerializeField] private bool showPlacementIndicator = true;

        [Header("Valid Placement Colors")]
        [Tooltip("Color when placement is valid")]
        [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.5f); // Green with transparency

        [Header("Invalid Placement Colors")]
        [Tooltip("Color when placement is invalid")]
        [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f); // Red with transparency

        [Header("Optional Material Color Change")]
        [Tooltip("Enable material color change during preview (OPTIONAL - can leave disabled)")]
        [SerializeField] private bool changePreviewMaterialColor = false;

        [Tooltip("Material color when placement is valid")]
        [SerializeField] private Color validMaterialColor = new Color(0f, 1f, 0f, 0.5f);

        [Tooltip("Material color when placement is invalid")]
        [SerializeField] private Color invalidMaterialColor = new Color(1f, 0f, 0f, 0.5f);

        // Internal state
        private SpriteRenderer squareSpriteRenderer;
        private bool isValid = true;
        private Renderer[] buildingRenderers;
        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            // Get sprite renderer if square indicator is assigned
            if (squareIndicator != null)
            {
                squareSpriteRenderer = squareIndicator.GetComponent<SpriteRenderer>();
                if (squareSpriteRenderer == null)
                {
                }
            }

            // Setup material color change if enabled
            if (changePreviewMaterialColor)
            {
                buildingRenderers = GetComponentsInChildren<Renderer>();
                propertyBlock = new MaterialPropertyBlock();
            }

            // Hide by default
            Hide();
        }

        /// <summary>
        /// Updates the placement indicator based on validity
        /// </summary>
        public void UpdatePlacementIndicator(bool valid)
        {
            isValid = valid;

            // Update square indicator color
            if (showPlacementIndicator && squareSpriteRenderer != null)
            {
                Color targetColor = valid ? validColor : invalidColor;
                squareSpriteRenderer.color = targetColor;
            }

            // Update material color if enabled
            if (changePreviewMaterialColor)
            {
                UpdatePreviewMaterialColor(valid);
            }
        }

        private void UpdatePreviewMaterialColor(bool valid)
        {
            if (buildingRenderers == null || propertyBlock == null)
            {
                return;
            }

            Color targetColor = valid ? validMaterialColor : invalidMaterialColor;

            foreach (var renderer in buildingRenderers)
            {
                if (renderer != null && !(renderer is SpriteRenderer)) // Skip sprite renderers
                {
                    renderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetColor("_Color", targetColor);

                    // Try BaseColor for URP materials
                    if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
                    {
                        propertyBlock.SetColor("_BaseColor", targetColor);
                    }

                    renderer.SetPropertyBlock(propertyBlock);
                }
            }
        }

        /// <summary>
        /// Shows the placement indicator
        /// </summary>
        public void Show()
        {
            if (showPlacementIndicator && squareIndicator != null)
            {
                squareIndicator.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the placement indicator
        /// </summary>
        public void Hide()
        {
            if (squareIndicator != null)
            {
                squareIndicator.SetActive(false);
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

        private void OnDestroy()
        {
            // Clear material property blocks on destroy
            if (changePreviewMaterialColor && buildingRenderers != null && propertyBlock != null)
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
