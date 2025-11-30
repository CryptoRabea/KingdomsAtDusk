using UnityEngine;
using UnityEngine.InputSystem;
using RTS.Core.Services;

namespace RTS.SaveLoad
{
    /// <summary>
    /// Handles keyboard input for save/load operations using the new Input System.
    /// F5 - Quick Save
    /// F9 - Quick Load
    /// F10 - Toggle In-Game Menu
    /// </summary>
    public class SaveLoadInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RTS.UI.InGameMenu inGameMenu;

        private ISaveLoadService saveLoadService;
        private IGameStateService gameStateService;
        private InputSystem_Actions inputActions;

        private void Awake()
        {
            // Initialize input actions
            inputActions = new InputSystem_Actions();
        }

        private void Start()
        {
            saveLoadService = ServiceLocator.TryGet<ISaveLoadService>();
            gameStateService = ServiceLocator.TryGet<IGameStateService>();

            if (saveLoadService == null)
            {
                Debug.LogWarning("SaveLoadInputHandler: ISaveLoadService not found!");
            }

            if (inGameMenu == null)
            {
                inGameMenu = FindAnyObjectByType<RTS.UI.InGameMenu>(FindObjectsInactive.Include);
                if (inGameMenu == null)
                {
                    Debug.LogWarning("SaveLoadInputHandler: InGameMenu not found in scene!");
                }
            }
        }

        private void OnEnable()
        {
            if (inputActions != null)
            {
                // Enable the SaveLoad action map
                inputActions.SaveLoad.Enable();

                // Subscribe to actions
                inputActions.SaveLoad.F5.performed += OnF5Pressed;
                inputActions.SaveLoad.F9.performed += OnF9Pressed;
                inputActions.SaveLoad.F10.performed += OnF10Pressed;
            }
        }

        private void OnDisable()
        {
            if (inputActions != null)
            {
                // Unsubscribe from actions
                inputActions.SaveLoad.F5.performed -= OnF5Pressed;
                inputActions.SaveLoad.F9.performed -= OnF9Pressed;
                inputActions.SaveLoad.F10.performed -= OnF10Pressed;

                // Disable the SaveLoad action map
                inputActions.SaveLoad.Disable();
            }
        }

        private void OnDestroy()
        {
            // Dispose of input actions
            inputActions?.Dispose();
        }

        private void OnF5Pressed(InputAction.CallbackContext context)
        {
            // Don't process if menu is open
            if (inGameMenu != null && inGameMenu.IsOpen)
                return;

            HandleQuickSave();
        }

        private void OnF9Pressed(InputAction.CallbackContext context)
        {
            // Don't process if menu is open
            if (inGameMenu != null && inGameMenu.IsOpen)
                return;

            HandleQuickLoad();
        }

        private void OnF10Pressed(InputAction.CallbackContext context)
        {
            // Toggle in-game menu
            ToggleMenu();
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
            if (inGameMenu == null)
                return;

            inGameMenu.ToggleMenu();
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
