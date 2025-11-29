using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using KingdomsAtDusk.FogOfWar;


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
            if (isInitialized)
            {
                Debug.LogWarning("[FogOfWarManager] Already initialized, skipping");
                return;
            }

            Debug.Log($"[FogOfWarManager] Starting initialization...");
            Debug.Log($"[FogOfWarManager] World Bounds: {config.worldBounds}");
            Debug.Log($"[FogOfWarManager] Cell Size: {config.cellSize}");

            // Create the grid
            grid = new FogOfWarGrid(config.worldBounds, config.cellSize);
            Debug.Log($"[FogOfWarManager] Grid created: {grid.Width}x{grid.Height} cells");

            // Initialize renderers
            if (fogRenderer != null)
            {
                Debug.Log("[FogOfWarManager] Initializing fog renderer...");
                fogRenderer.Initialize(this);
            }
            else
            {
                Debug.LogWarning("[FogOfWarManager] No fog renderer assigned!");
            }

            if (minimapRenderer != null)
            {
                Debug.Log("[FogOfWarManager] Initializing minimap renderer...");
                minimapRenderer.Initialize(this);
            }
            else
            {
                Debug.LogWarning("[FogOfWarManager] No minimap renderer assigned!");
            }

            // Register existing vision providers
            RegisterExistingVisionProviders();

            isInitialized = true;

            Debug.Log($"[FogOfWarManager] ✓ Initialization complete with {visionProviders.Count} vision providers");
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
            var providers = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
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

                // Force an immediate vision update when a new provider is registered
                if (isInitialized)
                {
                    UpdateVision();
                }
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
            }
        }

        /// <summary>
        /// Update vision based on all active vision providers
        /// </summary>
        private void UpdateVision()
        {
            if (grid == null)
            {
                Debug.LogWarning("[FogOfWarManager] UpdateVision called but grid is null!");
                return;
            }

            // Clear all currently visible cells (they become explored)
            grid.ClearVisibleCells();

            int activeProviders = 0;

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
                Vector3 providerPos = provider.Position;
                grid.RevealCircle(providerPos, provider.VisionRadius);
                activeProviders++;
            }

            // Log warning if no providers are providing vision
            if (activeProviders == 0 && visionProviders.Count > 0)
            {
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

        /// <summary>
        /// Debug command to print fog of war status
        /// </summary>
        [ContextMenu("Debug: Print Fog of War Status")]
        public void DebugPrintStatus()
        {
            Debug.Log("=== FOG OF WAR STATUS ===");
            Debug.Log($"Initialized: {isInitialized}");
            Debug.Log($"Grid: {(grid != null ? $"{grid.Width}x{grid.Height}" : "NULL")}");
            Debug.Log($"Vision Providers: {visionProviders.Count}");
            Debug.Log($"Local Player ID: {localPlayerId}");

            int friendlyProviders = 0;
            int enemyProviders = 0;

            foreach (var provider in visionProviders)
            {
                if (provider == null) continue;
                if (provider.OwnerId == localPlayerId)
                    friendlyProviders++;
                else
                    enemyProviders++;
            }

            Debug.Log($"Friendly Providers: {friendlyProviders}");
            Debug.Log($"Enemy Providers: {enemyProviders}");
            Debug.Log($"Fog Renderer: {(fogRenderer != null ? "Present" : "NULL")}");
            Debug.Log($"Minimap Renderer: {(minimapRenderer != null ? "Present" : "NULL")}");
            Debug.Log("========================");
        }

        /// <summary>
        /// Debug command to reveal entire map
        /// </summary>
        [ContextMenu("Debug: Reveal All")]
        public void DebugRevealAll()
        {
            RevealAll();
        }

        /// <summary>
        /// Debug command to hide entire map
        /// </summary>
        [ContextMenu("Debug: Hide All")]
        public void DebugHideAll()
        {
            HideAll();
        }
    }

