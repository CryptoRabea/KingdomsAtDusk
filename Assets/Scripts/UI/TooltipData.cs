using UnityEngine;
using System.Collections.Generic;
using RTS.Core.Services;

namespace RTS.UI
{
    /// <summary>
    /// Universal data structure for tooltips (buildings, units, towers, walls).
    /// </summary>
    [System.Serializable]
    public class TooltipData
    {
        public string title;
        public string description;
        public Sprite icon;

        // Costs
        public Dictionary<ResourceType, int> costs = new Dictionary<ResourceType, int>();

        // Optional Stats
        public bool showConstructionTime;
        public float constructionTime;

        public bool showHP;
        public int maxHP;

        public bool showDefence;
        public int defence;

        public bool showAttackDamage;
        public float attackDamage;

        public bool showAttackRange;
        public float attackRange;

        public bool showAttackSpeed;
        public float attackSpeed;

        /// <summary>
        /// Create TooltipData from BuildingDataSO
        /// </summary>
        public static TooltipData FromBuilding(RTS.Buildings.BuildingDataSO buildingData)
        {
            if (buildingData == null) return null;

            var tooltipData = new TooltipData
            {
                title = buildingData.buildingName,
                description = buildingData.description,
                icon = buildingData.icon,
                costs = buildingData.GetCosts(),

                // Construction time is always shown for buildings
                showConstructionTime = true,
                constructionTime = buildingData.constructionTime,

                // Optional stats from BuildingSO
                showHP = buildingData.showHP,
                maxHP = buildingData.maxHealth,

                showDefence = buildingData.showDefence,
                defence = buildingData.defence,

                showAttackDamage = buildingData.showAttackDamage,
                attackDamage = buildingData.attackDamage,

                showAttackRange = buildingData.showAttackRange,
                attackRange = buildingData.attackRange,

                showAttackSpeed = buildingData.showAttackSpeed,
                attackSpeed = buildingData.attackSpeed
            };

            return tooltipData;
        }

        /// <summary>
        /// Create TooltipData from TrainableUnitData
        /// </summary>
        public static TooltipData FromUnit(RTS.Buildings.TrainableUnitData unitData)
        {
            if (unitData == null || unitData.unitConfig == null) return null;

            var config = unitData.unitConfig;

            var tooltipData = new TooltipData
            {
                title = config.unitName,
                description = config.description,
                icon = config.unitIcon,
                costs = unitData.GetCosts(),

                // Training time (equivalent to construction time)
                showConstructionTime = true,
                constructionTime = unitData.trainingTime,

                // Unit stats from UnitConfigSO
                showHP = config.showHP,
                maxHP = (int)config.maxHealth,

                showDefence = config.showDefence,
                defence = config.defence,

                showAttackDamage = config.showAttackDamage,
                attackDamage = (int)config.attackDamage,

                showAttackRange = config.showAttackRange,
                attackRange = config.attackRange,

                showAttackSpeed = config.showAttackSpeed,
                attackSpeed = config.attackRate // attacks per second
            };

            return tooltipData;
        }
    }
}
