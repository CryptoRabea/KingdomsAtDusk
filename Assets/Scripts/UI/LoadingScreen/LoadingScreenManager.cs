using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace RTS.UI
{
    /// <summary>
    /// Manages loading screen display with progress bar and tips.
    /// Can be used before main menu or between scene transitions.
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
        [SerializeField] private float progressSmoothSpeed = 2f;

        [Header("Loading Tips")]
        [SerializeField] private string[] loadingTips = new string[]
        {
            "Tip: Build houses to increase your population",
            "Tip: Manage resources wisely for your kingdom",
            "Tip: Use control groups to manage your army",
            "Tip: Scout the fog of war to reveal the map",
            "Tip: Buildings can train different unit types"
        };

        private static LoadingScreenManager instance;
        private float targetProgress = 0f;
        private float currentProgress = 0f;
        private float loadingStartTime;
        private bool isLoading = false;

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
                yield return new WaitForSeconds(minimumDisplayTime - elapsedTime);
            }

            // Ensure progress reaches 100%
            SetProgress(1f);
            yield return new WaitForSeconds(0.2f);

            if (loadingScreenRoot != null)
            {
                loadingScreenRoot.SetActive(false);
            }

            isLoading = false;
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

            if (smoothProgress && Mathf.Abs(currentProgress - targetProgress) > 0.01f)
            {
                currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * progressSmoothSpeed);
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
