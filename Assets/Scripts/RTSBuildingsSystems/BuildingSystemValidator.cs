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

            ValidateBuildingSelectionManager();
            ValidateBuildings();
            ValidateBuildingDetailsUI();

        }

        private void ValidateBuildingSelectionManager()
        {

            var selectionManager = Object.FindAnyObjectByType<BuildingSelectionManager>();
            if (selectionManager == null)
            {
                return;
            }


            // Check input actions using reflection
            var clickAction = GetFieldValue<InputActionReference>(selectionManager, "clickAction");
            var rightClickAction = GetFieldValue<InputActionReference>(selectionManager, "rightClickAction");
            var positionAction = GetFieldValue<InputActionReference>(selectionManager, "positionAction");

            if (clickAction == null)
            {
            }
            else
            {
            }

            if (rightClickAction == null)
            {
            }
            else
            {
            }

            if (positionAction == null)
            {
            }
            else
            {
            }

            // Check layer masks
            var buildingLayer = GetFieldValue<LayerMask>(selectionManager, "buildingLayer");
            var groundLayer = GetFieldValue<LayerMask>(selectionManager, "groundLayer");

            if (buildingLayer.value == 0)
            {
            }
            else
            {
            }

            if (groundLayer.value == 0)
            {
            }
            else
            {
            }

            // Check camera
            var mainCamera = GetFieldValue<Camera>(selectionManager, "mainCamera");
            if (mainCamera == null)
            {
                if (Camera.main == null)
                {
                }
            }
            else
            {
            }
        }

        private void ValidateBuildings()
        {

            var buildings = Object.FindObjectsByType<Building>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (buildings.Length == 0)
            {
                return;
            }


            int validBuildings = 0;
            int invalidBuildings = 0;

            foreach (var building in buildings)
            {
                bool isValid = true;
                string buildingName = building.gameObject.name;

                // Check BuildingSelectable
                if (!building.TryGetComponent<BuildingSelectable>(out var selectable))
                {
                    isValid = false;
                }

                // Check Collider
                if (!building.TryGetComponent<Collider>(out var collider))
                {
                    isValid = false;
                }

                // Check Layer
                int buildingLayer = building.gameObject.layer;
                if (buildingLayer == 0) // Default layer
                {
                }

                // Check UnitTrainingQueue if can train units
                if (building.Data != null && building.Data.canTrainUnits)
                {
                    if (!building.TryGetComponent<UnitTrainingQueue>(out var trainingQueue))
                    {
                        isValid = false;
                    }
                }

                if (isValid)
                {
                    validBuildings++;
                }
                else
                {
                    invalidBuildings++;
                }
            }

        }

        private void ValidateBuildingDetailsUI()
        {

            var ui = FindAnyObjectByType<UI.BuildingDetailsUI>();
            if (ui == null)
            {
                return;
            }


            // Check critical references
            var panelRoot = GetFieldValue<GameObject>(ui, "panelRoot");
            if (panelRoot == null)
            {
            }
            else
            {
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
