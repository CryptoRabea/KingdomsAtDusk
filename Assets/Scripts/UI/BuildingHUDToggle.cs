using UnityEngine;
using UnityEngine.UI;

namespace RTS.UI
{
    /// <summary>
    /// Toggle button to show/hide the BuildingHUD panel.
    /// Attach this to a UI Button.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BuildingHUDToggle : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BuildingHUD buildingHUD;
        [SerializeField] private GameObject panelToToggle;

        [Header("Button Icons (Optional)")]
        [SerializeField] private Image buttonImage;
        [SerializeField] private Sprite iconWhenOpen;
        [SerializeField] private Sprite iconWhenClosed;

        [Header("Settings")]
        [SerializeField] private bool startOpen = true;

        private Button button;
        private bool isPanelOpen;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(TogglePanel);

            // Validate BuildingHUD reference
            if (buildingHUD == null && panelToToggle == null)
            {
            }
        }

        private void Start()
        {
            // Set initial state
            isPanelOpen = startOpen;
            UpdatePanelVisibility(false); // false = don't animate on start
        }

        public void TogglePanel()
        {
            // Sync with actual panel state before toggling
            SyncPanelState();

            isPanelOpen = !isPanelOpen;
            UpdatePanelVisibility(true);
        }

        private void SyncPanelState()
        {
            // Check the actual panel state to ensure we're in sync
            if (panelToToggle != null)
            {
                isPanelOpen = panelToToggle.activeSelf;
            }
        }

        public void ShowPanel()
        {
            isPanelOpen = true;
            UpdatePanelVisibility(true);
        }

        public void HidePanel()
        {
            isPanelOpen = false;
            UpdatePanelVisibility(true);
        }

        private void UpdatePanelVisibility(bool playSound)
        {
            // Update via BuildingHUD if available
            if (buildingHUD != null)
            {
                buildingHUD.SetPanelVisible(isPanelOpen);
            }

            // Or directly toggle the panel
            if (panelToToggle != null)
            {
                panelToToggle.SetActive(isPanelOpen);
            }

            // Update button icon
            UpdateButtonIcon();

            if (playSound)
            {
                // Optional: Play UI sound here
            }
        }

        private void UpdateButtonIcon()
        {
            if (buttonImage != null)
            {
                if (isPanelOpen && iconWhenOpen != null)
                {
                    buttonImage.sprite = iconWhenOpen;
                }
                else if (!isPanelOpen && iconWhenClosed != null)
                {
                    buttonImage.sprite = iconWhenClosed;
                }
            }
        }

        /// <summary>
        /// Get the current panel state.
        /// </summary>
        public bool IsPanelOpen => isPanelOpen;
    }
}
