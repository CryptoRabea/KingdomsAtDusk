using UnityEngine;
using CircularLensVision;
using KingdomsAtDusk.Core;

/// <summary>
/// Integration component that automatically sets up lens vision on units and buildings.
/// Works with the existing EventBus system to listen for unit spawns and building placements.
/// </summary>
public class LensVisionIntegration : MonoBehaviour
{
    [Header("Game Config Integration")]
    [Tooltip("Use settings from GameConfig ScriptableObject (if null, uses local settings)")]
    [SerializeField] private GameConfigSO gameConfig;

    [Tooltip("Override game config and use local settings")]
    [SerializeField] private bool useLocalSettings = false;

    [Header("Auto-Setup Configuration")]
    [Tooltip("Automatically add LensVisionTarget to spawned units")]
    [SerializeField] private bool autoSetupUnits = true;

    [Tooltip("Automatically add LensVisionTarget to placed buildings")]
    [SerializeField] private bool autoSetupBuildings = true;

    [Tooltip("Automatically add LensVisionTarget to obstacles with specific tags")]
    [SerializeField] private bool autoSetupObstacles = true;

    [Tooltip("Tags that identify obstacles (trees, vegetation, etc.)")]
    [SerializeField] private string[] obstacleTags = { "Tree", "Obstacle", "Vegetation", "Rock" };

    [Header("Unit Settings (Local Override)")]
    [Tooltip("X-Ray color for player units")]
    [SerializeField] private Color playerUnitXRayColor = new Color(0.3f, 0.7f, 1f, 0.8f);

    [Tooltip("X-Ray color for enemy units")]
    [SerializeField] private Color enemyUnitXRayColor = new Color(1f, 0.3f, 0.3f, 0.8f);

    [Header("Obstacle Settings (Local Override)")]
    [Tooltip("Transparency amount for obstacles in lens")]
    [SerializeField] private float obstacleTransparency = 0.3f;

    [Header("References")]
    [Tooltip("Reference to CircularLensVision controller (will auto-find if empty)")]
    [SerializeField] private CircularLensVision.CircularLensVision lensController;

    private void Awake()
    {
        // Load game config if not already assigned
        if (gameConfig == null && !useLocalSettings)
        {
            gameConfig = Resources.Load<GameConfigSO>("GameConfig");
        }

        // Find lens controller if not assigned
        if (lensController == null)
        {
            lensController = FindObjectOfType<CircularLensVision.CircularLensVision>();
        }

        if (lensController == null)
        {
            Debug.LogWarning("LensVisionIntegration: No CircularLensVision controller found in scene.", this);
        }

        // Apply settings from game config
        ApplyGameConfigSettings();
    }

    /// <summary>
    /// Apply settings from game config
    /// </summary>
    private void ApplyGameConfigSettings()
    {
        if (useLocalSettings || gameConfig == null) return;

        playerUnitXRayColor = gameConfig.playerUnitXRayColor;
        enemyUnitXRayColor = gameConfig.enemyUnitXRayColor;
        obstacleTransparency = gameConfig.obstacleTransparency;
    }

    /// <summary>
    /// Check if lens vision is enabled in game config
    /// </summary>
    private bool IsLensVisionEnabled()
    {
        if (useLocalSettings || gameConfig == null)
        {
            return true; // Use local settings, always enabled
        }

        return gameConfig.enableLensVision;
    }

