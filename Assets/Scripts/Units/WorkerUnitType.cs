using UnityEngine;

namespace KingdomsAtDusk.Units
{
    /// <summary>
    /// Defines the type of worker and what resource they gather.
    /// </summary>
    public enum WorkerUnitType
    {
        None,           // Not a worker unit
        LumberWorker,   // Gathers wood from trees/lumber mills
        Farmer,         // Gathers food from farms/fields
        Miner,          // Gathers gold from mines
        Stonecutter     // Gathers stone from quarries
    }

    /// <summary>
    /// Extension methods for WorkerUnitType to get associated resource types.
    /// </summary>
    public static class WorkerUnitTypeExtensions
    {
        public static ResourceType GetResourceType(this WorkerUnitType workerType)
        {
            return workerType switch
            {
                WorkerUnitType.LumberWorker => ResourceType.Wood,
                WorkerUnitType.Farmer => ResourceType.Food,
                WorkerUnitType.Miner => ResourceType.Gold,
                WorkerUnitType.Stonecutter => ResourceType.Stone,
                _ => ResourceType.Wood // Default fallback
            };
        }

        public static string GetWorkerName(this WorkerUnitType workerType)
        {
            return workerType switch
            {
                WorkerUnitType.LumberWorker => "Lumber Worker",
                WorkerUnitType.Farmer => "Farmer",
                WorkerUnitType.Miner => "Miner",
                WorkerUnitType.Stonecutter => "Stonecutter",
                _ => "Worker"
            };
        }
    }
}
