using UnityEngine;
using RTS.Core.Events;
using System.Collections.Generic;
using System.Linq;

namespace RTS.Core.Services
{
    /// <summary>
    /// DATA-DRIVEN ResourceManager - adding new resources is now trivial!
    /// Just add to ResourceType enum and set starting value.
    /// </summary>
    public class ResourceManager : MonoBehaviour, IResourcesService
    {
        [Header("Starting Resources")]
        [SerializeField] private int startingWood = 100;
        [SerializeField] private int startingFood = 100;
        [SerializeField] private int startingGold = 50;
        [SerializeField] private int startingStone = 50;
        // Add new starting resources here as needed

        // Dynamic storage - scales automatically!
        private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();

        // Legacy property accessors for backwards compatibility
        public int Wood => GetResource(ResourceType.Wood);
        public int Food => GetResource(ResourceType.Food);
        public int Gold => GetResource(ResourceType.Gold);
        public int Stone => GetResource(ResourceType.Stone);

        private void Awake()
        {
            InitializeResources();
        }

        private void InitializeResources()
        {
            // Initialize all resource types from enum
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                resources[type] = GetStartingAmount(type);
            }

            // Publish initial state
            PublishResourcesChanged(new Dictionary<ResourceType, int>());
        }

        private int GetStartingAmount(ResourceType type)
        {
            return type switch
            {
                ResourceType.Wood => startingWood,
                ResourceType.Food => startingFood,
                ResourceType.Gold => startingGold,
                ResourceType.Stone => startingStone,
                _ => 0
            };
        }

        #region IResourceService Implementation

        public int GetResource(ResourceType type)
        {
            return resources.TryGetValue(type, out int amount) ? amount : 0;
        }

        public bool CanAfford(Dictionary<ResourceType, int> costs)
        {
            if (costs == null) return true;

            foreach (var cost in costs)
            {
                if (GetResource(cost.Key) < cost.Value)
                    return false;
            }
            return true;
        }

        public bool SpendResources(Dictionary<ResourceType, int> costs)
        {
            if (!CanAfford(costs))
            {
                LogInsufficientResources(costs);
                PublishSpendEvent(costs, false);
                return false;
            }

            // Spend the resources
            Dictionary<ResourceType, int> deltas = new Dictionary<ResourceType, int>();
            foreach (var cost in costs)
            {
                resources[cost.Key] -= cost.Value;
                deltas[cost.Key] = -cost.Value;
            }

            PublishResourcesChanged(deltas);
            PublishSpendEvent(costs, true);
            return true;
        }

        public void AddResources(Dictionary<ResourceType, int> amounts)
        {
            if (amounts == null) return;

            Dictionary<ResourceType, int> deltas = new Dictionary<ResourceType, int>();

            foreach (var amount in amounts)
            {
                resources[amount.Key] = Mathf.Max(0, resources[amount.Key] + amount.Value);
                deltas[amount.Key] = amount.Value;
            }

            PublishResourcesChanged(deltas);
        }

        #endregion

        #region Event Publishing

        private void PublishResourcesChanged(Dictionary<ResourceType, int> deltas)
        {
            // Create event with deltas (0 if not in dictionary)
            EventBus.Publish(new ResourcesChangedEvent(
                deltas.GetValueOrDefault(ResourceType.Wood, 0),
                deltas.GetValueOrDefault(ResourceType.Food, 0),
                deltas.GetValueOrDefault(ResourceType.Gold, 0),
                deltas.GetValueOrDefault(ResourceType.Stone, 0)
            ));
        }

        private void PublishSpendEvent(Dictionary<ResourceType, int> costs, bool success)
        {
            EventBus.Publish(new ResourcesSpentEvent(
                costs.GetValueOrDefault(ResourceType.Wood, 0),
                costs.GetValueOrDefault(ResourceType.Food, 0),
                costs.GetValueOrDefault(ResourceType.Gold, 0),
                costs.GetValueOrDefault(ResourceType.Stone, 0),
                success
            ));
        }

        private void LogInsufficientResources(Dictionary<ResourceType, int> costs)
        {
            var missing = costs
                .Where(c => GetResource(c.Key) < c.Value)
                .Select(c => $"{c.Key}:{c.Value} (have {GetResource(c.Key)})")
                .ToArray();

        }

        #endregion

        #region Debug Methods

        [ContextMenu("Add 100 of Each Resource")]
        private void DebugAddResources()
        {
            var amounts = new Dictionary<ResourceType, int>();
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                amounts[type] = 100;
            }
            AddResources(amounts);
        }

        [ContextMenu("Remove 50 of Each Resource")]
        private void DebugRemoveResources()
        {
            var costs = new Dictionary<ResourceType, int>();
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                costs[type] = 50;
            }
            SpendResources(costs);
        }

        #endregion
    }
}