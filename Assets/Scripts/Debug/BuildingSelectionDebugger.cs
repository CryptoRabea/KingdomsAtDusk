using UnityEngine;
using RTS.Buildings;
using RTS.Core.Events;

namespace RTS.Debugging
{
    /// <summary>
    /// Debug script to diagnose building selection issues.
    /// Add this to any GameObject in the scene temporarily.
    /// </summary>
    public class BuildingSelectionDebugger : MonoBehaviour
    {
        [Header("Debugging")]
        [SerializeField] private bool logAllEvents = true;
        [SerializeField] private bool checkBuildingComponents = true;

        private void OnEnable()
        {
            if (logAllEvents)
            {
                EventBus.Subscribe<BuildingSelectedEvent>(OnBuildingSelected);
                EventBus.Subscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
            }
        }

        private void OnDisable()
        {
            if (logAllEvents)
            {
                EventBus.Unsubscribe<BuildingSelectedEvent>(OnBuildingSelected);
                EventBus.Unsubscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
            }
        }

        private void OnBuildingSelected(BuildingSelectedEvent evt)
        {
            Debug.Log($"🟢 BuildingSelectedEvent received! Building: {evt.Building.name}");

            if (checkBuildingComponents)
            {
                CheckBuildingSetup(evt.Building);
            }
        }

        private void OnBuildingDeselected(BuildingDeselectedEvent evt)
        {
            Debug.Log($"🔴 BuildingDeselectedEvent received! Building: {evt.Building.name}");
        }

        private void CheckBuildingSetup(GameObject building)
        {
            Debug.Log("--- Building Component Check ---");

            // Check BuildingSelectable
            var selectable = building.GetComponent<BuildingSelectable>();
            if (selectable != null)
            {
                Debug.Log($"✅ BuildingSelectable found. IsSelected: {selectable.IsSelected}");
            }
            else
            {
                Debug.LogError("❌ BuildingSelectable component is MISSING!");
            }

            // Check Building component
            var buildingComp = building.GetComponent<Building>();
            if (buildingComp != null)
            {
                Debug.Log($"✅ Building component found.");

                if (buildingComp.Data != null)
                {
                    Debug.Log($"✅ BuildingData found: {buildingComp.Data.buildingName}");
                    Debug.Log($"   - Can Train Units: {buildingComp.Data.canTrainUnits}");
                    Debug.Log($"   - Trainable Units Count: {buildingComp.Data.trainableUnits.Count}");
                }
                else
                {
                    Debug.LogError("❌ Building.Data is NULL! Assign a BuildingDataSO to the Building component.");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Building component not found.");
            }

            // Check UnitTrainingQueue
            var trainingQueue = building.GetComponent<UnitTrainingQueue>();
            if (trainingQueue != null)
            {
                Debug.Log($"✅ UnitTrainingQueue found. Queue count: {trainingQueue.QueueCount}");
            }
            else
            {
                Debug.LogWarning("⚠️ UnitTrainingQueue component not found (needed for training units).");
            }

            Debug.Log("--- End Component Check ---");
        }

        [ContextMenu("Check BuildingDetailsUI in Scene")]
        private void CheckBuildingDetailsUI()
        {
            var detailsUI = FindFirstObjectByType<RTS.UI.BuildingDetailsUI>();

            if (detailsUI != null)
            {
                Debug.Log("✅ BuildingDetailsUI found in scene!");
                Debug.Log($"   - GameObject: {detailsUI.gameObject.name}");
                Debug.Log($"   - Enabled: {detailsUI.enabled}");
                Debug.Log($"   - GameObject Active: {detailsUI.gameObject.activeInHierarchy}");

                // Use reflection to check panelRoot
                var field = typeof(RTS.UI.BuildingDetailsUI).GetField("panelRoot",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (field != null)
                {
                    var panelRoot = field.GetValue(detailsUI) as GameObject;
                    if (panelRoot != null)
                    {
                        Debug.Log($"✅ PanelRoot assigned: {panelRoot.name}");
                        Debug.Log($"   - Active: {panelRoot.activeSelf}");
                    }
                    else
                    {
                        Debug.LogError("❌ PanelRoot is NULL! Run the UI setup tool or assign it manually.");
                    }
                }
            }
            else
            {
                Debug.LogError("❌ BuildingDetailsUI NOT found in scene! Add it to your Canvas.");
            }
        }

        [ContextMenu("List All Buildings in Scene")]
        private void ListAllBuildings()
        {
            var buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
            Debug.Log($"Found {buildings.Length} buildings in scene:");

            foreach (var building in buildings)
            {
                var selectable = building.GetComponent<BuildingSelectable>();
                var trainingQueue = building.GetComponent<UnitTrainingQueue>();

                Debug.Log($"  - {building.gameObject.name}:");
                Debug.Log($"      Selectable: {(selectable != null ? "✅" : "❌")}");
                Debug.Log($"      TrainingQueue: {(trainingQueue != null ? "✅" : "❌")}");
                Debug.Log($"      Data: {(building.Data != null ? building.Data.buildingName : "NULL")}");
            }
        }
    }
}
