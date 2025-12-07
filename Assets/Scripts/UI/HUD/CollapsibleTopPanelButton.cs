using UnityEngine;
using UnityEngine.UI;

namespace RTSGame.UI
{
    /// <summary>
    /// Manages the collapsible top panel button that toggles the top bar visibility
    /// and adjusts the camera viewport accordingly.
    /// </summary>
    public class CollapsibleTopPanelButton : MonoBehaviour
    {
        [Header("Panel References")]
        [Tooltip("The top panel to collapse/expand")]
        [SerializeField] private RectTransform topPanel;

        [Tooltip("The button that stays visible when top panel is collapsed")]
        [SerializeField] private Button toggleButton;

        [Tooltip("Image component for the button icon")]
        [SerializeField] private Image buttonIcon;

        [Header("Button Icons")]
        [Tooltip("Icon shown when panel is expanded (default: >)")]
        [SerializeField] private Sprite expandedIcon;

        [Tooltip("Icon shown when panel is collapsed (default: <)")]
        [SerializeField] private Sprite collapsedIcon;

        [Header("Keyboard Shortcut")]
        [Tooltip("Keyboard shortcut to toggle the top panel (default: >)")]
        [SerializeField] private KeyCode toggleShortcut = KeyCode.Period; // > key

        [Header("Camera References")]
        [Tooltip("The RTS camera controller (will be auto-found if not set)")]
        [SerializeField] private Camera mainCamera;

        [Header("Animation Settings")]
        [Tooltip("Speed of the slide animation")]
        [SerializeField] private float animationSpeed = 5f;

        [Tooltip("Offset in pixels to slide the top panel up")]
        [SerializeField] private float slideOffset = 100f;

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
                toggleButton.onClick.AddListener(TogglePanel);
            }

            // Cache positions
            if (topPanel != null)
            {
                expandedPosition = topPanel.anchoredPosition;
                collapsedPosition = new Vector2(expandedPosition.x, expandedPosition.y + slideOffset);
                targetPosition = expandedPosition;
            }
        }

        private void Start()
        {
            // Ensure panel starts expanded
            if (topPanel != null)
            {
                topPanel.anchoredPosition = expandedPosition;
            }

            UpdateButtonIcon();
        }

        private void Update()
        {
            // Check for keyboard shortcut
            if (Input.GetKeyDown(toggleShortcut))
            {
                TogglePanel();
            }

            // Animate panel position
            if (isAnimating && topPanel != null)
            {
                topPanel.anchoredPosition = Vector2.Lerp(
                    topPanel.anchoredPosition,
                    targetPosition,
                    Time.deltaTime * animationSpeed
                );

                // Check if animation is complete
                if (Vector2.Distance(topPanel.anchoredPosition, targetPosition) < 0.1f)
                {
                    topPanel.anchoredPosition = targetPosition;
                    isAnimating = false;
                }
            }
        }

        /// <summary>
        /// Toggles the top panel between expanded and collapsed states
        /// </summary>
        public void TogglePanel()
        {
            if (isExpanded)
            {
                CollapsePanel();
            }
            else
            {
                ExpandPanel();
            }
        }

        /// <summary>
        /// Collapses the top panel and adjusts viewport
        /// </summary>
        public void CollapsePanel()
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
        /// Expands the top panel and restores viewport
        /// </summary>
        public void ExpandPanel()
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
        /// Returns true if the panel is currently expanded
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
            if (topPanel != null && Application.isPlaying)
            {
                expandedPosition = topPanel.anchoredPosition;
                collapsedPosition = new Vector2(expandedPosition.x, expandedPosition.y + slideOffset);
            }
        }
    }
}
