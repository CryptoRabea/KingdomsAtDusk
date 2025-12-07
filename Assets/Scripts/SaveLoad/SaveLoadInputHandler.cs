using RTS.Core.Services;
using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField] private SaveLoadMenu inGameMenu;

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
            }

            if (inGameMenu == null)
            {
                inGameMenu = FindAnyObjectByType<SaveLoadMenu>(FindObjectsInactive.Include);
                if (inGameMenu == null)
                {
                }
            }
        }

        private void OnEnable()
        {
            if (inputActions != null)
            {
                // Enable the Player action map
                inputActions.Player.Enable();

                // Subscribe to actions
                inputActions.Player.F5.performed += OnF5Pressed;
                inputActions.Player.F9.performed += OnF9Pressed;
                inputActions.Player.F10.performed += OnF10Pressed;
            }
        }

        private void OnDisable()
        {
            if (inputActions != null)
            {
                // Unsubscribe from actions
                inputActions.Player.F5.performed -= OnF5Pressed;
                inputActions.Player.F9.performed -= OnF9Pressed;
                inputActions.Player.F10.performed -= OnF10Pressed;

                // Disable the Player action map
                inputActions.Player.Disable();
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
            {
                return;
            }

            HandleQuickSave();
        }

        private void OnF9Pressed(InputAction.CallbackContext context)
        {

            // Don't process if menu is open
            if (inGameMenu != null && inGameMenu.IsOpen)
            {
                return;
            }

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
                return;
            }

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
                return;
            }

            if (!saveLoadService.SaveExists("QuickSave"))
            {
                ShowNotification("No Quick Save Found!", true);
                return;
            }

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
            {
                return;
            }


            if (inGameMenu.IsOpen)
                inGameMenu.CloseMenu();
            else
                inGameMenu.OpenMenu();
        }

        private void ShowNotification(string message, bool isError = false)
        {
            // For now, just log to console
            // In a real implementation, show a UI notification
            if (isError)
            {
            }
            else
            {
            }

            // Could publish an event for UI notification system
            // EventBus.Publish(new SaveLoadNotificationEvent(message, isError));
        }
    }
}
