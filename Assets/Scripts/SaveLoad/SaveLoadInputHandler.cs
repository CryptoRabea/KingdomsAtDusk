using UnityEngine;
using RTS.Core.Services;

namespace RTS.SaveLoad
{
    /// <summary>
    /// Handles keyboard input for save/load operations.
    /// F5 - Quick Save
    /// F9 - Quick Load
    /// F10/ESC - Toggle Save/Load Menu
    /// </summary>
    public class SaveLoadInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] internal SaveLoadMenu saveLoadMenu;

        [Header("Input Settings")]
        [SerializeField] internal KeyCode quickSaveKey = KeyCode.F5;
        [SerializeField] internal KeyCode quickLoadKey = KeyCode.F9;
        [SerializeField] internal KeyCode toggleMenuKey = KeyCode.F10;
        [SerializeField] internal bool allowEscapeToggle = true;

        private ISaveLoadService saveLoadService;
        private IGameStateService gameStateService;

        private void Start()
        {
            saveLoadService = ServiceLocator.TryGet<ISaveLoadService>();
            gameStateService = ServiceLocator.TryGet<IGameStateService>();

            if (saveLoadService == null)
            {
                Debug.LogWarning("SaveLoadInputHandler: ISaveLoadService not found!");
            }

            if (saveLoadMenu == null)
            {
                saveLoadMenu = FindAnyObjectByType<SaveLoadMenu>(FindObjectsInactive.Include);
                if (saveLoadMenu == null)
                {
                    Debug.LogWarning("SaveLoadInputHandler: SaveLoadMenu not found in scene!");
                }
            }
        }

        private void Update()
        {
            // Don't process input if menu is open (except for toggle keys)
            bool menuOpen = saveLoadMenu != null && saveLoadMenu.IsOpen;

            // Toggle menu (always works)
            if (Input.GetKeyDown(toggleMenuKey) || (allowEscapeToggle && Input.GetKeyDown(KeyCode.Escape)))
            {
                ToggleMenu();
                return;
            }

            // Don't process other inputs when menu is open
            if (menuOpen)
                return;

            // Quick Save (F5)
            if (Input.GetKeyDown(quickSaveKey))
            {
                HandleQuickSave();
            }

            // Quick Load (F9)
            if (Input.GetKeyDown(quickLoadKey))
            {
                HandleQuickLoad();
            }
        }

        private void HandleQuickSave()
        {
            if (saveLoadService == null)
            {
                Debug.LogWarning("Cannot quick save: Save service not available");
                return;
            }

            Debug.Log("Quick Save (F5) triggered...");
            bool success = saveLoadService.QuickSave();

            if (success)
            {
                ShowNotification("Game Saved (Quick Save)");
            }
            else
            {
                ShowNotification("Quick Save Failed!", true);
            }
        }

        private void HandleQuickLoad()
        {
            if (saveLoadService == null)
            {
                Debug.LogWarning("Cannot quick load: Save service not available");
                return;
            }

            if (!saveLoadService.SaveExists("QuickSave"))
            {
                ShowNotification("No Quick Save Found!", true);
                return;
            }

            Debug.Log("Quick Load (F9) triggered...");
            bool success = saveLoadService.QuickLoad();

            if (success)
            {
                ShowNotification("Game Loaded (Quick Save)");
            }
            else
            {
                ShowNotification("Quick Load Failed!", true);
            }
        }

        private void ToggleMenu()
        {
            if (saveLoadMenu == null)
                return;

            if (saveLoadMenu.IsOpen)
            {
                saveLoadMenu.CloseMenu();
            }
            else
            {
                saveLoadMenu.OpenMenu();
            }
        }

        private void ShowNotification(string message, bool isError = false)
        {
            // For now, just log to console
            // In a real implementation, show a UI notification
            if (isError)
            {
                Debug.LogWarning($"[Save/Load] {message}");
            }
            else
            {
                Debug.Log($"[Save/Load] {message}");
            }

            // Could publish an event for UI notification system
            // EventBus.Publish(new SaveLoadNotificationEvent(message, isError));
        }
    }
}
