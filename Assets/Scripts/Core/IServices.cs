using System.Collections.Generic;
using UnityEngine;
using RTSGame.Settings;
using AudioSettings = RTSGame.Settings.AudioSettings;

namespace RTS.Core.Services
{
    /// <summary>
    /// Interface for resource management system.
    /// </summary>
    public enum ResourceType
    {
        Wood,
        Food,
        Gold,
        Stone,
        // Iron,      // Future resource - just uncomment!
        // Mana,      // Future resource - just uncomment!
        // Population // Future resource - just uncomment!
    }

    /// <summary>
    /// Data-driven resource manager - adding new resources is now trivial!
    /// </summary>
    public interface IResourcesService
    {
        int GetResource(ResourceType type);
        bool CanAfford(Dictionary<ResourceType, int> costs);
        bool SpendResources(Dictionary<ResourceType, int> costs);
        void AddResources(Dictionary<ResourceType, int> amounts);

        // Legacy compatibility methods (optional)
        int Wood { get; }
        int Food { get; }
        int Gold { get; }
        int Stone { get; }
    }

    /// <summary>
    /// Helper class for building resource dictionaries easily.
    /// Usage: ResourceCost.Build().Wood(100).Stone(50).Create()
    /// </summary>
    public class ResourceCost
    {
        private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();

        public static ResourceCost Build() => new ResourceCost();

        public ResourceCost Wood(int amount) { resources[ResourceType.Wood] = amount; return this; }
        public ResourceCost Food(int amount) { resources[ResourceType.Food] = amount; return this; }
        public ResourceCost Gold(int amount) { resources[ResourceType.Gold] = amount; return this; }
        public ResourceCost Stone(int amount) { resources[ResourceType.Stone] = amount; return this; }

        public ResourceCost Add(ResourceType type, int amount)
        {
            resources[type] = amount;
            return this;
        }

        public Dictionary<ResourceType, int> Create() => new Dictionary<ResourceType, int>(resources);
    }









    /// <summary>
    /// Interface for happiness/morale system.
    /// </summary>
    public interface IHappinessService
    {
        float CurrentHappiness { get; }
        float TaxLevel { get; set; }

        void AddBuildingBonus(float bonus, string buildingName);
        void RemoveBuildingBonus(float bonus, string buildingName);
    }

    /// <summary>
    /// Interface for object pooling system.
    /// </summary>
    public interface IPoolService
    {
        T Get<T>(T prefab) where T : UnityEngine.Component;
        void Return<T>(T instance) where T : UnityEngine.Component;
        void Warmup<T>(T prefab, int count) where T : UnityEngine.Component;
        void Clear();
    }

    /// <summary>
    /// Interface for time/day-night cycle management.
    /// </summary>
    public interface ITimeService
    {
        float CurrentTime { get; }
        float DayProgress { get; } // 0-1
        int CurrentDay { get; }
        void SetTimeScale(float scale);
    }

    /// <summary>
    /// Interface for building management system.
    /// </summary>
    public interface IBuildingService
    {
        void StartPlacingBuilding(int buildingIndex);
        void CancelPlacement();
        bool IsPlacing { get; }
    }

    /// <summary>
    /// Interface for game state management.
    /// </summary>
    public interface IGameStateService
    {
        GameState CurrentState { get; }
        void ChangeState(GameState newState);
        void PauseGame();
        void ResumeGame();
        bool IsPaused { get; }
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    /// <summary>
    /// Interface for population and peasant management.
    /// </summary>
    public interface IPopulationService
    {
        int TotalPopulation { get; }
        int AvailablePeasants { get; }
        int AssignedPeasants { get; }
        int HousingCapacity { get; }

        void AddPopulation(int amount);
        void RemovePopulation(int amount);
        void UpdateHousingCapacity(int capacity);
        bool TryAssignPeasants(int amount, string workType, GameObject assignedTo);
        void ReleasePeasants(int amount, string workType, GameObject releasedFrom);
    }

