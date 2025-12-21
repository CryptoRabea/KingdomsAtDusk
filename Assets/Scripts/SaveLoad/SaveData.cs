using UnityEngine;
using System.Collections.Generic;
using RTS.Core.Services;
using RTS.Units.AI;

namespace RTS.SaveLoad
{
    /// <summary>
    /// Root save data container for the entire game state.
    /// </summary>
    [System.Serializable]
    public class GameSaveData
    {
        public string saveName;
        public string saveDate;
        public float playTime;
        public string gameVersion;

        // Core game state
        public GameStateData gameState;
        public ResourcesData resources;
        public HappinessData happiness;
        public TimeData time;

        // Optional systems
        public PopulationData population;
        public ReputationData reputation;

        // Entities
        public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
        public List<UnitSaveData> units = new List<UnitSaveData>();

        // World state
        public CameraData cameraState;

        public GameSaveData()
        {
            saveDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            gameVersion = Application.version;
        }
    }

    #region Core Systems Data

    [System.Serializable]
    public class GameStateData
    {
        public int currentState; // GameState enum as int
        public bool isPaused;
        public float timeScale;
    }

    [System.Serializable]
    public class ResourcesData
    {
        public int wood;
        public int food;
        public int gold;
        public int stone;

        public ResourcesData() { }

        public ResourcesData(IResourcesService service)
        {
            if (service != null)
            {
                wood = service.GetResource(ResourceType.Wood);
                food = service.GetResource(ResourceType.Food);
                gold = service.GetResource(ResourceType.Gold);
                stone = service.GetResource(ResourceType.Stone);
            }
        }

        public Dictionary<ResourceType, int> ToDictionary()
        {
            return new Dictionary<ResourceType, int>
            {
                { ResourceType.Wood, wood },
                { ResourceType.Food, food },
                { ResourceType.Gold, gold },
                { ResourceType.Stone, stone }
            };
        }
    }

    [System.Serializable]
    public class HappinessData
    {
        public float currentHappiness;
        public float taxLevel;
        public float buildingsHappinessBonus;
    }

    [System.Serializable]
    public class TimeData
    {
        public float currentTime;
        public int currentDay;
        public float dayProgress;
        public float timeScale;
    }

    [System.Serializable]
    public class PopulationData
    {
        public int totalPopulation;
        public int availablePeasants;
        public int assignedPeasants;
        public int housingCapacity;
    }

    [System.Serializable]
    public class ReputationData
    {
        public float currentReputation;
    }

    #endregion

    #region Building Data

    [System.Serializable]
    public class BuildingSaveData
    {
        // Identity
        public int instanceID;
        public string buildingDataName; // Name of the BuildingDataSO
        public string prefabPath; // Path to prefab for reconstruction

        // Transform
        public Vector3Serializable position;
        public QuaternionSerializable rotation;
        public Vector3Serializable scale;

        // State
        public bool isConstructed;
        public float constructionProgress;
        public bool requiresConstruction;

        // Resource generation
        public float resourceGenerationTimer;

        // Layer and tag
        public int layer;
        public string tag;

        // Team/ownership
        public bool isPlayerOwned = true;
        public int teamID = 0;
    }

    #endregion

    #region Unit Data

    [System.Serializable]
    public class UnitSaveData
    {
        // Identity
        public int instanceID;
        public string unitConfigName; // Name of the UnitConfigSO
        public string prefabPath; // Path to prefab for reconstruction

        // Transform
        public Vector3Serializable position;
        public QuaternionSerializable rotation;
        public Vector3Serializable scale;

        // Health
        public float currentHealth;
        public float maxHealth;
        public bool isDead;

        // Movement
        public bool hasDestination;
        public Vector3Serializable currentDestination;
        public float moveSpeed;
        public bool isMoving;

        // Combat
        public int currentTargetID; // Instance ID of target (-1 if none)
        public float attackDamage;
        public float attackRange;
        public float attackRate;
        public float lastAttackTime;

        // AI State
        public int aiState; // UnitStateType as int
        public int behaviorType; // AIBehaviorType as int
        public Vector3Serializable? aggroOriginPosition;
        public bool isOnForcedMove;
        public Vector3Serializable? forcedMoveDestination;

        // Layer and tag
        public int layer;
        public string tag;

        // Team/ownership
        public bool isPlayerOwned = true;
        public int teamID = 0;
    }

    #endregion

    #region Fog of War Data

    [System.Serializable]
   

    #endregion

    #region Camera Data

    public class CameraData
    {
        public Vector3Serializable position;
        public QuaternionSerializable rotation;
        public float fieldOfView;
        public float orthographicSize;
    }

    #endregion

    #region Serializable Unity Types

    /// <summary>
    /// Serializable Vector3 (Unity's Vector3 is not serializable by JsonUtility)
    /// </summary>
    [System.Serializable]
    public struct Vector3Serializable
    {
        public float x;
        public float y;
        public float z;

        public Vector3Serializable(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public static implicit operator Vector3(Vector3Serializable s) => s.ToVector3();
        public static implicit operator Vector3Serializable(Vector3 v) => new Vector3Serializable(v);
    }

    /// <summary>
    /// Serializable Quaternion
    /// </summary>
    [System.Serializable]
    public struct QuaternionSerializable
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public QuaternionSerializable(Quaternion quaternion)
        {
            x = quaternion.x;
            y = quaternion.y;
            z = quaternion.z;
            w = quaternion.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }

        public static implicit operator Quaternion(QuaternionSerializable s) => s.ToQuaternion();
        public static implicit operator QuaternionSerializable(Quaternion q) => new QuaternionSerializable(q);
    }

    #endregion
}
