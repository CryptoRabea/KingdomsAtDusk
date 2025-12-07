using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using RTS.Core.Services;
using RTS.SaveLoad;
using RTSGame.UI.Settings;

namespace RTS.UI
{
    /// <summary>
    /// Manages the main menu UI with buttons for:
    /// - New Game
    /// - Continue (Load Game)
    /// - Settings
    /// - Credits
    /// - Quit
    /// Uses the new Input System for keyboard/gamepad navigation
    /// Integrates with SaveLoadService for save game management
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Menu Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private MainMenuLoadPanel loadPanel;

        [Header("Advanced Settings (Optional)")]
        [SerializeField] private SettingsPanel settingsPanelController;

        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Settings Buttons")]
        [SerializeField] private Button backFromSettingsButton;

        [Header("Credits Buttons")]
        [SerializeField] private Button backFromCreditsButton;

        [Header("Version Display")]
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private string versionPrefix = "v";

        // Input System
        private InputSystem_Actions inputActions;
        private InputAction cancelAction;

        // Services
        private ISaveLoadService saveLoadService;

        private void Awake()
        {
            // Initialize Input System
            inputActions = new InputSystem_Actions();

            // Get the Cancel action from UI map for ESC/Back navigation
            cancelAction = inputActions.UI.Cancel;
        }

        private void OnEnable()
        {
            // Enable UI input actions
            inputActions.UI.Enable();

            // Subscribe to Cancel action (ESC key or gamepad B button)
            if (cancelAction != null)
            {
                cancelAction.performed += OnCancelPerformed;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from Cancel action
            if (cancelAction != null)
            {
                cancelAction.performed -= OnCancelPerformed;
            }

            // Disable UI input actions
            inputActions.UI.Disable();
        }

        private void Start()
        {
            // Get save load service
            saveLoadService = ServiceLocator.TryGet<ISaveLoadService>();

            // Try to find load panel if not assigned
            if (loadPanel == null)
            {
                loadPanel = FindAnyObjectByType<MainMenuLoadPanel>(FindObjectsInactive.Include);
            }

            // Try to find settings panel controller if not assigned
            if (settingsPanelController == null && settingsPanel != null)
            {
                settingsPanelController = settingsPanel.GetComponent<SettingsPanel>();
            }

            // Setup button listeners
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGameClicked);

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (creditsButton != null)
                creditsButton.onClick.AddListener(OnCreditsClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            if (backFromSettingsButton != null)
                backFromSettingsButton.onClick.AddListener(OnBackFromSettings);

            if (backFromCreditsButton != null)
                backFromCreditsButton.onClick.AddListener(OnBackFromCredits);

            // Show main menu panel
            ShowMainMenu();

            // Display version
            if (versionText != null)
            {
                versionText.text = $"{versionPrefix}{Application.version}";
            }

            // Check if save game exists for Continue button
            UpdateContinueButtonState();

        }

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            // Handle ESC key / Cancel button (gamepad B)
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                OnBackFromSettings();
            }
            else if (creditsPanel != null && creditsPanel.activeSelf)
            {
                OnBackFromCredits();
            }
            else if (mainMenuPanel != null && mainMenuPanel.activeSelf)
            {
                // On main menu, ESC quits the game
                OnQuitClicked();
            }
        }

        private void ShowMainMenu()
        {
            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(creditsPanel, false);
        }

        private void OnNewGameClicked()
        {

            // Clear any load-on-start flag
            PlayerPrefs.DeleteKey("LoadSaveOnStart");
            PlayerPrefs.Save();

            // Load game scene
            SceneTransitionManager.Instance.LoadGameScene();
        }

        private void OnContinueClicked()
        {

            if (!HasAnySaves())
            {
                return;
            }

            // Open load panel
            if (loadPanel != null)
            {
                SetPanelActive(mainMenuPanel, false);
                loadPanel.OpenPanel();
            }
            else
            {
            }
        }

        private void OnSettingsClicked()
        {
            SetPanelActive(mainMenuPanel, false);

            // Use the advanced settings panel controller if available
            if (settingsPanelController != null)
            {
                settingsPanelController.Open();
            }
            else
            {
                // Fallback to simple panel activation
                SetPanelActive(settingsPanel, true);
            }
        }

        private void OnCreditsClicked()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(creditsPanel, true);
        }

        private void OnQuitClicked()
        {
            SceneTransitionManager.Instance.QuitGame();
        }

        private void OnBackFromSettings()
        {

            // Close the advanced settings panel if it's being used
            if (settingsPanelController != null)
            {
                settingsPanelController.Close();
            }

            ShowMainMenu();
        }

        private void OnBackFromCredits()
        {
            ShowMainMenu();
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        private bool HasAnySaves()
        {
            if (saveLoadService == null)
                return false;

            string[] saves = saveLoadService.GetAllSaves();
            return saves != null && saves.Length > 0;
        }

        private void UpdateContinueButtonState()
        {
            if (continueButton != null)
            {
                // Disable continue button if no saves exist
                continueButton.interactable = HasAnySaves();
            }
        }

        // Public API for external scripts
        public void ShowSettings()
        {
            OnSettingsClicked();
        }

        public void ShowCredits()
        {
            OnCreditsClicked();
        }

        public void StartNewGame()
        {
            OnNewGameClicked();
        }
    }
}
