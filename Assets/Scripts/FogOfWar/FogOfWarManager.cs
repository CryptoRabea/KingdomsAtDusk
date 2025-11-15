using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace KingdomsAtDusk.FogOfWar
{
    /// <summary>
    /// Main manager for the fog of war system. Handles vision tracking and updates.
    /// </summary>
    public class FogOfWarManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private FogOfWarConfig config = new FogOfWarConfig();

        [Header("Player Settings")]
        [SerializeField] private int localPlayerId = 0; // The player we're showing fog of war for

        [Header("References")]
        [SerializeField] private FogOfWarRenderer fogRenderer;
        [SerializeField] private FogOfWarMinimapRenderer minimapRenderer;

        private FogOfWarGrid grid;
        private List<IVisionProvider> visionProviders = new List<IVisionProvider>();
        private float updateTimer;
        private bool isInitialized;

        public static FogOfWarManager Instance { get; private set; }

        public FogOfWarConfig Config => config;
        public FogOfWarGrid Grid => grid;
        public int LocalPlayerId => localPlayerId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;

            // Create the grid
            grid = new FogOfWarGrid(config.worldBounds, config.cellSize);

            // Initialize renderers
            if (fogRenderer != null)
            {
                fogRenderer.Initialize(this);
            }

            if (minimapRenderer != null)
            {
                minimapRenderer.Initialize(this);
            }

            // Register existing vision providers
            RegisterExistingVisionProviders();

            isInitialized = true;

            Debug.Log($"[FogOfWarManager] Initialized with {visionProviders.Count} vision providers");
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Update fog of war at specified interval
            updateTimer += Time.deltaTime;

            if (updateTimer >= config.updateInterval)
            {
                UpdateVision();
                updateTimer = 0f;
            }

            // Update fade timers
            UpdateFadeTimers();

            // Debug visualization
            if (config.enableDebugVisualization)
            {
                grid.DrawDebugGrid();
            }
        }

        /// <summary>
        /// Register existing units and buildings as vision providers
        /// </summary>
        private void RegisterExistingVisionProviders()
        {
            // Find all existing vision providers in the scene
            var providers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<IVisionProvider>();

            foreach (var provider in providers)
            {
                if (provider.OwnerId == localPlayerId)
                {
                    RegisterVisionProvider(provider);
                }
            }
        }

        /// <summary>
        /// Register a vision provider (unit, building, etc.)
        /// </summary>
        public void RegisterVisionProvider(IVisionProvider provider)
        {
            if (provider == null) return;

            if (!visionProviders.Contains(provider))
            {
                visionProviders.Add(provider);
                Debug.Log($"[FogOfWarManager] Registered vision provider: {provider.GameObject.name}");
            }
        }

        /// <summary>
        /// Unregister a vision provider
        /// </summary>
        public void UnregisterVisionProvider(IVisionProvider provider)
        {
            if (provider == null) return;

            if (visionProviders.Remove(provider))
            {
                Debug.Log($"[FogOfWarManager] Unregistered vision provider: {provider.GameObject.name}");
            }
        }

        /// <summary>
        /// Update vision based on all active vision providers
        /// </summary>
        private void UpdateVision()
        {
            if (grid == null) return;

            // Clear all currently visible cells (they become explored)
            grid.ClearVisibleCells();

            // Update vision for each active provider
            foreach (var provider in visionProviders.ToList())
            {
                if (provider == null || !provider.IsActive)
                {
                    visionProviders.Remove(provider);
                    continue;
                }

                // Only reveal vision for our team
                if (provider.OwnerId != localPlayerId)
                    continue;

                // Reveal circular area around the provider
                grid.RevealCircle(provider.Position, provider.VisionRadius);
            }

            // Notify renderers
            if (fogRenderer != null)
            {
                fogRenderer.OnVisionUpdated();
            }

            if (minimapRenderer != null)
            {
                minimapRenderer.OnVisionUpdated();
            }
        }

        /// <summary>
        /// Update fade timers for smooth transitions
        /// </summary>
        private void UpdateFadeTimers()
        {
            // This would be optimized in production to only update visible cells
            // For now, we update a subset each frame for performance
            int cellsUpdated = 0;

            for (int x = 0; x < grid.Width && cellsUpdated < config.maxCellUpdatesPerFrame; x++)
            {
                for (int y = 0; y < grid.Height && cellsUpdated < config.maxCellUpdatesPerFrame; y++)
                {
                    grid.UpdateVisibilityTimer(new Vector2Int(x, y), Time.deltaTime, config.fadeSpeed);
                    cellsUpdated++;
                }
            }
        }

        /// <summary>
        /// Check if a world position is visible
        /// </summary>
        public bool IsVisible(Vector3 worldPos)
        {
            if (grid == null) return true;
            return grid.GetState(worldPos) == VisionState.Visible;
        }

        /// <summary>
        /// Check if a world position is explored
        /// </summary>
        public bool IsExplored(Vector3 worldPos)
        {
            if (grid == null) return true;
            var state = grid.GetState(worldPos);
            return state == VisionState.Explored || state == VisionState.Visible;
        }

        /// <summary>
        /// Get vision state at a world position
        /// </summary>
        public VisionState GetVisionState(Vector3 worldPos)
        {
            if (grid == null) return VisionState.Visible;
            return grid.GetState(worldPos);
        }

        /// <summary>
        /// Force an immediate vision update
        /// </summary>
        public void ForceUpdate()
        {
            UpdateVision();
        }

        /// <summary>
        /// Reveal the entire map (cheat/debug)
        /// </summary>
        public void RevealAll()
        {
            if (grid == null) return;

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    grid.SetState(new Vector2Int(x, y), VisionState.Visible);
                }
            }

            if (fogRenderer != null)
            {
                fogRenderer.OnVisionUpdated();
            }

            if (minimapRenderer != null)
            {
                minimapRenderer.OnVisionUpdated();
            }

            Debug.Log("[FogOfWarManager] Revealed entire map");
        }

        /// <summary>
        /// Hide the entire map (reset)
        /// </summary>
        public void HideAll()
        {
            if (grid == null) return;

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    grid.SetState(new Vector2Int(x, y), VisionState.Unexplored);
                }
            }

            if (fogRenderer != null)
            {
                fogRenderer.OnVisionUpdated();
            }

            if (minimapRenderer != null)
            {
                minimapRenderer.OnVisionUpdated();
            }

            Debug.Log("[FogOfWarManager] Hidden entire map");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnDrawGizmos()
        {
            if (!config.enableDebugVisualization) return;

            // Draw world bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(config.worldBounds.center, config.worldBounds.size);
        }
    }
}
