using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Core;
using RTS.Core.Events;
using RTS.Core.Services;

namespace RTS.DayNightCycle
{
    /// <summary>
    /// UI component that displays the current time, day, and phase.
    /// Features include clock display, day counter, phase indicator, and optional progress bar.
    /// </summary>
    public class TimeDisplayUI : MonoBehaviour
    {
        [Header("=== TEXT ELEMENTS ===")]
        [Tooltip("Text element for displaying time (e.g., '14:30' or '2:30 PM')")]
        [SerializeField] private TextMeshProUGUI timeText;

        [Tooltip("Text element for displaying day number (e.g., 'Day 5')")]
        [SerializeField] private TextMeshProUGUI dayText;

        [Tooltip("Text element for displaying phase (e.g., 'Dawn', 'Day', 'Dusk', 'Night')")]
        [SerializeField] private TextMeshProUGUI phaseText;

        [Header("=== PROGRESS INDICATORS ===")]
        [Tooltip("Progress bar showing day progress (0-1)")]
        [SerializeField] private Slider dayProgressBar;

        [Tooltip("Image for day progress fill (for color changes)")]
        [SerializeField] private Image dayProgressFill;

        [Tooltip("Radial clock image (optional, shows time as radial progress)")]
        [SerializeField] private Image radialClockImage;

        [Header("=== ICONS ===")]
        [Tooltip("Sun icon (shown during day)")]
        [SerializeField] private GameObject sunIcon;

        [Tooltip("Moon icon (shown during night)")]
        [SerializeField] private GameObject moonIcon;

        [Tooltip("Phase icon image (changes based on phase)")]
        [SerializeField] private Image phaseIcon;

        [Tooltip("Dawn phase icon sprite")]
        [SerializeField] private Sprite dawnSprite;

        [Tooltip("Day phase icon sprite")]
        [SerializeField] private Sprite daySprite;

        [Tooltip("Dusk phase icon sprite")]
        [SerializeField] private Sprite duskSprite;

        [Tooltip("Night phase icon sprite")]
        [SerializeField] private Sprite nightSprite;

        [Header("=== DISPLAY SETTINGS ===")]
        [Tooltip("Use 24-hour format (true) or 12-hour AM/PM format (false)")]
        [SerializeField] private bool use24HourFormat = true;

        [Tooltip("Show day counter")]
        [SerializeField] private bool showDayCounter = true;

        [Tooltip("Show phase text")]
        [SerializeField] private bool showPhaseText = true;

        [Tooltip("Show progress bar")]
        [SerializeField] private bool showProgressBar = true;

        [Tooltip("Day counter prefix (e.g., 'Day ')")]
        [SerializeField] private string dayPrefix = "Day ";

        [Tooltip("Animate time changes")]
        [SerializeField] private bool animateChanges = true;

        [Header("=== COLOR SETTINGS ===")]
        [Tooltip("Text color during day")]
        [SerializeField] private Color dayTextColor = Color.white;

        [Tooltip("Text color during night")]
        [SerializeField] private Color nightTextColor = new Color(0.7f, 0.8f, 1f);

        [Tooltip("Progress bar color during day")]
        [SerializeField] private Color dayProgressColor = new Color(1f, 0.9f, 0.5f);

        [Tooltip("Progress bar color during dawn")]
        [SerializeField] private Color dawnProgressColor = new Color(1f, 0.6f, 0.4f);

        [Tooltip("Progress bar color during dusk")]
        [SerializeField] private Color duskProgressColor = new Color(1f, 0.4f, 0.3f);

        [Tooltip("Progress bar color during night")]
        [SerializeField] private Color nightProgressColor = new Color(0.3f, 0.4f, 0.8f);

        [Header("=== TIME SPEED CONTROLS ===")]
        [Tooltip("Show time speed controls")]
        [SerializeField] private bool showSpeedControls = false;