    /// <summary>
    /// Interface for reputation/fame management.
    /// </summary>
    public interface IReputationService
    {
        float CurrentReputation { get; }
        void ModifyReputation(float amount, string reason);
    }

    /// <summary>
    /// Interface for peasant workforce allocation (modular).
    /// Allows different systems to request peasant workers.
    /// </summary>
    public interface IPeasantWorkforceService
    {
        bool RequestWorkers(string workType, int amount, GameObject requester);
        void ReleaseWorkers(string workType, int amount, GameObject requester);
        int GetAssignedWorkers(GameObject requester);
        bool CanAssignWorkers(int amount);
    }

    /// <summary>
    /// Interface for save/load system.
    /// Handles game state persistence and restoration.
    /// </summary>
    public interface ISaveLoadService
    {
        /// <summary>
        /// Save the current game state to a file.
        /// </summary>
        /// <param name="saveName">Name of the save file</param>
        /// <param name="isAutoSave">Whether this is an auto-save</param>
        /// <param name="isQuickSave">Whether this is a quick-save</param>
        /// <returns>True if save was successful</returns>
        bool SaveGame(string saveName, bool isAutoSave = false, bool isQuickSave = false);

        /// <summary>
        /// Load a game state from a file.
        /// </summary>
        /// <param name="saveName">Name of the save file to load</param>
        /// <returns>True if load was successful</returns>
        bool LoadGame(string saveName);

        /// <summary>
        /// Quick save to a dedicated slot (F5).
        /// </summary>
        bool QuickSave();

        /// <summary>
        /// Quick load from the quick save slot.
        /// </summary>
        bool QuickLoad();

        /// <summary>
        /// Delete a save file.
        /// </summary>
        bool DeleteSave(string saveName);

        /// <summary>
        /// Get all available save files.
        /// </summary>
        string[] GetAllSaves();

        /// <summary>
        /// Get save file info without loading it.
        /// </summary>
        SaveFileInfo GetSaveInfo(string saveName);

        /// <summary>
        /// Check if a save file exists.
        /// </summary>
        bool SaveExists(string saveName);
    }

    /// <summary>
    /// Information about a save file.
    /// </summary>
    [System.Serializable]
    public class SaveFileInfo
    {
        public string fileName;
        public string saveName;
        public string saveDate;
        public float playTime;
        public string gameVersion;
        public long fileSize;
        public bool isAutoSave;
        public bool isQuickSave;
    }

    /// <summary>
    /// Interface for floating numbers and HP bars system.
    /// Displays damage, healing, resources, and health bars above game entities.
    /// </summary>
    public interface IFloatingNumberService
    {
        /// <summary>
        /// Show a damage number at the specified world position.
        /// </summary>
        void ShowDamageNumber(Vector3 worldPosition, float damageAmount, bool isCritical = false);

        /// <summary>
        /// Show a healing number at the specified world position.
        /// </summary>
        void ShowHealNumber(Vector3 worldPosition, float healAmount);

        /// <summary>
        /// Show a resource gain number at the specified world position.
        /// </summary>
        void ShowResourceNumber(Vector3 worldPosition, ResourceType resourceType, int amount);

        /// <summary>
        /// Show a repair number at the specified world position.
        /// </summary>
        void ShowRepairNumber(Vector3 worldPosition, float repairAmount);

        /// <summary>
        /// Show an experience gain number (for future XP system).
        /// </summary>
        void ShowExperienceNumber(Vector3 worldPosition, int xpAmount);

        /// <summary>
        /// Register a GameObject to have an HP bar displayed above it.
        /// </summary>
        /// <param name="target">The GameObject to track</param>
        /// <param name="getCurrentHealth">Function to get current health</param>
        /// <param name="getMaxHealth">Function to get max health</param>
        void RegisterHPBar(GameObject target, System.Func<float> getCurrentHealth, System.Func<float> getMaxHealth);

