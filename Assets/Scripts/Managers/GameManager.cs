using UnityEngine;
using RTS.Core.Services;
using RTS.Core.Pooling;
using Assets.Scripts.UI.FloatingNumbers;

namespace RTS.Managers
{
    /// <summary>
    /// Main game manager responsible for initializing all services in correct order.
    /// Entry point for the game's service architecture.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Service References")]
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private HappinessManager happinessManager;
        [SerializeField] private BuildingManager buildingManager;
        [SerializeField] private ObjectPool objectPool;

        [Header("Campfire System Services (Optional)")]
        [SerializeField] private PopulationManager populationManager;
        [SerializeField] private ReputationManager reputationManager;
        [SerializeField] private PeasantWorkforceManager peasantWorkforceManager;

        [Header("Save/Load System")]
        [SerializeField] private RTS.SaveLoad.SaveLoadManager saveLoadManager;

        [Header("UI Systems")]
        [SerializeField] private FloatingNumbersManager floatingNumbersManager;

        [Header("Settings & Audio")]
        [SerializeField] private RTSGame.Managers.RTSSettingsManager settingsManager;
        [SerializeField] private RTSGame.Managers.AudioManager audioManager;

        [Header("Settings")]
        [SerializeField] private bool initializeOnAwake = true;

        private static GameManager instance;
        public static GameManager Instance => instance;

        private IGameStateService gameStateService;

        private void Awake()
        {
            // Singleton pattern for GameManager only (it's the root)
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (initializeOnAwake)
            {
                InitializeServices();
            }
        }

        /// <summary>
        /// Initialize all game services in the correct order.
        /// </summary>
        public void InitializeServices()
        {

            // 1. Core services first
            InitializeObjectPool();
            
            // 2. Game state service
            gameStateService = new GameStateService();
            ServiceLocator.Register<IGameStateService>(gameStateService);

            // 3. Resource and happiness systems
            InitializeResourceManager();
            InitializeHappinessManager();

            // 4. Campfire system services (optional)
            InitializePopulationManager();
            InitializeReputationManager();
            InitializePeasantWorkforceManager();

            // 5. Building management system
            InitializeBuildingManager();

            // 6. Audio system (before settings, as settings depends on audio)
            InitializeAudioManager();

            // 7. Settings system
            InitializeSettingsManager();

            // 8. Save/Load system
            InitializeSaveLoadManager();

            // 9. UI systems (floating numbers, etc.)
            InitializeFloatingNumbersManager();

        }

        private void InitializeObjectPool()
        {
            if (objectPool == null)
            {
                var poolObject = new GameObject("ObjectPool");
                poolObject.transform.SetParent(transform);
                objectPool = poolObject.AddComponent<ObjectPool>();
            }

            ServiceLocator.Register<IPoolService>(objectPool);
        }

        private void InitializeResourceManager()
        {
            if (resourceManager == null)
            {
                return;
            }

            ServiceLocator.Register<IResourcesService>(resourceManager);
        }

        private void InitializeHappinessManager()
        {
            if (happinessManager == null)
            {
                return;
            }

            ServiceLocator.Register<IHappinessService>(happinessManager);
        }

        private void InitializePopulationManager()
        {
            if (populationManager == null)
            {
                // Population manager is optional
                return;
            }

            ServiceLocator.Register<IPopulationService>(populationManager);
        }

        private void InitializeReputationManager()
        {
            if (reputationManager == null)
            {
                // Reputation manager is optional
                return;
            }

            ServiceLocator.Register<IReputationService>(reputationManager);
        }

        private void InitializePeasantWorkforceManager()
        {
            if (peasantWorkforceManager == null)
            {
                // Workforce manager is optional
                return;
            }

            ServiceLocator.Register<IPeasantWorkforceService>(peasantWorkforceManager);
        }

        private void InitializeBuildingManager()
        {
            if (buildingManager == null)
            {
                // Try to find it in the scene
                buildingManager = FindAnyObjectByType<BuildingManager>();

                if (buildingManager == null)
                {
                    return;
                }
            }

            ServiceLocator.Register<IBuildingService>(buildingManager);
        }

        private void InitializeSaveLoadManager()
        {
            if (saveLoadManager == null)
            {
                // Try to find it in the scene
                saveLoadManager = FindAnyObjectByType<RTS.SaveLoad.SaveLoadManager>();

                if (saveLoadManager == null)
                {
                    return;
                }
            }

            ServiceLocator.Register<ISaveLoadService>(saveLoadManager);
        }

        private void InitializeFloatingNumbersManager()
        {
            if (floatingNumbersManager == null)
            {
                // Try to find it in the scene
                floatingNumbersManager = FindAnyObjectByType<FloatingNumbersManager>();

                if (floatingNumbersManager == null)
                {
                    return;
                }
            }

            ServiceLocator.Register<IFloatingNumberService>(floatingNumbersManager);
        }

        private void InitializeAudioManager()
        {
            if (audioManager == null)
            {
                // Try to find it in the scene
                audioManager = FindAnyObjectByType<RTSGame.Managers.AudioManager>();

                if (audioManager == null)
                {
                    // Create one if it doesn't exist
                    var audioObj = new GameObject("AudioManager");
                    audioObj.transform.SetParent(transform);
                    audioManager = audioObj.AddComponent<RTSGame.Managers.AudioManager>();
                }
            }

            ServiceLocator.Register<IAudioService>(audioManager);
        }

        private void InitializeSettingsManager()
        {
            if (settingsManager == null)
            {
                // Try to find it in the scene
                settingsManager = FindAnyObjectByType<RTSGame.Managers.RTSSettingsManager>();

                if (settingsManager == null)
                {
                    // Create one if it doesn't exist
                    var settingsObj = new GameObject("RTSSettingsManager");
                    settingsObj.transform.SetParent(transform);
                    settingsManager = settingsObj.AddComponent<RTSGame.Managers.RTSSettingsManager>();
                }
            }

            ServiceLocator.Register<ISettingsService>(settingsManager);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                ServiceLocator.Clear();
                instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            ServiceLocator.Clear();
        }

        #region Public API

        public void StartNewGame()
        {
            gameStateService?.ChangeState(GameState.Playing);
        }

        public void PauseGame()
        {
            gameStateService?.PauseGame();
        }

        public void ResumeGame()
        {
            gameStateService?.ResumeGame();
        }

        public void EndGame(bool victory)
        {
            gameStateService?.ChangeState(victory ? GameState.Victory : GameState.GameOver);
        }

        #endregion
    }

    /// <summary>
    /// Simple game state service implementation.
    /// </summary>
    public class GameStateService : IGameStateService
    {
        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public bool IsPaused { get; private set; }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            var oldState = CurrentState;
            CurrentState = newState;


            // Handle state-specific logic
            switch (newState)
            {
                case GameState.Paused:
                    Time.timeScale = 0f;
                    IsPaused = true;
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    IsPaused = false;
                    break;
                case GameState.GameOver:
                case GameState.Victory:
                    Time.timeScale = 0f;
                    break;
            }
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
            }
        }

        public void SetTimeScale(float scale)
        {
            if (!IsPaused)
            {
                Time.timeScale = Mathf.Max(0, scale);
            }
        }
    }
}