        [Tooltip("Button to pause time")]
        [SerializeField] private Button pauseButton;

        [Tooltip("Button to set normal speed")]
        [SerializeField] private Button normalSpeedButton;

        [Tooltip("Button to set fast speed")]
        [SerializeField] private Button fastSpeedButton;

        [Tooltip("Text showing current speed")]
        [SerializeField] private TextMeshProUGUI speedText;

        [Tooltip("Fast time scale")]
        [SerializeField] private float fastTimeScale = 3f;

        [Header("=== ANIMATION ===")]
        [Tooltip("Animator for UI animations (optional)")]
        [SerializeField] private Animator uiAnimator;

        [Tooltip("Animation trigger for phase change")]
        [SerializeField] private string phaseChangeTrigger = "PhaseChange";

        [Tooltip("Animation trigger for hour change")]
        [SerializeField] private string hourChangeTrigger = "HourChange";

        [Tooltip("Animation trigger for new day")]
        [SerializeField] private string newDayTrigger = "NewDay";

        // ===== Private State =====
        private DayNightCycleManager cycleManager;
        private ITimeService timeService;
        private DayPhase currentPhase;
        private int currentDay;
        private Color currentTextColor;
        private Color currentProgressColor;

        #region Unity Lifecycle

        private void Start()
        {
            Initialize();
            SubscribeToEvents();
            SetupButtons();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (timeService == null) return;

            UpdateTimeDisplay();
            UpdateProgressBar();
            UpdateColors();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // Try to get the time service
            timeService = ServiceLocator.Get<ITimeService>();

            // Fallback to direct reference
            if (timeService == null)
            {
                cycleManager = DayNightCycleManager.Instance;
                if (cycleManager == null)
                {
                    cycleManager = FindAnyObjectByType<DayNightCycleManager>();
                }
                timeService = cycleManager;
            }

            if (timeService == null)
            {
               UnityEngine. Debug.LogWarning("[TimeDisplayUI] ITimeService not found! UI will be disabled.");
                gameObject.SetActive(false);
                return;
            }

            // Cache cycle manager for additional features
            if (cycleManager == null)
            {
                cycleManager = timeService as DayNightCycleManager;
            }

            // Get initial state
            currentPhase = timeService.CurrentPhase;
            currentDay = timeService.CurrentDay;

            // Setup initial colors
            currentTextColor = GetTextColorForPhase(currentPhase);
            currentProgressColor = GetProgressColorForPhase(currentPhase);

            // Setup initial icons
            UpdatePhaseIcons(currentPhase);

            // Initial UI update
            ForceUpdateAll();
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<DayPhaseChangedEvent>(OnPhaseChanged);
            EventBus.Subscribe<HourChangedEvent>(OnHourChanged);
            EventBus.Subscribe<NewDayEvent>(OnNewDay);
            EventBus.Subscribe<TimeScaleChangedEvent>(OnTimeScaleChanged);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<DayPhaseChangedEvent>(OnPhaseChanged);
            EventBus.Unsubscribe<HourChangedEvent>(OnHourChanged);
            EventBus.Unsubscribe<NewDayEvent>(OnNewDay);
            EventBus.Unsubscribe<TimeScaleChangedEvent>(OnTimeScaleChanged);
        }

        private void SetupButtons()
        {
            if (!showSpeedControls) return;

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(() =>
                {
                    if (timeService != null)
                    {
                        if (timeService.IsTimePaused)
                            timeService.ResumeTime();
                        else
                            timeService.PauseTime();
                        UpdateSpeedText();
                    }
                });
            }

            if (normalSpeedButton != null)
            {
                normalSpeedButton.onClick.AddListener(() =>
                {
                    if (timeService != null)
                    {
                        timeService.SetTimeScale(1f);
                        UpdateSpeedText();
                    }
                });
            }

