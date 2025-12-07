using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTS.Core.Services;
using RTS.Core.Events;
using RTS.Buildings;
using RTS.Units;
using RTS.Units.AI;
using KingdomsAtDusk.FogOfWar;
using FischlWorks_FogWar;

namespace RTS.SaveLoad
{
    /// <summary>
    /// Main save/load manager handling all game state persistence.
    /// Implements ISaveLoadService and coordinates with all game systems.
    /// </summary>
    public class SaveLoadManager : MonoBehaviour, ISaveLoadService
    {
        [Header("Settings")]
        [SerializeField] private SaveLoadSettings settings;

        [Header("References")]
        [SerializeField] private Camera mainCamera;

        // Runtime state
        private float playTime = 0f;
        private Dictionary<int, GameObject> loadedEntitiesMap = new Dictionary<int, GameObject>();

        #region Initialization

        private void Awake()
        {
            EnsureSaveDirectoryExists();
        }

        private void Start()
        {
            // Check if we should auto-load a save
            CheckAutoLoad();
        }

        private void Update()
        {
            // Track playtime
            if (Time.timeScale > 0)
            {
                playTime += Time.deltaTime;
            }
        }

        private void CheckAutoLoad()
        {
            // Check if there's a save to auto-load
            if (PlayerPrefs.HasKey("LoadSaveOnStart"))
            {
                string saveToLoad = PlayerPrefs.GetString("LoadSaveOnStart");
                PlayerPrefs.DeleteKey("LoadSaveOnStart");
                PlayerPrefs.Save();

                if (!string.IsNullOrEmpty(saveToLoad))
                {
                    // Use a small delay to ensure all systems are initialized
                    StartCoroutine(DelayedLoad(saveToLoad));
                }
            }
        }

        private System.Collections.IEnumerator DelayedLoad(string saveName)
        {
            // Wait for one frame to ensure all systems are ready
            yield return null;

            bool success = LoadGame(saveName);
            if (!success)
            {
            }
        }

        private void EnsureSaveDirectoryExists()
        {
            string savePath = settings.GetSaveDirectoryPath();
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                Log($"Created save directory: {savePath}");
            }
        }

        #endregion

        #region ISaveLoadService Implementation

        public bool SaveGame(string saveName, bool isAutoSave = false, bool isQuickSave = false)
        {
            try
            {
                Log($"Saving game: {saveName} (AutoSave: {isAutoSave}, QuickSave: {isQuickSave})");

                // Collect all game state
                GameSaveData saveData = CollectGameState();
                saveData.saveName = saveName;
                saveData.playTime = playTime;

                // Serialize to JSON
                string json = JsonUtility.ToJson(saveData, true);

                // Apply compression/encryption if enabled
                if (settings.useCompression)
                {
                    json = CompressString(json);
                }
                if (settings.useEncryption)
                {
                    json = EncryptString(json);
                }

                // Write to file
                string filePath = settings.GetSaveFilePath(saveName);
                File.WriteAllText(filePath, json);

                Log($" Game saved successfully: {filePath}");

                // Publish save event
                EventBus.Publish(new GameSavedEvent(saveName, isAutoSave, isQuickSave));

                return true;
            }
            catch (System.Exception ex)
            {
                return false;
            }
        }

        public bool LoadGame(string saveName)
        {
            try
            {
                Log($"Loading game: {saveName}");

                string filePath = settings.GetSaveFilePath(saveName);
                if (!File.Exists(filePath))
                {
                    return false;
                }

                // Read file
                string json = File.ReadAllText(filePath);

                // Decrypt/decompress if needed
                if (settings.useEncryption)
                {
                    json = DecryptString(json);
                }
                if (settings.useCompression)
                {
                    json = DecompressString(json);
                }

                // Deserialize
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

                // Clear current game state
                ClearCurrentGame();

                // Restore game state
                RestoreGameState(saveData);

                Log($" Game loaded successfully: {saveName}");

                // Publish load event
                EventBus.Publish(new GameLoadedEvent(saveName));

                return true;
            }
            catch (System.Exception ex)
            {
                return false;
            }
        }

