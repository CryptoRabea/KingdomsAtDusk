using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Core.Services;
using RTS.SaveLoad;

namespace RTS.UI
{
    /// <summary>
    /// In-game pause menu with save/load functionality.
    /// Opened with F10 or ESC key.
    /// Features:
    /// - Resume: Return to game
    /// - Save Now: Quick save with timestamp (doesn't open panel)
    /// - Save/Load: Opens SaveManagementPanel
    /// - Main Menu: Return to main menu
    /// - Quit: Quit game
    /// </summary>
    public class InGameMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveNowButton;
        [SerializeField] private Button saveLoadButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private SaveManagementPanel saveManagementPanel;

        [Header("Settings")]
        [SerializeField] private bool pauseGameWhenOpen = true;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private ISaveLoadService saveLoadService;
        private IGameStateService gameStateService;
        private bool isOpen = false;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (menuPanel != null)
                menuPanel.SetActive(false);

            SetupButtonListeners();
        }

        private void Start()
        {
            saveLoadService = ServiceLocator.TryGet<ISaveLoadService>();
            gameStateService = ServiceLocator.TryGet<IGameStateService>();

            if (saveLoadService == null)
            {
                Debug.LogError("InGameMenu: ISaveLoadService not found!");
            }

            // Initialize save management panel
            if (saveManagementPanel != null)
            {
                saveManagementPanel.SetMainMenuMode(false);
            }
        }

        private void SetupButtonListeners()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);
            if (saveNowButton != null)
                saveNowButton.onClick.AddListener(OnSaveNowClicked);
            if (saveLoadButton != null)
                saveLoadButton.onClick.AddListener(OnSaveLoadClicked);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
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

        public void ToggleMenu()
        {
            if (isOpen)
                CloseMenu();
            else
                OpenMenu();
        }

        private void OnResumeClicked()
        {
            CloseMenu();
        }

        private void OnSaveNowClicked()
        {
            if (saveLoadService == null)
            {
                Debug.LogWarning("Cannot save: Save service not available");
                return;
            }

            // Generate timestamped save name
            string saveName = "Manual_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            Debug.Log($"Save Now: Creating save '{saveName}'...");
            bool success = saveLoadService.SaveGame(saveName);

            if (success)
            {
                Debug.Log($"Game saved successfully: {saveName}");
                ShowNotification($"Game Saved: {saveName}");
            }
            else
            {
                Debug.LogError($"Failed to save game: {saveName}");
                ShowNotification("Save Failed!", true);
            }
        }

        private void OnSaveLoadClicked()
        {
            if (saveManagementPanel != null)
            {
                // Hide in-game menu
                if (menuPanel != null)
                    menuPanel.SetActive(false);

                // Open save management panel
                saveManagementPanel.OpenPanel(isLoading: false);
            }
            else
            {
                Debug.LogWarning("SaveManagementPanel not assigned!");
            }
        }

        private void OnMainMenuClicked()
        {
            Debug.Log("Returning to main menu...");

            // Optionally show confirmation dialog here

            // Find SceneTransitionManager
            var sceneManager = FindAnyObjectByType<SceneTransitionManager>();
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

        private void OnQuitClicked()
        {
            Debug.Log("Quitting game...");

            // Optionally show confirmation dialog here

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void ShowNotification(string message, bool isError = false)
        {
            // For now, just log to console
            // In a real implementation, show a UI notification
            if (isError)
            {
                Debug.LogWarning($"[InGameMenu] {message}");
            }
            else
            {
                Debug.Log($"[InGameMenu] {message}");
            }

            // Could publish an event for UI notification system
            // EventBus.Publish(new NotificationEvent(message, isError));
        }
    }
}
