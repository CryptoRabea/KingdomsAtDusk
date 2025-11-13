using UnityEngine;
using RTS.Buildings;
using RTS.UI;
using UnityEngine.InputSystem;

namespace RTS.Debugging
{
    /// <summary>
    /// Quick diagnostic tool to check why Building Details Panel isn't opening.
    /// Add this to any GameObject in the scene temporarily for debugging.
    /// </summary>
    public class BuildingDetailsDiagnostic : MonoBehaviour
    {
        [Header("Run Diagnostics")]
        [SerializeField] private bool runOnStart = true;

        private void Start()
        {
            if (runOnStart)
            {
                Invoke(nameof(RunDiagnostics), 1f); // Delay to let everything initialize
            }
        }

        [ContextMenu("Run Full Diagnostics")]
        public void RunDiagnostics()
        {
            Debug.Log("========================================");
            Debug.Log("🔍 BUILDING DETAILS PANEL DIAGNOSTICS");
            Debug.Log("========================================\n");

            CheckBuildingDetailsUI();
            CheckBuildingSelectionManager();
            CheckBuildings();
            CheckInputSystem();

            Debug.Log("\n========================================");
            Debug.Log("✅ DIAGNOSTICS COMPLETE");
            Debug.Log("========================================");
        }

        private void CheckBuildingDetailsUI()
        {
            Debug.Log("--- 1. Checking BuildingDetailsUI ---");

            var detailsUI = FindFirstObjectByType<BuildingDetailsUI>();
            if (detailsUI == null)
            {
                Debug.LogError("❌ PROBLEM FOUND: BuildingDetailsUI component NOT found in scene!");
                Debug.LogError("   FIX: Go to Tools > RTS > Setup Building Training UI");
                Debug.LogError("   Then click 'Create Both (Recommended)'");
                return;
            }

            Debug.Log($"✅ BuildingDetailsUI found on: {detailsUI.gameObject.name}");
            Debug.Log($"   - Component enabled: {detailsUI.enabled}");
            Debug.Log($"   - GameObject active: {detailsUI.gameObject.activeInHierarchy}");

            // Check GameObject structure
            Transform panelChild = detailsUI.transform.Find("BuildingDetailsPanel");
            if (panelChild == null)
            {
                Debug.LogError("❌ PROBLEM FOUND: BuildingDetailsPanel child not found!");
                Debug.LogError("   EXPECTED STRUCTURE:");
                Debug.LogError("   BuildingDetailsUI (GameObject, ACTIVE)");
                Debug.LogError("   └─ BuildingDetailsPanel (GameObject, INACTIVE)");
                Debug.LogError("   FIX: Delete and recreate using Tools > RTS > Setup Building Training UI");
            }
            else
            {
                Debug.Log($"✅ Correct structure found: {detailsUI.gameObject.name} -> {panelChild.name}");
                Debug.Log($"   - Parent active: {detailsUI.gameObject.activeSelf}");
                Debug.Log($"   - Child active: {panelChild.gameObject.activeSelf}");

                if (detailsUI.gameObject.activeSelf == false)
                {
                    Debug.LogError("❌ PROBLEM FOUND: BuildingDetailsUI GameObject is INACTIVE!");
                    Debug.LogError("   The component GameObject must be ACTIVE to receive events!");
                    Debug.LogError("   FIX: Activate the BuildingDetailsUI GameObject in the hierarchy");
                }
            }

            // Check panelRoot using reflection
            var panelRootField = typeof(BuildingDetailsUI).GetField("panelRoot",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (panelRootField != null)
            {
                var panelRoot = panelRootField.GetValue(detailsUI) as GameObject;
                if (panelRoot == null)
                {
                    Debug.LogError("❌ PROBLEM FOUND: panelRoot reference is NULL!");
                    Debug.LogError("   FIX: Run Tools > RTS > Setup Building Training UI again");
                }
                else
                {
                    Debug.Log($"✅ PanelRoot reference assigned: {panelRoot.name}");
                    Debug.Log($"   - Panel should start INACTIVE and show when building selected");
                }
            }

            // Check trainUnitButtonPrefab
            var prefabField = typeof(BuildingDetailsUI).GetField("trainUnitButtonPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (prefabField != null)
            {
                var prefab = prefabField.GetValue(detailsUI) as GameObject;
                if (prefab == null)
                {
                    Debug.LogWarning("⚠️ trainUnitButtonPrefab is NULL!");
                    Debug.LogWarning("   This is needed for buildings that train units.");
                    Debug.LogWarning("   FIX: Run Tools > RTS > Setup Building Training UI");
                }
                else
                {
                    Debug.Log($"✅ trainUnitButtonPrefab assigned: {prefab.name}");
                }
            }

            Debug.Log("");
        }

        private void CheckBuildingSelectionManager()
        {
            Debug.Log("--- 2. Checking BuildingSelectionManager ---");

            var selectionManager = FindFirstObjectByType<BuildingSelectionManager>();
            if (selectionManager == null)
            {
                Debug.LogError("❌ PROBLEM FOUND: BuildingSelectionManager NOT found in scene!");
                Debug.LogError("   FIX: Add BuildingSelectionManager component to a GameObject");
                Debug.LogError("   (e.g., GameManager or InputManager)");
                return;
            }

            Debug.Log($"✅ BuildingSelectionManager found on: {selectionManager.gameObject.name}");
            Debug.Log($"   - Component enabled: {selectionManager.enabled}");
            Debug.Log($"   - GameObject active: {selectionManager.gameObject.activeInHierarchy}");

            // Check input actions using reflection
            var clickActionField = typeof(BuildingSelectionManager).GetField("clickAction",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var positionActionField = typeof(BuildingSelectionManager).GetField("positionAction",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (clickActionField != null)
            {
                var clickAction = clickActionField.GetValue(selectionManager) as InputActionReference;
                if (clickAction == null)
                {
                    Debug.LogError("❌ PROBLEM FOUND: clickAction is NULL!");
                    Debug.LogError("   FIX: Assign 'Click' action from your Input Action Asset");
                }
                else
                {
                    Debug.Log($"✅ clickAction assigned");
                }
            }

            if (positionActionField != null)
            {
                var positionAction = positionActionField.GetValue(selectionManager) as InputActionReference;
                if (positionAction == null)
                {
                    Debug.LogError("❌ PROBLEM FOUND: positionAction is NULL!");
                    Debug.LogError("   FIX: Assign 'Position' action from your Input Action Asset");
                }
                else
                {
                    Debug.Log($"✅ positionAction assigned");
                }
            }

            // Check buildingLayer
            var buildingLayerField = typeof(BuildingSelectionManager).GetField("buildingLayer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (buildingLayerField != null)
            {
                var buildingLayer = (LayerMask)buildingLayerField.GetValue(selectionManager);
                if (buildingLayer.value == 0)
                {
                    Debug.LogError("❌ PROBLEM FOUND: buildingLayer is NOT SET (value = 0)!");
                    Debug.LogError("   FIX: Set buildingLayer in BuildingSelectionManager inspector");
                    Debug.LogError("   Should be set to your 'Building' layer");
                }
                else
                {
                    Debug.Log($"✅ buildingLayer set: {buildingLayer.value}");
                }
            }

            Debug.Log("");
        }

        private void CheckBuildings()
        {
            Debug.Log("--- 3. Checking Buildings in Scene ---");

            var buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
            if (buildings.Length == 0)
            {
                Debug.LogWarning("⚠️ No Building components found in scene!");
                Debug.LogWarning("   This is OK if you haven't placed any buildings yet.");
                Debug.Log("");
                return;
            }

            Debug.Log($"Found {buildings.Length} buildings in scene:");

            int missingComponents = 0;
            int missingData = 0;
            int missingCollider = 0;
            int wrongLayer = 0;

            foreach (var building in buildings)
            {
                var selectable = building.GetComponent<BuildingSelectable>();
                var collider = building.GetComponent<Collider>();
                bool hasData = building.Data != null;

                string status = "✅";
                string issues = "";

                if (selectable == null)
                {
                    status = "❌";
                    issues += " Missing BuildingSelectable!";
                    missingComponents++;
                }

                if (!hasData)
                {
                    status = "❌";
                    issues += " Missing BuildingData!";
                    missingData++;
                }

                if (collider == null)
                {
                    status = "⚠️";
                    issues += " Missing Collider!";
                    missingCollider++;
                }

                // Check layer
                if (building.gameObject.layer != LayerMask.NameToLayer("Building"))
                {
                    status = "⚠️";
                    issues += $" Wrong layer ({LayerMask.LayerToName(building.gameObject.layer)})!";
                    wrongLayer++;
                }

                Debug.Log($"  {status} {building.gameObject.name}{issues}");
            }

            if (missingComponents > 0)
            {
                Debug.LogError($"\n❌ PROBLEM FOUND: {missingComponents} building(s) missing BuildingSelectable component!");
                Debug.LogError("   FIX: Add BuildingSelectable component to each building GameObject");
            }

            if (missingData > 0)
            {
                Debug.LogError($"\n❌ PROBLEM FOUND: {missingData} building(s) missing BuildingData!");
                Debug.LogError("   FIX: Assign a BuildingDataSO to the Building component");
            }

            if (missingCollider > 0)
            {
                Debug.LogWarning($"\n⚠️ WARNING: {missingCollider} building(s) missing Collider!");
                Debug.LogWarning("   FIX: Add a Collider component (Box/Mesh) to make building clickable");
            }

            if (wrongLayer > 0)
            {
                Debug.LogWarning($"\n⚠️ WARNING: {wrongLayer} building(s) on wrong layer!");
                Debug.LogWarning("   FIX: Set GameObject layer to 'Building'");
                Debug.LogWarning("   (Create 'Building' layer if it doesn't exist)");
            }

            Debug.Log("");
        }

        private void CheckInputSystem()
        {
            Debug.Log("--- 4. Checking Input System ---");

            // Check if new Input System is enabled
            #if ENABLE_INPUT_SYSTEM
            Debug.Log("✅ New Input System is enabled");
            #else
            Debug.LogError("❌ PROBLEM FOUND: New Input System is NOT enabled!");
            Debug.LogError("   FIX: Go to Edit > Project Settings > Player");
            Debug.LogError("   Set 'Active Input Handling' to 'Input System Package (New)' or 'Both'");
            #endif

            // Check if Input Action Asset exists
            var inputAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            if (inputAssets.Length == 0)
            {
                Debug.LogWarning("⚠️ No InputActionAsset found in project!");
                Debug.LogWarning("   You may need to create one with 'Click' and 'Position' actions");
            }
            else
            {
                Debug.Log($"✅ Found {inputAssets.Length} InputActionAsset(s)");
            }

            Debug.Log("");
        }

        [ContextMenu("List Event Subscribers")]
        public void ListEventSubscribers()
        {
            Debug.Log("--- Event System Check ---");

            // This would require accessing EventBus internals
            // For now, just check if BuildingDetailsUI is subscribing
            var detailsUI = FindFirstObjectByType<BuildingDetailsUI>();
            if (detailsUI != null && detailsUI.enabled)
            {
                Debug.Log("✅ BuildingDetailsUI is enabled and should be subscribed to events");
            }
            else
            {
                Debug.LogError("❌ BuildingDetailsUI is either missing or disabled!");
            }
        }
    }
}
