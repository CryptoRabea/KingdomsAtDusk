using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RTS.Units.Formation
{
    /// <summary>
    /// Manages custom formations including save/load and CRUD operations
    /// </summary>
    public class CustomFormationManager : MonoBehaviour
    {
        private static CustomFormationManager _instance;
        public static CustomFormationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CustomFormationManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CustomFormationManager");
                        _instance = go.AddComponent<CustomFormationManager>();
#if !UNITY_EDITOR
                        DontDestroyOnLoad(go);
#endif
                    }
                }
                return _instance;
            }
        }

        private CustomFormationsContainer _customFormations;
        private string _saveFilePath;

        // Events
        public event Action<List<CustomFormationData>> OnFormationsChanged;
        public event Action<CustomFormationData> OnFormationAdded;
        public event Action<CustomFormationData> OnFormationUpdated;
        public event Action<string> OnFormationDeleted;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

#if UNITY_EDITOR
            // In editor, don't use DontDestroyOnLoad to avoid cleanup warnings
            // The instance will be recreated when needed
#else
            DontDestroyOnLoad(gameObject);
#endif

            Initialize();
        }

        private void OnDestroy()
        {
            // Clear the static instance when this object is destroyed
            if (_instance == this)
            {
                _instance = null;
            }

            // Unsubscribe all events to prevent memory leaks
            OnFormationsChanged = null;
            OnFormationAdded = null;
            OnFormationUpdated = null;
            OnFormationDeleted = null;
        }

        private void Initialize()
        {
            _saveFilePath = Path.Combine(Application.persistentDataPath, "CustomFormations.json");
            LoadFormations();
        }

        /// <summary>
        /// Get all custom formations
        /// </summary>
        public List<CustomFormationData> GetAllFormations()
        {
            return _customFormations.formations;
        }

        /// <summary>
        /// Get a formation by ID
        /// </summary>
        public CustomFormationData GetFormation(string id)
        {
            return _customFormations.formations.FirstOrDefault(f => f.id == id);
        }

        /// <summary>
        /// Get a formation by name
        /// </summary>
        public CustomFormationData GetFormationByName(string name)
        {
            return _customFormations.formations.FirstOrDefault(f => f.name == name);
        }

        /// <summary>
        /// Create a new custom formation
        /// </summary>
        public CustomFormationData CreateFormation(string name = "New Formation")
        {
            CustomFormationData newFormation = new CustomFormationData(name);
            _customFormations.formations.Add(newFormation);

            OnFormationAdded?.Invoke(newFormation);
            OnFormationsChanged?.Invoke(_customFormations.formations);

            SaveFormations();
            return newFormation;
        }

        /// <summary>
        /// Update an existing formation
        /// </summary>
        public bool UpdateFormation(CustomFormationData updatedFormation)
        {
            int index = _customFormations.formations.FindIndex(f => f.id == updatedFormation.id);
            if (index != -1)
            {
                updatedFormation.modifiedDate = DateTime.Now;
                _customFormations.formations[index] = updatedFormation;

                OnFormationUpdated?.Invoke(updatedFormation);
                OnFormationsChanged?.Invoke(_customFormations.formations);

                SaveFormations();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Rename a formation
        /// </summary>
        public bool RenameFormation(string id, string newName)
        {
            CustomFormationData formation = GetFormation(id);
            if (formation != null)
            {
                formation.name = newName;
                formation.modifiedDate = DateTime.Now;

                OnFormationUpdated?.Invoke(formation);
                OnFormationsChanged?.Invoke(_customFormations.formations);

                SaveFormations();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Delete a formation
        /// </summary>
        public bool DeleteFormation(string id)
        {
            int removed = _customFormations.formations.RemoveAll(f => f.id == id);
            if (removed > 0)
            {
                OnFormationDeleted?.Invoke(id);
                OnFormationsChanged?.Invoke(_customFormations.formations);

                SaveFormations();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Duplicate an existing formation
        /// </summary>
        public CustomFormationData DuplicateFormation(string id)
        {
            CustomFormationData original = GetFormation(id);
            if (original != null)
            {
                CustomFormationData clone = original.Clone();
                _customFormations.formations.Add(clone);

                OnFormationAdded?.Invoke(clone);
                OnFormationsChanged?.Invoke(_customFormations.formations);

                SaveFormations();
                return clone;
            }
            return null;
        }

        /// <summary>
        /// Check if a formation name already exists
        /// </summary>
        public bool FormationNameExists(string name, string excludeId = null)
        {
            return _customFormations.formations.Any(f =>
                f.name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                f.id != excludeId);
        }

        /// <summary>
        /// Get a unique formation name
        /// </summary>
        public string GetUniqueFormationName(string baseName)
        {
            string name = baseName;
            int counter = 1;

            while (FormationNameExists(name))
            {
                name = $"{baseName} ({counter})";
                counter++;
            }

            return name;
        }

        /// <summary>
        /// Save all formations to disk
        /// </summary>
        public void SaveFormations()
        {
            try
            {
                string json = JsonUtility.ToJson(_customFormations, true);
                File.WriteAllText(_saveFilePath, json);
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Load formations from disk
        /// </summary>
        public void LoadFormations()
        {
            try
            {
                if (File.Exists(_saveFilePath))
                {
                    string json = File.ReadAllText(_saveFilePath);
                    _customFormations = JsonUtility.FromJson<CustomFormationsContainer>(json);
                }
                else
                {
                    _customFormations = new CustomFormationsContainer();
                }
            }
            catch (Exception ex)
            {
                _customFormations = new CustomFormationsContainer();
            }

            OnFormationsChanged?.Invoke(_customFormations.formations);
        }

        /// <summary>
        /// Clear all custom formations (with confirmation in production)
        /// </summary>
        public void ClearAllFormations()
        {
            _customFormations.formations.Clear();
            OnFormationsChanged?.Invoke(_customFormations.formations);
            SaveFormations();
        }

        /// <summary>
        /// Export formations to a specific file
        /// </summary>
        public bool ExportFormations(string filePath)
        {
            try
            {
                string json = JsonUtility.ToJson(_customFormations, true);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Import formations from a specific file
        /// </summary>
        public bool ImportFormations(string filePath, bool merge = true)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                string json = File.ReadAllText(filePath);
                CustomFormationsContainer imported = JsonUtility.FromJson<CustomFormationsContainer>(json);

                if (!merge)
                {
                    _customFormations = imported;
                }
                else
                {
                    foreach (var formation in imported.formations)
                    {
                        // Generate new ID to avoid conflicts
                        formation.id = Guid.NewGuid().ToString();
                        // Ensure unique name
                        formation.name = GetUniqueFormationName(formation.name);
                        _customFormations.formations.Add(formation);
                    }
                }

                OnFormationsChanged?.Invoke(_customFormations.formations);
                SaveFormations();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
