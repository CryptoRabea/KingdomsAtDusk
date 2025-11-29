using UnityEngine;
using RTS.Core.Services;
using RTS.Core.Pooling;

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
            Debug.Log("Initializing game services...");

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

            // 6. Save/Load system
            InitializeSaveLoadManager();

            Debug.Log("All services initialized successfully!");
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
                Debug.LogError("ResourceManager not assigned in GameManager!");
                return;
            }

            ServiceLocator.Register<IResourcesService>(resourceManager);
        }

        private void InitializeHappinessManager()
        {
            if (happinessManager == null)
            {
                Debug.LogError("HappinessManager not assigned in GameManager!");
                return;
            }

            ServiceLocator.Register<IHappinessService>(happinessManager);
        }

        private void InitializePopulationManager()
        {
            if (populationManager == null)
            {
                // Population manager is optional
                Debug.Log("PopulationManager not assigned - campfire peasant system disabled");
                return;
            }

            ServiceLocator.Register<IPopulationService>(populationManager);
            Debug.Log("PopulationManager registered as IPopulationService");
        }

        private void InitializeReputationManager()
        {
            if (reputationManager == null)
            {
                // Reputation manager is optional
                Debug.Log("ReputationManager not assigned - reputation system disabled");
                return;
            }

            ServiceLocator.Register<IReputationService>(reputationManager);
            Debug.Log("ReputationManager registered as IReputationService");
        }

        private void InitializePeasantWorkforceManager()
        {
            if (peasantWorkforceManager == null)
            {
                // Workforce manager is optional
                Debug.Log("PeasantWorkforceManager not assigned - worker allocation disabled");
                return;
            }

            ServiceLocator.Register<IPeasantWorkforceService>(peasantWorkforceManager);
            Debug.Log("PeasantWorkforceManager registered as IPeasantWorkforceService");
        }

        private void InitializeBuildingManager()
        {
            if (buildingManager == null)
            {
                // Try to find it in the scene
                buildingManager = FindAnyObjectByType<BuildingManager>();

                if (buildingManager == null)
                {
                    Debug.LogWarning("BuildingManager not assigned and not found in scene!");
                    return;
                }
            }

            ServiceLocator.Register<IBuildingService>(buildingManager);
            Debug.Log("BuildingManager registered as IBuildingService");
        }

        private void InitializeSaveLoadManager()
        {
            if (saveLoadManager == null)
            {
                // Try to find it in the scene
                saveLoadManager = FindAnyObjectByType<RTS.SaveLoad.SaveLoadManager>();

                if (saveLoadManager == null)
                {
                    Debug.LogWarning("SaveLoadManager not assigned and not found in scene!");
                    return;
                }
            }

            ServiceLocator.Register<ISaveLoadService>(saveLoadManager);
            Debug.Log("SaveLoadManager registered as ISaveLoadService");
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

            Debug.Log($"Game state changed: {oldState} -> {newState}");

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
