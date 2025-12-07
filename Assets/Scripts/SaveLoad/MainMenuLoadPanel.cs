using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RTS.Core.Services;

namespace RTS.SaveLoad
{
    /// <summary>
    /// Main menu load game panel.
    /// Displays save files and allows loading them.
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
        [SerializeField] private string gameSceneName = "GameScene";

        private ISaveLoadService saveLoadService;
        private List<SaveListItem> saveListItems = new List<SaveListItem>();
        private SaveListItem selectedSaveItem = null;

        private void Awake()
        {
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
            saveLoadService = ServiceLocator.TryGet<ISaveLoadService>();

            if (saveLoadService == null)
            {
            }
        }

        public void OpenPanel()
        {
            if (loadPanel == null)
            {
                return;
            }

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

        private void CreateSaveListItem(string saveName)
        {
            if (saveListItemPrefab == null || saveListContent == null)
                return;

            GameObject itemObj = Instantiate(saveListItemPrefab, saveListContent);
            if (itemObj.TryGetComponent<SaveListItem>(out var item))
            {
            }

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

            UpdateButtonStates();
        }

        private void OnLoadButtonClicked()
        {
            if (saveLoadService == null || selectedSaveItem == null)
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
            if (saveLoadService == null || selectedSaveItem == null)
                return;

            string saveName = selectedSaveItem.SaveInfo.saveName;


            bool success = saveLoadService.DeleteSave(saveName);

            if (success)
            {
                RefreshSaveList();
            }
            else
            {
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
            if (saveLoadService == null)
                return false;

            string[] saves = saveLoadService.GetAllSaves();
            return saves != null && saves.Length > 0;
        }
    }
}
