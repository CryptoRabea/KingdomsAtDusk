using UnityEngine;
using System.Collections.Generic;
using System;

namespace RTS.Units.Formation
{
    /// <summary>
    /// ScriptableObject that stores user-created custom formations.
    /// This is created at runtime and populated from saved JSON data.
    /// Provides a structured way to manage user formations alongside default formations.
    /// </summary>
    [CreateAssetMenu(fileName = "UserCustomFormationSettings", menuName = "RTS/User Custom Formation Settings")]
    public class UserCustomFormationSettingsSO : ScriptableObject
    {
        [Header("User Custom Formations")]
        [SerializeField]
        private List<CustomFormationData> customFormations = new List<CustomFormationData>();

        [Header("Settings")]
        [SerializeField]
        private float defaultCustomFormationSpacing = 2.5f;

        [SerializeField]
        private int maxCustomFormations = 50;

        // Events for formation changes
        public event Action<List<CustomFormationData>> OnFormationsChanged;
        public event Action<CustomFormationData> OnFormationAdded;
        public event Action<CustomFormationData> OnFormationUpdated;
        public event Action<string> OnFormationDeleted;

        /// <summary>
        /// Gets all custom formations
        /// </summary>
        public List<CustomFormationData> CustomFormations => new List<CustomFormationData>(customFormations);

        /// <summary>
        /// Gets formations that should appear in the quick list dropdown
        /// </summary>
        public List<CustomFormationData> QuickListFormations
        {
            get
            {
                return customFormations.FindAll(f => f.isInQuickList);
            }
        }

        /// <summary>
        /// Gets the default spacing for custom formations
        /// </summary>
        public float DefaultCustomFormationSpacing => defaultCustomFormationSpacing;

        /// <summary>
        /// Gets the maximum number of custom formations allowed
        /// </summary>
        public int MaxCustomFormations => maxCustomFormations;

        /// <summary>
        /// Gets the total number of custom formations
        /// </summary>
        public int FormationCount => customFormations.Count;

        /// <summary>
        /// Initializes the SO with formations from saved data
        /// </summary>
        public void Initialize(List<CustomFormationData> formations)
        {
            customFormations = formations ?? new List<CustomFormationData>();
            OnFormationsChanged?.Invoke(customFormations);
        }

        /// <summary>
        /// Gets a formation by its ID
        /// </summary>
        public CustomFormationData GetFormation(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return customFormations.Find(f => f.id == id);
        }

        /// <summary>
        /// Adds a new formation
        /// </summary>
        public bool AddFormation(CustomFormationData formation)
        {
            if (formation == null)
                return false;

            if (customFormations.Count >= maxCustomFormations)
            {
                Debug.LogWarning($"Cannot add formation: Maximum limit of {maxCustomFormations} reached");
                return false;
            }

            if (customFormations.Exists(f => f.id == formation.id))
            {
                Debug.LogWarning($"Formation with ID {formation.id} already exists");
                return false;
            }

            customFormations.Add(formation);
            OnFormationAdded?.Invoke(formation);
            OnFormationsChanged?.Invoke(customFormations);
            return true;
        }

        /// <summary>
        /// Updates an existing formation
        /// </summary>
        public bool UpdateFormation(CustomFormationData updatedFormation)
        {
            if (updatedFormation == null)
                return false;

            int index = customFormations.FindIndex(f => f.id == updatedFormation.id);
            if (index == -1)
            {
                Debug.LogWarning($"Formation with ID {updatedFormation.id} not found");
                return false;
            }

            updatedFormation.modifiedDate = DateTime.Now;
            customFormations[index] = updatedFormation;
            OnFormationUpdated?.Invoke(updatedFormation);
            OnFormationsChanged?.Invoke(customFormations);
            return true;
        }

        /// <summary>
        /// Removes a formation by ID
        /// </summary>
        public bool RemoveFormation(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            int removed = customFormations.RemoveAll(f => f.id == id);
            if (removed > 0)
            {
                OnFormationDeleted?.Invoke(id);
                OnFormationsChanged?.Invoke(customFormations);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a formation name already exists
        /// </summary>
        public bool FormationNameExists(string name, string excludeId = null)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return customFormations.Exists(f =>
                f.name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                f.id != excludeId);
        }

        /// <summary>
        /// Generates a unique formation name
        /// </summary>
        public string GetUniqueFormationName(string baseName)
        {
            if (string.IsNullOrEmpty(baseName))
                baseName = "Custom Formation";

            string uniqueName = baseName;
            int counter = 1;

            while (FormationNameExists(uniqueName))
            {
                uniqueName = $"{baseName} ({counter})";
                counter++;
            }

            return uniqueName;
        }

        /// <summary>
        /// Clears all formations
        /// </summary>
        public void ClearAllFormations()
        {
            customFormations.Clear();
            OnFormationsChanged?.Invoke(customFormations);
        }

        /// <summary>
        /// Sorts formations by name
        /// </summary>
        public void SortFormationsByName()
        {
            customFormations.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            OnFormationsChanged?.Invoke(customFormations);
        }

        /// <summary>
        /// Sorts formations by creation date
        /// </summary>
        public void SortFormationsByDate()
        {
            customFormations.Sort((a, b) => DateTime.Compare(b.createdDate, a.createdDate));
            OnFormationsChanged?.Invoke(customFormations);
        }

        /// <summary>
        /// Gets formations sorted by most recently modified
        /// </summary>
        public List<CustomFormationData> GetRecentFormations(int count = 5)
        {
            var sorted = new List<CustomFormationData>(customFormations);
            sorted.Sort((a, b) => DateTime.Compare(b.modifiedDate, a.modifiedDate));
            return sorted.GetRange(0, Mathf.Min(count, sorted.Count));
        }

        /// <summary>
        /// Validates all formations and removes invalid ones
        /// </summary>
        public int ValidateAndCleanup()
        {
            int removedCount = customFormations.RemoveAll(f =>
                f == null ||
                string.IsNullOrEmpty(f.id) ||
                f.positions == null ||
                f.positions.Count == 0);

            if (removedCount > 0)
            {
                OnFormationsChanged?.Invoke(customFormations);
            }

            return removedCount;
        }

        /// <summary>
        /// Creates a runtime instance (useful for runtime-only data)
        /// </summary>
        public static UserCustomFormationSettingsSO CreateRuntimeInstance()
        {
            var instance = CreateInstance<UserCustomFormationSettingsSO>();
            instance.customFormations = new List<CustomFormationData>();
            return instance;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor utility to create a new formation settings asset
        /// </summary>
        [UnityEditor.MenuItem("Assets/Create/RTS/User Custom Formation Settings (Runtime)", priority = 1)]
        public static void CreateAsset()
        {
            var asset = CreateInstance<UserCustomFormationSettingsSO>();
            UnityEditor.AssetDatabase.CreateAsset(asset, "Assets/UserCustomFormationSettings.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.EditorUtility.FocusProjectWindow();
            UnityEditor.Selection.activeObject = asset;
        }
#endif
    }
}
