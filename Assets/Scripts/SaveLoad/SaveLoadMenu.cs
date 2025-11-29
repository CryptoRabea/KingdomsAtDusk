using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using RTS.Core.Services;

namespace RTS.SaveLoad
{
    /// <summary>
    /// In-game save/load menu UI controller.
    /// Opens with F10/ESC, pauses game, allows save/load operations.
    /// </summary>
    public class SaveLoadMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private TMP_InputField saveNameInput;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button backToMainMenuButton;
        [SerializeField] private Button saveAndQuitButton;
        [SerializeField] private Button quitWithoutSavingButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform saveListContent;
        [SerializeField] private GameObject saveListItemPrefab;

        [Header("Settings")]
        [SerializeField] private bool pauseGameWhenOpen = true;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private ISaveLoadService saveLoadService;
        private IGameStateService gameStateService;
        private List<SaveListItem> saveListItems = new List<SaveListItem>();
        private SaveListItem selectedSaveItem = null;
        private bool isOpen = false;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            // Hide menu initially
            if (menuPanel != null)
                menuPanel.SetActive(false);

            // Setup button listeners
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeButtonClicked);
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveButtonClicked);
            if (loadButton != null)
                loadButton.onClick.AddListener(OnLoadButtonClicked);
            if (deleteButton != null)
                deleteButton.onClick.AddListener(OnDeleteButtonClicked);
            if (backToMainMenuButton != null)
                backToMainMenuButton.onClick.AddListener(OnBackToMainMenuClicked);
            if (saveAndQuitButton != null)
                saveAndQuitButton.onClick.AddListener(OnSaveAndQuitClicked);
            if (quitWithoutSavingButton != null)
                quitWithoutSavingButton.onClick.AddListener(OnQuitWithoutSavingClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseMenu);

            // Disable load/delete buttons initially
            UpdateButtonStates();
        }

        private void Start()
        {
            saveLoadService = ServiceLocator.TryGet<ISaveLoadService>();
            gameStateService = ServiceLocator.TryGet<IGameStateService>();

            if (saveLoadService == null)
            {
                Debug.LogError("SaveLoadMenu: ISaveLoadService not found!");
            }
        }

        public void OpenMenu()
        {
            if (menuPanel == null)
            {
                Debug.LogError("Menu panel not assigned!");
                return;
            }

            isOpen = true;
            menuPanel.SetActive(true);

            // Pause game
            if (pauseGameWhenOpen && gameStateService != null)
            {
                gameStateService.PauseGame();
            }

            // Refresh save list
            RefreshSaveList();
        }

        public void CloseMenu()
        {
            if (menuPanel == null)
                return;

            isOpen = false;
            menuPanel.SetActive(false);

            // Resume game
            if (pauseGameWhenOpen && gameStateService != null)
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

            // Create list items
            foreach (var saveName in saves)
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
                Debug.LogWarning("Save name cannot be empty!");
                return;
            }

            // Check if save already exists
            if (saveLoadService.SaveExists(saveName))
            {
                // In a real implementation, show confirmation dialog
                Debug.Log($"Overwriting existing save: {saveName}");
            }

            // Perform save
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

            // Perform load
            bool success = saveLoadService.LoadGame(saveName);

            if (success)
            {
                Debug.Log($"Game loaded: {saveName}");
                CloseMenu();
            }
            else
            {
                Debug.LogError($"Failed to load game: {saveName}");
            }
        }

        private void OnDeleteButtonClicked()
        {
            if (saveLoadService == null || selectedSaveItem == null)
                return;

            string saveName = selectedSaveItem.SaveInfo.saveName;

            // In a real implementation, show confirmation dialog
            Debug.Log($"Deleting save: {saveName}");

            bool success = saveLoadService.DeleteSave(saveName);

            if (success)
            {
                Debug.Log($"Save deleted: {saveName}");
                RefreshSaveList();
            }
            else
            {
                Debug.LogError($"Failed to delete save: {saveName}");
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

        private void OnResumeButtonClicked()
        {
            CloseMenu();
        }

        private void OnBackToMainMenuClicked()
        {
            if (saveLoadService == null)
                return;

            // Show confirmation dialog (for now, just log)
            Debug.Log("Returning to main menu...");

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
                Debug.Log($"Game saved: {saveName}. Quitting...");
                QuitGame();
            }
            else
            {
                Debug.LogError($"Failed to save game before quitting!");
            }
        }

        private void OnQuitWithoutSavingClicked()
        {
            Debug.Log("Quitting without saving...");
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
