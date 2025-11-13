using System.Collections.Generic;
using System.Linq;

namespace RTS.Editor
{
    /// <summary>
    /// Static class that provides definitions for all extractable systems.
    /// Add new systems here to make them available for extraction.
    /// </summary>
    public static class SystemDefinitions
    {
        private static List<SystemDefinition> allSystems;

        /// <summary>
        /// Get all available system definitions.
        /// </summary>
        public static List<SystemDefinition> GetAllSystems()
        {
            if (allSystems == null)
            {
                InitializeSystems();
            }
            return allSystems;
        }

        /// <summary>
        /// Get a system definition by name.
        /// </summary>
        public static SystemDefinition GetSystemByName(string name)
        {
            return GetAllSystems().FirstOrDefault(s => s.Name == name);
        }

        /// <summary>
        /// Initialize all system definitions.
        /// </summary>
        private static void InitializeSystems()
        {
            allSystems = new List<SystemDefinition>
            {
                CreateResourceSystem(),
                CreateHappinessSystem(),
                CreateBuildingSystem(),
                CreateWallSystem(),
                CreateEventSystem(),
                CreateServiceLocator(),
                CreatePoolingSystem(),
                CreateTimeSystem(),
                CreateSelectionSystem(),
                CreateUISystem()
            };
        }

        private static SystemDefinition CreateResourceSystem()
        {
            return new SystemDefinition
            {
                Name = "Resource Management System",
                PackageName = "resource-management",
                Category = "Core Systems",
                Description = "Complete resource management system with support for multiple resource types (Wood, Food, Gold, Stone). Data-driven design makes adding new resources trivial.",
                Features = new List<string>
                {
                    "Multiple resource types support",
                    "Resource spending and affordability checks",
                    "Event-driven resource updates",
                    "Helper class for building resource dictionaries",
                    "Easy to extend with new resource types"
                },
                Files = new List<string>
                {
                    "Assets/Scripts/Core/IServices.cs",
                    "Assets/Scripts/Managers/ResourceManager.cs"
                },
                Dependencies = new List<string>
                {
                    "Event System",
                    "Service Locator"
                },
                UnityDependencies = new Dictionary<string, string>(),
                Keywords = new[] { "resources", "economy", "management", "rts" },
                QuickStart = "Add ResourceManager component to a GameObject. Use ServiceLocator.Register() to register it. Access via IResourcesService interface.",
                TechnicalDetails = "Dictionary-based resource storage with event notifications. Supports dynamic resource types through enum extension.",
                UsageExample = @"// Get resources
IResourcesService resources = ServiceLocator.Get<IResourcesService>();
int wood = resources.GetResource(ResourceType.Wood);

// Spend resources
var costs = ResourceCost.Build().Wood(100).Stone(50).Create();
bool success = resources.SpendResources(costs);",
                Configuration = "Configure starting resources in ResourceManager component inspector.",
                BestPractices = new List<string>
                {
                    "Always use IResourcesService interface, never ResourceManager directly",
                    "Use ResourceCost.Build() for clean cost definitions",
                    "Subscribe to ResourcesChangedEvent for UI updates",
                    "Check CanAfford() before attempting to spend resources"
                }
            };
        }

        private static SystemDefinition CreateHappinessSystem()
        {
            return new SystemDefinition
            {
                Name = "Happiness System",
                PackageName = "happiness-system",
                Category = "Core Systems",
                Description = "Population happiness/morale system with tax management and building bonuses.",
                Features = new List<string>
                {
                    "Dynamic happiness calculation",
                    "Tax level management",
                    "Building happiness bonuses",
                    "Event-driven updates"
                },
                Files = new List<string>
                {
                    "Assets/Scripts/Core/IServices.cs",
                    "Assets/Scripts/Managers/HappinessManager.cs"
                },
                Dependencies = new List<string>
                {
                    "Event System",
                    "Service Locator"
                },
                UnityDependencies = new Dictionary<string, string>(),
                Keywords = new[] { "happiness", "morale", "management", "rts" },
                QuickStart = "Add HappinessManager to scene. Register with ServiceLocator. Buildings automatically apply bonuses.",
                TechnicalDetails = "Dictionary-based bonus tracking with automatic happiness recalculation on changes.",
                UsageExample = @"IHappinessService happiness = ServiceLocator.Get<IHappinessService>();
float current = happiness.CurrentHappiness;
happiness.TaxLevel = 0.2f; // 20% tax",
                Configuration = "Set base happiness and tax settings in HappinessManager inspector.",
                BestPractices = new List<string>
                {
                    "Keep happiness above 50% for optimal gameplay",
                    "Balance taxes with happiness bonuses from buildings"
                }
            };
        }

