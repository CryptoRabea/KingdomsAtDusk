using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RTSBuildingsSystems
{
    /// <summary>
    /// Handles the world-space UI for building construction progress and health display.
    /// Shows blue progress bar filling right-to-left during construction,
    /// then switches to health bar after completion.
    /// </summary>
    public class BuildingProgressUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas worldCanvas;
        [SerializeField] private Image progressBarBackground;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI buildingNameText;

        [Header("Construction Settings")]
        [SerializeField] private Color constructionColor = new Color(0.2f, 0.5f, 1f, 0.8f);
        [SerializeField] private Color constructionBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        [Header("Health Settings")]
        [SerializeField] private Color healthyColor = new Color(0f, 1f, 0f, 0.8f);
        [SerializeField] private Color damagedColor = new Color(1f, 0.65f, 0f, 0.8f);
        [SerializeField] private Color criticalColor = new Color(1f, 0f, 0f, 0.8f);
        [SerializeField] private float damagedThreshold = 0.6f;
        [SerializeField] private float criticalThreshold = 0.3f;

        [Header("Animation")]
        [SerializeField] private bool animateBar = true;
        [SerializeField] private float animationSpeed = 5f;

        [Header("Camera Settings")]
        [SerializeField] private bool faceCamera = true;
        [SerializeField] private Vector3 uiOffset = new Vector3(0, 3, 0);

        private Camera mainCamera;
        private float targetFillAmount = 0f;
        private bool isConstruction = true;
        private Building building;
        private BuildingHealth buildingHealth;

        private void Start()
        {
            mainCamera = Camera.main;
            building = GetComponentInParent<Building>();
            buildingHealth = GetComponentInParent<BuildingHealth>();

            // Setup canvas
            if (worldCanvas != null)
            {
                worldCanvas.worldCamera = mainCamera;
                worldCanvas.transform.position = transform.position + uiOffset;
            }

            // Initialize colors
            if (progressBarBackground != null)
            {
                progressBarBackground.color = constructionBackgroundColor;
            }

            if (progressBarFill != null)
            {
                progressBarFill.color = constructionColor;
                progressBarFill.fillAmount = 0f;
                progressBarFill.type = Image.Type.Filled;
                progressBarFill.fillMethod = Image.FillMethod.Horizontal;
                progressBarFill.fillOrigin = (int)Image.OriginHorizontal.Right; // Right to left
            }

            // Set building name
            if (buildingNameText != null && building != null)
            {
                buildingNameText.text = building.BuildingName;
            }
        }

        private void Update()
        {
            // Face camera
            if (faceCamera && mainCamera != null && worldCanvas != null)
            {
                worldCanvas.transform.rotation = Quaternion.LookRotation(worldCanvas.transform.position - mainCamera.transform.position);
            }

            // Animate bar fill
            if (animateBar && progressBarFill != null)
            {
                float currentFill = progressBarFill.fillAmount;
                progressBarFill.fillAmount = Mathf.Lerp(currentFill, targetFillAmount, Time.deltaTime * animationSpeed);
            }
        }

        /// <summary>
        /// Update construction progress (0-1)
        /// </summary>
        public void UpdateProgress(float progress, bool isConstructing)
        {
            isConstruction = isConstructing;
            targetFillAmount = Mathf.Clamp01(progress);

            if (!animateBar && progressBarFill != null)
            {
                progressBarFill.fillAmount = targetFillAmount;
            }

            // Update colors for construction
            if (isConstruction && progressBarFill != null)
            {
                progressBarFill.color = constructionColor;
                progressBarFill.fillOrigin = (int)Image.OriginHorizontal.Right; // Right to left
            }

            // Update text
            if (progressText != null)
            {
                if (isConstruction)
                {
                    progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
                }
            }
        }

        /// <summary>
        /// Update health display (0-1)
        /// </summary>
        public void UpdateHealth(float healthPercent)
        {
            isConstruction = false;
            targetFillAmount = Mathf.Clamp01(healthPercent);

            if (!animateBar && progressBarFill != null)
            {
                progressBarFill.fillAmount = targetFillAmount;
            }

            // Update colors based on health
            if (progressBarFill != null)
            {
                progressBarFill.fillOrigin = (int)Image.OriginHorizontal.Left; // Left to right for health

                if (healthPercent >= damagedThreshold)
                {
                    progressBarFill.color = healthyColor;
                }
                else if (healthPercent >= criticalThreshold)
                {
                    progressBarFill.color = damagedColor;
                }
                else
                {
                    progressBarFill.color = criticalColor;
                }
            }

            // Update text
            if (progressText != null && buildingHealth != null)
            {
                progressText.text = $"{Mathf.RoundToInt(buildingHealth.CurrentHealth)}/{Mathf.RoundToInt(buildingHealth.MaxHealth)}";
            }
        }

        /// <summary>
        /// Show/hide the UI
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (worldCanvas != null)
            {
                worldCanvas.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Update building name display
        /// </summary>
        public void SetBuildingName(string name)
        {
            if (buildingNameText != null)
            {
                buildingNameText.text = name;
            }
        }
    }
}
