using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Core.Events;
using RTS.Units;
using RTS.Buildings;

namespace RTS.UI
{
    /// <summary>
    /// Complete example of subscribing to both Unit Group and Building Group events.
    /// This shows how to integrate group notifications into your UI system.
    /// 
    /// Attach this to a UI GameObject to display group assignment feedback.
    /// </summary>
    public class ControlGroupFeedbackUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private Image feedbackPanel;
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private bool enableSoundEffects = true;

        [Header("Colors")]
        [SerializeField] private Color unitGroupColor = new Color(0.3f, 1f, 0.3f, 0.8f);
        [SerializeField] private Color buildingGroupColor = new Color(1f, 0.8f, 0.2f, 0.8f);
        [SerializeField] private Color recallColor = new Color(0.5f, 0.8f, 1f, 0.8f);

        [Header("Optional Audio")]
        [SerializeField] private AudioClip groupSavedSound;
        [SerializeField] private AudioClip groupRecalledSound;
        [SerializeField] private AudioClip doubleClickSound;

        private AudioSource audioSource;
        private Coroutine currentFadeOut;

        private void Awake()
        {
            // Get or add AudioSource for sound effects
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && enableSoundEffects)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }

            // Hide feedback panel initially
            if (feedbackPanel != null)
            {
                feedbackPanel.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // ===== SUBSCRIBE TO UNIT GROUP EVENTS =====
            EventBus.Subscribe<UnitGroupSavedEvent>(OnUnitGroupSaved);
            EventBus.Subscribe<UnitGroupRecalledEvent>(OnUnitGroupRecalled);

            // ===== SUBSCRIBE TO BUILDING GROUP EVENTS =====
            EventBus.Subscribe<BuildingGroupSavedEvent>(OnBuildingGroupSaved);
            EventBus.Subscribe<BuildingGroupRecalledEvent>(OnBuildingGroupRecalled);
        }

        private void OnDisable()
        {
            // ===== UNSUBSCRIBE FROM UNIT GROUP EVENTS =====
            EventBus.Unsubscribe<UnitGroupSavedEvent>(OnUnitGroupSaved);
            EventBus.Unsubscribe<UnitGroupRecalledEvent>(OnUnitGroupRecalled);

            // ===== UNSUBSCRIBE FROM BUILDING GROUP EVENTS =====
            EventBus.Unsubscribe<BuildingGroupSavedEvent>(OnBuildingGroupSaved);
            EventBus.Unsubscribe<BuildingGroupRecalledEvent>(OnBuildingGroupRecalled);
        }

        #region Unit Group Event Handlers

        /// <summary>
        /// Called when units are saved to a group (Ctrl+Number)
        /// </summary>
        private void OnUnitGroupSaved(UnitGroupSavedEvent evt)
        {
            string message = $"⚔️ {evt.UnitCount} units assigned to Group {evt.GroupNumber}";
            
            Debug.Log($"[ControlGroups] {message}");
            
            ShowFeedback(message, unitGroupColor);
            PlaySound(groupSavedSound);
        }

        /// <summary>
        /// Called when a unit group is recalled (Number key)
        /// </summary>
        private void OnUnitGroupRecalled(UnitGroupRecalledEvent evt)
        {
            string message;
            
            if (evt.WasDoubleTap)
            {
                message = $"⚔️ Group {evt.GroupNumber} selected and centered ({evt.UnitCount} units)";
                PlaySound(doubleClickSound);
            }
            else
            {
                message = $"⚔️ Group {evt.GroupNumber} selected ({evt.UnitCount} units)";
                PlaySound(groupRecalledSound);
            }
            
            Debug.Log($"[ControlGroups] {message}");
            
            ShowFeedback(message, recallColor);
        }

        #endregion

        #region Building Group Event Handlers

        /// <summary>
        /// Called when a building is saved to a group (Ctrl+Number)
        /// </summary>
        private void OnBuildingGroupSaved(BuildingGroupSavedEvent evt)
        {
            string message = $"🏰 {evt.BuildingName} assigned to Group {evt.GroupNumber}";
            
            Debug.Log($"[ControlGroups] {message}");
            
            ShowFeedback(message, buildingGroupColor);
            PlaySound(groupSavedSound);
        }

        /// <summary>
        /// Called when a building group is recalled (Number key)
        /// </summary>
        private void OnBuildingGroupRecalled(BuildingGroupRecalledEvent evt)
        {
            string message;
            
            if (evt.WasDoubleTap)
            {
                message = $"🏰 {evt.BuildingName} selected and centered";
                PlaySound(doubleClickSound);
            }
            else
            {
                message = $"🏰 {evt.BuildingName} selected";
                PlaySound(groupRecalledSound);
            }
            
            Debug.Log($"[ControlGroups] {message}");
            
            ShowFeedback(message, recallColor);
        }

        #endregion

        #region UI Display Methods

        /// <summary>
        /// Shows feedback message on screen
        /// </summary>
        private void ShowFeedback(string message, Color color)
        {
            if (feedbackText == null) return;

            // Cancel any existing fade-out
            if (currentFadeOut != null)
            {
                StopCoroutine(currentFadeOut);
            }

            // Show the panel
            if (feedbackPanel != null)
            {
                feedbackPanel.gameObject.SetActive(true);
                feedbackPanel.color = color;
            }

            // Set the text
            feedbackText.text = message;
            feedbackText.color = Color.white;

            // Start fade-out timer
            currentFadeOut = StartCoroutine(FadeOutAfterDelay());
        }

        /// <summary>
        /// Fades out the feedback panel after a delay
        /// </summary>
        private System.Collections.IEnumerator FadeOutAfterDelay()
        {
            // Wait for display duration
            yield return new UnityEngine.WaitForSeconds(displayDuration);

            // Fade out over 0.5 seconds
            float elapsed = 0f;
            float fadeDuration = 0.5f;

            Color textStartColor = feedbackText.color;
            Color panelStartColor = feedbackPanel != null ? feedbackPanel.color : Color.clear;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / fadeDuration);

                if (feedbackText != null)
                {
                    Color textColor = textStartColor;
                    textColor.a = alpha;
                    feedbackText.color = textColor;
                }

                if (feedbackPanel != null)
                {
                    Color panelColor = panelStartColor;
                    panelColor.a = panelStartColor.a * alpha;
                    feedbackPanel.color = panelColor;
                }

                yield return null;
            }

            // Hide the panel
            if (feedbackPanel != null)
            {
                feedbackPanel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Plays a sound effect
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (!enableSoundEffects || audioSource == null || clip == null) return;
            
            audioSource.PlayOneShot(clip);
        }

        #endregion
    }

    // ============================================================
    // MINIMAL EXAMPLE (No UI, just console logging)
    // ============================================================

    /// <summary>
    /// Minimal example showing just the event subscription pattern
    /// without any UI. Perfect for understanding the basics.
    /// </summary>
    public class SimpleGroupListener : MonoBehaviour
    {
        private void OnEnable()
        {
            // Subscribe to all group events
            EventBus.Subscribe<UnitGroupSavedEvent>(OnUnitGroupSaved);
            EventBus.Subscribe<UnitGroupRecalledEvent>(OnUnitGroupRecalled);
            EventBus.Subscribe<BuildingGroupSavedEvent>(OnBuildingGroupSaved);
            EventBus.Subscribe<BuildingGroupRecalledEvent>(OnBuildingGroupRecalled);
        }

        private void OnDisable()
        {
            // Always unsubscribe to prevent memory leaks
            EventBus.Unsubscribe<UnitGroupSavedEvent>(OnUnitGroupSaved);
            EventBus.Unsubscribe<UnitGroupRecalledEvent>(OnUnitGroupRecalled);
            EventBus.Unsubscribe<BuildingGroupSavedEvent>(OnBuildingGroupSaved);
            EventBus.Unsubscribe<BuildingGroupRecalledEvent>(OnBuildingGroupRecalled);
        }

        private void OnUnitGroupSaved(UnitGroupSavedEvent evt)
        {
            Debug.Log($"Unit group {evt.GroupNumber} saved with {evt.UnitCount} units");
        }

        private void OnUnitGroupRecalled(UnitGroupRecalledEvent evt)
        {
            Debug.Log($"Unit group {evt.GroupNumber} recalled: {evt.UnitCount} units" +
                     (evt.WasDoubleTap ? " [Camera centered]" : ""));
        }

        private void OnBuildingGroupSaved(BuildingGroupSavedEvent evt)
        {
            Debug.Log($"Building '{evt.BuildingName}' saved to group {evt.GroupNumber}");
        }

        private void OnBuildingGroupRecalled(BuildingGroupRecalledEvent evt)
        {
            Debug.Log($"Building '{evt.BuildingName}' recalled from group {evt.GroupNumber}" +
                     (evt.WasDoubleTap ? " [Camera centered]" : ""));
        }
    }

    // ============================================================
    // ADVANCED EXAMPLE (Group Indicator UI)
    // ============================================================

    /// <summary>
    /// Advanced example: Shows group numbers above units/buildings
    /// that are assigned to groups. Updates in real-time.
    /// </summary>
    public class GroupIndicatorDisplay : MonoBehaviour
    {
        [Header("Group Display Settings")]
        [SerializeField] private GameObject groupIndicatorPrefab; // Should have a TextMeshPro component
        [SerializeField] private Vector3 indicatorOffset = new Vector3(0, 3, 0);
        [SerializeField] private bool showOnlyAssignedGroups = true;

        // Dictionary to track which groups have which objects
        private System.Collections.Generic.Dictionary<int, GameObject> unitGroupIndicators = 
            new System.Collections.Generic.Dictionary<int, GameObject>();
        
        private System.Collections.Generic.Dictionary<int, GameObject> buildingGroupIndicators = 
            new System.Collections.Generic.Dictionary<int, GameObject>();

        private UnitGroupManager unitGroupManager;
        private BuildingGroupManager buildingGroupManager;

        private void Start()
        {
            unitGroupManager = FindFirstObjectByType<UnitGroupManager>();
            buildingGroupManager = FindFirstObjectByType<BuildingGroupManager>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<UnitGroupSavedEvent>(OnUnitGroupSaved);
            EventBus.Subscribe<BuildingGroupSavedEvent>(OnBuildingGroupSaved);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<UnitGroupSavedEvent>(OnUnitGroupSaved);
            EventBus.Unsubscribe<BuildingGroupSavedEvent>(OnBuildingGroupSaved);
        }

        private void OnUnitGroupSaved(UnitGroupSavedEvent evt)
        {
            // Update indicator for this group
            UpdateUnitGroupIndicator(evt.GroupNumber);
        }

        private void OnBuildingGroupSaved(BuildingGroupSavedEvent evt)
        {
            // Update indicator for this group
            UpdateBuildingGroupIndicator(evt.GroupNumber);
        }

        private void UpdateUnitGroupIndicator(int groupNumber)
        {
            if (unitGroupManager == null) return;

            // Get units in this group
            var units = unitGroupManager.GetGroup(groupNumber);
            
            // TODO: Create/update visual indicators for these units
            Debug.Log($"Would update indicators for {units.Count} units in group {groupNumber}");
        }

        private void UpdateBuildingGroupIndicator(int groupNumber)
        {
            if (buildingGroupManager == null) return;

            // Get building in this group
            var building = buildingGroupManager.GetGroup(groupNumber);
            
            if (building != null)
            {
                // TODO: Create/update visual indicator for this building
                Debug.Log($"Would update indicator for building '{building.name}' in group {groupNumber}");
            }
        }
    }
}
