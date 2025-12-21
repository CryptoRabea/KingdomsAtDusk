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
    /// Main menu load game panel - simplified and self-contained.
    /// Works independently without requiring any external assets.
    /// </summary>
    public class MainMenuLoadPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform saveListContent;
        [SerializeField] private GameObject saveListItemPrefab;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI noSavesText;

        [Header("Settings")]
        [SerializeField] private string saveDirectory = "Saves";
        [SerializeField] private string saveFileExtension = ".sav";
        [SerializeField] private string gameSceneName = "GameScene";

        private List<SaveListItem> saveListItems = new List<SaveListItem>();
        private SaveListItem selectedSaveItem = null;
        private bool isInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;

            // Setup button listeners
            if (loadButton != null)
                loadButton.onClick.AddListener(OnLoadButtonClicked);
            if (deleteButton != null)
                deleteButton.onClick.AddListener(OnDeleteButtonClicked);
            if (backButton != null)
                backButton.onClick.AddListener(ClosePanel);

            UpdateButtonStates();
        }

        /// <summary>
        /// Opens the load panel and refreshes the save list.
        /// Call this from MainMenuManager when the Continue/Load button is clicked.
        /// </summary>
        public void OpenPanel()
        {
            Debug.Log("[MainMenuLoadPanel] OpenPanel called");

            // Ensure initialized
            Initialize();

            // Show the panel (this GameObject)
            gameObject.SetActive(true);

            Debug.Log($"[MainMenuLoadPanel] Panel active: {gameObject.activeSelf}");

            // Refresh the save list
            RefreshSaveList();
        }

        /// <summary>
        /// Closes the load panel.
        /// </summary>
        public void ClosePanel()
        {
            Debug.Log("[MainMenuLoadPanel] ClosePanel called");
            gameObject.SetActive(false);

            // Return to main menu
            var mainMenuManager = FindAnyObjectByType<RTS.UI.MainMenuManager>();
            if (mainMenuManager != null)
            {
                mainMenuManager.ReturnToMainMenu();
            }
        }

        private void RefreshSaveList()
        {
            Debug.Log("[MainMenuLoadPanel] RefreshSaveList called");

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
            Debug.Log($"[MainMenuLoadPanel] Found {saves.Length} save files");

            // Show/hide "no saves" message
            if (noSavesText != null)
            {
                noSavesText.gameObject.SetActive(saves.Length == 0);
            }

            // Create list items
            if (saveListContent != null)
            {
                foreach (var saveName in saves)
                {
                    CreateSaveListItem(saveName);
                }
            }
            else
            {
                Debug.LogWarning("[MainMenuLoadPanel] saveListContent is not assigned!");
            }

            UpdateButtonStates();
        }

        private string[] GetAllSavesFromDisk()
        {
            string savePath = GetSaveDirectoryPath();
            Debug.Log($"[MainMenuLoadPanel] Looking for saves in: {savePath}");

            if (!Directory.Exists(savePath))
            {
                Debug.Log($"[MainMenuLoadPanel] Save directory does not exist");
                return new string[0];
            }

            try
            {
                string[] files = Directory.GetFiles(savePath, "*" + saveFileExtension);
                Debug.Log($"[MainMenuLoadPanel] Found files: {string.Join(", ", files.Select(Path.GetFileName))}");
                return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MainMenuLoadPanel] Error reading save directory: {e.Message}");
                return new string[0];
            }
        }

        private string GetSaveDirectoryPath()
        {
            return Path.Combine(Application.persistentDataPath, saveDirectory);
        }

        private string GetSaveFilePath(string saveName)
        {
            string fileName = saveName;
            if (!fileName.EndsWith(saveFileExtension))
            {
                fileName += saveFileExtension;
            }
            return Path.Combine(GetSaveDirectoryPath(), fileName);
        }

        private SaveFileInfo GetSaveInfoFromDisk(string saveName)
        {
            try
            {
                string filePath = GetSaveFilePath(saveName);
                if (!File.Exists(filePath))
                    return null;

                FileInfo fileInfo = new FileInfo(filePath);

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
            if (saveListContent == null)
            {
                Debug.LogWarning("[MainMenuLoadPanel] Cannot create save list item - saveListContent is null");
                return;
            }

            GameObject itemObj = null;

            // Use prefab if available, otherwise create a simple button
            if (saveListItemPrefab != null)
            {
                itemObj = Instantiate(saveListItemPrefab, saveListContent);
            }
            else
            {
                // Create a simple fallback UI element
                itemObj = CreateSimpleSaveButton(saveName);
            }

            if (itemObj == null) return;

            // Try to get SaveListItem component
            if (itemObj.TryGetComponent<SaveListItem>(out var item))
            {
                SaveFileInfo info = GetSaveInfoFromDisk(saveName);
                item.Initialize(info ?? new SaveFileInfo { saveName = saveName, fileName = saveName });
                item.OnSelected += OnSaveItemSelected;
                saveListItems.Add(item);
            }
            else
            {
                // If no SaveListItem component, add click handler directly
                var button = itemObj.GetComponent<Button>();
                if (button != null)
                {
                    string capturedSaveName = saveName;
                    button.onClick.AddListener(() => OnSimpleSaveSelected(capturedSaveName, itemObj));
                }
            }
        }

        private GameObject CreateSimpleSaveButton(string saveName)
        {
            // Create a simple button for the save entry
            GameObject buttonObj = new GameObject($"Save_{saveName}");
            buttonObj.transform.SetParent(saveListContent, false);

            // Add RectTransform
            var rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 60);

            // Add LayoutElement for vertical layout
            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.minHeight = 60;
            layoutElement.preferredHeight = 60;

            // Add background image
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            // Add button
            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;

            // Create text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            var info = GetSaveInfoFromDisk(saveName);
            text.text = $"{saveName}\n<size=12>{info?.saveDate.ToString("g") ?? "Unknown date"}</size>";
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;

            return buttonObj;
        }

        private string selectedSimpleSaveName = null;
        private GameObject selectedSimpleButton = null;

        private void OnSimpleSaveSelected(string saveName, GameObject buttonObj)
        {
            Debug.Log($"[MainMenuLoadPanel] Selected save: {saveName}");

            // Reset previous selection color
            if (selectedSimpleButton != null)
            {
                var prevImage = selectedSimpleButton.GetComponent<Image>();
                if (prevImage != null) prevImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }

            // Set new selection
            selectedSimpleSaveName = saveName;
            selectedSimpleButton = buttonObj;

            // Highlight selected
            var image = buttonObj.GetComponent<Image>();
            if (image != null) image.color = new Color(0.2f, 0.5f, 0.2f, 1f);

            UpdateButtonStates();
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

            Debug.Log($"[MainMenuLoadPanel] Selected: {item.SaveInfo.saveName}");
            UpdateButtonStates();
        }

        private void OnLoadButtonClicked()
        {
            string saveName = null;

            if (selectedSaveItem != null)
            {
                saveName = selectedSaveItem.SaveInfo.saveName;
            }
            else if (!string.IsNullOrEmpty(selectedSimpleSaveName))
            {
                saveName = selectedSimpleSaveName;
            }

            if (string.IsNullOrEmpty(saveName))
            {
                Debug.LogWarning("[MainMenuLoadPanel] No save selected");
                return;
            }

            Debug.Log($"[MainMenuLoadPanel] Loading save: {saveName}");

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
                Debug.Log("[MainMenuLoadPanel] No SceneTransitionManager found, loading directly");
                UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
            }
        }

        private void OnDeleteButtonClicked()
        {
            string saveName = null;

            if (selectedSaveItem != null)
            {
                saveName = selectedSaveItem.SaveInfo.saveName;
            }
            else if (!string.IsNullOrEmpty(selectedSimpleSaveName))
            {
                saveName = selectedSimpleSaveName;
            }

            if (string.IsNullOrEmpty(saveName))
                return;

            string filePath = GetSaveFilePath(saveName);
            Debug.Log($"[MainMenuLoadPanel] Deleting save: {filePath}");

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
                Debug.LogError($"[MainMenuLoadPanel] Failed to delete save file: {e.Message}");
            }
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = selectedSaveItem != null || !string.IsNullOrEmpty(selectedSimpleSaveName);

            if (loadButton != null)
                loadButton.interactable = hasSelection;
            if (deleteButton != null)
                deleteButton.interactable = hasSelection;
        }

        /// <summary>
        /// Checks if there are any save files available.
        /// Called by MainMenuManager to determine if Continue button should be enabled.
        /// </summary>
        public bool HasSaves()
        {
            string[] saves = GetAllSavesFromDisk();
            bool hasSaves = saves != null && saves.Length > 0;
            Debug.Log($"[MainMenuLoadPanel] HasSaves: {hasSaves}");
            return hasSaves;
        }
    }
}
