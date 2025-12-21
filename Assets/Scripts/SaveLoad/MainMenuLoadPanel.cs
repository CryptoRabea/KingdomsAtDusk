using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTS.Core.Services;

namespace RTS.SaveLoad
{
    /// <summary>
    /// Main menu load game panel.
    /// Displays save files and allows loading them.
    /// Works independently without requiring ISaveLoadService to be registered.
    /// Should be used in the MainMenu scene.
    /// </summary>
    public class MainMenuLoadPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject loadPanel;
        [SerializeField] private Transform saveListContent;
        [SerializeField] private GameObject saveListItemPrefab;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI noSavesText;

        [Header("Settings")]
        [SerializeField] private SaveLoadSettings settings;
        [SerializeField] private string gameSceneName = "GameScene";

        private List<SaveListItem> saveListItems = new List<SaveListItem>();
        private SaveListItem selectedSaveItem = null;

        private void Awake()
        {
            // Auto-reference self if loadPanel not assigned
            if (loadPanel == null)
                loadPanel = gameObject;

            // Hide panel initially
            if (loadPanel != null)
                loadPanel.SetActive(false);

            // Setup button listeners
            if (loadButton != null)
                loadButton.onClick.AddListener(OnLoadButtonClicked);
            if (deleteButton != null)
                deleteButton.onClick.AddListener(OnDeleteButtonClicked);
            if (backButton != null)
                backButton.onClick.AddListener(ClosePanel);

            UpdateButtonStates();
        }

        private void Start()
        {
            // Try to find settings if not assigned
            if (settings == null)
            {
                settings = Resources.Load<SaveLoadSettings>("SaveLoadSettings");
            }
        }

        public void OpenPanel()
        {
            // Ensure references are set (Awake/Start may not have run if object started disabled)
            if (loadPanel == null)
                loadPanel = gameObject;

            if (settings == null)
                settings = Resources.Load<SaveLoadSettings>("SaveLoadSettings");

            loadPanel.SetActive(true);
            RefreshSaveList();
        }

        public void ClosePanel()
        {
            if (loadPanel != null)
            {
                loadPanel.SetActive(false);
            }
        }

        private void RefreshSaveList()
        {
            if (saveListContent == null)
                return;

            // Clear existing items
            foreach (var item in saveListItems)
            {
                if (item != null && item.gameObject != null)
                    Destroy(item.gameObject);
            }
            saveListItems.Clear();
            selectedSaveItem = null;

            // Get all saves directly from file system
            string[] saves = GetAllSavesFromDisk();

            // Show/hide "no saves" message
            if (noSavesText != null)
            {
                noSavesText.gameObject.SetActive(saves.Length == 0);
            }

            // Create list items
            foreach (var saveName in saves)
            {
                CreateSaveListItem(saveName);
            }

            UpdateButtonStates();
        }

        private string[] GetAllSavesFromDisk()
        {
            string savePath = GetSaveDirectoryPath();
            if (!Directory.Exists(savePath))
            {
                return new string[0];
            }

            string extension = settings != null ? settings.saveFileExtension : ".sav";
            string[] files = Directory.GetFiles(savePath, "*" + extension);
            return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();
        }

        private string GetSaveDirectoryPath()
        {
            string saveDir = settings != null ? settings.saveDirectory : "Saves";
            return Path.Combine(Application.persistentDataPath, saveDir);
        }

        private string GetSaveFilePath(string saveName)
        {
            string extension = settings != null ? settings.saveFileExtension : ".sav";
            string fileName = saveName;
            if (!fileName.EndsWith(extension))
            {
                fileName += extension;
            }
            return Path.Combine(GetSaveDirectoryPath(), fileName);
        }

        private SaveFileInfo GetSaveInfoFromDisk(string saveName)
        {
            try
            {
                string filePath = GetSaveFilePath(saveName);
                if (!File.Exists(filePath))
                {
                    return null;
                }

                FileInfo fileInfo = new FileInfo(filePath);

                // Create basic info from file metadata
                var info = new SaveFileInfo
                {
                    fileName = Path.GetFileName(filePath),
                    saveName = saveName,
                    saveDate = fileInfo.LastWriteTime,
                    fileSize = fileInfo.Length,
                    isAutoSave = saveName.StartsWith("AutoSave"),
                    isQuickSave = saveName.StartsWith("QuickSave")
                };

                // Try to read save data for more details
                try
                {
                    string json = File.ReadAllText(filePath);
                    GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
                    if (saveData != null)
                    {
                        info.playTime = saveData.playTime;
                        info.gameVersion = saveData.gameVersion;
                    }
                }
                catch
                {
                    // Ignore parsing errors, use basic info
                }

                return info;
            }
            catch
            {
                return new SaveFileInfo { saveName = saveName, fileName = saveName };
            }
        }

        private void CreateSaveListItem(string saveName)
        {
            if (saveListItemPrefab == null || saveListContent == null)
                return;

            GameObject itemObj = Instantiate(saveListItemPrefab, saveListContent);
            if (itemObj.TryGetComponent<SaveListItem>(out var item))
            {
                SaveFileInfo info = GetSaveInfoFromDisk(saveName);
                item.Initialize(info ?? new SaveFileInfo { saveName = saveName, fileName = saveName });
                item.OnSelected += OnSaveItemSelected;
                saveListItems.Add(item);
            }
        }

        private void OnSaveItemSelected(SaveListItem item)
        {
            // Deselect previous
            if (selectedSaveItem != null)
            {
                selectedSaveItem.SetSelected(false);
            }

            // Select new
            selectedSaveItem = item;
            selectedSaveItem.SetSelected(true);

            UpdateButtonStates();
        }

        private void OnLoadButtonClicked()
        {
            if (selectedSaveItem == null)
                return;

            string saveName = selectedSaveItem.SaveInfo.saveName;

            // Store the save name to load after scene loads
            PlayerPrefs.SetString("LoadSaveOnStart", saveName);
            PlayerPrefs.Save();

            // Load game scene
            var sceneManager = FindAnyObjectByType<RTS.UI.SceneTransitionManager>();
            if (sceneManager != null)
            {
                sceneManager.LoadGameScene();
            }
            else
            {
                // Fallback to direct scene loading
                UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
            }
        }

        private void OnDeleteButtonClicked()
        {
            if (selectedSaveItem == null)
                return;

            string saveName = selectedSaveItem.SaveInfo.saveName;
            string filePath = GetSaveFilePath(saveName);

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    RefreshSaveList();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete save file: {e.Message}");
            }
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = selectedSaveItem != null;

            if (loadButton != null)
                loadButton.interactable = hasSelection;
            if (deleteButton != null)
                deleteButton.interactable = hasSelection;
        }

        // Public API
        public bool HasSaves()
        {
            if (settings == null)
                settings = Resources.Load<SaveLoadSettings>("SaveLoadSettings");

            string[] saves = GetAllSavesFromDisk();
            return saves != null && saves.Length > 0;
        }
    }
}
