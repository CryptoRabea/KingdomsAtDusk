using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace RTS.UI
{
    /// <summary>
    /// Manages loading screen display with progress bar and tips.
    /// Can be used before main menu or between scene transitions.
    /// Includes performance optimizations for smoother loading.
    /// </summary>
    public class LoadingScreenManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject loadingScreenRoot;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI loadingTipText;
        [SerializeField] private Image backgroundImage;

        [Header("Settings")]
        [SerializeField] private float minimumDisplayTime = 1f;
        [SerializeField] private bool smoothProgress = true;
        [SerializeField] private float progressSmoothSpeed = 3f;

        [Header("Performance Optimizations")]
        [Tooltip("Run garbage collection during loading to reduce stutters during gameplay")]
        [SerializeField] private bool runGCDuringLoading = true;
        [Tooltip("Unload unused assets during loading")]
        [SerializeField] private bool unloadUnusedAssets = true;
        [Tooltip("Lower quality settings during loading for faster load")]
        [SerializeField] private bool reducedQualityDuringLoad = false;

        [Header("Loading Tips")]
        [SerializeField] private string[] loadingTips = new string[]
        {
            "Tip: Build houses to increase your population",
            "Tip: Manage resources wisely for your kingdom",
            "Tip: Use control groups to manage your army",
            "Tip: Scout the fog of war to reveal the map",
            "Tip: Buildings can train different unit types",
            "Tip: Walls slow down enemy units",
            "Tip: Towers can be built on walls for defense",
            "Tip: Keep your peasants happy for better production"
        };

        private static LoadingScreenManager instance;
        private float targetProgress = 0f;
        private float currentProgress = 0f;
        private float loadingStartTime;
        private bool isLoading = false;
        private int originalVSyncCount;
        private int originalTargetFrameRate;
        private AsyncOperation cleanupOperation;

        public static LoadingScreenManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<LoadingScreenManager>();
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

            if (loadingScreenRoot != null)
            {
                loadingScreenRoot.SetActive(false);
            }
        }

        /// <summary>
        /// Show the loading screen with optional tip
        /// </summary>
        public void Show(bool showTip = true)
        {
            if (loadingScreenRoot == null)
            {
                return;
            }

            loadingScreenRoot.SetActive(true);
            isLoading = true;
            loadingStartTime = Time.realtimeSinceStartup;
            targetProgress = 0f;
            currentProgress = 0f;
            UpdateProgress(0f);

            // Apply performance optimizations during loading
            ApplyLoadingOptimizations();

            // Start background cleanup operations
            StartCoroutine(PerformBackgroundOptimizations());

            if (showTip && loadingTipText != null && loadingTips.Length > 0)
            {
                string randomTip = loadingTips[Random.Range(0, loadingTips.Length)];
                loadingTipText.text = randomTip;
                loadingTipText.gameObject.SetActive(true);
            }
            else if (loadingTipText != null)
            {
                loadingTipText.gameObject.SetActive(false);
            }
        }

        private void ApplyLoadingOptimizations()
        {
            // Store original settings
            originalVSyncCount = QualitySettings.vSyncCount;
            originalTargetFrameRate = Application.targetFrameRate;

            // Disable VSync during loading for faster asset loading
            if (reducedQualityDuringLoad)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1; // Uncapped
            }
        }

        private void RestoreSettings()
        {
            // Restore original settings
            if (reducedQualityDuringLoad)
            {
                QualitySettings.vSyncCount = originalVSyncCount;
                Application.targetFrameRate = originalTargetFrameRate;
            }
        }

        private IEnumerator PerformBackgroundOptimizations()
        {
            // Wait a frame to let loading screen appear
            yield return null;

            // Run garbage collection during loading to reduce stutters during gameplay
            if (runGCDuringLoading)
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
            }

            yield return null;

            // Unload unused assets
            if (unloadUnusedAssets)
            {
                cleanupOperation = Resources.UnloadUnusedAssets();
                while (cleanupOperation != null && !cleanupOperation.isDone)
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Hide the loading screen (respects minimum display time)
        /// </summary>
        public void Hide()
        {
            StartCoroutine(HideCoroutine());
        }

        private IEnumerator HideCoroutine()
        {
            // Ensure minimum display time
            float elapsedTime = Time.realtimeSinceStartup - loadingStartTime;
            if (elapsedTime < minimumDisplayTime)
            {
                yield return new WaitForSecondsRealtime(minimumDisplayTime - elapsedTime);
            }

            // Wait for cleanup operation to complete
            if (cleanupOperation != null && !cleanupOperation.isDone)
            {
                yield return cleanupOperation;
            }

            // Final garbage collection before gameplay starts
            if (runGCDuringLoading)
            {
                System.GC.Collect();
            }

            // Ensure progress reaches 100%
            SetProgress(1f);
            yield return new WaitForSecondsRealtime(0.2f);

            // Restore original settings before hiding
            RestoreSettings();

            if (loadingScreenRoot != null)
            {
                loadingScreenRoot.SetActive(false);
            }

            isLoading = false;
            cleanupOperation = null;
        }

        /// <summary>
        /// Set loading progress (0 to 1)
        /// </summary>
        public void SetProgress(float progress)
        {
            targetProgress = Mathf.Clamp01(progress);

            if (!smoothProgress)
            {
                currentProgress = targetProgress;
                UpdateProgress(currentProgress);
            }
        }

        /// <summary>
        /// Set loading message
        /// </summary>
        public void SetMessage(string message)
        {
            if (progressText != null)
            {
                progressText.text = message;
            }
        }

        private void Update()
        {
            if (!isLoading) return;

            // Use unscaledDeltaTime so progress updates even when game is paused
            if (smoothProgress && Mathf.Abs(currentProgress - targetProgress) > 0.01f)
            {
                currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.unscaledDeltaTime * progressSmoothSpeed);
                UpdateProgress(currentProgress);
            }
        }

        private void UpdateProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            if (progressText != null && string.IsNullOrEmpty(progressText.text))
            {
                progressText.text = $"Loading... {(progress * 100f):F0}%";
            }
        }

        /// <summary>
        /// Set background image (useful for scene-specific loading screens)
        /// </summary>
        public void SetBackgroundImage(Sprite sprite)
        {
            if (backgroundImage != null)
            {
                backgroundImage.sprite = sprite;
            }
        }

        /// <summary>
        /// Check if loading screen is currently shown
        /// </summary>
        public bool IsShowing()
        {
            return isLoading && loadingScreenRoot != null && loadingScreenRoot.activeSelf;
        }
    }
}