        public bool QuickSave()
        {
            return SaveGame(settings.quickSaveSlotName, false, true);
        }

        public bool QuickLoad()
        {
            if (!SaveExists(settings.quickSaveSlotName))
            {
                return false;
            }
            return LoadGame(settings.quickSaveSlotName);
        }

        public bool DeleteSave(string saveName)
        {
            try
            {
                string filePath = settings.GetSaveFilePath(saveName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Log($"Deleted save: {saveName}");
                    return true;
                }
                return false;
            }
            catch (System.Exception ex)
            {
                return false;
            }
        }

        public string[] GetAllSaves()
        {
            string savePath = settings.GetSaveDirectoryPath();
            if (!Directory.Exists(savePath))
            {
                return new string[0];
            }

            string[] files = Directory.GetFiles(savePath, "*" + settings.saveFileExtension);
            return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();
        }

        public SaveFileInfo GetSaveInfo(string saveName)
        {
            try
            {
                string filePath = settings.GetSaveFilePath(saveName);
                if (!File.Exists(filePath))
                {
                    return null;
                }

                FileInfo fileInfo = new FileInfo(filePath);

                // Try to read save metadata without full deserialization
                string json = File.ReadAllText(filePath);

                if (settings.useEncryption)
                    json = DecryptString(json);
                if (settings.useCompression)
                    json = DecompressString(json);

                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

                return new SaveFileInfo
                {
                    fileName = saveName + settings.saveFileExtension,
                    saveName = saveData.saveName,
                    saveDate = saveData.saveDate,
                    playTime = saveData.playTime,
                    gameVersion = saveData.gameVersion,
                    fileSize = fileInfo.Length,
                    isAutoSave = saveName.StartsWith("AutoSave"),
                    isQuickSave = saveName == settings.quickSaveSlotName
                };
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }

        public bool SaveExists(string saveName)
        {
            string filePath = settings.GetSaveFilePath(saveName);
            return File.Exists(filePath);
        }

        #endregion

        #region Game State Collection

        private GameSaveData CollectGameState()
        {
            GameSaveData data = new GameSaveData();

            // Core game state
            data.gameState = CollectGameStateData();
            data.resources = CollectResourcesData();
            data.happiness = CollectHappinessData();
            data.time = CollectTimeData();

            // Optional systems
            data.population = CollectPopulationData();
            data.reputation = CollectReputationData();

            // Entities
            data.buildings = CollectBuildingsData();
            data.units = CollectUnitsData();

            // World state
            data.fogOfWar = CollectFogOfWarData();
            data.cameraState = CollectCameraData();

            return data;
        }

        private GameStateData CollectGameStateData()
        {
            var gameStateService = ServiceLocator.TryGet<IGameStateService>();
            return new GameStateData
            {
                currentState = (int)(gameStateService?.CurrentState ?? GameState.Playing),
                isPaused = gameStateService?.IsPaused ?? false,
                timeScale = Time.timeScale
            };
        }

        private ResourcesData CollectResourcesData()
        {
            var resourceService = ServiceLocator.TryGet<IResourcesService>();
            return new ResourcesData(resourceService);
        }

        private HappinessData CollectHappinessData()
        {
            var happinessService = ServiceLocator.TryGet<IHappinessService>();
            if (happinessService == null)
                return new HappinessData();

            return new HappinessData
            {
                currentHappiness = happinessService.CurrentHappiness,
                taxLevel = happinessService.TaxLevel,
                buildingsHappinessBonus = 0f // This is internal to HappinessManager
            };
        }

        private TimeData CollectTimeData()
        {
            var timeService = ServiceLocator.TryGet<ITimeService>();
            if (timeService == null)
                return new TimeData { timeScale = Time.timeScale };

            return new TimeData
            {
                currentTime = timeService.CurrentTime,
                currentDay = timeService.CurrentDay,
                dayProgress = timeService.DayProgress,
                timeScale = Time.timeScale
            };
        }

        private PopulationData CollectPopulationData()
        {
            var populationService = ServiceLocator.TryGet<IPopulationService>();
            if (populationService == null)
                return null;

            return new PopulationData
            {
                totalPopulation = populationService.TotalPopulation,
                availablePeasants = populationService.AvailablePeasants,
                assignedPeasants = populationService.AssignedPeasants,
                housingCapacity = populationService.HousingCapacity
            };
        }

        private ReputationData CollectReputationData()
        {
            var reputationService = ServiceLocator.TryGet<IReputationService>();
            if (reputationService == null)
                return null;

            return new ReputationData
            {
                currentReputation = reputationService.CurrentReputation
            };
        }

        private List<BuildingSaveData> CollectBuildingsData()
        {
            List<BuildingSaveData> buildingsData = new List<BuildingSaveData>();

            // Find all buildings in the scene
            Building[] buildings = FindObjectsByType<Building>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var building in buildings)
            {
                try
                {
                    BuildingSaveData buildingData = new BuildingSaveData
                    {
                        instanceID = building.gameObject.GetInstanceID(),
                        buildingDataName = building.Data != null ? building.Data.name : "",
                        prefabPath = GetPrefabPath(building.Data?.buildingPrefab),

                        position = building.transform.position,
                        rotation = building.transform.rotation,
                        scale = building.transform.localScale,

                        isConstructed = building.IsConstructed,
                        constructionProgress = building.ConstructionProgress,
                        requiresConstruction = true, // Can be made configurable

                        layer = building.gameObject.layer,
                        tag = building.gameObject.tag,

                        // Determine team/ownership based on layer
                        isPlayerOwned = building.gameObject.layer != LayerMask.NameToLayer("Enemy"),
                        teamID = building.gameObject.layer == LayerMask.NameToLayer("Enemy") ? 1 : 0
                    };

                    buildingsData.Add(buildingData);
                }
                catch (System.Exception ex)
                {
                }
            }

            Log($"Collected {buildingsData.Count} buildings");
            return buildingsData;
        }

