using System.Collections.Generic;
using UnityEngine;

namespace TopDownWallBuilding.Core.Services
{
    /// <summary>
    /// Resource types supported by the wall building system.
    /// </summary>
    public enum ResourceType
    {
        Wood,
        Food,
        Gold,
        Stone
    }

    /// <summary>
    /// Interface for resource management system.
    /// Implement this in your game to provide resource functionality to the wall system.
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
}