        /// <summary>
        /// Unregister a GameObject's HP bar.
        /// </summary>
        void UnregisterHPBar(GameObject target);

        /// <summary>
        /// Get the current settings configuration.
        /// </summary>
        KAD.UI.FloatingNumbers.FloatingNumbersSettings Settings { get; }

        /// <summary>
        /// Refresh settings (call after modifying settings).
        /// </summary>
        void RefreshSettings();

        /// <summary>
        /// Show blood gush particle effect.
        /// </summary>
        void ShowBloodGush(Vector3 worldPosition, Vector3 direction, int particleCount = -1);

        /// <summary>
        /// Show blood decal on ground.
        /// </summary>
        void ShowBloodDecal(Vector3 worldPosition);

        /// <summary>
        /// Start blood dripping for a wounded unit.
        /// </summary>
        void StartBloodDripping(GameObject target, System.Func<float> getCurrentHealth, System.Func<float> getMaxHealth);

        /// <summary>
        /// Stop blood dripping for a unit.
        /// </summary>
        void StopBloodDripping(GameObject target);
    }

    /// <summary>
    /// Interface for audio management system.
    /// Manages all audio channels (Master, Music, SFX, UI, Voice) and audio devices.
    /// </summary>
    public interface IAudioService
    {
        // Volume Control
        float MasterVolume { get; set; }
        float MusicVolume { get; set; }
        float SFXVolume { get; set; }
        float UIVolume { get; set; }
        float VoiceVolume { get; set; }

        // Audio Settings
        float SpatialBlend { get; set; }
        DynamicRange DynamicRange { get; set; }
        BattleSFXIntensity BattleSFXIntensity { get; set; }
        UnitVoiceStyle UnitVoiceStyle { get; set; }
        bool AlertNotifications { get; set; }

        // Audio Device Management
        string CurrentAudioDevice { get; }
        string[] GetAvailableAudioDevices();
        void SetAudioDevice(string deviceName);

        // Playback Control
        void PlayMusic(string musicName);
        void StopMusic();
        void PlaySFX(string sfxName, Vector3 position = default);
        void PlayUISFX(string sfxName);
        void PlayVoice(string voiceName);

        // Utility
        void ApplySettings(AudioSettings settings);
        void RefreshAudioSources();
    }

    /// <summary>
    /// Interface for game settings management system.
    /// Manages all game settings including graphics, audio, gameplay, controls, etc.
    /// </summary>
    public interface ISettingsService
    {
        // Settings Access
        GameSettings CurrentSettings { get; }
        GeneralSettings General { get; }
        GraphicsSettings Graphics { get; }
        AudioSettings Audio { get; }
        GameplaySettings Gameplay { get; }
        ControlSettings Controls { get; }
        UISettings UI { get; }
        AccessibilitySettings Accessibility { get; }
        NetworkSettings Network { get; }
        SystemSettings System { get; }

        // Settings Management
        void LoadSettings();
        void SaveSettings();
        void ResetToDefaults();
        void ApplySettings();
        void ApplyGraphicsSettings();
        void ApplyAudioSettings();
        void ApplyGameplaySettings();
        void ApplyControlSettings();
        void ApplyUISettings();

        // Quality Presets
        void ApplyQualityPreset(QualityPreset preset);

        // Events
        event System.Action OnSettingsChanged;
        event System.Action<QualityPreset> OnQualityPresetChanged;
    }

    /// <summary>
    /// Interface for animal spawning system.
    /// Manages wildlife spawning across different biomes.
    /// </summary>
    public interface IAnimalSpawnerService
    {
        void StartSpawning();
        void StopSpawning();
        void SpawnAnimal(RTS.Animals.AnimalConfigSO config, Vector3 position);
        int GetAnimalCount();
        int GetAnimalCount(RTS.Animals.AnimalType type);
    }

}