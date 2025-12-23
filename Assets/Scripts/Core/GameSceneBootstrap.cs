using System.Collections;
using UnityEngine;
using RTS.UI;

namespace RTS.Core
{
    /// <summary>
    /// Bootstraps the GameScene when played directly from the editor.
    /// Shows loading screen during initialization for smoother startup.
    /// Also provides staggered initialization for better performance.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before GameManager and other systems
    public class GameSceneBootstrap : MonoBehaviour
    {
        [Header("Loading Screen Settings")]
        [SerializeField] private bool showLoadingOnDirectPlay = true;
        [SerializeField] private float minimumLoadingTime = 1.5f;
        [SerializeField] private bool enableStaggeredInit = true;

        [Header("Initialization Steps")]
        [SerializeField] private int framesToSpreadInit = 10;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        // Static flag to track if we came from a scene transition
        private static bool cameFromTransition = false;
        private static bool hasInitialized = false;

        private LoadingScreenManager loadingScreen;
        private bool isInitializing = false;

        /// <summary>
        /// Call this before loading GameScene to skip the bootstrap loading screen
        /// </summary>
        public static void MarkAsTransition()
        {
            cameFromTransition = true;
        }

        /// <summary>
        /// Reset the transition flag (called when returning to main menu)
        /// </summary>
        public static void ResetTransitionFlag()
        {
            cameFromTransition = false;
            hasInitialized = false;
        }

        private void Awake()
        {
            // Check if we need to show loading screen
            bool needsLoadingScreen = showLoadingOnDirectPlay &&
                                       !cameFromTransition &&
                                       !IsLoadingScreenActive();

            if (needsLoadingScreen)
            {
                if (showDebugLogs)
                    UnityEngine.Debug.Log("GameSceneBootstrap: Direct play detected, showing loading screen");

                StartCoroutine(BootstrapWithLoadingScreen());
            }
            else
            {
                if (showDebugLogs)
                    UnityEngine.Debug.Log("GameSceneBootstrap: Came from transition or loading screen already active");

                // Reset flag for next time
                cameFromTransition = false;
            }
        }

        private bool IsLoadingScreenActive()
        {
            // Try to find existing loading screen
            loadingScreen = LoadingScreenManager.Instance;
            return loadingScreen != null && loadingScreen.IsShowing();
        }

        private IEnumerator BootstrapWithLoadingScreen()
        {
            isInitializing = true;
            float startTime = Time.realtimeSinceStartup;

            // Find or create loading screen
            loadingScreen = FindOrCreateLoadingScreen();

            if (loadingScreen != null)
            {
                loadingScreen.Show(true);
                loadingScreen.SetMessage("Initializing...");
                loadingScreen.SetProgress(0f);
            }

            // Wait a frame to ensure loading screen is visible
            yield return null;

            // Phase 1: Pre-initialization (10%)
            if (loadingScreen != null)
            {
                loadingScreen.SetMessage("Loading core systems...");
                loadingScreen.SetProgress(0.1f);
            }
            yield return null;

            // Phase 2: Let Unity initialize scene objects (10-30%)
            if (enableStaggeredInit)
            {
                for (int i = 0; i < framesToSpreadInit / 2; i++)
                {
                    if (loadingScreen != null)
                    {
                        float progress = 0.1f + (0.2f * (i / (float)(framesToSpreadInit / 2)));
                        loadingScreen.SetProgress(progress);
                    }
                    yield return null;
                }
            }

            // Phase 3: Resource loading (30-50%)
            if (loadingScreen != null)
            {
                loadingScreen.SetMessage("Loading resources...");
                loadingScreen.SetProgress(0.3f);
            }

            // Trigger garbage collection during loading to prevent stutters later
            System.GC.Collect();
            yield return null;

            if (loadingScreen != null)
            {
                loadingScreen.SetProgress(0.5f);
            }

            // Phase 4: System initialization (50-70%)
            if (loadingScreen != null)
            {
                loadingScreen.SetMessage("Initializing game systems...");
            }

            if (enableStaggeredInit)
            {
                for (int i = 0; i < framesToSpreadInit / 2; i++)
                {
                    if (loadingScreen != null)
                    {
                        float progress = 0.5f + (0.2f * (i / (float)(framesToSpreadInit / 2)));
                        loadingScreen.SetProgress(progress);
                    }
                    yield return null;
                }
            }

            // Phase 5: Final setup (70-90%)
            if (loadingScreen != null)
            {
                loadingScreen.SetMessage("Preparing world...");
                loadingScreen.SetProgress(0.7f);
            }

            yield return null;

            if (loadingScreen != null)
            {
                loadingScreen.SetProgress(0.9f);
            }

            // Ensure minimum loading time for visual feedback
            float elapsedTime = Time.realtimeSinceStartup - startTime;
            if (elapsedTime < minimumLoadingTime)
            {
                float remainingTime = minimumLoadingTime - elapsedTime;
                float waitedTime = 0f;

                while (waitedTime < remainingTime)
                {
                    waitedTime += Time.unscaledDeltaTime;
                    if (loadingScreen != null)
                    {
                        float progress = 0.9f + (0.1f * (waitedTime / remainingTime));
                        loadingScreen.SetProgress(Mathf.Min(progress, 0.99f));
                    }
                    yield return null;
                }
            }

            // Phase 6: Complete (100%)
            if (loadingScreen != null)
            {
                loadingScreen.SetMessage("Ready!");
                loadingScreen.SetProgress(1f);
            }

            yield return new WaitForSecondsRealtime(0.3f);

            // Hide loading screen
            if (loadingScreen != null)
            {
                loadingScreen.Hide();
            }

            isInitializing = false;
            hasInitialized = true;

            if (showDebugLogs)
                UnityEngine.Debug.Log($"GameSceneBootstrap: Initialization complete in {Time.realtimeSinceStartup - startTime:F2}s");
        }

        private LoadingScreenManager FindOrCreateLoadingScreen()
        {
            // First try to find existing
            var existing = LoadingScreenManager.Instance;
            if (existing != null)
            {
                return existing;
            }

            // Try to find in scene (might not be instance yet)
            existing = FindAnyObjectByType<LoadingScreenManager>();
            if (existing != null)
            {
                return existing;
            }

            // Try to load from resources
            var prefab = Resources.Load<GameObject>("LoadingScreen");
            if (prefab != null)
            {
                var instance = Instantiate(prefab);
                DontDestroyOnLoad(instance);
                return instance.GetComponent<LoadingScreenManager>();
            }

            if (showDebugLogs)
                UnityEngine.Debug.LogWarning("GameSceneBootstrap: Could not find or create LoadingScreenManager");

            return null;
        }

        /// <summary>
        /// Check if bootstrap initialization is still in progress
        /// </summary>
        public bool IsInitializing => isInitializing;

        /// <summary>
        /// Check if bootstrap has completed initialization
        /// </summary>
        public static bool HasInitialized => hasInitialized;
    }
}
