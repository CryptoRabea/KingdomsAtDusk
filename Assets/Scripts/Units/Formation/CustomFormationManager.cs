using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RTS.Units.Formation
{
    /// <summary>
    /// Manages custom formations including save/load and CRUD operations
    /// </summary>
    public class CustomFormationManager : MonoBehaviour
    {
        private static CustomFormationManager _instance;
        private static bool _applicationIsQuitting = false;

        public static CustomFormationManager Instance
        {
            get
            {
                // Don't create new instances when the application is quitting
                if (_applicationIsQuitting)
                {
                    return null;
                }

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

        // ScriptableObject that holds user custom formation data at runtime
        private UserCustomFormationSettingsSO _userFormationSettings;

        // Events
        public event Action<List<CustomFormationData>> OnFormationsChanged;
        public event Action<CustomFormationData> OnFormationAdded;
        public event Action<CustomFormationData> OnFormationUpdated;
        public event Action<string> OnFormationDeleted;

        /// <summary>
        /// Gets the UserCustomFormationSettingsSO instance
        /// </summary>
        public UserCustomFormationSettingsSO UserFormationSettings => _userFormationSettings;

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
            // Subscribe to play mode state changes to ensure proper cleanup
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#else
            DontDestroyOnLoad(gameObject);
#endif

            Initialize();
        }

#if UNITY_EDITOR
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Reset the quitting flag when entering play mode
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _applicationIsQuitting = false;
            }
            // Clean up when exiting play mode
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                _applicationIsQuitting = true;
                if (_instance == this)
                {
                    _instance = null;
                    EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                    if (gameObject != null)
                    {
                        DestroyImmediate(gameObject);
                    }
                }
            }
        }
#endif

        private void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        private void OnDestroy()
        {
            // Mark as quitting to prevent new instance creation during shutdown
            _applicationIsQuitting = true;

            // Clear the static instance when this object is destroyed
            if (_instance == this)
            {
                _instance = null;
            }

#if UNITY_EDITOR
            // Unsubscribe from editor events
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif

            // Unsubscribe all events to prevent memory leaks
            OnFormationsChanged = null;
            OnFormationAdded = null;
            OnFormationUpdated = null;
            OnFormationDeleted = null;
        }

        private void Initialize()
        {
            _saveFilePath = Path.Combine(Application.persistentDataPath, "CustomFormations.json");

            // Create runtime instance of UserCustomFormationSettingsSO
            _userFormationSettings = UserCustomFormationSettingsSO.CreateRuntimeInstance();

            // Note: We don't forward SO events to avoid double-firing
            // CustomFormationManager handles its own events directly

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

            // Sync to ScriptableObject - reinitialize to avoid duplication
            if (_userFormationSettings != null)
            {
                _userFormationSettings.Initialize(_customFormations.formations);
            }

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

                // Sync to ScriptableObject - reinitialize to avoid duplication
                if (_userFormationSettings != null)
                {
                    _userFormationSettings.Initialize(_customFormations.formations);
                }

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

                // Sync to ScriptableObject
                if (_userFormationSettings != null)
                {
                    _userFormationSettings.Initialize(_customFormations.formations);
                }

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
                // Sync to ScriptableObject
                if (_userFormationSettings != null)
                {
                    _userFormationSettings.Initialize(_customFormations.formations);
                }

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

                // Sync to ScriptableObject
                if (_userFormationSettings != null)
                {
                    _userFormationSettings.Initialize(_customFormations.formations);
                }

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
            catch (Exception)
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
            catch (Exception)
            {
                _customFormations = new CustomFormationsContainer();
            }

            // Initialize the ScriptableObject with loaded formations
            if (_userFormationSettings != null)
            {
                _userFormationSettings.Initialize(_customFormations.formations);
            }

            OnFormationsChanged?.Invoke(_customFormations.formations);
        }

        /// <summary>
        /// Clear all custom formations (with confirmation in production)
        /// </summary>
        public void ClearAllFormations()
        {
            _customFormations.formations.Clear();

            // Sync to ScriptableObject
            if (_userFormationSettings != null)
            {
                _userFormationSettings.Initialize(_customFormations.formations);
            }

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
            catch (Exception)
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

                // Sync to ScriptableObject once after all changes
                if (_userFormationSettings != null)
                {
                    _userFormationSettings.Initialize(_customFormations.formations);
                }

                OnFormationsChanged?.Invoke(_customFormations.formations);
                SaveFormations();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
