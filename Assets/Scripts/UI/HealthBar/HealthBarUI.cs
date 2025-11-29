using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Units.Components;
using RTS.Buildings.Components;

namespace RTS.UI.HealthBar
{
    /// <summary>
    /// World-space health bar that follows a unit or building
    /// Automatically attaches to UnitHealth or BuildingHealth
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Settings")]
        [SerializeField] private bool showHealthText = false;
        [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);
        [SerializeField] private bool hideWhenFull = true;
        [SerializeField] private bool alwaysFaceCamera = true;

        [Header("Colors")]
        [SerializeField] private Gradient healthGradient = new Gradient()
        {
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.red, 0f),
                new GradientColorKey(Color.yellow, 0.5f),
                new GradientColorKey(Color.green, 1f)
            }
        };

        [Header("Animation")]
        [SerializeField] private float smoothSpeed = 10f;
        [SerializeField] private bool animateChanges = true;

        private UnitHealth unitHealth;
        private BuildingHealth buildingHealth;
        private Transform targetTransform;
        private Camera mainCamera;
        private float targetFillAmount = 1f;
        private float currentFillAmount = 1f;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            // Get health component
            unitHealth = GetComponentInParent<UnitHealth>();
            buildingHealth = GetComponentInParent<BuildingHealth>();

            if (unitHealth == null && buildingHealth == null)
            {
                Debug.LogWarning($"HealthBarUI on {gameObject.name}: No UnitHealth or BuildingHealth found in parent!");
                enabled = false;
                return;
            }

            targetTransform = unitHealth != null ? unitHealth.transform : buildingHealth.transform;
            mainCamera = Camera.main;

            // Setup canvas
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }

            if (canvas != null)
            {
                canvas.worldCamera = mainCamera;
            }

            // Setup canvas group for fading
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Initial update
            UpdateHealthBar();
        }

        private void OnEnable()
        {
            if (unitHealth != null)
            {
                unitHealth.OnHealthChanged += OnHealthChanged;
            }

            if (buildingHealth != null)
            {
                buildingHealth.OnHealthChanged += OnHealthChanged;
            }
        }

        private void OnDisable()
        {
            if (unitHealth != null)
            {
                unitHealth.OnHealthChanged -= OnHealthChanged;
            }

            if (buildingHealth != null)
            {
                buildingHealth.OnHealthChanged -= OnHealthChanged;
            }
        }

        private void LateUpdate()
        {
            if (targetTransform == null)
                return;

            // Update position
            transform.position = targetTransform.position + offset;

            // Face camera
            if (alwaysFaceCamera && mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
            }

            // Animate fill amount
            if (animateChanges)
            {
                currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * smoothSpeed);
                if (fillImage != null)
                {
                    fillImage.fillAmount = currentFillAmount;
                }
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            UpdateHealthBar();
        }

        private void UpdateHealthBar()
        {
            float current = unitHealth != null ? unitHealth.CurrentHealth : buildingHealth.CurrentHealth;
            float max = unitHealth != null ? unitHealth.MaxHealth : buildingHealth.MaxHealth;
            float healthPercentage = max > 0 ? current / max : 0f;

            targetFillAmount = healthPercentage;

            if (!animateChanges && fillImage != null)
            {
                fillImage.fillAmount = healthPercentage;
                currentFillAmount = healthPercentage;
            }

            // Update color
            if (fillImage != null)
            {
                fillImage.color = healthGradient.Evaluate(healthPercentage);
            }

            // Update text
            if (healthText != null && showHealthText)
            {
                healthText.text = $"{current:F0}/{max:F0}";
            }

            // Hide/show based on health
            if (hideWhenFull && canvasGroup != null)
            {
                canvasGroup.alpha = healthPercentage >= 1f ? 0f : 1f;
            }
        }

        #region Auto Setup

        public static GameObject CreateHealthBar(GameObject target, GameObject healthBarPrefab)
        {
            if (target == null || healthBarPrefab == null)
                return null;

            GameObject healthBarObj = Instantiate(healthBarPrefab, target.transform);
            return healthBarObj;
        }

        #endregion

        private void OnValidate()
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }

            if (fillImage == null)
            {
                fillImage = GetComponentInChildren<Image>();
            }
        }
    }
}
