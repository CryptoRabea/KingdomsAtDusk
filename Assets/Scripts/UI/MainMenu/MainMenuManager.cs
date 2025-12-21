using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RTS.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        private enum MenuState
        {
            Main,
            Load,
            Settings,
            Credits
        }

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject loadPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Load Panel UI")]
        [SerializeField] private Transform saveListContent;
        [SerializeField] private GameObject saveListItemPrefab;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI noSavesText;

        [Header("Save Settings")]
        [SerializeField] private string saveDirectory = "Saves";
        [SerializeField] private string saveExtension = ".sav";

        [Header("Version")]
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private string versionPrefix = "v";

        private MenuState currentState = MenuState.Main;
        private readonly List<GameObject> saveButtons = new();
        private string selectedSave;

        private InputSystem_Actions input;
        private InputAction cancelAction;

        // ---------------- INIT ----------------

        private void Awake()
        {
            input = new InputSystem_Actions();
            cancelAction = input.UI.Cancel;

            newGameButton.onClick.AddListener(StartNewGame);
            continueButton.onClick.AddListener(OpenLoadPanel);
            loadGameButton.onClick.AddListener(OpenLoadPanel);
            settingsButton.onClick.AddListener(() => SwitchState(MenuState.Settings));
            creditsButton.onClick.AddListener(() => SwitchState(MenuState.Credits));
            quitButton.onClick.AddListener(QuitGame);

            loadButton.onClick.AddListener(LoadSelectedSave);
            deleteButton.onClick.AddListener(DeleteSelectedSave);
            backButton.onClick.AddListener(ReturnToMainMenu);
        }

        private void OnEnable()
        {
            input.UI.Enable();
            cancelAction.performed += OnCancel;
        }

        private void OnDisable()
        {
            cancelAction.performed -= OnCancel;
            input.UI.Disable();
        }

        private void Start()
        {
            SwitchState(MenuState.Main);

            if (versionText != null)
                versionText.text = $"{versionPrefix}{Application.version}";

            UpdateContinueButtons();
        }

        // ---------------- INPUT ----------------

        private void OnCancel(InputAction.CallbackContext ctx)
        {
            if (currentState == MenuState.Main)
                QuitGame();
            else
                ReturnToMainMenu();
        }

        // ---------------- STATE ----------------

        private void SwitchState(MenuState state)
        {
            currentState = state;

            mainMenuPanel.SetActive(state == MenuState.Main);
            loadPanel.SetActive(state == MenuState.Load);
            settingsPanel.SetActive(state == MenuState.Settings);
            creditsPanel.SetActive(state == MenuState.Credits);

            if (state == MenuState.Load)
                RefreshSaveList();
        }

        public void ReturnToMainMenu()
        {
            SwitchState(MenuState.Main);
        }

        // ---------------- MAIN MENU ----------------

        private void StartNewGame()
        {
            PlayerPrefs.DeleteKey("LoadSaveOnStart");
            PlayerPrefs.Save();
            FindAnyObjectByType<SceneTransitionManager>()?.LoadGameScene();
        }

        private void QuitGame()
        {
            Application.Quit();
        }

        private void UpdateContinueButtons()
        {
            bool hasSaves = GetSaveFiles().Length > 0;
            continueButton.interactable = hasSaves;
            loadGameButton.interactable = hasSaves;
        }

        // ---------------- LOAD PANEL ----------------

        private void OpenLoadPanel()
        {
            if (GetSaveFiles().Length == 0)
                return;

            SwitchState(MenuState.Load);
        }

        private void RefreshSaveList()
        {
            foreach (var btn in saveButtons)
                Destroy(btn);

            saveButtons.Clear();
            selectedSave = null;

            var saves = GetSaveFiles();
            noSavesText.gameObject.SetActive(saves.Length == 0);

            foreach (var save in saves)
            {
                var obj = Instantiate(saveListItemPrefab, saveListContent);
                var text = obj.GetComponentInChildren<TextMeshProUGUI>();
                var button = obj.GetComponent<Button>();

                text.text = save;
                button.onClick.AddListener(() => SelectSave(save, obj));

                saveButtons.Add(obj);
            }

            UpdateLoadButtons();
        }

        private void SelectSave(string saveName, GameObject obj)
        {
            selectedSave = saveName;

            foreach (var b in saveButtons)
                b.GetComponent<Image>().color = Color.white;

            obj.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f);
            UpdateLoadButtons();
        }

        private void UpdateLoadButtons()
        {
            bool valid = !string.IsNullOrEmpty(selectedSave);
            loadButton.interactable = valid;
            deleteButton.interactable = valid;
        }

        private void LoadSelectedSave()
        {
            if (string.IsNullOrEmpty(selectedSave))
                return;

            PlayerPrefs.SetString("LoadSaveOnStart", selectedSave);
            PlayerPrefs.Save();

            FindAnyObjectByType<SceneTransitionManager>()?.LoadGameScene();
        }

        private void DeleteSelectedSave()
        {
            if (string.IsNullOrEmpty(selectedSave))
                return;

            string path = Path.Combine(
                Application.persistentDataPath,
                saveDirectory,
                selectedSave + saveExtension
            );

            if (File.Exists(path))
                File.Delete(path);

            RefreshSaveList();
            UpdateContinueButtons();
        }

        // ---------------- FILE SYSTEM ----------------

        private string[] GetSaveFiles()
        {
            string dir = Path.Combine(Application.persistentDataPath, saveDirectory);
            if (!Directory.Exists(dir))
                return new string[0];

            return Directory
                .GetFiles(dir, "*" + saveExtension)
                .Select(Path.GetFileNameWithoutExtension)
                .ToArray();
        }
    }
}
