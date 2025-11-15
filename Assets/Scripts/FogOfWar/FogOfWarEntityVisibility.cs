using UnityEngine;
using System.Collections.Generic;

namespace KingdomsAtDusk.FogOfWar
{
    /// <summary>
    /// Controls visibility of entities based on fog of war state.
    /// Hides enemy units/buildings that are not in visible areas.
    /// </summary>
    public class FogOfWarEntityVisibility : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool isPlayerOwned = false;
        [SerializeField] private float updateInterval = 0.2f;
        [SerializeField] private bool hideInExplored = true; // Hide in explored (dark) areas

        [Header("Visibility Control")]
        [SerializeField] private List<Renderer> renderersToControl = new List<Renderer>();
        [SerializeField] private List<Canvas> canvasesToControl = new List<Canvas>();

        private float updateTimer;
        private bool currentlyVisible = true;
        private Transform cachedTransform;

        private void Awake()
        {
            cachedTransform = transform;

            // Auto-detect renderers if none specified
            if (renderersToControl.Count == 0)
            {
                renderersToControl.AddRange(GetComponentsInChildren<Renderer>(true));
            }

            // Auto-detect canvases if none specified
            if (canvasesToControl.Count == 0)
            {
                canvasesToControl.AddRange(GetComponentsInChildren<Canvas>(true));
            }
        }

        private void Update()
        {
            // Player-owned entities are always visible
            if (isPlayerOwned) return;

            // Only update at specified interval for performance
            updateTimer += Time.deltaTime;
            if (updateTimer < updateInterval) return;

            updateTimer = 0f;

            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (FogOfWarManager.Instance == null) return;

            VisionState state = FogOfWarManager.Instance.GetVisionState(cachedTransform.position);

            bool shouldBeVisible = state == VisionState.Visible;

            // Also show in explored areas if configured
            if (!hideInExplored && state == VisionState.Explored)
            {
                shouldBeVisible = true;
            }

            // Only update if visibility changed
            if (shouldBeVisible != currentlyVisible)
            {
                SetVisible(shouldBeVisible);
                currentlyVisible = shouldBeVisible;
            }
        }

        private void SetVisible(bool visible)
        {
            // Control renderers
            foreach (var renderer in renderersToControl)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }

            // Control canvases (for health bars, etc.)
            foreach (var canvas in canvasesToControl)
            {
                if (canvas != null)
                {
                    canvas.enabled = visible;
                }
            }
        }

        /// <summary>
        /// Set whether this entity is owned by the player
        /// </summary>
        public void SetPlayerOwned(bool owned)
        {
            isPlayerOwned = owned;

            if (isPlayerOwned)
            {
                SetVisible(true);
                currentlyVisible = true;
            }
        }

        /// <summary>
        /// Manually add a renderer to control
        /// </summary>
        public void AddRenderer(Renderer renderer)
        {
            if (renderer != null && !renderersToControl.Contains(renderer))
            {
                renderersToControl.Add(renderer);
            }
        }

        /// <summary>
        /// Manually add a canvas to control
        /// </summary>
        public void AddCanvas(Canvas canvas)
        {
            if (canvas != null && !canvasesToControl.Contains(canvas))
            {
                canvasesToControl.Add(canvas);
            }
        }

        private void OnEnable()
        {
            // Force visibility update when enabled
            updateTimer = updateInterval;
        }
    }
}
