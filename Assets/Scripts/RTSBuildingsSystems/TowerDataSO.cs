using UnityEngine;

namespace RTS.Buildings
{
    /// <summary>
    /// Tower type enumeration for different attack types.
    /// </summary>
    public enum TowerType
    {
        Arrow,      // Fast, single target
        Fire,       // Area damage over time
        Catapult    // Slow, high damage, area effect
    }

    /// <summary>
    /// ScriptableObject for tower-specific configuration.
    /// Extends BuildingDataSO with combat properties.
    /// Create via: Right-click in Project > Create > RTS > TowerData
    /// </summary>
    [CreateAssetMenu(fileName = "TowerData", menuName = "RTS/TowerData")]
    public class TowerDataSO : BuildingDataSO
    {
        [Header("Tower Properties")]
        [Tooltip("Type of tower (Arrow, Fire, Catapult)")]
        public TowerType towerType = TowerType.Arrow;

        [Header("Combat Settings")]
        [Tooltip("How far the tower can detect and attack enemies")]
        public float attackRange = 15f;

        [Tooltip("Damage dealt per attack")]
        public float attackDamage = 20f;

        [Tooltip("Attacks per second")]
        public float attackRate = 1f;

        [Tooltip("Layers that this tower can target (e.g., Enemy layer)")]
        public LayerMask targetLayers;

        [Header("Projectile Settings")]
        [Tooltip("Prefab for the projectile (arrow, fireball, boulder)")]
        public GameObject projectilePrefab;

        [Tooltip("Speed of the projectile")]
        public float projectileSpeed = 15f;

        [Tooltip("Spawn point offset from tower center (height adjustment)")]
        public Vector3 projectileSpawnOffset = new Vector3(0, 2f, 0);

        [Header("Special Effects (Fire Tower)")]
        [Tooltip("For fire towers: damage over time per second")]
        public float dotDamage = 5f;

        [Tooltip("For fire towers: duration of burning effect")]
        public float dotDuration = 3f;

        [Header("Area Effects (Fire/Catapult)")]
        [Tooltip("For fire/catapult: area of effect radius")]
        public float aoeRadius = 3f;

        [Tooltip("Does this tower deal area damage?")]
        public bool hasAreaDamage = false;

        [Header("Wall Replacement")]
        [Tooltip("Can this tower be placed on walls?")]
        public bool canReplaceWalls = true;

        [Tooltip("Snap distance to walls for placement")]
        public float wallSnapDistance = 2f;

        /// <summary>
        /// Get tower description with combat stats.
        /// </summary>
        public override string GetFullDescription()
        {
            string baseDesc = base.GetFullDescription();

            baseDesc += $"\n\n--- Tower Stats ---";
            baseDesc += $"\nType: {towerType}";
            baseDesc += $"\nAttack Range: {attackRange}m";
            baseDesc += $"\nDamage: {attackDamage}";
            baseDesc += $"\nAttack Rate: {attackRate}/s";

            if (hasAreaDamage)
            {
                baseDesc += $"\nArea Radius: {aoeRadius}m";
            }

            if (towerType == TowerType.Fire && dotDamage > 0)
            {
                baseDesc += $"\nBurn Damage: {dotDamage}/s for {dotDuration}s";
            }

            return baseDesc;
        }
    }
}
