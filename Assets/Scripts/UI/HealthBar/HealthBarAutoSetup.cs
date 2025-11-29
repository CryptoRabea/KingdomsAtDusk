using UnityEngine;
using RTS.Units.Components;
using RTS.Buildings.Components;

namespace RTS.UI.HealthBar
{
    /// <summary>
    /// Automatically creates and attaches health bars to units and buildings
    /// Attach this component to automatically spawn a health bar
    /// </summary>
    public class HealthBarAutoSetup : MonoBehaviour
    {
        [Header("Prefab Reference")]
        [SerializeField] private GameObject healthBarPrefab;

        [Header("Settings")]
        [SerializeField] private bool createOnAwake = true;
        [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);

        private GameObject spawnedHealthBar;

        private void Awake()
        {
            if (createOnAwake)
            {
                CreateHealthBar();
            }
        }

        public void CreateHealthBar()
        {
            if (healthBarPrefab == null)
            {
                // Try to find it in Resources
                healthBarPrefab = Resources.Load<GameObject>("Prefabs/UI/HealthBar");

                if (healthBarPrefab == null)
                {
                    Debug.LogWarning($"HealthBarAutoSetup on {gameObject.name}: No health bar prefab assigned and couldn't find in Resources!");
                    return;
                }
            }

            // Check if we have a health component
            UnitHealth unitHealth = GetComponent<UnitHealth>();
            BuildingHealth buildingHealth = GetComponent<BuildingHealth>();

            if (unitHealth == null && buildingHealth == null)
            {
                Debug.LogWarning($"HealthBarAutoSetup on {gameObject.name}: No UnitHealth or BuildingHealth component found!");
                return;
            }

            // Don't create if already exists
            if (spawnedHealthBar != null)
            {
                Debug.LogWarning($"HealthBarAutoSetup on {gameObject.name}: Health bar already exists!");
                return;
            }

            // Create health bar
            spawnedHealthBar = Instantiate(healthBarPrefab, transform);
            spawnedHealthBar.name = "HealthBar";

            // Position it
            spawnedHealthBar.transform.localPosition = offset;

            Debug.Log($"Created health bar for {gameObject.name}");
        }

        public void RemoveHealthBar()
        {
            if (spawnedHealthBar != null)
            {
                Destroy(spawnedHealthBar);
                spawnedHealthBar = null;
            }
        }

        private void OnDestroy()
        {
            RemoveHealthBar();
        }

        #region Editor Utilities

        [ContextMenu("Create Health Bar Now")]
        private void EditorCreateHealthBar()
        {
            CreateHealthBar();
        }

        [ContextMenu("Remove Health Bar")]
        private void EditorRemoveHealthBar()
        {
            RemoveHealthBar();
        }

        [ContextMenu("Auto Setup All Units")]
        private void AutoSetupAllUnits()
        {
            #if UNITY_EDITOR
            UnitHealth[] allUnits = FindObjectsOfType<UnitHealth>();
            int added = 0;

            foreach (var unit in allUnits)
            {
                if (unit.GetComponent<HealthBarAutoSetup>() == null)
                {
                    var setup = unit.gameObject.AddComponent<HealthBarAutoSetup>();
                    setup.healthBarPrefab = healthBarPrefab;
                    added++;
                }
            }

            Debug.Log($"Added HealthBarAutoSetup to {added} units");
            #endif
        }

        [ContextMenu("Auto Setup All Buildings")]
        private void AutoSetupAllBuildings()
        {
            #if UNITY_EDITOR
            BuildingHealth[] allBuildings = FindObjectsOfType<BuildingHealth>();
            int added = 0;

            foreach (var building in allBuildings)
            {
                if (building.GetComponent<HealthBarAutoSetup>() == null)
                {
                    var setup = building.gameObject.AddComponent<HealthBarAutoSetup>();
                    setup.healthBarPrefab = healthBarPrefab;
                    added++;
                }
            }

            Debug.Log($"Added HealthBarAutoSetup to {added} buildings");
            #endif
        }

        #endregion
    }
}
