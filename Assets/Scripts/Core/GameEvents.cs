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

    public struct FormationChangedEvent
    {
        public RTS.Units.Formation.FormationType FormationType;

        public FormationChangedEvent(RTS.Units.Formation.FormationType formationType)
        {
            FormationType = formationType;
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

    public struct UnitGroupSavedEvent
    {
        public int GroupNumber;
        public int UnitCount;

        public UnitGroupSavedEvent(int groupNumber, int unitCount)
        {
            GroupNumber = groupNumber;
            UnitCount = unitCount;
        }
    }

    public struct UnitGroupRecalledEvent
    {
        public int GroupNumber;
        public int UnitCount;
        public bool WasDoubleTap;

        public UnitGroupRecalledEvent(int groupNumber, int unitCount, bool wasDoubleTap)
        {
            GroupNumber = groupNumber;
            UnitCount = unitCount;
            WasDoubleTap = wasDoubleTap;
        }
    }

    public struct UnitHoveredEvent
    {
        public GameObject Unit;
        public bool IsHovered;

        public UnitHoveredEvent(GameObject unit, bool isHovered)
        {
            Unit = unit;
            IsHovered = isHovered;
        }
    }

    public struct AllVisibleUnitsSelectedEvent
    {
        public int UnitCount;

        public AllVisibleUnitsSelectedEvent(int unitCount)
        {
            UnitCount = unitCount;
        }
    }


    public struct BuildingGroupSavedEvent
    {
        public int GroupNumber;
        public string BuildingName;

        public BuildingGroupSavedEvent(int groupNumber, string buildingName)
        {
            GroupNumber = groupNumber;
            BuildingName = buildingName;
        }
    }

    public struct BuildingGroupRecalledEvent
    {
        public int GroupNumber;
        public string BuildingName;
        public bool WasDoubleTap;

        public BuildingGroupRecalledEvent(int groupNumber, string buildingName, bool wasDoubleTap)
        {
            GroupNumber = groupNumber;
            BuildingName = buildingName;
            WasDoubleTap = wasDoubleTap;
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

    // ==================== POPULATION & PEASANT EVENTS ====================

    public struct PopulationChangedEvent
    {
        public int TotalPopulation;
        public int AvailablePeasants;
        public int AssignedPeasants;
        public int HousingCapacity;

        public PopulationChangedEvent(int total, int available, int assigned, int housing)
        {
            TotalPopulation = total;
            AvailablePeasants = available;
            AssignedPeasants = assigned;
            HousingCapacity = housing;
        }
    }

    public struct PeasantAssignedEvent
    {
        public string WorkType; // "Building", "Training", "Resource", etc.
        public int Amount;
        public GameObject AssignedTo; // The building/location they're assigned to

        public PeasantAssignedEvent(string workType, int amount, GameObject assignedTo)
        {
            WorkType = workType;
            Amount = amount;
            AssignedTo = assignedTo;
        }
    }

    public struct PeasantReleasedEvent
    {
        public string WorkType;
        public int Amount;
        public GameObject ReleasedFrom;

        public PeasantReleasedEvent(string workType, int amount, GameObject releasedFrom)
        {
            WorkType = workType;
            Amount = amount;
            ReleasedFrom = releasedFrom;
        }
    }

    public struct CampfireGatheringChangedEvent
    {
        public GameObject Campfire;
        public int PeasantCount;
        public float HappinessMultiplier;

        public CampfireGatheringChangedEvent(GameObject campfire, int count, float happinessMultiplier)
        {
            Campfire = campfire;
            PeasantCount = count;
            HappinessMultiplier = happinessMultiplier;
        }
    }

    // ==================== REPUTATION EVENTS ====================

    public struct ReputationChangedEvent
    {
        public float NewReputation;
        public float Delta;
        public string Reason;

        public ReputationChangedEvent(float newReputation, float delta, string reason)
        {
            NewReputation = newReputation;
            Delta = delta;
            Reason = reason;
        }
    }

    // ==================== ANIMAL EVENTS ====================

    public struct AnimalSpawnedEvent
    {
        public GameObject Animal;
        public RTS.Animals.AnimalType AnimalType;
        public Vector3 Position;

        public AnimalSpawnedEvent(GameObject animal, RTS.Animals.AnimalType animalType, Vector3 position)
        {
            Animal = animal;
            AnimalType = animalType;
            Position = position;
        }
    }

    public struct AnimalDiedEvent
    {
        public GameObject Animal;
        public RTS.Animals.AnimalType AnimalType;

        public AnimalDiedEvent(GameObject animal, RTS.Animals.AnimalType animalType)
        {
            Animal = animal;
            AnimalType = animalType;
        }
    }

    // ==================== UNIT COMMAND EVENTS ====================

    /// <summary>
    /// Event published when a unit receives a move command
    /// </summary>
    public struct UnitMoveCommandEvent
    {
        public GameObject Unit;
        public Vector3 Destination;

        public UnitMoveCommandEvent(GameObject unit, Vector3 destination)
        {
            Unit = unit;
            Destination = destination;
        }
    }

    /// <summary>
    /// Event published when a unit receives an attack command
    /// </summary>
    public struct UnitAttackCommandEvent
    {
        public GameObject Unit;
        public GameObject Target;
        public AttackTargetType TargetType;

        public UnitAttackCommandEvent(GameObject unit, GameObject target, AttackTargetType targetType)
        {
            Unit = unit;
            Target = target;
            TargetType = targetType;
        }
    }

    /// <summary>
    /// Type of target being attacked
    /// </summary>
    public enum AttackTargetType
    {
        Unit,
        Building
    }

    // ==================== TIME & DAY-NIGHT CYCLE EVENTS ====================

    /// <summary>
    /// Event published when a new in-game hour begins.
    /// </summary>
    public struct HourChangedEvent
    {
        public int PreviousHour;
        public int NewHour;
        public int CurrentDay;

        public HourChangedEvent(int previousHour, int newHour, int currentDay)
        {
            PreviousHour = previousHour;
            NewHour = newHour;
            CurrentDay = currentDay;
        }
    }

    /// <summary>
    /// Event published when the day phase changes (Dawn, Day, Dusk, Night).
    /// </summary>
    public struct DayPhaseChangedEvent
    {
        public RTS.DayNightCycle.DayPhase PreviousPhase;
        public RTS.DayNightCycle.DayPhase NewPhase;
        public int CurrentDay;

        public DayPhaseChangedEvent(RTS.DayNightCycle.DayPhase previousPhase, RTS.DayNightCycle.DayPhase newPhase, int currentDay)
        {
            PreviousPhase = previousPhase;
            NewPhase = newPhase;
            CurrentDay = currentDay;
        }
    }

    /// <summary>
    /// Event published when a new day begins (midnight transition).
    /// </summary>
    public struct NewDayEvent
    {
        public int PreviousDay;
        public int NewDay;

        public NewDayEvent(int previousDay, int newDay)
        {
            PreviousDay = previousDay;
            NewDay = newDay;
        }
    }

    /// <summary>
    /// Event published when the time scale changes.
    /// </summary>
    public struct TimeScaleChangedEvent
    {
        public float PreviousScale;
        public float NewScale;

        public TimeScaleChangedEvent(float previousScale, float newScale)
        {
            PreviousScale = previousScale;
            NewScale = newScale;
        }
    }

    /// <summary>
    /// Event published when dawn begins (useful for gameplay triggers).
    /// </summary>
    public struct DawnStartedEvent
    {
        public int CurrentDay;

        public DawnStartedEvent(int currentDay)
        {
            CurrentDay = currentDay;
        }
    }

    /// <summary>
    /// Event published when night begins (useful for enemy spawning, etc.).
    /// </summary>
    public struct NightStartedEvent
    {
        public int CurrentDay;

        public NightStartedEvent(int currentDay)
        {
            CurrentDay = currentDay;
        }
    }

    /// <summary>
    /// Event published continuously with time updates (for UI and smooth animations).
    /// Throttled to avoid performance issues - fires every frame when subscribed.
    /// </summary>
    public struct TimeUpdatedEvent
    {
        public float CurrentHour;      // Current hour with decimal (e.g., 14.5 = 2:30 PM)
        public float DayProgress;      // 0-1 progress through the day
        public int CurrentDay;         // Current day number
        public RTS.DayNightCycle.DayPhase CurrentPhase;

        public TimeUpdatedEvent(float currentHour, float dayProgress, int currentDay, RTS.DayNightCycle.DayPhase currentPhase)
        {
            CurrentHour = currentHour;
            DayProgress = dayProgress;
            CurrentDay = currentDay;
            CurrentPhase = currentPhase;
        }
    }

    /// <summary>
    /// Event published when time is paused or resumed.
    /// </summary>
    public struct TimePausedEvent
    {
        public bool IsPaused;

        public TimePausedEvent(bool isPaused)
        {
            IsPaused = isPaused;
        }
    }
}