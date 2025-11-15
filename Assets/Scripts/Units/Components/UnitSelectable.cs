using UnityEngine;
using System.Collections.Generic;
using RTS.Core.Events;

namespace RTS.Units
{
    /// <summary>
    /// Component marking units as selectable.
    /// Lightweight and event-driven.
    /// ADD THIS TO EACH UNIT YOU WANT TO SELECT.
    /// </summary>
    public class UnitSelectable : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private bool useColorHighlight = true;
        [SerializeField] private Color selectedColor = Color.green;
        [SerializeField] private GameObject hoverIndicator;

        private bool isSelected;
        private bool isHovered;
        private Renderer[] renderers;
        private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
        // ✅ FIX: Use MaterialPropertyBlock to avoid creating material instances during render pass
        private MaterialPropertyBlock propertyBlock;
        private static readonly int ColorPropertyID = Shader.PropertyToID("_Color");
        private static readonly int BaseColorPropertyID = Shader.PropertyToID("_BaseColor");

        public bool IsSelected { get { return isSelected; } }
        public bool IsHovered { get { return isHovered; } }

        private void Awake()
        {
            if (useColorHighlight)
            {
                renderers = GetComponentsInChildren<Renderer>();
                propertyBlock = new MaterialPropertyBlock();

                foreach (var rend in renderers)
                {
                    if (rend != null && rend.sharedMaterial != null)
                    {
                        // ✅ FIX: Use sharedMaterial instead of material
                        originalColors[rend] = rend.sharedMaterial.color;
                    }
                }
            }
        }

        public void Select()
        {
            if (isSelected) return;

            isSelected = true;

            // Visual feedback
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(true);
            }

            if (useColorHighlight && propertyBlock != null)
            {
                foreach (var rend in renderers)
                {
                    if (rend != null)
                    {
                        // ✅ FIX: Use MaterialPropertyBlock to change color without creating instances
                        rend.GetPropertyBlock(propertyBlock);
                        propertyBlock.SetColor(ColorPropertyID, selectedColor);
                        propertyBlock.SetColor(BaseColorPropertyID, selectedColor);
                        rend.SetPropertyBlock(propertyBlock);
                    }
                }
            }

            EventBus.Publish(new UnitSelectedEvent(gameObject));
        }

        public void Deselect()
        {
            if (!isSelected) return;

            isSelected = false;

            // Remove visual feedback
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }

            if (useColorHighlight && propertyBlock != null)
            {
                foreach (var rend in renderers)
                {
                    if (rend != null && originalColors.TryGetValue(rend, out Color original))
                    {
                        // ✅ FIX: Use MaterialPropertyBlock to restore original color
                        rend.GetPropertyBlock(propertyBlock);
                        propertyBlock.SetColor(ColorPropertyID, original);
                        propertyBlock.SetColor(BaseColorPropertyID, original);
                        rend.SetPropertyBlock(propertyBlock);
                    }
                }
            }

            EventBus.Publish(new UnitDeselectedEvent(gameObject));
        }

        /// <summary>
        /// Sets hover highlight state for this unit.
        /// </summary>
        public void SetHoverHighlight(bool hover, Color hoverColor)
        {
            // Don't hover highlight selected units
            if (isSelected)
                return;

            isHovered = hover;

            // Show/hide hover indicator if present
            if (hoverIndicator != null)
            {
                hoverIndicator.SetActive(hover);
            }

            // Apply hover color if using color highlight
            if (useColorHighlight && propertyBlock != null)
            {
                foreach (var rend in renderers)
                {
                    if (rend != null)
                    {
                        rend.GetPropertyBlock(propertyBlock);

                        if (hover)
                        {
                            propertyBlock.SetColor(ColorPropertyID, hoverColor);
                            propertyBlock.SetColor(BaseColorPropertyID, hoverColor);
                        }
                        else if (originalColors.TryGetValue(rend, out Color original))
                        {
                            propertyBlock.SetColor(ColorPropertyID, original);
                            propertyBlock.SetColor(BaseColorPropertyID, original);
                        }

                        rend.SetPropertyBlock(propertyBlock);
                    }
                }
            }
        }
    }
}
