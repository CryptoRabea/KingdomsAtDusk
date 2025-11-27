using TopDownWallBuilding.Core.Services;
using UnityEngine;

namespace TopDownWallBuilding.Core.Events
{
    // ==================== RESOURCE EVENTS ====================

    public struct ResourcesChangedEvent
    {
        public int WoodDelta;
        public int FoodDelta;
        public int GoldDelta;
        public int StoneDelta;

        public ResourcesChangedEvent(int wood, int food, int gold, int stone)
        {
            WoodDelta = wood;
            FoodDelta = food;
            GoldDelta = gold;
            StoneDelta = stone;
        }
    }

    public struct ResourcesSpentEvent
    {
        public int Wood;
        public int Food;
        public int Gold;
        public int Stone;
        public bool Success;

        public ResourcesSpentEvent(int wood, int food, int gold, int stone, bool success)
        {
            Wood = wood;
            Food = food;
            Gold = gold;
            Stone = stone;
            Success = success;
        }
    }

    // ==================== BUILDING/WALL EVENTS ====================

    public struct BuildingPlacedEvent
    {
        public GameObject Building { get; }
        public Vector3 Position { get; }

        public BuildingPlacedEvent(GameObject building, Vector3 position)
        {
            Building = building;
            Position = position;
        }
    }

    /// <summary>
    /// Event published when a building/wall completes construction.
    /// </summary>
    public struct BuildingCompletedEvent
    {
        public GameObject Building { get; }
        public string BuildingName { get; }

        public BuildingCompletedEvent(GameObject building, string buildingName)
        {
            Building = building;
            BuildingName = buildingName;
        }
    }

    /// <summary>
    /// Event published when a building/wall is destroyed/demolished.
    /// </summary>
    public struct BuildingDestroyedEvent
    {
        public GameObject Building { get; }
        public string BuildingName { get; }

        public BuildingDestroyedEvent(GameObject building, string buildingName)
        {
            Building = building;
            BuildingName = buildingName;
        }
    }

    /// <summary>
    /// Event published when building placement fails.
    /// </summary>
    public struct BuildingPlacementFailedEvent
    {
        public string Reason { get; }

        public BuildingPlacementFailedEvent(string reason)
        {
            Reason = reason;
        }
    }
}