            if (fastSpeedButton != null)
            {
                fastSpeedButton.onClick.AddListener(() =>
                {
                    if (timeService != null)
                    {
                        timeService.SetTimeScale(fastTimeScale);
                        UpdateSpeedText();
                    }
                });
            }

            UpdateSpeedText();
        }

        #endregion

        #region Event Handlers

        private void OnPhaseChanged(DayPhaseChangedEvent evt)
        {
            currentPhase = evt.NewPhase;
            UpdatePhaseIcons(evt.NewPhase);
            UpdatePhaseText();

            if (animateChanges && uiAnimator != null)
            {
                uiAnimator.SetTrigger(phaseChangeTrigger);
            }
        }

        private void OnHourChanged(HourChangedEvent evt)
        {
            if (animateChanges && uiAnimator != null)
            {
                uiAnimator.SetTrigger(hourChangeTrigger);
            }
        }

        private void OnNewDay(NewDayEvent evt)
        {
            currentDay = evt.NewDay;
            UpdateDayText();

            if (animateChanges && uiAnimator != null)
            {
                uiAnimator.SetTrigger(newDayTrigger);
            }
        }

        private void OnTimeScaleChanged(TimeScaleChangedEvent evt)
        {
            UpdateSpeedText();
        }

        #endregion

        #region UI Updates

        private void UpdateTimeDisplay()
        {
            if (timeText != null)
            {
                timeText.text = timeService.GetFormattedTime(use24HourFormat);
            }
        }

        private void UpdateDayText()
        {
            if (dayText != null && showDayCounter)
            {
                dayText.text = $"{dayPrefix}{currentDay}";
            }
        }

        private void UpdatePhaseText()
        {
            if (phaseText != null && showPhaseText)
            {
                phaseText.text = timeService.GetPhaseName();
            }
        }

        private void UpdateProgressBar()
        {
            if (!showProgressBar) return;

            float progress = timeService.DayProgress;

            if (dayProgressBar != null)
            {
                dayProgressBar.value = progress;
            }

            if (radialClockImage != null)
            {
                radialClockImage.fillAmount = progress;
            }
        }

        private void UpdateColors()
        {
            // Target colors based on phase
            Color targetTextColor = GetTextColorForPhase(currentPhase);
            Color targetProgressColor = GetProgressColorForPhase(currentPhase);

            // Smooth color transitions
            if (animateChanges)
            {
                currentTextColor = Color.Lerp(currentTextColor, targetTextColor, Time.deltaTime * 3f);
                currentProgressColor = Color.Lerp(currentProgressColor, targetProgressColor, Time.deltaTime * 3f);
            }
            else
            {
                currentTextColor = targetTextColor;
                currentProgressColor = targetProgressColor;
            }

            // Apply colors
            if (timeText != null)
                timeText.color = currentTextColor;

            if (dayText != null)
                dayText.color = currentTextColor;

            if (phaseText != null)
                phaseText.color = currentTextColor;

            if (dayProgressFill != null)
                dayProgressFill.color = currentProgressColor;

            if (radialClockImage != null)
                radialClockImage.color = currentProgressColor;
        }

        private void UpdatePhaseIcons(DayPhase phase)
        {
            // Sun/Moon icons
            bool isDay = phase == DayPhase.Day || phase == DayPhase.Dawn;

            if (sunIcon != null)
                sunIcon.SetActive(isDay);

            if (moonIcon != null)
                moonIcon.SetActive(!isDay);

            // Phase-specific icon
            if (phaseIcon != null)
            {
                switch (phase)
                {
                    case DayPhase.Dawn:
                        if (dawnSprite != null) phaseIcon.sprite = dawnSprite;
                        break;
                    case DayPhase.Day:
                        if (daySprite != null) phaseIcon.sprite = daySprite;
                        break;
                    case DayPhase.Dusk:
                        if (duskSprite != null) phaseIcon.sprite = duskSprite;
                        break;
                    case DayPhase.Night:
                        if (nightSprite != null) phaseIcon.sprite = nightSprite;
                        break;
                }
            }
        }

