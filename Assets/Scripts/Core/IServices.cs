using System.Collections.Generic;

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
    public interface IResourceService
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

}