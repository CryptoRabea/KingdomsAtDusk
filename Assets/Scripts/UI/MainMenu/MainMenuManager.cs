using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RTS.UI
{
    /// <summary>
    /// Manages the main menu UI with buttons for:
    /// - New Game
    /// - Continue
    /// - Settings
    /// - Credits
    /// - Quit
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Menu Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Settings Buttons")]
        [SerializeField] private Button backFromSettingsButton;

        [Header("Credits Buttons")]
        [SerializeField] private Button backFromCreditsButton;

        [Header("Save Management")]
        [SerializeField] private RTS.SaveLoad.SaveManagementPanel saveManagementPanel;

        [Header("Version Display")]
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private string versionPrefix = "v";

        private void Start()
        {
            // Setup button listeners
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGameClicked);

            if (loadGameButton != null)
                loadGameButton.onClick.AddListener(OnLoadGameClicked);

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

            // Initialize save management panel
            if (saveManagementPanel != null)
            {
                saveManagementPanel.SetMainMenuMode(true);
            }

            Debug.Log("[MainMenu] Main menu initialized");
        }

        private void ShowMainMenu()
        {
            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(creditsPanel, false);
        }

        private void OnNewGameClicked()
        {
            Debug.Log("[MainMenu] New Game clicked");

            // Load game scene
            SceneTransitionManager.Instance.LoadGameScene();
        }

        private void OnLoadGameClicked()
        {
            Debug.Log("[MainMenu] Load Game clicked");

            if (saveManagementPanel != null)
            {
                saveManagementPanel.OpenPanel(isLoading: true);
            }
            else
            {
                Debug.LogWarning("SaveManagementPanel not assigned!");
            }
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenu] Settings clicked");
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(settingsPanel, true);
        }

        private void OnCreditsClicked()
        {
            Debug.Log("[MainMenu] Credits clicked");
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(creditsPanel, true);
        }

        private void OnQuitClicked()
        {
            Debug.Log("[MainMenu] Quit clicked");
            SceneTransitionManager.Instance.QuitGame();
        }

        private void OnBackFromSettings()
        {
            Debug.Log("[MainMenu] Back from Settings");
            ShowMainMenu();
        }

        private void OnBackFromCredits()
        {
            Debug.Log("[MainMenu] Back from Credits");
            ShowMainMenu();
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
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
