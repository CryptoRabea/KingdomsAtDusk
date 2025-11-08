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

        private bool isSelected;
        private Renderer[] renderers;
        private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();

        public bool IsSelected { get { return isSelected; } }

        private void Awake()
        {
            if (useColorHighlight)
            {
                renderers = GetComponentsInChildren<Renderer>();
                foreach (var rend in renderers)
                {
                    if (rend != null && rend.material != null)
                    {
                        originalColors[rend] = rend.material.color;
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

            if (useColorHighlight)
            {
                foreach (var rend in renderers)
                {
                    if (rend != null && rend.material != null)
                    {
                        rend.material.color = selectedColor;
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

            if (useColorHighlight)
            {
                foreach (var rend in renderers)
                {
                    if (rend != null && originalColors.TryGetValue(rend, out Color original))
                    {
                        rend.material.color = original;
                    }
                }
            }

            EventBus.Publish(new UnitDeselectedEvent(gameObject));
        }
    }
}
