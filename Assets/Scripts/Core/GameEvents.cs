using RTS.Core.Services;
using UnityEngine;

namespace RTS.Core.Events
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
        public bool Success;

        public ResourcesSpentEvent(int wood, int food, int gold, int stone, bool success)
        {
            Wood = wood;
            Food = food;
            Gold = gold;
            Success = success;
        }
    }

    // ==================== HAPPINESS EVENTS ====================
    
    public struct HappinessChangedEvent
    {
        public float NewHappiness;
        public float Delta;

        public HappinessChangedEvent(float newHappiness, float delta)
        {
            NewHappiness = newHappiness;
            Delta = delta;
        }
    }

    public struct BuildingBonusChangedEvent
    {
        public float BonusDelta;
        public string BuildingName;

        public BuildingBonusChangedEvent(float delta, string buildingName)
        {
            BonusDelta = delta;
            BuildingName = buildingName;
        }
    }

    // ==================== BUILDING EVENTS ====================

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
    /// Event published when a building completes construction.
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
    /// Event published when a building is destroyed/demolished.
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
    /// Event published when a building generates resources.
    /// </summary>
    public struct ResourcesGeneratedEvent
    {
        public string BuildingName { get; }
        public ResourceType ResourceType { get; }
        public int Amount { get; }

        public ResourcesGeneratedEvent(string buildingName, ResourceType resourceType, int amount)
        {
            BuildingName = buildingName;
            ResourceType = resourceType;
            Amount = amount;
        }
    }

    /// <summary>
    /// Event published when construction progress updates (optional, for UI).
    /// </summary>
    public struct ConstructionProgressEvent
    {
        public GameObject Building { get; }
        public string BuildingName { get; }
        public float Progress { get; } // 0-1

        public ConstructionProgressEvent(GameObject building, string buildingName, float progress)
        {
            Building = building;
            BuildingName = buildingName;
            Progress = progress;
        }
    }

    // ==================== UNIT EVENTS ====================

    public struct UnitSpawnedEvent
    {
        public GameObject Unit;
        public Vector3 Position;

        public UnitSpawnedEvent(GameObject unit, Vector3 position)
        {
            Unit = unit;
            Position = position;
        }
    }

    public struct UnitDiedEvent
    {
        public GameObject Unit;
        public bool WasEnemy;

        public UnitDiedEvent(GameObject unit, bool wasEnemy)
        {
            Unit = unit;
            WasEnemy = wasEnemy;
        }
    }

    public struct UnitHealthChangedEvent
    {
        public GameObject Unit;
        public float CurrentHealth;
        public float MaxHealth;
        public float Delta;

        public UnitHealthChangedEvent(GameObject unit, float current, float max, float delta)
        {
            Unit = unit;
            CurrentHealth = current;
            MaxHealth = max;
            Delta = delta;
        }
    }

    public struct UnitStateChangedEvent
    {
        public GameObject Unit;
        public int OldState;
        public int NewState;

        public UnitStateChangedEvent(GameObject unit, int oldState, int newState)
        {
            Unit = unit;
            OldState = oldState;
            NewState = newState;
        }
    }

    // ==================== COMBAT EVENTS ====================
    
    public struct DamageDealtEvent
    {
        public GameObject Attacker;
        public GameObject Target;
        public float Damage;

        public DamageDealtEvent(GameObject attacker, GameObject target, float damage)
        {
            Attacker = attacker;
            Target = target;
            Damage = damage;
        }
    }

    public struct HealingAppliedEvent
    {
        public GameObject Healer;
        public GameObject Target;
        public float Amount;

        public HealingAppliedEvent(GameObject healer, GameObject target, float amount)
        {
            Healer = healer;
            Target = target;
            Amount = amount;
        }
    }

    // ==================== WAVE EVENTS ====================
    
    public struct WaveStartedEvent
    {
        public int WaveNumber;
        public int EnemyCount;

        public WaveStartedEvent(int wave, int count)
        {
            WaveNumber = wave;
            EnemyCount = count;
        }
    }

    public struct WaveCompletedEvent
    {
        public int WaveNumber;

        public WaveCompletedEvent(int wave)
        {
            WaveNumber = wave;
        }
    }

    // ==================== SELECTION EVENTS ====================

    public struct UnitSelectedEvent
    {
        public GameObject Unit;

        public UnitSelectedEvent(GameObject unit)
        {
            Unit = unit;
        }
    }

    public struct UnitDeselectedEvent
    {
        public GameObject Unit;

        public UnitDeselectedEvent(GameObject unit)
        {
            Unit = unit;
        }
    }

    public struct SelectionChangedEvent
    {
        public int SelectionCount;

        public SelectionChangedEvent(int count)
        {
            SelectionCount = count;
        }
    }

    public struct BuildingSelectedEvent
    {
        public GameObject Building;

        public BuildingSelectedEvent(GameObject building)
        {
            Building = building;
        }
    }

    public struct BuildingDeselectedEvent
    {
        public GameObject Building;

        public BuildingDeselectedEvent(GameObject building)
        {
            Building = building;
        }
    }

    // ==================== UNIT TRAINING EVENTS ====================

    public struct UnitTrainingStartedEvent
    {
        public GameObject Building;
        public string UnitName;

        public UnitTrainingStartedEvent(GameObject building, string unitName)
        {
            Building = building;
            UnitName = unitName;
        }
    }

    public struct UnitTrainingCompletedEvent
    {
        public GameObject Building;
        public GameObject Unit;
        public string UnitName;

        public UnitTrainingCompletedEvent(GameObject building, GameObject unit, string unitName)
        {
            Building = building;
            Unit = unit;
            UnitName = unitName;
        }
    }

    public struct TrainingProgressEvent
    {
        public GameObject Building;
        public string UnitName;
        public float Progress; // 0-1

        public TrainingProgressEvent(GameObject building, string unitName, float progress)
        {
            Building = building;
            UnitName = unitName;
            Progress = progress;
        }
    }
}