    private void OnEnable()
    {
        // Subscribe to EventBus events
        EventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);
        EventBus.Subscribe<UnitDespawnedEvent>(OnUnitDespawned);
        EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
        EventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
    }

    private void OnDisable()
    {
        // Unsubscribe from EventBus events
        EventBus.Unsubscribe<UnitSpawnedEvent>(OnUnitSpawned);
        EventBus.Unsubscribe<UnitDespawnedEvent>(OnUnitDespawned);
        EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
        EventBus.Unsubscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
    }

    private void Start()
    {
        // Only setup if lens vision is enabled
        if (!IsLensVisionEnabled()) return;

        // Setup existing obstacles in scene
        if (autoSetupObstacles)
        {
            SetupExistingObstacles();
        }

        // Setup existing units and buildings
        if (autoSetupUnits)
        {
            SetupExistingUnits();
        }

        if (autoSetupBuildings)
        {
            SetupExistingBuildings();
        }
    }

    private void OnUnitSpawned(UnitSpawnedEvent evt)
    {
        if (!IsLensVisionEnabled() || !autoSetupUnits || evt.Unit == null) return;

        SetupUnit(evt.Unit);
    }

    private void OnUnitDespawned(UnitDespawnedEvent evt)
    {
        // LensVisionTarget will automatically unregister on disable/destroy
    }

    private void OnBuildingPlaced(BuildingPlacedEvent evt)
    {
        if (!IsLensVisionEnabled() || !autoSetupBuildings || evt.Building == null) return;

        SetupBuilding(evt.Building);
    }

    private void OnBuildingDestroyed(BuildingDestroyedEvent evt)
    {
        // LensVisionTarget will automatically unregister on disable/destroy
    }

    private void SetupUnit(GameObject unit)
    {
        if (unit == null) return;

        // Check if already has LensVisionTarget
        LensVisionTarget existing = unit.GetComponent<LensVisionTarget>();
        if (existing != null) return;

        // Add LensVisionTarget component
        LensVisionTarget target = unit.AddComponent<LensVisionTarget>();

        // Determine if player or enemy unit
        bool isPlayerUnit = unit.layer == LayerMask.NameToLayer("Player");
        Color xrayColor = isPlayerUnit ? playerUnitXRayColor : enemyUnitXRayColor;

        // Configure target using reflection or public methods
        target.SetXRayColor(xrayColor);

        // Register with controller
        if (lensController != null)
        {
            target.SetLensController(lensController);
        }
    }

    private void SetupBuilding(GameObject building)
    {
        if (building == null) return;

        // Check if already has LensVisionTarget
        LensVisionTarget existing = building.GetComponent<LensVisionTarget>();
        if (existing != null) return;

        // Buildings are treated as obstacles
        LensVisionTarget target = building.AddComponent<LensVisionTarget>();
        target.SetTransparencyAmount(obstacleTransparency);

        // Register with controller
        if (lensController != null)
        {
            target.SetLensController(lensController);
        }
    }

    private void SetupObstacle(GameObject obstacle)
    {
        if (obstacle == null) return;

        // Check if already has LensVisionTarget
        LensVisionTarget existing = obstacle.GetComponent<LensVisionTarget>();
        if (existing != null) return;

        // Add LensVisionTarget component
        LensVisionTarget target = obstacle.AddComponent<LensVisionTarget>();
        target.SetTransparencyAmount(obstacleTransparency);

        // Register with controller
        if (lensController != null)
        {
            target.SetLensController(lensController);
        }
    }

    private void SetupExistingObstacles()
    {
        foreach (string tag in obstacleTags)
        {
            GameObject[] obstacles = GameObject.FindGameObjectsWithTag(tag);
            foreach (var obstacle in obstacles)
            {
                SetupObstacle(obstacle);
            }
        }
    }

    private void SetupExistingUnits()
    {
        // Find all GameObjects with UnitAIController or similar unit components
        var unitControllers = FindObjectsOfType<UnitAIController>();
        foreach (var controller in unitControllers)
        {
            SetupUnit(controller.gameObject);
        }
    }

    private void SetupExistingBuildings()
    {
        // Find all GameObjects with Building component
        var buildings = FindObjectsOfType<Building>();
        foreach (var building in buildings)
        {
            SetupBuilding(building.gameObject);
        }
    }

    /// <summary>
    /// Manually setup lens vision on a specific GameObject
    /// </summary>
    public void SetupLensVisionTarget(GameObject target, bool isUnit)
    {
        if (target == null) return;

        if (isUnit)
        {
            SetupUnit(target);
        }
        else
        {
            SetupObstacle(target);
        }
    }

    /// <summary>
    /// Update all lens vision colors (useful for team changes)
    /// </summary>
    public void RefreshAllTargetColors()
    {
        LensVisionTarget[] targets = FindObjectsOfType<LensVisionTarget>();

        foreach (var target in targets)
        {
            if (target.Type == LensVisionTarget.TargetType.Unit)
            {
                bool isPlayerUnit = target.gameObject.layer == LayerMask.NameToLayer("Player");
                Color xrayColor = isPlayerUnit ? playerUnitXRayColor : enemyUnitXRayColor;
                target.SetXRayColor(xrayColor);
            }
            else
            {
                target.SetTransparencyAmount(obstacleTransparency);
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Setup All Objects in Scene")]
    private void SetupAllObjectsInScene()
    {
        SetupExistingObstacles();
        SetupExistingUnits();
        SetupExistingBuildings();
        Debug.Log("LensVisionIntegration: Setup complete for all objects in scene.");
    }

    [ContextMenu("Remove All Lens Vision Targets")]
    private void RemoveAllLensVisionTargets()
    {
        LensVisionTarget[] targets = FindObjectsOfType<LensVisionTarget>();
        int count = targets.Length;

        foreach (var target in targets)
        {
            DestroyImmediate(target);
        }

        Debug.Log($"LensVisionIntegration: Removed {count} LensVisionTarget components.");
    }
#endif
}
