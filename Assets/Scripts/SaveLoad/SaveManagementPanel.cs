using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using RTS.Core.Services;

namespace RTS.SaveLoad
{
    /// <summary>
    /// Dedicated panel for managing saves: create, load, rename, duplicate, delete.
    /// Can be used in both main menu and in-game.
    /// </summary>
    public class SaveManagementPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_InputField saveNameInput;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button renameButton;
        [SerializeField] private Button duplicateButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform saveListContent;
        [SerializeField] private GameObject saveListItemPrefab;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmationDialog;
        [SerializeField] private TextMeshProUGUI confirmationText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        [Header("Settings")]
        [SerializeField] private bool pauseGameWhenOpen = true;
        [SerializeField] private bool isMainMenuMode = false; // Set to true if used in main menu

        private ISaveLoadService saveLoadService;
        private IGameStateService gameStateService;
        private List<SaveListItem> saveListItems = new List<SaveListItem>();
        private SaveListItem selectedSaveItem = null;
        private System.Action pendingAction = null;
        private bool isOpen = false;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            SetupButtonListeners();
        }

        private void Start()
        {
            saveLoadService = ServiceLocator.TryGet<ISaveLoadService>();
            if (!isMainMenuMode)
            {
                gameStateService = ServiceLocator.TryGet<IGameStateService>();
            }

            if (saveLoadService == null)
            {
                Debug.LogError("SaveManagementPanel: ISaveLoadService not found!");
            }
        }

        private void SetupButtonListeners()
        {
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveButtonClicked);
            if (loadButton != null)
                loadButton.onClick.AddListener(OnLoadButtonClicked);
            if (renameButton != null)
                renameButton.onClick.AddListener(OnRenameButtonClicked);
            if (duplicateButton != null)
                duplicateButton.onClick.AddListener(OnDuplicateButtonClicked);
            if (deleteButton != null)
                deleteButton.onClick.AddListener(OnDeleteButtonClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePanel);

            if (confirmYesButton != null)
                confirmYesButton.onClick.AddListener(OnConfirmYes);
            if (confirmNoButton != null)
                confirmNoButton.onClick.AddListener(OnConfirmNo);

            UpdateButtonStates();
        }

        public void OpenPanel(bool isLoading = false)
        {
            if (panelRoot == null) return;

            isOpen = true;
            panelRoot.SetActive(true);

            // Update title
            if (titleText != null)
            {
                titleText.text = isLoading ? "Load Game" : "Save / Load Game";
            }

            // Hide save button if in loading mode (main menu)
            if (saveButton != null && isMainMenuMode)
            {
                saveButton.gameObject.SetActive(!isLoading);
            }

            // Pause game if in-game
            if (pauseGameWhenOpen && !isMainMenuMode && gameStateService != null)
            {
                gameStateService.PauseGame();
            }

            RefreshSaveList();
        }

        public void ClosePanel()
        {
            if (panelRoot == null) return;

            isOpen = false;
            panelRoot.SetActive(false);

            // Resume game if in-game
            if (pauseGameWhenOpen && !isMainMenuMode && gameStateService != null)
            {
                gameStateService.ResumeGame();
            }
        }

        private void RefreshSaveList()
        {
            if (saveLoadService == null || saveListContent == null)
                return;

            // Clear existing items
            foreach (var item in saveListItems)
            {
                if (item != null && item.gameObject != null)
                    Destroy(item.gameObject);
            }
            saveListItems.Clear();
            selectedSaveItem = null;

            // Get all saves
            string[] saves = saveLoadService.GetAllSaves();

            // Sort by date (most recent first)
            var sortedSaves = saves
                .Select(name => new { name, info = saveLoadService.GetSaveInfo(name) })
                .Where(x => x.info != null)
                .OrderByDescending(x => x.info.saveDate)
                .Select(x => x.name)
                .ToArray();

            // Create list items
            foreach (var saveName in sortedSaves)
            {
                CreateSaveListItem(saveName);
            }

            UpdateButtonStates();
        }

        private void CreateSaveListItem(string saveName)
        {
            if (saveListItemPrefab == null || saveListContent == null)
                return;

            GameObject itemObj = Instantiate(saveListItemPrefab, saveListContent);
            SaveListItem item = itemObj.GetComponent<SaveListItem>();

            if (item != null)
            {
                SaveFileInfo info = saveLoadService.GetSaveInfo(saveName);
                if (info == null)
                {
                    info = new SaveFileInfo
                    {
                        saveName = saveName,
                        fileName = saveName,
                        saveDate = "Unknown",
                        playTime = 0,
                        gameVersion = Application.version
                    };
                }

                item.Initialize(info);
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

            // Update input field with selected save name (without extension)
            if (saveNameInput != null)
            {
                string displayName = item.SaveInfo.saveName;
                // Remove prefixes like [AUTO] or [QUICK]
                if (item.SaveInfo.isAutoSave)
                    displayName = displayName.Replace("[AUTO] ", "").Replace("AutoSave_", "");
                if (item.SaveInfo.isQuickSave)
                    displayName = displayName.Replace("[QUICK] ", "").Replace("QuickSave", "");

                saveNameInput.text = displayName;
            }

            UpdateButtonStates();
        }

        private void OnSaveButtonClicked()
        {
            if (saveLoadService == null || saveNameInput == null)
                return;

            string saveName = saveNameInput.text.Trim();
            if (string.IsNullOrEmpty(saveName))
            {
                saveName = "Manual_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                Debug.LogWarning("Save name was empty, using default: " + saveName);
            }

            // Check if save already exists
            if (saveLoadService.SaveExists(saveName))
            {
                ShowConfirmation($"Overwrite save '{saveName}'?", () => PerformSave(saveName));
            }
            else
            {
                PerformSave(saveName);
            }
        }

        private void PerformSave(string saveName)
        {
            if (saveLoadService == null)
                return;

            bool success = saveLoadService.SaveGame(saveName);

            if (success)
            {
                Debug.Log($"Game saved: {saveName}");
                RefreshSaveList();
                saveNameInput.text = "";
            }
            else
            {
                Debug.LogError($"Failed to save game: {saveName}");
            }
        }

        private void OnLoadButtonClicked()
        {
            if (saveLoadService == null || selectedSaveItem == null)
                return;

            string saveName = selectedSaveItem.SaveInfo.saveName;

            // Show confirmation if in-game
            if (!isMainMenuMode)
            {
                ShowConfirmation($"Load save '{saveName}'? Unsaved progress will be lost.", () => PerformLoad(saveName));
            }
            else
            {
                PerformLoad(saveName);
            }
        }

        private void PerformLoad(string saveName)
        {
            if (saveLoadService == null)
                return;

            bool success = saveLoadService.LoadGame(saveName);

            if (success)
            {
                Debug.Log($"Game loaded: {saveName}");
                ClosePanel();
            }
            else
            {
                Debug.LogError($"Failed to load game: {saveName}");
            }
        }

        private void OnRenameButtonClicked()
        {
            if (saveLoadService == null || selectedSaveItem == null || saveNameInput == null)
                return;

            string oldName = selectedSaveItem.SaveInfo.saveName;
            string newName = saveNameInput.text.Trim();

            if (string.IsNullOrEmpty(newName))
            {
                Debug.LogWarning("Cannot rename to empty name!");
                return;
            }

            if (newName == oldName)
            {
                Debug.LogWarning("New name is the same as old name!");
                return;
            }

            if (saveLoadService.SaveExists(newName))
            {
                Debug.LogWarning($"A save with name '{newName}' already exists!");
                return;
            }

            // Rename by loading and resaving
            try
            {
                string oldPath = ((SaveLoadSettings)Resources.LoadAll<SaveLoadSettings>("")[0])?.GetSaveFilePath(oldName);
                string newPath = ((SaveLoadSettings)Resources.LoadAll<SaveLoadSettings>("")[0])?.GetSaveFilePath(newName);

                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Move(oldPath, newPath);
                    Debug.Log($"Renamed save from '{oldName}' to '{newName}'");
                    RefreshSaveList();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to rename save: {ex.Message}");
            }
        }

        private void OnDuplicateButtonClicked()
        {
            if (saveLoadService == null || selectedSaveItem == null || saveNameInput == null)
                return;

            string sourceName = selectedSaveItem.SaveInfo.saveName;
            string newName = saveNameInput.text.Trim();

            if (string.IsNullOrEmpty(newName))
            {
                newName = sourceName + "_Copy";
            }

            if (saveLoadService.SaveExists(newName))
            {
                Debug.LogWarning($"A save with name '{newName}' already exists!");
                return;
            }

            // Duplicate by copying file
            try
            {
                var settings = Resources.LoadAll<SaveLoadSettings>("").FirstOrDefault();
                if (settings != null)
                {
                    string sourcePath = settings.GetSaveFilePath(sourceName);
                    string newPath = settings.GetSaveFilePath(newName);

                    if (System.IO.File.Exists(sourcePath))
                    {
                        System.IO.File.Copy(sourcePath, newPath);
                        Debug.Log($"Duplicated save '{sourceName}' to '{newName}'");
                        RefreshSaveList();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to duplicate save: {ex.Message}");
            }
        }

        private void OnDeleteButtonClicked()
        {
            if (saveLoadService == null || selectedSaveItem == null)
                return;

            string saveName = selectedSaveItem.SaveInfo.saveName;
            ShowConfirmation($"Delete save '{saveName}'? This cannot be undone!", () => PerformDelete(saveName));
        }

        private void PerformDelete(string saveName)
        {
            if (saveLoadService == null)
                return;

            bool success = saveLoadService.DeleteSave(saveName);

            if (success)
            {
                Debug.Log($"Save deleted: {saveName}");
                RefreshSaveList();
                saveNameInput.text = "";
            }
            else
            {
                Debug.LogError($"Failed to delete save: {saveName}");
            }
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = selectedSaveItem != null;
            bool hasInputText = saveNameInput != null && !string.IsNullOrEmpty(saveNameInput.text.Trim());

            if (loadButton != null)
                loadButton.interactable = hasSelection;
            if (renameButton != null)
                renameButton.interactable = hasSelection && hasInputText;
            if (duplicateButton != null)
                duplicateButton.interactable = hasSelection;
            if (deleteButton != null)
                deleteButton.interactable = hasSelection;
            if (saveButton != null)
                saveButton.interactable = !isMainMenuMode; // Only allow saving in-game
        }

        private void ShowConfirmation(string message, System.Action onConfirm)
        {
            if (confirmationDialog == null) return;

            pendingAction = onConfirm;
            if (confirmationText != null)
                confirmationText.text = message;

            confirmationDialog.SetActive(true);
        }

        private void OnConfirmYes()
        {
            if (confirmationDialog != null)
                confirmationDialog.SetActive(false);

            pendingAction?.Invoke();
            pendingAction = null;
        }

        private void OnConfirmNo()
        {
            if (confirmationDialog != null)
                confirmationDialog.SetActive(false);

            pendingAction = null;
        }

        // Public API
        public void SetMainMenuMode(bool isMainMenu)
        {
            isMainMenuMode = isMainMenu;
        }
    }
}
