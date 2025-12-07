using UnityEngine;
using UnityEngine.UI;

namespace RTSGame.UI
{
    /// <summary>
    /// Manages the collapsible HUD button that toggles the main HUD panel visibility
    /// and adjusts the camera viewport accordingly.
    /// </summary>
    public class CollapsibleHUDButton : MonoBehaviour
    {
        [Header("Panel References")]
        [Tooltip("The main HUD panel to collapse/expand")]
        [SerializeField] private RectTransform hudPanel;

        [Tooltip("The button that stays visible when HUD is collapsed")]
        [SerializeField] private Button toggleButton;

        [Tooltip("Image component for the button icon")]
        [SerializeField] private Image buttonIcon;

        [Header("Button Icons")]
        [Tooltip("Icon shown when HUD is expanded (default: <)")]
        [SerializeField] private Sprite expandedIcon;

        [Tooltip("Icon shown when HUD is collapsed (default: >)")]
        [SerializeField] private Sprite collapsedIcon;

        [Header("Keyboard Shortcut")]
        [Tooltip("Keyboard shortcut to toggle the HUD (default: <)")]
        [SerializeField] private KeyCode toggleShortcut = KeyCode.Comma; // < key

        [Header("Camera References")]
        [Tooltip("The RTS camera controller (will be auto-found if not set)")]
        [SerializeField] private Camera mainCamera;

        [Header("Animation Settings")]
        [Tooltip("Speed of the slide animation")]
        [SerializeField] private float animationSpeed = 5f;

        [Tooltip("Offset in pixels to slide the HUD panel down")]
        [SerializeField] private float slideOffset = 500f;

        // Cached viewport values
        private Rect cachedViewportRect;
        private Vector2 targetPosition;
        private Vector2 expandedPosition;
        private Vector2 collapsedPosition;
        private bool isExpanded = true;
        private bool isAnimating = false;

        private void Awake()
        {
            // Find camera if not set
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // Cache initial viewport values
            if (mainCamera != null)
            {
                cachedViewportRect = mainCamera.rect;
            }

            // Set up button listener
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(ToggleHUD);
            }

            // Cache positions
            if (hudPanel != null)
            {
                expandedPosition = hudPanel.anchoredPosition;
                collapsedPosition = new Vector2(expandedPosition.x, expandedPosition.y - slideOffset);
                targetPosition = expandedPosition;
            }
        }

        private void Start()
        {
            // Ensure HUD starts expanded
            if (hudPanel != null)
            {
                hudPanel.anchoredPosition = expandedPosition;
            }

            UpdateButtonIcon();
        }

        private void Update()
        {
            // Check for keyboard shortcut
            if (Input.GetKeyDown(toggleShortcut))
            {
                ToggleHUD();
            }

            // Animate panel position
            if (isAnimating && hudPanel != null)
            {
                hudPanel.anchoredPosition = Vector2.Lerp(
                    hudPanel.anchoredPosition,
                    targetPosition,
                    Time.deltaTime * animationSpeed
                );

                // Check if animation is complete
                if (Vector2.Distance(hudPanel.anchoredPosition, targetPosition) < 0.1f)
                {
                    hudPanel.anchoredPosition = targetPosition;
                    isAnimating = false;
                }
            }
        }

        /// <summary>
        /// Toggles the HUD panel between expanded and collapsed states
        /// </summary>
        public void ToggleHUD()
        {
            if (isExpanded)
            {
                CollapseHUD();
            }
            else
            {
                ExpandHUD();
            }
        }

        /// <summary>
        /// Collapses the HUD panel and adjusts viewport
        /// </summary>
        public void CollapseHUD()
        {
            isExpanded = false;
            targetPosition = collapsedPosition;
            isAnimating = true;

            // Set viewport to full screen
            if (mainCamera != null)
            {
                mainCamera.rect = new Rect(0, 0, 1, 1);
            }

            UpdateButtonIcon();
        }

        /// <summary>
        /// Expands the HUD panel and restores viewport
        /// </summary>
        public void ExpandHUD()
        {
            isExpanded = true;
            targetPosition = expandedPosition;
            isAnimating = true;

            // Restore cached viewport
            if (mainCamera != null)
            {
                mainCamera.rect = cachedViewportRect;
            }

            UpdateButtonIcon();
        }

        /// <summary>
        /// Updates the button icon based on current state
        /// </summary>
        private void UpdateButtonIcon()
        {
            if (buttonIcon != null)
            {
                buttonIcon.sprite = isExpanded ? expandedIcon : collapsedIcon;
            }
        }

        /// <summary>
        /// Returns true if the HUD is currently expanded
        /// </summary>
        public bool IsExpanded => isExpanded;

        /// <summary>
        /// Re-cache the viewport values (useful if viewport changes during gameplay)
        /// </summary>
        public void RecacheViewport()
        {
            if (mainCamera != null && isExpanded)
            {
                cachedViewportRect = mainCamera.rect;
            }
        }

        private void OnValidate()
        {
            // Update positions when values change in inspector
            if (hudPanel != null && Application.isPlaying)
            {
                expandedPosition = hudPanel.anchoredPosition;
                collapsedPosition = new Vector2(expandedPosition.x, expandedPosition.y - slideOffset);
            }
        }
    }
}
