using UnityEngine;
using UnityEngine.UI;

namespace KAD.UI.FloatingNumbers
{
    /// <summary>
    /// Health bar that follows a game object (unit or building).
    /// Automatically hides when at full health (if configured).
    /// </summary>
    public class HPBar : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup canvasGroup;

        private GameObject target;
        private System.Func<float> getCurrentHealth;
        private System.Func<float> getMaxHealth;
        private FloatingNumbersSettings settings;

        private Camera mainCamera;
        private float currentHealthPercentage = 1f;
        private bool isInitialized;
        private int frameCounter;

        private void Awake()
        {
            if (canvas == null)
                canvas = GetComponent<Canvas>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvas != null)
            {
                canvas.renderMode = RenderMode.WorldSpace;
            }
        }

        /// <summary>
        /// Initialize the HP bar for a target GameObject.
        /// </summary>
        public void Initialize(
            GameObject target,
            System.Func<float> getCurrentHealth,
            System.Func<float> getMaxHealth,
            FloatingNumbersSettings settings)
        {
            this.target = target;
            this.getCurrentHealth = getCurrentHealth;
            this.getMaxHealth = getMaxHealth;
            this.settings = settings;

            mainCamera = Camera.main;
            isInitialized = true;
            frameCounter = 0;

            // Configure canvas
            if (canvas != null)
            {
                canvas.worldCamera = mainCamera;
            }

            // Set initial size
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(
                    settings.HPBarWidth * 100f,
                    settings.HPBarHeight * 100f
                );
            }

            // Set background color
            if (backgroundImage != null)
            {
                backgroundImage.color = settings.HPBarBackgroundColor;
            }

            UpdateHealthBar();
            gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (!isInitialized || target == null)
            {
                return;
            }

            // Update position to follow target
            UpdatePosition();

            // Update health bar periodically (not every frame for performance)
            frameCounter++;
            if (frameCounter >= settings.HPBarUpdateInterval)
            {
                frameCounter = 0;
                UpdateHealthBar();
            }
        }

        private void UpdatePosition()
        {
            if (target == null || mainCamera == null)
                return;

            // Position above target
            Vector3 worldPosition = target.transform.position + Vector3.up * settings.HPBarOffset;
            transform.position = worldPosition;

            // Make HP bar face camera
            if (mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
            }
        }

        private void UpdateHealthBar()
        {
            if (getCurrentHealth == null || getMaxHealth == null)
                return;

            float currentHP = getCurrentHealth();
            float maxHP = getMaxHealth();

            if (maxHP <= 0)
            {
                currentHealthPercentage = 0f;
            }
            else
            {
                currentHealthPercentage = Mathf.Clamp01(currentHP / maxHP);
            }

            // Update fill amount
            if (fillImage != null)
            {
                fillImage.fillAmount = currentHealthPercentage;

                // Update color based on health percentage
                fillImage.color = settings.GetHPBarColor(currentHealthPercentage);
            }

            // Show/hide based on settings
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (canvasGroup == null)
                return;

            bool shouldShow = true;

            // Hide if HP bars are disabled
            if (!settings.ShowHPBars)
            {
                shouldShow = false;
            }
            // Hide if only showing when damaged and at full health
            else if (settings.HPBarsOnlyWhenDamaged && currentHealthPercentage >= 0.99f)
            {
                shouldShow = false;
            }
            // Future: Hide if only showing for selected units and this unit is not selected
            // This would require integration with the selection system

            canvasGroup.alpha = shouldShow ? 1f : 0f;
            canvasGroup.interactable = shouldShow;
            canvasGroup.blocksRaycasts = shouldShow;
        }

        /// <summary>
        /// Manually refresh the HP bar (useful when settings change).
        /// </summary>
        public void Refresh()
        {
            UpdateHealthBar();
        }

        /// <summary>
        /// Check if this HP bar belongs to the specified target.
        /// </summary>
        public bool IsForTarget(GameObject obj)
        {
            return target == obj;
        }

        /// <summary>
        /// Get the target GameObject this HP bar is tracking.
        /// </summary>
        public GameObject GetTarget()
        {
            return target;
        }

        /// <summary>
        /// Clean up when returning to pool or destroying.
        /// </summary>
        public void Cleanup()
        {
            target = null;
            getCurrentHealth = null;
            getMaxHealth = null;
            isInitialized = false;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
