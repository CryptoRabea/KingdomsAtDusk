using UnityEngine;
using RTS.Core.Events;

namespace RTS.Buildings
{
    /// <summary>
    /// Tower building component - extends Building with combat capabilities.
    /// Attach this to tower prefabs alongside TowerCombat component.
    /// </summary>
    [RequireComponent(typeof(TowerCombat))]
    public class Tower : Building
    {
        [Header("Tower Specific")]
        [SerializeField] private TowerDataSO towerData;
        [SerializeField] private GameObject wallReplacementEffect; // Optional VFX when replacing wall

        private TowerCombat towerCombat;
        private GameObject replacedWall; // Reference to wall that was replaced

        public TowerDataSO TowerData => towerData;
        public TowerCombat Combat => towerCombat;

        private new void Start()
        {
            // Get TowerCombat component
            towerCombat = GetComponent<TowerCombat>();
            if (towerCombat == null)
            {
                Debug.LogError($"Tower {name} is missing TowerCombat component!");
            }

            // Set tower data on combat component
            if (towerCombat != null && towerData != null)
            {
                towerCombat.SetTowerData(towerData);
            }

            // Call base Start
            base.Start();

            // Publish tower placed event
            if (towerData != null)
            {
                EventBus.Publish(new TowerPlacedEvent(gameObject, transform.position, towerData.towerType));
            }
        }

        /// <summary>
        /// Set the tower data (called by placement system).
        /// </summary>
        public void SetTowerData(TowerDataSO data)
        {
            towerData = data;

            // Also set on base Building component
            SetData(data);

            // Set on combat component
            if (towerCombat != null)
            {
                towerCombat.SetTowerData(data);
            }
        }

        /// <summary>
        /// Set reference to wall that was replaced (for tracking).
        /// </summary>
        public void SetReplacedWall(GameObject wall)
        {
            replacedWall = wall;
        }

        /// <summary>
        /// Get the wall that was replaced by this tower (if any).
        /// </summary>
        public GameObject GetReplacedWall()
        {
            return replacedWall;
        }

        private new void OnDestroy()
        {
            // Optionally: Restore wall when tower is destroyed
            // (This is a design choice - you might want to leave it destroyed)

            // Publish tower destroyed event
            if (towerData != null)
            {
                EventBus.Publish(new TowerDestroyedEvent(gameObject, towerData.towerType));
            }

            // Call base OnDestroy
            base.OnDestroy();
        }

        #region Debug

        [ContextMenu("Print Tower Stats")]
        private void DebugPrintStats()
        {
            if (towerData == null)
            {
                Debug.Log("No tower data assigned!");
                return;
            }

            Debug.Log($"=== Tower Stats: {towerData.buildingName} ===");
            Debug.Log($"Type: {towerData.towerType}");
            Debug.Log($"Attack Range: {towerData.attackRange}");
            Debug.Log($"Damage: {towerData.attackDamage}");
            Debug.Log($"Attack Rate: {towerData.attackRate}/s");
            Debug.Log($"Area Damage: {towerData.hasAreaDamage} (Radius: {towerData.aoeRadius})");

            if (towerData.towerType == TowerType.Fire)
            {
                Debug.Log($"DOT: {towerData.dotDamage}/s for {towerData.dotDuration}s");
            }
        }

        #endregion
    }

    #region Events

    /// <summary>
    /// Event published when a tower is placed.
    /// </summary>
    public struct TowerPlacedEvent
    {
        public GameObject Tower { get; }
        public Vector3 Position { get; }
        public TowerType Type { get; }

        public TowerPlacedEvent(GameObject tower, Vector3 position, TowerType type)
        {
            Tower = tower;
            Position = position;
            Type = type;
        }
    }

    /// <summary>
    /// Event published when a tower is destroyed.
    /// </summary>
    public struct TowerDestroyedEvent
    {
        public GameObject Tower { get; }
        public TowerType Type { get; }

        public TowerDestroyedEvent(GameObject tower, TowerType type)
        {
            Tower = tower;
            Type = type;
        }
    }

    #endregion
}
