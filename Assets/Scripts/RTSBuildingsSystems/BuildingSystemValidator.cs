using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RTS.Buildings
{
    /// <summary>
    /// Validation script to help diagnose building selection and spawning issues.
    /// Run this from the Unity menu: Tools > RTS > Validate Building System
    /// </summary>
    public class BuildingSystemValidator : MonoBehaviour
    {
        [Header("Run Validation")]
        [SerializeField] private bool runValidationOnStart = false;

        private void Start()
        {
            if (runValidationOnStart)
            {
                ValidateSystem();
            }
        }

        [ContextMenu("Validate Building System")]
        public void ValidateSystem()
        {
            Debug.Log("=== Building System Validation Started ===");

            ValidateBuildingSelectionManager();
            ValidateBuildings();
            ValidateBuildingDetailsUI();

            Debug.Log("=== Building System Validation Complete ===");
        }

        private void ValidateBuildingSelectionManager()
        {
            Debug.Log("\n--- Checking BuildingSelectionManager ---");

            var selectionManager = Object.FindAnyObjectByType<BuildingSelectionManager>();
            if (selectionManager == null)
            {
                Debug.LogError("[ERROR] CRITICAL: No BuildingSelectionManager found in scene!");
                Debug.LogError("   Fix: Add BuildingSelectionManager component to a GameObject (like GameManager)");
                return;
            }

            Debug.Log($"[OK] BuildingSelectionManager found on: {selectionManager.gameObject.name}");

            // Check input actions using reflection
            var clickAction = GetFieldValue<InputActionReference>(selectionManager, "clickAction");
            var rightClickAction = GetFieldValue<InputActionReference>(selectionManager, "rightClickAction");
            var positionAction = GetFieldValue<InputActionReference>(selectionManager, "positionAction");

            if (clickAction == null)
            {
                Debug.LogError("[ERROR] Click Action not assigned!");
                Debug.LogError("   Fix: Assign 'Click' Input Action Reference in inspector");
            }
            else
            {
                Debug.Log($"[OK] Click Action assigned: {clickAction.action?.name ?? "null"}");
            }

            if (rightClickAction == null)
            {
                Debug.LogError("[ERROR] Right Click Action not assigned!");
                Debug.LogError("   Fix: Assign 'Right Click' Input Action Reference in inspector");
            }
            else
            {
                Debug.Log($"[OK] Right Click Action assigned: {rightClickAction.action?.name ?? "null"}");
            }

            if (positionAction == null)
            {
                Debug.LogError("[ERROR] Position Action not assigned!");
                Debug.LogError("   Fix: Assign 'Position' Input Action Reference in inspector");
            }
            else
            {
                Debug.Log($"[OK] Position Action assigned: {positionAction.action?.name ?? "null"}");
            }

            // Check layer masks
            var buildingLayer = GetFieldValue<LayerMask>(selectionManager, "buildingLayer");
            var groundLayer = GetFieldValue<LayerMask>(selectionManager, "groundLayer");

            if (buildingLayer.value == 0)
            {
                Debug.LogError("[ERROR] Building Layer not set!");
                Debug.LogError("   Fix: Set building layer mask in inspector (e.g., 'Building' layer)");
            }
            else
            {
                Debug.Log($"[OK] Building Layer mask: {buildingLayer.value}");
            }

            if (groundLayer.value == 0)
            {
                Debug.LogWarning("[WARNING] Ground Layer not set!");
                Debug.LogWarning("   Fix: Set ground layer mask in inspector (e.g., 'Ground' or 'Default' layer)");
            }
            else
            {
                Debug.Log($"[OK] Ground Layer mask: {groundLayer.value}");
            }

            // Check camera
            var mainCamera = GetFieldValue<Camera>(selectionManager, "mainCamera");
            if (mainCamera == null)
            {
                Debug.LogWarning("[WARNING] Main Camera not assigned (will auto-find Camera.main)");
                if (Camera.main == null)
                {
                    Debug.LogError("[ERROR] CRITICAL: No Camera.main found in scene!");
                }
            }
            else
            {
                Debug.Log($"[OK] Main Camera assigned: {mainCamera.gameObject.name}");
            }
        }

        private void ValidateBuildings()
        {
            Debug.Log("\n--- Checking Buildings ---");

            var buildings = Object.FindObjectsByType<Building>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (buildings.Length == 0)
            {
                Debug.LogWarning("[WARNING] No buildings found in scene");
                return;
            }

            Debug.Log($"Found {buildings.Length} building(s)");

            int validBuildings = 0;
            int invalidBuildings = 0;

            foreach (var building in buildings)
            {
                bool isValid = true;
                string buildingName = building.gameObject.name;

                // Check BuildingSelectable
                if (!building.TryGetComponent<BuildingSelectable>(out var selectable))
                {
                    Debug.LogError($"[ERROR] {buildingName}: Missing BuildingSelectable component!");
                    Debug.LogError($"   Fix: Add BuildingSelectable component to {buildingName}");
                    isValid = false;
                }

                // Check Collider
                if (!building.TryGetComponent<Collider>(out var collider))
                {
                    Debug.LogError($"[ERROR] {buildingName}: Missing Collider component!");
                    Debug.LogError($"   Fix: Add a Collider (BoxCollider, etc.) to {buildingName}");
                    isValid = false;
                }

                // Check Layer
                int buildingLayer = building.gameObject.layer;
                if (buildingLayer == 0) // Default layer
                {
                    Debug.LogWarning($"[WARNING] {buildingName}: On Default layer (should be on 'Building' layer)");
                    Debug.LogWarning($"   Fix: Set layer to 'Building' for {buildingName}");
                }

                // Check UnitTrainingQueue if can train units
                if (building.Data != null && building.Data.canTrainUnits)
                {
                    if (!building.TryGetComponent<UnitTrainingQueue>(out var trainingQueue))
                    {
                        Debug.LogError($"[ERROR] {buildingName}: Can train units but missing UnitTrainingQueue!");
                        Debug.LogError($"   Fix: Add UnitTrainingQueue component to {buildingName}");
                        isValid = false;
                    }
                }

                if (isValid)
                {
                    validBuildings++;
                    Debug.Log($"[OK] {buildingName}: Valid");
                }
                else
                {
                    invalidBuildings++;
                }
            }

            Debug.Log($"\nBuilding Summary: {validBuildings} valid, {invalidBuildings} invalid");
        }

        private void ValidateBuildingDetailsUI()
        {
            Debug.Log("\n--- Checking BuildingDetailsUI ---");

            var ui = FindAnyObjectByType<UI.BuildingDetailsUI>();
            if (ui == null)
            {
                Debug.LogWarning("[WARNING] No BuildingDetailsUI found in scene");
                Debug.LogWarning("   This is optional but recommended for training units");
                return;
            }

            Debug.Log($"[OK] BuildingDetailsUI found on: {ui.gameObject.name}");

            // Check critical references
            var panelRoot = GetFieldValue<GameObject>(ui, "panelRoot");
            if (panelRoot == null)
            {
                Debug.LogError("[ERROR] Panel Root not assigned!");
                Debug.LogError("   Fix: Assign the UI panel GameObject in inspector");
            }
            else
            {
                Debug.Log($"[OK] Panel Root assigned: {panelRoot.name}");
            }
        }

        /// <summary>
        /// Helper to get private field values using reflection
        /// </summary>
        private T GetFieldValue<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);

            if (field != null)
            {
                var value = field.GetValue(obj);
                if (value is T typedValue)
                    return typedValue;
            }

            return default(T);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor menu item to run validation
    /// </summary>
    public static class BuildingSystemValidatorMenu
    {
        [MenuItem("Tools/RTS/Validate Building System")]
        public static void ValidateFromMenu()
        {
            var validator = Object.FindAnyObjectByType<BuildingSystemValidator>();
            if (validator == null)
            {
                // Create temporary validator
                var temp = new GameObject("Temp Validator");
                validator = temp.AddComponent<BuildingSystemValidator>();
                validator.ValidateSystem();
                Object.DestroyImmediate(temp);
            }
            else
            {
                validator.ValidateSystem();
            }
        }
    }
#endif
}