        private static SystemDefinition CreateBuildingSystem()
        {
            return new SystemDefinition
            {
                Name = "Building System",
                PackageName = "building-system",
                Category = "Gameplay Systems",
                Description = "Complete RTS building system with placement, construction, and data-driven configuration.",
                Features = new List<string>
                {
                    "Visual placement preview (green/red)",
                    "Grid snapping",
                    "Collision detection",
                    "Terrain validation",
                    "Resource cost checking",
                    "Construction time simulation",
                    "Data-driven building configuration (ScriptableObjects)",
                    "Happiness bonuses from buildings",
                    "Resource generation buildings"
                },
                Files = new List<string>
                {
                    "Assets/Scripts/Managers/BuildingManager.cs",
                    "Assets/Scripts/RTSBuildingsSystems/Building.cs",
                    "Assets/Scripts/RTSBuildingsSystems/BuildingDataSO.cs",
                    "Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs",
                    "Assets/Scripts/RTSBuildingsSystems/BuildingSelectable.cs",
                    "Assets/Scripts/RTSBuildingsSystems/BuildingSelectionManager.cs"
                },
                Dependencies = new List<string>
                {
                    "Resource Management System",
                    "Happiness System",
                    "Event System",
                    "Service Locator",
                    "Selection System"
                },
                UnityDependencies = new Dictionary<string, string>
                {
                    { "com.unity.inputsystem", "1.4.4" }
                },
                Keywords = new[] { "building", "construction", "placement", "rts" },
                QuickStart = "Add BuildingManager to scene. Assign BuildingDataSO assets. Connect to UI buttons.",
                TechnicalDetails = "Data-driven architecture using ScriptableObjects. Event-driven placement and construction.",
                UsageExample = @"BuildingManager manager = FindFirstObjectByType<BuildingManager>();
BuildingDataSO wallData = manager.GetBuildingByName(""Stone Wall"");
manager.StartPlacingBuilding(wallData);",
                Configuration = "Create BuildingDataSO assets for each building type. Configure costs, construction time, and bonuses.",
                BestPractices = new List<string>
                {
                    "Use BuildingDataSO as source of truth",
                    "Keep grid size consistent across systems",
                    "Always check resource affordability before placement",
                    "Use event system for placement/destruction notifications"
                }
            };
        }

        private static SystemDefinition CreateWallSystem()
        {
            return new SystemDefinition
            {
                Name = "Wall Connection System",
                PackageName = "wall-system",
                Category = "Gameplay Systems",
                Description = "Advanced modular wall system with automatic connections, pole-to-pole placement, and multiple construction modes.",
                Features = new List<string>
                {
                    "Automatic neighbor detection and connection",
                    "16-state bitmask connection system",
                    "Pole-to-pole placement mode",
                    "Real-time resource preview",
                    "Line preview between poles",
                    "Multiple construction modes (Instant, Timed, Segment-based)",
                    "Worker assignment system",
                    "Visual construction progress",
                    "Seamless integration with Building System"
                },
                Files = new List<string>
                {
                    "Assets/Scripts/RTSBuildingsSystems/WallConnectionSystem.cs",
                    "Assets/Scripts/RTSBuildingsSystems/WallPlacementController.cs",
                    "Assets/Scripts/RTSBuildingsSystems/WallSegmentConstructor.cs",
                    "Assets/Scripts/RTSBuildingsSystems/ConstructionMode.cs",
                    "Assets/Scripts/UI/WallResourcePreviewUI.cs"
                },
                Dependencies = new List<string>
                {
                    "Building System",
                    "Resource Management System",
                    "Event System"
                },
                UnityDependencies = new Dictionary<string, string>
                {
                    { "com.unity.inputsystem", "1.4.4" }
                },
                Keywords = new[] { "wall", "fortification", "defense", "rts", "modular" },
                QuickStart = "See POLE_TO_POLE_WALL_SYSTEM.md for detailed setup instructions. Add WallPlacementController to scene and connect to BuildingManager.",
                TechnicalDetails = "Static grid-based registry with O(1) neighbor lookups. Bitmask state encoding for 16 visual variants. Event-driven updates.",
                UsageExample = @"// Walls automatically use pole-to-pole placement
BuildingManager manager = FindFirstObjectByType<BuildingManager>();
BuildingDataSO wallData = manager.GetBuildingByName(""Stone Wall"");
manager.StartPlacingBuilding(wallData); // Click twice to place walls",
                Configuration = "Configure WallPlacementController in scene. Set construction mode on wall prefabs. Assign 16 mesh variants to WallConnectionSystem.",
                BestPractices = new List<string>
                {
                    "Keep gridSize=1.0 across all wall systems",
                    "Create all 16 mesh variants for proper connections",
                    "Use pole-to-pole mode for long walls",
                    "Choose construction mode based on gameplay needs",
                    "Assign workers programmatically for SegmentWithWorkers mode"
                }
            };
        }

