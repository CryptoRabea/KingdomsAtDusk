using UnityEngine;

namespace RTS.Buildings
{
    /// <summary>
    /// Construction visual that makes the building appear to grow from the ground upward.
    /// Uses a clipping plane shader approach to progressively reveal the building.
    /// </summary>
    public class GroundUpConstructionVisual : BaseConstructionVisual
    {
        [Header("Ground Up Settings")]
        [SerializeField] private bool useClippingPlane = true; // Use shader clipping (requires compatible shader)
        [SerializeField] private bool useScale = false; // Alternative: use Y-scale
        [SerializeField] private float heightOffset = 0f; // Offset from ground level

        [Header("Material Settings")]
        [SerializeField] private Color constructionTint = new Color(1f, 0.8f, 0.4f, 1f); // Orange construction tint
        [SerializeField] private bool useTint = true;

        private MaterialPropertyBlock propertyBlock;
        private float buildingHeight;
        private Vector3 buildingBottom;
        private Vector3 originalScale;

        // Shader property IDs (for performance)
        private static readonly int ClipHeightID = Shader.PropertyToID("_ClipHeight");
        private static readonly int ColorPropertyID = Shader.PropertyToID("_Color");
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

        protected override void Initialize()
        {
            propertyBlock = new MaterialPropertyBlock();

            // Calculate building dimensions
            CalculateBuildingDimensions();

            // Store original scale for scale-based animation
            originalScale = transform.localScale;

            // Initialize visual state
            UpdateVisual(0f);
        }

        private void CalculateBuildingDimensions()
        {
            // Get the height of the building
            buildingHeight = combinedBounds.size.y;
            buildingBottom = new Vector3(
                combinedBounds.center.x,
                combinedBounds.min.y + heightOffset,
                combinedBounds.center.z
            );
        }

        protected override void UpdateVisual(float progress)
        {
            if (useClippingPlane)
            {
                UpdateClippingPlaneVisual(progress);
            }
            else if (useScale)
            {
                UpdateScaleVisual(progress);
            }

            // Apply construction tint
            if (useTint)
            {
                ApplyConstructionTint(progress);
            }
        }

        private void UpdateClippingPlaneVisual(float progress)
        {
            // Calculate the current height based on progress
            float currentHeight = buildingBottom.y + (buildingHeight * progress);

            // Apply clipping height to all renderers
            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(ClipHeightID, currentHeight);
                rend.SetPropertyBlock(propertyBlock);
            }
        }

        private void UpdateScaleVisual(float progress)
        {
            // Scale the building from 0 to full height on Y axis
            Vector3 newScale = new Vector3(
                originalScale.x,
                originalScale.y * progress,
                originalScale.z
            );

            transform.localScale = newScale;

            // Adjust position to keep bottom at ground level
            float heightDifference = buildingHeight * (1f - progress) * 0.5f;
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                -heightDifference,
                transform.localPosition.z
            );
        }

        private void ApplyConstructionTint(float progress)
        {
            // Fade from construction tint to normal color as construction progresses
            Color currentTint = Color.Lerp(constructionTint, Color.white, progress);

            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                rend.GetPropertyBlock(propertyBlock);

                // Try different color property names (different shaders use different properties)
                propertyBlock.SetColor(ColorPropertyID, currentTint);
                propertyBlock.SetColor(BaseColorID, currentTint);

                rend.SetPropertyBlock(propertyBlock);
            }
        }

        protected override void Cleanup()
        {
            // Reset scale to original
            if (useScale)
            {
                transform.localScale = originalScale;
                transform.localPosition = Vector3.zero;
            }

            // Reset material properties
            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorPropertyID, Color.white);
                propertyBlock.SetColor(BaseColorID, Color.white);
                rend.SetPropertyBlock(propertyBlock);
            }
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Draw current construction height
            if (Application.isPlaying && buildingHeight > 0)
            {
                float currentHeight = buildingBottom.y + (buildingHeight * currentProgress);

                Gizmos.color = Color.green;
                Gizmos.DrawLine(
                    new Vector3(combinedBounds.min.x, currentHeight, combinedBounds.min.z),
                    new Vector3(combinedBounds.max.x, currentHeight, combinedBounds.max.z)
                );
            }
        }
#endif
    }
}
