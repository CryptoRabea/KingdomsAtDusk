using UnityEngine;
using System.Collections.Generic;
using RTS.Core.Events;
using KAD.RTSBuildingsSystems;

namespace RTS.Buildings
{
    /// <summary>
    /// Component marking buildings as selectable.
    /// Lightweight and event-driven.
    /// ADD THIS TO EACH BUILDING YOU WANT TO SELECT.
    /// </summary>
    public class BuildingSelectable : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private bool useColorHighlight = true;
        [SerializeField] private Color selectedColor = Color.cyan;

        private bool isSelected;
        private Renderer[] renderers;
        private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
        //  FIX: Use MaterialPropertyBlock to avoid creating material instances during render pass
        private MaterialPropertyBlock propertyBlock;
        private static readonly int ColorPropertyID = Shader.PropertyToID("_Color");
        private static readonly int BaseColorPropertyID = Shader.PropertyToID("_BaseColor");

        // Optional advanced visualizer component
        private BuildingSelectionVisualizer selectionVisualizer;

        // Building reference for audio playback
        private Building building;

        public bool IsSelected { get { return isSelected; } }

        private void Awake()
        {
            // Check for optional advanced visualizer component
            selectionVisualizer = GetComponent<BuildingSelectionVisualizer>();

            // Get building reference for audio playback
            building = GetComponent<Building>();

            if (useColorHighlight)
            {
                renderers = GetComponentsInChildren<Renderer>();
                propertyBlock = new MaterialPropertyBlock();

                foreach (var rend in renderers)
                {
                    if (rend != null && rend.sharedMaterial != null)
                    {
                        //  FIX: Use sharedMaterial instead of material
                        originalColors[rend] = rend.sharedMaterial.color;
                    }
                }
            }
        }

        public void Select()
        {
            if (isSelected) return;

            isSelected = true;

            // Play selection audio
            PlaySelectionAudio();

            // Use advanced visualizer if available
            if (selectionVisualizer != null)
            {
                selectionVisualizer.ShowSelection();
            }
            else
            {
                // Fallback to legacy visual feedback
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
                            //  FIX: Use MaterialPropertyBlock to change color without creating instances
                            rend.GetPropertyBlock(propertyBlock);
                            propertyBlock.SetColor(ColorPropertyID, selectedColor);
                            propertyBlock.SetColor(BaseColorPropertyID, selectedColor);
                            rend.SetPropertyBlock(propertyBlock);
                        }
                    }
                }
            }

            EventBus.Publish(new BuildingSelectedEvent(gameObject));
        }

        public void Deselect()
        {
            if (!isSelected) return;

            isSelected = false;

            // Use advanced visualizer if available
            if (selectionVisualizer != null)
            {
                selectionVisualizer.HideSelection();
            }
            else
            {
                // Fallback to legacy visual feedback removal
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
                            //  FIX: Use MaterialPropertyBlock to restore original color
                            rend.GetPropertyBlock(propertyBlock);
                            propertyBlock.SetColor(ColorPropertyID, original);
                            propertyBlock.SetColor(BaseColorPropertyID, original);
                            rend.SetPropertyBlock(propertyBlock);
                        }
                    }
                }
            }

            EventBus.Publish(new BuildingDeselectedEvent(gameObject));
        }

        private void PlaySelectionAudio()
        {
            if (building != null && building.Data != null && building.Data.selectionAudio != null)
            {
                AudioSource.PlayClipAtPoint(building.Data.selectionAudio, transform.position);
            }
        }
    }
}
