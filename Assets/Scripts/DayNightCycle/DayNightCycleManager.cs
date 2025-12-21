using UnityEngine;
using RTS.Core.Services;
using RTS.Core.Events;
using RTS.Core;
using RTS.SaveLoad;

namespace RTS.DayNightCycle
{
    /// <summary>
    /// Core manager for the day-night cycle system.
    /// Implements ITimeService and handles all time progression logic.
    /// This is the central hub that other day-night components subscribe to.
    /// </summary>
    public class DayNightCycleManager : MonoBehaviour, ITimeService
    {
        [Header("=== CONFIGURATION ===")]
        [SerializeField] private DayNightConfigSO config;

        [Header("=== RUNTIME SETTINGS ===")]
        [Tooltip("Override starting hour at runtime (uses config if -1)")]
        [SerializeField] private float overrideStartingHour = -1f;

        [Tooltip("Enable time progression")]
        [SerializeField] private bool enableTimeProgression = true;

        [Header("=== DEBUG ===")]
        [SerializeField] private bool showDebugInfo = false;

        // ===== Private State =====
        private float currentTimeHours;    // Current time in hours (0-24)
        private int currentDay = 1;        // Current day number
        private float timeScale = 1f;      // Time scale multiplier
        private bool isTimePaused = false;
        private DayPhase currentPhase;
        private DayPhase previousPhase;
        private int previousHour = -1;

        // ===== Cached Values for Performance =====
        private float cachedDayProgress;
        private int cachedHour;
        private int cachedMinute;

        // ===== Singleton Access (optional) =====
        private static DayNightCycleManager instance;
        public static DayNightCycleManager Instance => instance;

        #region ITimeService Implementation

        public float CurrentTime => currentTimeHours;
        public int CurrentHour => cachedHour;
        public int CurrentMinute => cachedMinute;
        public float DayProgress => cachedDayProgress;
        public int CurrentDay => currentDay;
        public DayPhase CurrentPhase => currentPhase;
        public float TimeScale => timeScale;
        public bool IsTimePaused => isTimePaused;

        public bool IsDaytime => currentPhase == DayPhase.Day || currentPhase == DayPhase.Dawn;
        public bool IsNighttime => currentPhase == DayPhase.Night || currentPhase == DayPhase.Dusk;

        public void SetTimeScale(float scale)
        {
            float previousScale = timeScale;
            timeScale = Mathf.Max(0f, scale);

            if (!Mathf.Approximately(previousScale, timeScale))
            {
                EventBus.Publish(new TimeScaleChangedEvent(previousScale, timeScale));
            }
        }

        public void PauseTime()
        {
            if (!isTimePaused)
            {
                isTimePaused = true;
                EventBus.Publish(new TimePausedEvent(true));
            }
        }

        public void ResumeTime()
        {
            if (isTimePaused)
            {
                isTimePaused = false;
                EventBus.Publish(new TimePausedEvent(false));
            }
        }

        public void SetTime(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            int previousDayCheck = currentDay;

            // Check if we're wrapping to a new day
            if (hour < currentTimeHours && currentTimeHours > 20f && hour < 4f)
            {
                currentDay++;
                EventBus.Publish(new NewDayEvent(previousDayCheck, currentDay));
            }

            currentTimeHours = hour;
            UpdateCachedValues();
            CheckPhaseChange();
            CheckHourChange();
        }

        public void AdvanceTime(float hours)
        {
            SetTime(currentTimeHours + hours);
        }

        public string GetFormattedTime(bool use24Hour = true)
        {
            int hours = cachedHour;
            int minutes = cachedMinute;

            if (use24Hour)
            {
                return $"{hours:D2}:{minutes:D2}";
            }
            else
            {
                string period = hours >= 12 ? "PM" : "AM";
                int displayHours = hours % 12;
                if (displayHours == 0) displayHours = 12;
                return $"{displayHours}:{minutes:D2} {period}";
            }
        }

        public string GetPhaseName()
        {
            return currentPhase.ToString();
        }