        private List<UnitSaveData> CollectUnitsData()
        {
            List<UnitSaveData> unitsData = new List<UnitSaveData>();

            // Find all units in the scene
            UnitAIController[] units = FindObjectsByType<UnitAIController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var unit in units)
            {
                try
                {
                    var health = unit.Health;
                    var movement = unit.Movement;
                    var combat = unit.Combat;

                    UnitSaveData unitData = new UnitSaveData
                    {
                        instanceID = unit.gameObject.GetInstanceID(),
                        unitConfigName = unit.Config != null ? unit.Config.name : "",
                        prefabPath = "", // Would need to store prefab reference in UnitConfig

                        position = unit.transform.position,
                        rotation = unit.transform.rotation,
                        scale = unit.transform.localScale,

                        // Health
                        currentHealth = health != null ? health.CurrentHealth : 100f,
                        maxHealth = health != null ? health.MaxHealth : 100f,
                        isDead = health != null && health.IsDead,

                        // Movement
                        hasDestination = movement != null && movement.IsMoving,
                        currentDestination = Vector3.zero, // Movement doesn't expose current destination
                        moveSpeed = movement != null ? movement.Speed : 0f,
                        isMoving = movement != null && movement.IsMoving,

                        // Combat
                        currentTargetID = combat != null && combat.CurrentTarget != null
                            ? combat.CurrentTarget.gameObject.GetInstanceID()
                            : -1,
                        attackDamage = combat != null ? combat.AttackDamage : 0f,
                        attackRange = combat != null ? combat.AttackRange : 0f,
                        attackRate = combat != null ? combat.AttackRate : 0f,

                        // AI State
                        aiState = (int)unit.CurrentStateType,
                        behaviorType = 0, // Would need to expose from UnitAIController
                        aggroOriginPosition = unit.AggroOriginPosition.HasValue
                            ? new Vector3Serializable(unit.AggroOriginPosition.Value)
                            : (Vector3Serializable?)null,
                        isOnForcedMove = unit.IsOnForcedMove,
                        forcedMoveDestination = unit.ForcedMoveDestination.HasValue
                            ? new Vector3Serializable(unit.ForcedMoveDestination.Value)
                            : (Vector3Serializable?)null,

                        layer = unit.gameObject.layer,
                        tag = unit.gameObject.tag,

                        // Determine team/ownership
                        isPlayerOwned = unit.gameObject.layer != LayerMask.NameToLayer("Enemy"),
                        teamID = unit.gameObject.layer == LayerMask.NameToLayer("Enemy") ? 1 : 0
                    };

                    unitsData.Add(unitData);
                }
                catch (System.Exception ex)
                {
                }
            }

