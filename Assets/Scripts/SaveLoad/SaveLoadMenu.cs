using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using RTS.Core.Services;
using RTSGame.UI.Settings;

namespace RTS.SaveLoad
{
    /// <summary>
    /// In-game save/load menu UI controller.
    /// Opens with F10/ESC, pauses game, allows save/load operations.
    /// Supports separate Save and Load modes with dedicated UI sections.
    /// </summary>
    public class SaveLoadMenu : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject savePanel;
        [SerializeField] private GameObject loadPanel;

        [Header("Save Panel UI")]
        [SerializeField] private TMP_InputField saveNameInput;
        [SerializeField] private Button performSaveButton;
        [SerializeField] private Button cancelSaveButton;
        [SerializeField] private Transform saveListContentSave;
        [SerializeField] private GameObject saveListItemPrefab;
        [SerializeField] private TextMeshProUGUI savePanelTitle;

        [Header("Load Panel UI")]
        [SerializeField] private Transform saveListContentLoad;
        [SerializeField] private Button performLoadButton;
        [SerializeField] private Button cancelLoadButton;
        [SerializeField] private TextMeshProUGUI loadPanelTitle;

        [Header("Shared Buttons")]
        [SerializeField] private Button showSavePanelButton;
        [SerializeField] private Button showLoadPanelButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button renameButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button backToMainMenuButton;
        [SerializeField] private Button saveAndQuitButton;
        [SerializeField] private Button quitWithoutSavingButton;
        [SerializeField] private Button closeButton;

        [Header("Settings Panel (Optional)")]
        [SerializeField] private SettingsPanel settingsPanelController;
        [SerializeField] private GameObject settingsPanel;

        [Header("Settings")]
        [SerializeField] private bool pauseGameWhenOpen = true;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private ISaveLoadService saveLoadService;
        private IGameStateService gameStateService;
        private List<SaveListItem> saveListItems = new List<SaveListItem>();
        private SaveListItem selectedSaveItem = null;
        private bool isOpen = false;
        private float previousTimeScale = 1f;

        public enum MenuMode { Main, Save, Load }
        private MenuMode currentMode = MenuMode.Main;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            // Hide menu initially
            if (menuPanel != null)
                menuPanel.SetActive(false);
            if (savePanel != null)
                savePanel.SetActive(false);
            if (loadPanel != null)
                loadPanel.SetActive(false);

