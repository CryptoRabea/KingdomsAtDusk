using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace RTS.UI
{
    /// <summary>
    /// Manages scene transitions with loading screens.
    /// Handles: Main Menu → Game Scene, and Game Scene → Main Menu
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string gameSceneName = "GameScene";

        [Header("Loading Settings")]
        [SerializeField] private float artificialLoadDelay = 0.5f; // Minimum time to show loading screen
        [SerializeField] private bool showLoadingTips = true;

        private static SceneTransitionManager instance;
        private bool isTransitioning = false;

        public static SceneTransitionManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject managerObj = new GameObject("SceneTransitionManager");
                    instance = managerObj.AddComponent<SceneTransitionManager>();
                    DontDestroyOnLoad(managerObj);
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Load the main menu scene
        /// </summary>
        public void LoadMainMenu()
        {
            if (isTransitioning)
            {
                Debug.LogWarning("Scene transition already in progress!");
                return;
            }

            StartCoroutine(LoadSceneAsync(mainMenuSceneName));
        }

        /// <summary>
        /// Load the game scene
        /// </summary>
        public void LoadGameScene()
        {
            if (isTransitioning)
            {
                Debug.LogWarning("Scene transition already in progress!");
                return;
            }

            StartCoroutine(LoadSceneAsync(gameSceneName));
        }

        /// <summary>
        /// Load any scene by name
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (isTransitioning)
            {
                Debug.LogWarning("Scene transition already in progress!");
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneName));
        }

        /// <summary>
        /// Reload the current scene
        /// </summary>
        public void ReloadCurrentScene()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            LoadScene(currentScene);
        }

        /// <summary>
        /// Quit the application
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("Quitting game...");

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            isTransitioning = true;

            Debug.Log($"[SceneTransition] Loading scene: {sceneName}");

            // Show loading screen
            LoadingScreenManager loadingScreen = LoadingScreenManager.Instance;
            if (loadingScreen != null)
            {
                loadingScreen.Show(showLoadingTips);
                loadingScreen.SetMessage($"Loading {sceneName}...");
            }

            // Small delay to ensure loading screen is visible
            yield return new WaitForSeconds(0.1f);

            // Start loading the scene
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            float progress = 0f;

            // Update progress while loading
            while (!asyncLoad.isDone)
            {
                // AsyncOperation progress goes from 0 to 0.9
                progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

                if (loadingScreen != null)
                {
                    loadingScreen.SetProgress(progress * 0.9f); // Reserve last 10% for activation
                }

                // Scene is ready, waiting for activation
                if (asyncLoad.progress >= 0.9f)
                {
                    // Artificial delay to show loading screen
                    if (artificialLoadDelay > 0)
                    {
                        yield return new WaitForSeconds(artificialLoadDelay);
                    }

                    // Update to 100%
                    if (loadingScreen != null)
                    {
                        loadingScreen.SetProgress(1f);
                        loadingScreen.SetMessage("Ready!");
                    }

                    yield return new WaitForSeconds(0.3f);

                    // Activate the scene
                    asyncLoad.allowSceneActivation = true;
                    break;
                }

                yield return null;
            }

            // Wait for scene to fully load
            yield return new WaitForSeconds(0.2f);

            // Hide loading screen
            if (loadingScreen != null)
            {
                loadingScreen.Hide();
            }

            isTransitioning = false;

            Debug.Log($"[SceneTransition] Scene loaded: {sceneName}");
        }

        /// <summary>
        /// Check if a scene transition is currently in progress
        /// </summary>
        public bool IsTransitioning()
        {
            return isTransitioning;
        }
    }
}