            Log($"Collected {unitsData.Count} units");
            return unitsData;
        }

        private FogOfWarData CollectFogOfWarData()
        {
            // Find FogOfWarManager in scene
            var fowManager = FindAnyObjectByType<csFogWar>();
            if (fowManager == null)
            {
                Log("No FogOfWarManager found in scene");
                return new FogOfWarData();
            }

            // Access the grid through reflection or make it public
            // For now, return empty data - will need FogOfWarManager modifications
            Log("Fog of War data collection requires FogOfWarManager modifications");
            return new FogOfWarData();
        }

        private CameraData CollectCameraData()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return new CameraData();

            return new CameraData
            {
                position = mainCamera.transform.position,
                rotation = mainCamera.transform.rotation,
                fieldOfView = mainCamera.fieldOfView,
                orthographicSize = mainCamera.orthographicSize
            };
        }

        #endregion

        #region Game State Restoration

        private void ClearCurrentGame()
        {
            Log("Clearing current game state...");

            // Destroy all buildings
            Building[] buildings = FindObjectsByType<Building>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var building in buildings)
            {
                Destroy(building.gameObject);
            }

            // Destroy all units
            UnitAIController[] units = FindObjectsByType<UnitAIController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var unit in units)
            {
                Destroy(unit.gameObject);
            }

            // Clear entity map
            loadedEntitiesMap.Clear();

            Log("Game state cleared");
        }

        private void RestoreGameState(GameSaveData saveData)
        {
            Log("Restoring game state...");

            // Restore core systems
            RestoreGameStateData(saveData.gameState);
            RestoreResourcesData(saveData.resources);
            RestoreHappinessData(saveData.happiness);
            RestoreTimeData(saveData.time);

            // Restore optional systems
            if (saveData.population != null)
                RestorePopulationData(saveData.population);
            if (saveData.reputation != null)
                RestoreReputationData(saveData.reputation);

            // Restore entities
            RestoreBuildingsData(saveData.buildings);
            RestoreUnitsData(saveData.units);

            // Restore world state
            if (saveData.fogOfWar != null)
                RestoreFogOfWarData(saveData.fogOfWar);
            if (saveData.cameraState != null)
                RestoreCameraData(saveData.cameraState);

            // Update playtime
            playTime = saveData.playTime;

            Log("Game state restored");
        }

        private void RestoreGameStateData(GameStateData data)
        {
            if (data == null) return;

            var gameStateService = ServiceLocator.TryGet<IGameStateService>();
            if (gameStateService != null)
            {
                gameStateService.ChangeState((GameState)data.currentState);
                Time.timeScale = data.timeScale;
            }
        }

        private void RestoreResourcesData(ResourcesData data)
        {
            if (data == null) return;

            var resourceService = ServiceLocator.TryGet<IResourcesService>();
            if (resourceService != null)
            {
                // Clear current resources and set new values
                var currentResources = new Dictionary<ResourceType, int>();
                foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
                {
                    currentResources[type] = -resourceService.GetResource(type);
                }
                resourceService.AddResources(currentResources);

                // Add saved resources
                resourceService.AddResources(data.ToDictionary());
            }
        }

        private void RestoreHappinessData(HappinessData data)
        {
            if (data == null) return;

            var happinessService = ServiceLocator.TryGet<IHappinessService>();
            if (happinessService != null)
            {
                happinessService.TaxLevel = data.taxLevel;
                // Current happiness will be recalculated
            }
        }

        private void RestoreTimeData(TimeData data)
        {
            if (data == null) return;

            var timeService = ServiceLocator.TryGet<ITimeService>();
            if (timeService != null)
            {
                // Would need methods to set time in ITimeService
                timeService.SetTimeScale(data.timeScale);
            }
        }

        private void RestorePopulationData(PopulationData data)
        {
            // Population service would need restore methods
            Log("Population data restoration not fully implemented");
        }

        private void RestoreReputationData(ReputationData data)
        {
            var reputationService = ServiceLocator.TryGet<IReputationService>();
            if (reputationService != null && data != null)
            {
                // Would need a setter method in IReputationService
            }
        }

        private void RestoreBuildingsData(List<BuildingSaveData> buildingsData)
        {
            if (buildingsData == null || buildingsData.Count == 0)
                return;

            Log($"Restoring {buildingsData.Count} buildings...");

            int restoredCount = 0;

            foreach (var buildingData in buildingsData)
            {
                try
                {
                    // Find the BuildingDataSO by name using multiple strategies
                    BuildingDataSO data = FindBuildingDataSO(buildingData.buildingDataName);
                    if (data == null || data.buildingPrefab == null)
                    {
                        continue;
                    }

                    // Instantiate building
                    GameObject buildingObj = Instantiate(data.buildingPrefab, buildingData.position.ToVector3(), buildingData.rotation.ToQuaternion());
                    buildingObj.transform.localScale = buildingData.scale.ToVector3();
                    buildingObj.layer = buildingData.layer;
                    buildingObj.tag = buildingData.tag;

                    // Configure building component
                    if (buildingObj.TryGetComponent<Building>(out var building))
                    {
                        building.SetData(data);
                        if (buildingData.isConstructed)
                        {
                            building.InstantComplete();
                        }
                    }

                    // Store in entity map for cross-referencing
                    loadedEntitiesMap[buildingData.instanceID] = buildingObj;
                    restoredCount++;
                    Log($"  ✓ Restored building: {buildingData.buildingDataName} at {buildingData.position.ToVector3()}");
                }
                catch (System.Exception ex)
                {
                }
            }

            Log($"Restored {restoredCount}/{buildingsData.Count} buildings");
        }

        private void RestoreUnitsData(List<UnitSaveData> unitsData)
        {
            if (unitsData == null || unitsData.Count == 0)
                return;

            Log($"Restoring {unitsData.Count} units...");

            int restoredCount = 0;

            foreach (var unitData in unitsData)
            {
                try
                {
                    // Find the UnitConfigSO by name using multiple strategies
                    UnitConfigSO config = FindUnitConfigSO(unitData.unitConfigName);
                    if (config == null || config.unitPrefab == null)
                    {
                        continue;
                    }

                    // Instantiate unit
                    GameObject unitObj = Instantiate(config.unitPrefab, unitData.position.ToVector3(), unitData.rotation.ToQuaternion());
                    unitObj.transform.localScale = unitData.scale.ToVector3();
                    unitObj.layer = unitData.layer;
                    unitObj.tag = unitData.tag;

                    // Publish spawn event for UnitSelectionManager cache
                    EventBus.Publish(new UnitSpawnedEvent(unitObj, unitObj.transform.position));

                    // Restore health
                    if (unitObj.TryGetComponent<UnitHealth>(out var health))
                    {
                        health.SetMaxHealth(unitData.maxHealth);
                        health.SetHealth(unitData.currentHealth);
                    }

                    // Restore movement
                    if (unitObj.TryGetComponent<UnitMovement>(out var movement))
                    {
                        movement.SetSpeed(unitData.moveSpeed);
                    }

                    // Restore combat
                    if (unitObj.TryGetComponent<UnitCombat>(out var combat))
                    {
                        combat.SetAttackDamage(unitData.attackDamage);
                        combat.SetAttackRange(unitData.attackRange);
                        combat.SetAttackRate(unitData.attackRate);
                    }

                    // Store in entity map
                    loadedEntitiesMap[unitData.instanceID] = unitObj;
                    restoredCount++;
                    Log($"  ✓ Restored unit: {unitData.unitConfigName} at {unitData.position.ToVector3()}");
                }
                catch (System.Exception ex)
                {
                }
            }

            // Second pass: restore targets and AI state (after all units are loaded)
            RestoreUnitReferences(unitsData);

            Log($"Restored {restoredCount}/{unitsData.Count} units");
        }

        private void RestoreUnitReferences(List<UnitSaveData> unitsData)
        {
            foreach (var unitData in unitsData)
            {
                if (!loadedEntitiesMap.ContainsKey(unitData.instanceID))
                    continue;

                GameObject unitObj = loadedEntitiesMap[unitData.instanceID];
                if (unitObj.TryGetComponent<UnitCombat>(out var combat))
                {
                }
                var aiController = unitObj.GetComponent<UnitAIController>();

                // Restore combat target
                if (combat != null && unitData.currentTargetID != -1 && loadedEntitiesMap.ContainsKey(unitData.currentTargetID))
                {
                    combat.SetTarget(loadedEntitiesMap[unitData.currentTargetID].transform);
                }

                // Restore AI state
                if (aiController != null)
                {
                    // Would need methods to restore AI state
                    if (unitData.aggroOriginPosition.HasValue)
                    {
                        // Set aggro origin
                    }
                    if (unitData.isOnForcedMove && unitData.forcedMoveDestination.HasValue)
                    {
                        aiController.SetForcedMove(true, unitData.forcedMoveDestination.Value.ToVector3());
                    }
                }
            }
        }

        private void RestoreFogOfWarData(FogOfWarData data)
        {
            // Would require FogOfWarManager modifications
            Log("Fog of War restoration not fully implemented");
        }

        private void RestoreCameraData(CameraData data)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera != null && data != null)
            {
                mainCamera.transform.position = data.position.ToVector3();
                mainCamera.transform.rotation = data.rotation.ToQuaternion();
                mainCamera.fieldOfView = data.fieldOfView;
                mainCamera.orthographicSize = data.orthographicSize;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Find BuildingDataSO using multiple fallback strategies.
        /// </summary>
        private BuildingDataSO FindBuildingDataSO(string buildingDataName)
        {
            if (string.IsNullOrEmpty(buildingDataName))
            {
                return null;
            }

            Log($"Searching for BuildingDataSO: {buildingDataName}");

            // Strategy 1: Try Resources.LoadAll (only works if assets are in Resources folder)
            BuildingDataSO[] allBuildingsInResources = Resources.LoadAll<BuildingDataSO>("");
            Log($"  Strategy 1 (Resources): Found {allBuildingsInResources.Length} BuildingDataSO assets");
            if (allBuildingsInResources.Length > 0)
            {
                foreach (var b in allBuildingsInResources)
                {
                    Log($"    - {b.name}");
                }
            }

            BuildingDataSO result = allBuildingsInResources.FirstOrDefault(b => b.name == buildingDataName);
            if (result != null)
            {
                Log($"  ✓ Found via Resources: {result.name}");
                return result;
            }

            // Strategy 2: Search in known Resources subfolders
            string[] resourcePaths = new string[]
            {
                "Buildings",
                "ScriptableObjects/Buildings",
                "Data/Buildings",
                "BuildingData",
                "SO/Buildings"
            };

            foreach (var path in resourcePaths)
            {
                BuildingDataSO[] assets = Resources.LoadAll<BuildingDataSO>(path);
                Log($"  Strategy 2 (Resources/{path}): Found {assets.Length} assets");
                result = assets.FirstOrDefault(b => b.name == buildingDataName);
                if (result != null)
                {
                    Log($"  ✓ Found in Resources/{path}: {result.name}");
                    return result;
                }
            }

            // Strategy 3: Find existing Building in scene and get its Data
            Building[] existingBuildings = FindObjectsByType<Building>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Log($"  Strategy 3 (Scene Search): Found {existingBuildings.Length} existing buildings");
            foreach (var building in existingBuildings)
            {
                if (building.Data != null && building.Data.name == buildingDataName)
                {
                    Log($"  ✓ Found via existing building in scene: {building.Data.name}");
                    return building.Data;
                }
            }

            // Strategy 4: Try Object.FindObjectsByType to find any BuildingDataSO instances
            BuildingDataSO[] allLoadedAssets = FindObjectsByType<BuildingDataSO>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Log($"  Strategy 4 (FindObjectsByType): Found {allLoadedAssets.Length} loaded assets");
            result = allLoadedAssets.FirstOrDefault(b => b.name == buildingDataName);
            if (result != null)
            {
                Log($"  ✓ Found via FindObjectsByType: {result.name}");
                return result;
            }

            return null;
        }

        /// <summary>
        /// Find UnitConfigSO using multiple fallback strategies.
        /// </summary>
        private UnitConfigSO FindUnitConfigSO(string unitConfigName)
        {
            if (string.IsNullOrEmpty(unitConfigName))
            {
                return null;
            }

            Log($"Searching for UnitConfigSO: {unitConfigName}");

            // Strategy 1: Try Resources.LoadAll (only works if assets are in Resources folder)
            UnitConfigSO[] allUnitsInResources = Resources.LoadAll<UnitConfigSO>("");
            Log($"  Strategy 1 (Resources): Found {allUnitsInResources.Length} UnitConfigSO assets");
            if (allUnitsInResources.Length > 0)
            {
                foreach (var u in allUnitsInResources)
                {
                    Log($"    - {u.name}");
                }
            }

            UnitConfigSO result = allUnitsInResources.FirstOrDefault(u => u.name == unitConfigName);
            if (result != null)
            {
                Log($"  ✓ Found via Resources: {result.name}");
                return result;
            }

            // Strategy 2: Search in known Resources subfolders
            string[] resourcePaths = new string[]
            {
                "Units",
                "ScriptableObjects/Units",
                "Data/Units",
                "UnitConfigs",
                "SO/Units"
            };

            foreach (var path in resourcePaths)
            {
                UnitConfigSO[] assets = Resources.LoadAll<UnitConfigSO>(path);
                Log($"  Strategy 2 (Resources/{path}): Found {assets.Length} assets");
                result = assets.FirstOrDefault(u => u.name == unitConfigName);
                if (result != null)
                {
                    Log($"  ✓ Found in Resources/{path}: {result.name}");
                    return result;
                }
            }

            // Strategy 3: Find existing Unit in scene and get its Config
            UnitAIController[] existingUnits = FindObjectsByType<UnitAIController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Log($"  Strategy 3 (Scene Search): Found {existingUnits.Length} existing units");
            foreach (var unit in existingUnits)
            {
                if (unit.Config != null && unit.Config.name == unitConfigName)
                {
                    Log($"  ✓ Found via existing unit in scene: {unit.Config.name}");
                    return unit.Config;
                }
            }

            // Strategy 4: Try Object.FindObjectsByType to find any UnitConfigSO instances
            UnitConfigSO[] allLoadedAssets = FindObjectsByType<UnitConfigSO>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Log($"  Strategy 4 (FindObjectsByType): Found {allLoadedAssets.Length} loaded assets");
            result = allLoadedAssets.FirstOrDefault(u => u.name == unitConfigName);
            if (result != null)
            {
                Log($"  ✓ Found via FindObjectsByType: {result.name}");
                return result;
            }

            return null;
        }

        private string GetPrefabPath(GameObject prefab)
        {
            if (prefab == null) return "";
            // In a real implementation, would use AssetDatabase in editor
            // For runtime, prefabs should be in Resources folder
            return prefab.name;
        }

        private string CompressString(string str)
        {
            // Simple compression (could use GZip in real implementation)
            return str;
        }

        private string DecompressString(string str)
        {
            return str;
        }

        private string EncryptString(string str)
        {
            // Simple XOR encryption (not secure, just obfuscation)
            return str;
        }

        private string DecryptString(string str)
        {
            return str;
        }

        private void Log(string message)
        {
            if (settings.enableDebugLogging)
            {
            }
        }

        #endregion
    }

    #region Events

    public struct GameSavedEvent
    {
        public string SaveName;
        public bool IsAutoSave;
        public bool IsQuickSave;

        public GameSavedEvent(string saveName, bool isAutoSave, bool isQuickSave)
        {
            SaveName = saveName;
            IsAutoSave = isAutoSave;
            IsQuickSave = isQuickSave;
        }
    }

    public struct GameLoadedEvent
    {
        public string SaveName;

        public GameLoadedEvent(string saveName)
        {
            SaveName = saveName;
        }
    }

    #endregion
}