        private void UpdateSpeedText()
        {
            if (speedText == null || timeService == null) return;

            if (timeService.IsTimePaused)
            {
                speedText.text = "PAUSED";
            }
            else
            {
                float scale = timeService.TimeScale;
                if (Mathf.Approximately(scale, 1f))
                    speedText.text = "1x";
                else
                    speedText.text = $"{scale:F1}x";
            }
        }

        private void ForceUpdateAll()
        {
            UpdateTimeDisplay();
            UpdateDayText();
            UpdatePhaseText();
            UpdateProgressBar();
            UpdatePhaseIcons(currentPhase);
            UpdateSpeedText();

            // Force immediate color application
            currentTextColor = GetTextColorForPhase(currentPhase);
            currentProgressColor = GetProgressColorForPhase(currentPhase);

            if (timeText != null)
                timeText.color = currentTextColor;

            if (dayText != null)
                dayText.color = currentTextColor;

            if (phaseText != null)
                phaseText.color = currentTextColor;

            if (dayProgressFill != null)
                dayProgressFill.color = currentProgressColor;
        }

        #endregion

        #region Helper Methods

        private Color GetTextColorForPhase(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.Day:
                case DayPhase.Dawn:
                    return dayTextColor;
                case DayPhase.Night:
                case DayPhase.Dusk:
                    return nightTextColor;
                default:
                    return dayTextColor;
            }
        }

        private Color GetProgressColorForPhase(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.Dawn:
                    return dawnProgressColor;
                case DayPhase.Day:
                    return dayProgressColor;
                case DayPhase.Dusk:
                    return duskProgressColor;
                case DayPhase.Night:
                    return nightProgressColor;
                default:
                    return dayProgressColor;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set whether to use 24-hour format.
        /// </summary>
        public void SetUse24HourFormat(bool use24Hour)
        {
            use24HourFormat = use24Hour;
        }

        /// <summary>
        /// Show or hide the day counter.
        /// </summary>
        public void SetShowDayCounter(bool show)
        {
            showDayCounter = show;
            if (dayText != null)
                dayText.gameObject.SetActive(show);
        }

        /// <summary>
        /// Show or hide the phase text.
        /// </summary>
        public void SetShowPhaseText(bool show)
        {
            showPhaseText = show;
            if (phaseText != null)
                phaseText.gameObject.SetActive(show);
        }

        /// <summary>
        /// Show or hide the progress bar.
        /// </summary>
        public void SetShowProgressBar(bool show)
        {
            showProgressBar = show;
            if (dayProgressBar != null)
                dayProgressBar.gameObject.SetActive(show);
        }

        /// <summary>
        /// Skip to dawn (wrapper for quick access).
        /// </summary>
        public void SkipToDawn()
        {
            if (cycleManager != null)
                cycleManager.SkipToPhase(DayPhase.Dawn);
        }

        /// <summary>
        /// Skip to noon (wrapper for quick access).
        /// </summary>
        public void SkipToNoon()
        {
            if (timeService != null)
                timeService.SetTime(12f);
        }

        /// <summary>
        /// Skip to dusk (wrapper for quick access).
        /// </summary>
        public void SkipToDusk()
        {
            if (cycleManager != null)
                cycleManager.SkipToPhase(DayPhase.Dusk);
        }

        /// <summary>
        /// Skip to night (wrapper for quick access).
        /// </summary>
        public void SkipToNight()
        {
            if (cycleManager != null)
                cycleManager.SkipToPhase(DayPhase.Night);
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Update visibility in editor
            if (dayText != null)
                dayText.gameObject.SetActive(showDayCounter);

            if (phaseText != null)
                phaseText.gameObject.SetActive(showPhaseText);

            if (dayProgressBar != null)
                dayProgressBar.gameObject.SetActive(showProgressBar);
        }
#endif

        #endregion
    }
}