        public TimeData GetSaveData()
        {
            return new TimeData
            {
                currentTime = currentTimeHours,
                currentDay = currentDay,
                dayProgress = cachedDayProgress,
                timeScale = timeScale
            };
        }

        public void LoadSaveData(TimeData data)
        {
            if (data == null) return;

            currentTimeHours = data.currentTime;
            currentDay = data.currentDay;
            timeScale = data.timeScale;

            UpdateCachedValues();
            currentPhase = config.GetPhaseForHour(currentTimeHours);
            previousPhase = currentPhase;
            previousHour = cachedHour;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            ValidateConfig();
            InitializeTime();
        }

        private void Start()
        {
            // Register with ServiceLocator
            ServiceLocator.Register<ITimeService>(this);

            // Publish initial state
            UpdateCachedValues();
            currentPhase = config.GetPhaseForHour(currentTimeHours);
            previousPhase = currentPhase;
            previousHour = cachedHour;

            // Fire initial time update
            PublishTimeUpdate();
        }

        private void Update()
        {
            if (!enableTimeProgression || isTimePaused || config == null)
                return;

            UpdateTime();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        #endregion

        #region Private Methods

        private void ValidateConfig()
        {
            if (config == null)
            {
                Debug.LogError("[DayNightCycleManager] No DayNightConfigSO assigned! Creating default config.");
                config = ScriptableObject.CreateInstance<DayNightConfigSO>();
            }
        }

        private void InitializeTime()
        {
            // Set starting time
            currentTimeHours = overrideStartingHour >= 0f ? overrideStartingHour : config.StartingHour;
            currentTimeHours = Mathf.Repeat(currentTimeHours, 24f);

            UpdateCachedValues();
        }

        private void UpdateTime()
        {
            // Calculate time delta based on config
            float hoursPerSecond = config.HoursPerSecond;
            float timeDelta = hoursPerSecond * timeScale * Time.deltaTime;

            // Advance time
            currentTimeHours += timeDelta;

            // Handle day wrap
            if (currentTimeHours >= 24f)
            {
                currentTimeHours -= 24f;
                int previousDayNum = currentDay;
                currentDay++;
                EventBus.Publish(new NewDayEvent(previousDayNum, currentDay));
            }

            // Update cached values
            UpdateCachedValues();

            // Check for hour and phase changes
            CheckHourChange();
            CheckPhaseChange();

            // Publish continuous time update
            PublishTimeUpdate();
        }

        private void UpdateCachedValues()
        {
            cachedDayProgress = currentTimeHours / 24f;
            cachedHour = Mathf.FloorToInt(currentTimeHours);
            cachedMinute = Mathf.FloorToInt((currentTimeHours - cachedHour) * 60f);
        }

        private void CheckHourChange()
        {
            if (cachedHour != previousHour)
            {
                EventBus.Publish(new HourChangedEvent(previousHour, cachedHour, currentDay));
                previousHour = cachedHour;
            }
        }

        private void CheckPhaseChange()
        {
            currentPhase = config.GetPhaseForHour(currentTimeHours);

            if (currentPhase != previousPhase)
            {
                EventBus.Publish(new DayPhaseChangedEvent(previousPhase, currentPhase, currentDay));

                // Publish specific phase events
                switch (currentPhase)
                {
                    case DayPhase.Dawn:
                        EventBus.Publish(new DawnStartedEvent(currentDay));
                        break;
                    case DayPhase.Night:
                        EventBus.Publish(new NightStartedEvent(currentDay));
                        break;
                }

                previousPhase = currentPhase;
            }
        }

        private void PublishTimeUpdate()
        {
            EventBus.Publish(new TimeUpdatedEvent(
                currentTimeHours,
                cachedDayProgress,
                currentDay,
                currentPhase
            ));
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the current configuration.
        /// </summary>
        public DayNightConfigSO Config => config;

        /// <summary>
        /// Set a new configuration at runtime.
        /// </summary>
        public void SetConfig(DayNightConfigSO newConfig)
        {
            if (newConfig != null)
            {
                config = newConfig;
            }
        }

        /// <summary>
        /// Enable or disable time progression.
        /// </summary>
        public void SetTimeProgressionEnabled(bool enabled)
        {
            enableTimeProgression = enabled;
        }

        /// <summary>
        /// Get normalized sun rotation angle (0-360 based on time).
        /// </summary>
        public float GetSunRotationAngle()
        {
            // Sun rises at dawn, sets at dusk
            // Map 6:00 (sunrise) to 0 degrees, 18:00 (sunset) to 180 degrees
            float sunProgress = (currentTimeHours - 6f) / 12f;
            return sunProgress * 180f + config.SunRotationOffset;
        }

        /// <summary>
        /// Get normalized moon rotation angle (0-360 based on time).
        /// </summary>
        public float GetMoonRotationAngle()
        {
            // Moon is opposite to sun
            float moonProgress = (currentTimeHours + 6f) / 12f;
            return moonProgress * 180f + config.MoonRotationOffset;
        }

        /// <summary>
        /// Get interpolated sun intensity for current time.
        /// </summary>
        public float GetCurrentSunIntensity()
        {
            return config.GetSunIntensityForHour(currentTimeHours);
        }

        /// <summary>
        /// Get interpolated sun color for current time.
        /// </summary>
        public Color GetCurrentSunColor()
        {
            return config.GetSunColorForHour(currentTimeHours);
        }

        /// <summary>
        /// Get interpolated ambient color for current time.
        /// </summary>
        public Color GetCurrentAmbientColor()
        {
            return config.GetAmbientColorForHour(currentTimeHours);
        }

        /// <summary>
        /// Get moon intensity for current time.
        /// </summary>
        public float GetCurrentMoonIntensity()
        {
            if (currentPhase == DayPhase.Night)
                return config.MoonIntensityNight;
            if (currentPhase == DayPhase.Dusk)
                return Mathf.Lerp(config.MoonIntensityDay, config.MoonIntensityNight, config.GetPhaseProgress(currentTimeHours));
            if (currentPhase == DayPhase.Dawn)
                return Mathf.Lerp(config.MoonIntensityNight, config.MoonIntensityDay, config.GetPhaseProgress(currentTimeHours));
            return config.MoonIntensityDay;
        }

        /// <summary>
        /// Get star visibility for current time (0-1).
        /// </summary>
        public float GetCurrentStarsVisibility()
        {
            return config.GetStarsVisibility(currentTimeHours);
        }

        /// <summary>
        /// Skip to specific phase (dawn, day, dusk, night).
        /// </summary>
        public void SkipToPhase(DayPhase targetPhase)
        {
            switch (targetPhase)
            {
                case DayPhase.Dawn:
                    SetTime(config.DawnStartHour);
                    break;
                case DayPhase.Day:
                    SetTime(config.DawnEndHour);
                    break;
                case DayPhase.Dusk:
                    SetTime(config.DuskStartHour);
                    break;
                case DayPhase.Night:
                    SetTime(config.DuskEndHour);
                    break;
            }
        }

        /// <summary>
        /// Skip to next day (same time).
        /// </summary>
        public void SkipToNextDay()
        {
            int previousDayNum = currentDay;
            currentDay++;
            EventBus.Publish(new NewDayEvent(previousDayNum, currentDay));
        }

        #endregion

        #region Editor Debug

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"=== Day-Night Cycle Debug ===");
            GUILayout.Label($"Time: {GetFormattedTime()} ({currentTimeHours:F2}h)");
            GUILayout.Label($"Day: {currentDay}");
            GUILayout.Label($"Phase: {currentPhase}");
            GUILayout.Label($"Day Progress: {cachedDayProgress:P1}");
            GUILayout.Label($"Time Scale: {timeScale:F2}x");
            GUILayout.Label($"Paused: {isTimePaused}");
            GUILayout.Label($"Sun Intensity: {GetCurrentSunIntensity():F2}");
            GUILayout.Label($"Moon Intensity: {GetCurrentMoonIntensity():F2}");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void OnValidate()
        {
            if (overrideStartingHour > 24f)
                overrideStartingHour = 24f;
        }
#endif

        #endregion
    }
}