            // Setup button listeners
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeButtonClicked);
            if (showSavePanelButton != null)
                showSavePanelButton.onClick.AddListener(() => ShowMode(MenuMode.Save));
            if (showLoadPanelButton != null)
                showLoadPanelButton.onClick.AddListener(() => ShowMode(MenuMode.Load));
            if (performSaveButton != null)
                performSaveButton.onClick.AddListener(OnSaveButtonClicked);
            if (performLoadButton != null)
                performLoadButton.onClick.AddListener(OnLoadButtonClicked);
            if (cancelSaveButton != null)
                cancelSaveButton.onClick.AddListener(() => ShowMode(MenuMode.Main));
            if (cancelLoadButton != null)
                cancelLoadButton.onClick.AddListener(() => ShowMode(MenuMode.Main));
            if (deleteButton != null)
                deleteButton.onClick.AddListener(OnDeleteButtonClicked);
            if (renameButton != null)
                renameButton.onClick.AddListener(OnRenameButtonClicked);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            if (backToMainMenuButton != null)
                backToMainMenuButton.onClick.AddListener(OnBackToMainMenuClicked);
            if (saveAndQuitButton != null)
                saveAndQuitButton.onClick.AddListener(OnSaveAndQuitClicked);
            if (quitWithoutSavingButton != null)
                quitWithoutSavingButton.onClick.AddListener(OnQuitWithoutSavingClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseMenu);

            // Listen to input field changes to update button states
            if (saveNameInput != null)
            {
                saveNameInput.onValueChanged.AddListener((value) => UpdateButtonStates());
            }

            // Disable load/delete buttons initially
            UpdateButtonStates();
        }

        private void Start()
        {
            saveLoadService = ServiceLocator.TryGet<ISaveLoadService>();
            gameStateService = ServiceLocator.TryGet<IGameStateService>();

            if (saveLoadService == null)
            {
            }

            // Try to find settings panel controller if not assigned
            if (settingsPanelController == null && settingsPanel != null)
            {
                settingsPanelController = settingsPanel.GetComponent<SettingsPanel>();
            }

            // Try to find settings panel in scene if not assigned
            if (settingsPanelController == null)
            {
                settingsPanelController = FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
            }
        }

        public void OpenMenu()
        {

            if (menuPanel == null)
            {
                return;
            }


            isOpen = true;

            // Store previous time scale and pause game
            if (pauseGameWhenOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;

                // Also pause through game state service if available
                if (gameStateService != null)
                {
                    gameStateService.PauseGame();
                }
            }

            // Show main menu mode
            ShowMode(MenuMode.Main);
        }

        public void CloseMenu()
        {
            if (menuPanel == null)
                return;

            isOpen = false;
            menuPanel.SetActive(false);
            if (savePanel != null)
                savePanel.SetActive(false);
            if (loadPanel != null)
                loadPanel.SetActive(false);

            // Resume game with previous time scale
            if (pauseGameWhenOpen)
            {
                Time.timeScale = previousTimeScale;

                // Also resume through game state service if available
                if (gameStateService != null)
                {
                    gameStateService.ResumeGame();
                }
            }
        }

        private void ShowMode(MenuMode mode)
        {
            currentMode = mode;

            // Show/hide appropriate panels
            if (menuPanel != null)
            {
                menuPanel.SetActive(mode == MenuMode.Main);
            }
            else
            {
            }

            if (savePanel != null)
            {
                savePanel.SetActive(mode == MenuMode.Save);
            }
            else if (mode == MenuMode.Save)
            {
            }

            if (loadPanel != null)
            {
                loadPanel.SetActive(mode == MenuMode.Load);
            }
            else if (mode == MenuMode.Load)
            {
            }

            // Refresh save list when showing save or load mode
            if (mode == MenuMode.Save || mode == MenuMode.Load)
            {
                RefreshSaveList();
            }

            // Clear input field when showing save mode
            if (mode == MenuMode.Save && saveNameInput != null)
            {
                saveNameInput.text = "";
            }

            UpdateButtonStates();
        }

        private void RefreshSaveList()
        {

            if (saveLoadService == null)
            {
                return;
            }

            // Determine which content area to use based on current mode
            Transform targetContent = currentMode == MenuMode.Save ? saveListContentSave : saveListContentLoad;

            if (targetContent == null)
            {
                return;
            }


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
            foreach (var save in saves)
            {
            }

            // Create list items in the appropriate content area
            foreach (var saveName in saves)
            {
                CreateSaveListItem(saveName, targetContent);
            }

            UpdateButtonStates();
        }

        private void CreateSaveListItem(string saveName, Transform content)
        {
            if (saveListItemPrefab == null)
            {
                return;
            }

            if (content == null)
            {
                return;
            }

            GameObject itemObj = Instantiate(saveListItemPrefab, content);
            if (itemObj.TryGetComponent<SaveListItem>(out var item))
            {
            }

            if (item != null)
            {
                SaveFileInfo info = saveLoadService.GetSaveInfo(saveName);
                if (info != null)
                {
                }
                else
                {
                }

                item.Initialize(info ?? new SaveFileInfo { saveName = saveName, fileName = saveName });
                item.OnSelected += OnSaveItemSelected;
                saveListItems.Add(item);
            }
            else
            {
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

            // Update input field with selected save name
            if (saveNameInput != null)
            {
                saveNameInput.text = item.SaveInfo.saveName;
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
                return;
            }

            // Check if save already exists
            if (saveLoadService.SaveExists(saveName))
            {
                // In a real implementation, show confirmation dialog
            }

            // Perform save
            bool success = saveLoadService.SaveGame(saveName);

            if (success)
            {
                RefreshSaveList();
                saveNameInput.text = "";
            }
            else
            {
            }
        }

        private void OnLoadButtonClicked()
        {
            if (saveLoadService == null || selectedSaveItem == null)
                return;

            string saveName = selectedSaveItem.SaveInfo.saveName;

            // Perform load
            bool success = saveLoadService.LoadGame(saveName);

            if (success)
            {
                CloseMenu();
            }
            else
            {
            }
        }

        private void OnDeleteButtonClicked()
        {
            if (saveLoadService == null || selectedSaveItem == null)
                return;

            string saveName = selectedSaveItem.SaveInfo.saveName;

            // In a real implementation, show confirmation dialog

            bool success = saveLoadService.DeleteSave(saveName);

            if (success)
            {
                RefreshSaveList();
            }
            else
            {
            }
        }

        private void OnRenameButtonClicked()
        {
            if (saveLoadService == null || selectedSaveItem == null)
                return;

            if (saveNameInput == null || string.IsNullOrWhiteSpace(saveNameInput.text))
            {
                return;
            }

            string oldName = selectedSaveItem.SaveInfo.saveName;
            string newName = saveNameInput.text.Trim();

            if (oldName == newName)
            {
                return;
            }

            // Check if new name already exists
            if (saveLoadService.SaveExists(newName))
            {
                return;
            }

            // Rename by loading, saving with new name, then deleting old
            try
            {
                // Load the save data
                var saveLoadManager = saveLoadService as SaveLoadManager;
                if (saveLoadManager != null)
                {
                    // Get the settings field using reflection to access GetSaveFilePath
                    var settingsField = typeof(SaveLoadManager).GetField("settings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (settingsField != null)
                    {
                        var settings = settingsField.GetValue(saveLoadManager) as SaveLoadSettings;
                        if (settings != null)
                        {
                            string oldFilePath = settings.GetSaveFilePath(oldName);
                            string newFilePath = settings.GetSaveFilePath(newName);

                            if (System.IO.File.Exists(oldFilePath))
                            {
                                // Copy the file with new name
                                System.IO.File.Copy(oldFilePath, newFilePath, false);

                                // Delete the old file
                                System.IO.File.Delete(oldFilePath);

                                RefreshSaveList();
                                saveNameInput.text = "";
                            }
                            else
                            {
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
            }
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = selectedSaveItem != null;
            bool hasValidSaveName = saveNameInput != null && !string.IsNullOrWhiteSpace(saveNameInput.text);

            // Update save panel buttons
            if (performSaveButton != null)
                performSaveButton.interactable = hasValidSaveName || hasSelection;

            // Update load panel buttons
            if (performLoadButton != null)
                performLoadButton.interactable = hasSelection;

            // Update shared buttons
            if (deleteButton != null)
                deleteButton.interactable = hasSelection;
            if (renameButton != null)
                renameButton.interactable = hasSelection && hasValidSaveName;
        }

        private void OnResumeButtonClicked()
        {
            CloseMenu();
        }

        private void OnSettingsButtonClicked()
        {

            // Hide the save/load menu temporarily
            if (menuPanel != null)
                menuPanel.SetActive(false);
            if (savePanel != null)
                savePanel.SetActive(false);
            if (loadPanel != null)
                loadPanel.SetActive(false);

            // Open the settings panel
            if (settingsPanelController != null)
            {
                settingsPanelController.Open();
            }
            else if (settingsPanel != null)
            {
                // Fallback to simple panel activation
                settingsPanel.SetActive(true);
            }
            else
            {
                // Show the menu again since we can't open settings
                ShowMode(MenuMode.Main);
            }
        }

        private void OnBackToMainMenuClicked()
        {
            if (saveLoadService == null)
                return;

            // Show confirmation dialog (for now, just log)

            // Find SceneTransitionManager
            var sceneManager = FindAnyObjectByType<RTS.UI.SceneTransitionManager>();
            if (sceneManager != null)
            {
                sceneManager.LoadMainMenu();
            }
            else
            {
                // Fallback to direct scene loading
                UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        private void OnSaveAndQuitClicked()
        {
            if (saveLoadService == null)
                return;

            // Generate auto-save name
            string saveName = "AutoSave_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            // Perform save
            bool success = saveLoadService.SaveGame(saveName);

            if (success)
            {
                QuitGame();
            }
            else
            {
            }
        }

        private void OnQuitWithoutSavingClicked()
        {
            QuitGame();
        }

        private void QuitGame()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }

    /// <summary>
    /// Individual save list item component.
    /// </summary>
    public class SaveListItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI saveNameText;
        [SerializeField] private TextMeshProUGUI saveDateText;
        [SerializeField] private TextMeshProUGUI playTimeText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button selectButton;

        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = Color.green;
        [SerializeField] private Color autoSaveColor = Color.yellow;
        [SerializeField] private Color quickSaveColor = Color.cyan;

        private SaveFileInfo saveInfo;
        private bool isSelected = false;

        public SaveFileInfo SaveInfo => saveInfo;
        public event System.Action<SaveListItem> OnSelected;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(() => OnSelected?.Invoke(this));
            }
        }

        public void Initialize(SaveFileInfo info)
        {
            saveInfo = info;
            UpdateDisplay();
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (saveInfo == null)
                return;

            // Update text
            if (saveNameText != null)
            {
                string displayName = saveInfo.saveName;
                if (saveInfo.isAutoSave)
                    displayName = "[AUTO] " + displayName;
                if (saveInfo.isQuickSave)
                    displayName = "[QUICK] " + displayName;

                saveNameText.text = displayName;
            }

            if (saveDateText != null)
            {
                saveDateText.text = saveInfo.saveDate ?? "Unknown Date";
            }

            if (playTimeText != null)
            {
                int hours = (int)(saveInfo.playTime / 3600);
                int minutes = (int)((saveInfo.playTime % 3600) / 60);
                playTimeText.text = $"{hours:00}:{minutes:00}";
            }

            // Update background color
            if (backgroundImage != null)
            {
                Color color = normalColor;

                if (isSelected)
                    color = selectedColor;
                else if (saveInfo.isQuickSave)
                    color = quickSaveColor;
                else if (saveInfo.isAutoSave)
                    color = autoSaveColor;

                backgroundImage.color = color;
            }
        }
    }
}
