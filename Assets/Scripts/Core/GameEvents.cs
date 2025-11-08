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
        public GameObject Building;
        public Vector3 Position;

        public BuildingPlacedEvent(GameObject building, Vector3 position)
        {
            Building = building;
            Position = position;
        }
    }

    public struct BuildingCompletedEvent
    {
        public GameObject Building;
        public string BuildingType;

        public BuildingCompletedEvent(GameObject building, string buildingType)
        {
            Building = building;
            BuildingType = buildingType;
        }
    }

    public struct BuildingDestroyedEvent
    {
        public GameObject Building;
        public string BuildingType;

        public BuildingDestroyedEvent(GameObject building, string buildingType)
        {
            Building = building;
            BuildingType = buildingType;
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
}