        private static SystemDefinition CreateEventSystem()
        {
            return new SystemDefinition
            {
                Name = "Event System",
                PackageName = "event-system",
                Category = "Core Systems",
                Description = "Type-safe event bus for decoupled communication between systems.",
                Features = new List<string>
                {
                    "Type-safe events",
                    "Subscribe/Unsubscribe pattern",
                    "No coupling between publishers and subscribers",
                    "Easy to add new event types"
                },
                Files = new List<string>
                {
                    "Assets/Scripts/Core/EventBus.cs",
                    "Assets/Scripts/Core/Events.cs"
                },
                Dependencies = new List<string>(),
                UnityDependencies = new Dictionary<string, string>(),
                Keywords = new[] { "events", "messaging", "decoupling", "architecture" },
                QuickStart = "Define events as classes. Use EventBus.Subscribe/Publish/Unsubscribe. Always unsubscribe in OnDestroy.",
                TechnicalDetails = "Dictionary-based event routing with Action delegates. Generic type parameter ensures type safety.",
                UsageExample = @"// Subscribe
EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);

// Publish
EventBus.Publish(new BuildingPlacedEvent(building, position));

// Unsubscribe
EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);",
                Configuration = "No configuration needed. Add event definitions to Events.cs.",
                BestPractices = new List<string>
                {
                    "Always unsubscribe in OnDestroy to prevent memory leaks",
                    "Keep event data immutable",
                    "Name events with past tense (BuildingPlaced, not PlaceBuilding)",
                    "Don't use events for immediate responses - use direct calls"
                }
            };
        }

        private static SystemDefinition CreateServiceLocator()
        {
            return new SystemDefinition
            {
                Name = "Service Locator",
                PackageName = "service-locator",
                Category = "Core Systems",
                Description = "Dependency injection pattern implementation for accessing game services.",
                Features = new List<string>
                {
                    "Interface-based service registration",
                    "Type-safe service retrieval",
                    "Global service access without singletons",
                    "Easy to test and mock"
                },
                Files = new List<string>
                {
                    "Assets/Scripts/Core/ServiceLocator.cs",
                    "Assets/Scripts/Core/IServices.cs"
                },
                Dependencies = new List<string>(),
                UnityDependencies = new Dictionary<string, string>(),
                Keywords = new[] { "dependency-injection", "services", "architecture" },
                QuickStart = "Register services with ServiceLocator.Register<IService>(implementation). Access via ServiceLocator.Get<IService>().",
                TechnicalDetails = "Dictionary-based service storage with interface keys. Supports TryGet for optional services.",
                UsageExample = @"// Register
ServiceLocator.Register<IResourcesService>(resourceManager);

// Get
IResourcesService resources = ServiceLocator.Get<IResourcesService>();

// Try Get (returns null if not found)
var service = ServiceLocator.TryGet<IResourcesService>();",
                Configuration = "Services are typically registered in Awake() or Start() methods of manager classes.",
                BestPractices = new List<string>
                {
                    "Always code against interfaces, not implementations",
                    "Register services early (Awake/Start)",
                    "Use TryGet for optional services",
                    "Don't abuse - use for truly global services only"
                }
            };
        }

        private static SystemDefinition CreatePoolingSystem()
        {
            return new SystemDefinition
            {
                Name = "Object Pooling System",
                PackageName = "object-pooling",
                Category = "Performance Systems",
                Description = "Efficient object pooling for frequently instantiated/destroyed objects.",
                Features = new List<string>
                {
                    "Automatic pool creation",
                    "Warmup support",
                    "Generic type support",
                    "Pool clearing"
                },
                Files = new List<string>
                {
                    "Assets/Scripts/Core/ObjectPool.cs"
                },
                Dependencies = new List<string>
                {
                    "Service Locator"
                },
                UnityDependencies = new Dictionary<string, string>(),
                Keywords = new[] { "pooling", "performance", "optimization" },
                QuickStart = "Use IPoolService.Get<T>(prefab) instead of Instantiate. Return with IPoolService.Return<T>(instance).",
                TechnicalDetails = "Dictionary-based pools per prefab type. Automatic GameObject activation/deactivation.",
                UsageExample = @"IPoolService pool = ServiceLocator.Get<IPoolService>();

// Warmup
pool.Warmup(bulletPrefab, 50);

// Get from pool
Bullet bullet = pool.Get(bulletPrefab);

// Return to pool
pool.Return(bullet);",
                Configuration = "Optional: Warmup pools in Start() for frequently used objects.",
                BestPractices = new List<string>
                {
                    "Warmup pools for objects spawned frequently",
                    "Always return objects to pool when done",
                    "Reset object state before returning to pool"
                }
            };
        }

        private static SystemDefinition CreateTimeSystem()
        {
            return new SystemDefinition
            {
                Name = "Time System",
                PackageName = "time-system",
                Category = "Core Systems",
                Description = "Day/night cycle and time management system.",
                Features = new List<string>
                {
                    "Day/night cycle tracking",
                    "Time scale control",
                    "Current day tracking",
                    "Progress percentage"
                },
                Files = new List<string>
                {
                    "Assets/Scripts/Managers/TimeManager.cs"
                },
                Dependencies = new List<string>
                {
                    "Service Locator",
                    "Event System"
                },
                UnityDependencies = new Dictionary<string, string>(),
                Keywords = new[] { "time", "day-night", "cycle" },
                QuickStart = "Add TimeManager to scene. Access via ITimeService interface.",
                TechnicalDetails = "Time.deltaTime based accumulation with configurable day length.",
                UsageExample = @"ITimeService time = ServiceLocator.Get<ITimeService>();
float progress = time.DayProgress; // 0-1
int day = time.CurrentDay;
time.SetTimeScale(2.0f); // 2x speed",
                Configuration = "Configure day length in TimeManager inspector.",
                BestPractices = new List<string>
                {
                    "Use DayProgress for lighting transitions",
                    "Subscribe to day change events for scheduled activities"
                }
            };
        }

        private static SystemDefinition CreateSelectionSystem()
        {
            return new SystemDefinition
            {
                Name = "Building Selection System",
                PackageName = "selection-system",
                Category = "UI Systems",
                Description = "Building selection and highlighting system with camera integration.",
                Features = new List<string>
                {
                    "Click-to-select buildings",
                    "Visual selection feedback",
                    "Selection change events",
                    "Multiple selection modes"
                },
                Files = new List<string>
                {
                    "Assets/Scripts/RTSBuildingsSystems/BuildingSelectable.cs",
                    "Assets/Scripts/RTSBuildingsSystems/BuildingSelectionManager.cs"
                },
                Dependencies = new List<string>
                {
                    "Event System"
                },
                UnityDependencies = new Dictionary<string, string>
                {
                    { "com.unity.inputsystem", "1.4.4" }
                },
                Keywords = new[] { "selection", "input", "ui" },
                QuickStart = "Add BuildingSelectionManager to scene. Add BuildingSelectable to selectable objects.",
                TechnicalDetails = "Raycast-based selection with input system integration.",
                UsageExample = @"// Buildings with BuildingSelectable can be clicked
// Manager automatically handles selection and events",
                Configuration = "Assign camera and configure input in BuildingSelectionManager.",
                BestPractices = new List<string>
                {
                    "Add BuildingSelectable to all interactive buildings",
                    "Subscribe to BuildingSelectedEvent for UI updates"
                }
            };
        }

        private static SystemDefinition CreateUISystem()
        {
            return new SystemDefinition
            {
                Name = "RTS UI System",
                PackageName = "rts-ui-system",
                Category = "UI Systems",
                Description = "Complete UI system for RTS games including building buttons, resource displays, and detail panels.",
                Features = new List<string>
                {
                    "Building placement buttons",
                    "Resource display with icons",
                    "Building detail panels",
                    "Happiness display",
                    "Tooltips"
                },
                Files = new List<string>
                {
                    "Assets/Scripts/RTSBuildingsSystems/BuildingButton.cs",
                    "Assets/Scripts/RTSBuildingsSystems/BuildingHUD.cs",
                    "Assets/Scripts/UI/BuildingDetailsUI.cs",
                    "Assets/Scripts/RTSBuildingsSystems/BuildingTooltip.cs"
                },
                Dependencies = new List<string>
                {
                    "Building System",
                    "Resource Management System",
                    "Event System",
                    "Selection System"
                },
                UnityDependencies = new Dictionary<string, string>
                {
                    { "com.unity.textmeshpro", "3.0.6" }
                },
                Keywords = new[] { "ui", "interface", "hud" },
                QuickStart = "Use editor tools: Tools > RTS > Setup Building Training UI to auto-generate UI.",
                TechnicalDetails = "Event-driven UI updates. TextMeshPro for text rendering.",
                UsageExample = @"// UI updates automatically via events
// Use BuildingButton for building placement
// BuildingDetailsUI shows selected building info",
                Configuration = "Use setup tools in Tools > RTS menu for automatic configuration.",
                BestPractices = new List<string>
                {
                    "Subscribe to events for dynamic UI updates",
                    "Use TextMeshPro for all text",
                    "Keep UI responsive with event-driven updates"
                }
            };
        }
    }
}
