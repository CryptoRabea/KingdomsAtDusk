using UnityEngine;
using RTS.Core;
using RTS.Core.Events;

namespace RTS.DayNightCycle
{
    /// <summary>
    /// Controls ambient audio based on the day-night cycle.
    /// Handles crossfading between day and night ambient sounds,
    /// triggering specific sounds at certain times (dawn chorus, crickets, etc.).
    /// </summary>
    public class DayNightAmbientController : MonoBehaviour
    {
        [System.Serializable]
        public class AmbientAudioLayer
        {
            [Tooltip("Name of this audio layer (for debugging)")]
            public string layerName = "Ambient";

            [Tooltip("Audio source for this layer")]
            public AudioSource audioSource;

            [Tooltip("Audio clip to play")]
            public AudioClip clip;

            [Tooltip("Maximum volume for this layer")]
            [Range(0f, 1f)]
            public float maxVolume = 1f;

            [Tooltip("When this layer should be active")]
            public DayPhase[] activePhases = { DayPhase.Day };

            [Tooltip("Fade in duration in seconds")]
            public float fadeInDuration = 2f;

            [Tooltip("Fade out duration in seconds")]
            public float fadeOutDuration = 2f;

            [Tooltip("Should this loop?")]
            public bool loop = true;

            [HideInInspector]
            public float currentVolume;

            [HideInInspector]
            public float targetVolume;

            [HideInInspector]
            public bool isPlaying;
        }

        [System.Serializable]
        public class TimedSoundEffect
        {
            [Tooltip("Name of this sound effect")]
            public string effectName = "Sound Effect";

            [Tooltip("Audio clips to choose from (random selection)")]
            public AudioClip[] clips;

            [Tooltip("Hour to trigger this sound (0-24)")]
            [Range(0f, 24f)]
            public float triggerHour = 6f;

            [Tooltip("Random hour variance (+/- this value)")]
            [Range(0f, 2f)]
            public float hourVariance = 0.5f;

            [Tooltip("Trigger on specific phases only")]
            public bool usePhaseFilter = false;

            [Tooltip("Which phases to trigger on")]
            public DayPhase[] triggerPhases;

            [Tooltip("Volume of this sound")]
            [Range(0f, 1f)]
            public float volume = 1f;

            [Tooltip("Delay between repeats (0 = play once per trigger)")]
            public float repeatDelay = 0f;

            [Tooltip("Number of repeats (0 = infinite during active period)")]
            public int repeatCount = 1;

            [HideInInspector]
            public bool hasTriggeredThisHour;

            [HideInInspector]
            public int currentRepeatCount;

            [HideInInspector]
            public float nextRepeatTime;
        }

        [Header("=== AMBIENT LAYERS ===")]
        [Tooltip("Continuous ambient audio layers that crossfade based on time")]
        [SerializeField] private AmbientAudioLayer[] ambientLayers;

        [Header("=== TIMED SOUND EFFECTS ===")]
        [Tooltip("Sound effects triggered at specific times")]
        [SerializeField] private TimedSoundEffect[] timedSoundEffects;

        [Header("=== AUDIO SOURCES ===")]
        [Tooltip("Audio source for one-shot effects")]
        [SerializeField] private AudioSource oneShotAudioSource;

        [Tooltip("Create audio sources automatically if not assigned")]
        [SerializeField] private bool autoCreateAudioSources = true;

        [Header("=== TRANSITION SETTINGS ===")]
        [Tooltip("Global volume multiplier")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;

        [Tooltip("Crossfade smoothness")]
        [SerializeField, Range(0.1f, 10f)] private float crossfadeSpeed = 2f;

        [Header("=== RANDOM AMBIENT SOUNDS ===")]
        [Tooltip("Enable random ambient sounds (birds, wind, etc.)")]
        [SerializeField] private bool enableRandomSounds = true;

        [Tooltip("Random day sounds")]
        [SerializeField] private AudioClip[] randomDaySounds;

        [Tooltip("Random night sounds")]
        [SerializeField] private AudioClip[] randomNightSounds;

        [Tooltip("Minimum interval between random sounds (seconds)")]
        [SerializeField] private float randomSoundMinInterval = 10f;

        [Tooltip("Maximum interval between random sounds (seconds)")]
        [SerializeField] private float randomSoundMaxInterval = 30f;

        [Tooltip("Random sound volume")]
        [SerializeField, Range(0f, 1f)] private float randomSoundVolume = 0.5f;

        [Header("=== DEBUG ===")]
        [SerializeField] private bool showDebugInfo = false;

        // ===== Private State =====
        private DayNightCycleManager cycleManager;
        private DayPhase currentPhase;
        private int lastHour = -1;
        private float nextRandomSoundTime;

        #region Unity Lifecycle

        private void Start()
        {
            Initialize();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (cycleManager == null) return;

            UpdateAmbientLayers();
            CheckTimedSoundEffects();
            CheckRandomSounds();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // Get cycle manager reference
            cycleManager = DayNightCycleManager.Instance;
            if (cycleManager == null)
            {
                cycleManager = FindAnyObjectByType<DayNightCycleManager>();
            }

            if (cycleManager == null)
            {
                Debug.LogWarning("[DayNightAmbientController] DayNightCycleManager not found!");
                enabled = false;
                return;
            }

            currentPhase = cycleManager.CurrentPhase;

            // Setup audio sources for layers
            if (autoCreateAudioSources)
            {
                SetupAudioSources();
            }

            // Initialize ambient layers
            foreach (var layer in ambientLayers)
            {
                if (layer.audioSource != null && layer.clip != null)
                {
                    layer.audioSource.clip = layer.clip;
                    layer.audioSource.loop = layer.loop;
                    layer.audioSource.volume = 0f;
                    layer.currentVolume = 0f;

                    // Check if should be playing
                    if (IsLayerActiveForPhase(layer, currentPhase))
                    {
                        layer.targetVolume = layer.maxVolume;
                        layer.audioSource.Play();
                        layer.isPlaying = true;
                    }
                }
            }

            // Initialize one-shot audio source
            if (oneShotAudioSource == null && autoCreateAudioSources)
            {
                GameObject audioObj = new GameObject("OneShotAudioSource");
                audioObj.transform.SetParent(transform);
                oneShotAudioSource = audioObj.AddComponent<AudioSource>();
                oneShotAudioSource.playOnAwake = false;
                oneShotAudioSource.spatialBlend = 0f;
            }

            // Set initial random sound time
            nextRandomSoundTime = Time.time + Random.Range(randomSoundMinInterval, randomSoundMaxInterval);
        }

        private void SetupAudioSources()
        {
            for (int i = 0; i < ambientLayers.Length; i++)
            {
                var layer = ambientLayers[i];
                if (layer.audioSource == null)
                {
                    GameObject audioObj = new GameObject($"AmbientLayer_{layer.layerName}");
                    audioObj.transform.SetParent(transform);
                    layer.audioSource = audioObj.AddComponent<AudioSource>();
                    layer.audioSource.playOnAwake = false;
                    layer.audioSource.spatialBlend = 0f; // 2D audio
                }
            }
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<DayPhaseChangedEvent>(OnPhaseChanged);
            EventBus.Subscribe<HourChangedEvent>(OnHourChanged);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<DayPhaseChangedEvent>(OnPhaseChanged);
            EventBus.Unsubscribe<HourChangedEvent>(OnHourChanged);
        }

        #endregion

        #region Event Handlers

        private void OnPhaseChanged(DayPhaseChangedEvent evt)
        {
            currentPhase = evt.NewPhase;

            // Update target volumes for all layers
            foreach (var layer in ambientLayers)
            {
                if (IsLayerActiveForPhase(layer, currentPhase))
                {
                    layer.targetVolume = layer.maxVolume;

                    // Start playing if not already
                    if (!layer.isPlaying && layer.audioSource != null)
                    {
                        layer.audioSource.Play();
                        layer.isPlaying = true;
                    }
                }
                else
                {
                    layer.targetVolume = 0f;
                }
            }
        }

        private void OnHourChanged(HourChangedEvent evt)
        {
            lastHour = evt.NewHour;

            // Reset timed sound triggers for new hour
            foreach (var effect in timedSoundEffects)
            {
                int triggerHourInt = Mathf.FloorToInt(effect.triggerHour);
                if (evt.NewHour != triggerHourInt)
                {
                    effect.hasTriggeredThisHour = false;
                    effect.currentRepeatCount = 0;
                }
            }
        }

        #endregion

        #region Ambient Layer Updates

        private void UpdateAmbientLayers()
        {
            foreach (var layer in ambientLayers)
            {
                if (layer.audioSource == null) continue;

                // Determine fade speed based on whether we're fading in or out
                float fadeSpeed;
                if (layer.targetVolume > layer.currentVolume)
                {
                    fadeSpeed = 1f / Mathf.Max(0.1f, layer.fadeInDuration);
                }
                else
                {
                    fadeSpeed = 1f / Mathf.Max(0.1f, layer.fadeOutDuration);
                }

                // Smooth volume transition
                layer.currentVolume = Mathf.MoveTowards(
                    layer.currentVolume,
                    layer.targetVolume,
                    fadeSpeed * Time.deltaTime
                );

                // Apply volume with master multiplier
                layer.audioSource.volume = layer.currentVolume * masterVolume;

                // Stop audio source when fully faded out
                if (layer.currentVolume <= 0.001f && layer.isPlaying && layer.targetVolume <= 0f)
                {
                    layer.audioSource.Stop();
                    layer.isPlaying = false;
                }
            }
        }

        private bool IsLayerActiveForPhase(AmbientAudioLayer layer, DayPhase phase)
        {
            if (layer.activePhases == null || layer.activePhases.Length == 0)
                return true; // Active for all phases if none specified

            foreach (var activePhase in layer.activePhases)
            {
                if (activePhase == phase)
                    return true;
            }

            return false;
        }

        #endregion

        #region Timed Sound Effects

        private void CheckTimedSoundEffects()
        {
            if (cycleManager == null) return;

            float currentTime = cycleManager.CurrentTime;
            int currentHour = cycleManager.CurrentHour;

            foreach (var effect in timedSoundEffects)
            {
                // Check if we should trigger this effect
                if (ShouldTriggerEffect(effect, currentTime, currentHour))
                {
                    PlayTimedEffect(effect);
                }

                // Handle repeats
                if (effect.repeatDelay > 0 && effect.currentRepeatCount > 0)
                {
                    if (Time.time >= effect.nextRepeatTime)
                    {
                        if (effect.repeatCount == 0 || effect.currentRepeatCount < effect.repeatCount)
                        {
                            PlayTimedEffect(effect);
                        }
                    }
                }
            }
        }

        private bool ShouldTriggerEffect(TimedSoundEffect effect, float currentTime, int currentHour)
        {
            // Check if already triggered this hour
            if (effect.hasTriggeredThisHour)
                return false;

            // Check hour
            int triggerHourInt = Mathf.FloorToInt(effect.triggerHour);
            float triggerMinute = (effect.triggerHour - triggerHourInt) * 60f;

            if (currentHour != triggerHourInt)
                return false;

            // Check minute with variance
            int currentMinute = cycleManager.CurrentMinute;
            float minMinute = triggerMinute - effect.hourVariance * 60f;
            float maxMinute = triggerMinute + effect.hourVariance * 60f;

            if (currentMinute < minMinute || currentMinute > maxMinute)
                return false;

            // Check phase filter
            if (effect.usePhaseFilter && effect.triggerPhases != null && effect.triggerPhases.Length > 0)
            {
                bool phaseMatch = false;
                foreach (var phase in effect.triggerPhases)
                {
                    if (phase == currentPhase)
                    {
                        phaseMatch = true;
                        break;
                    }
                }
                if (!phaseMatch)
                    return false;
            }

            return true;
        }

        private void PlayTimedEffect(TimedSoundEffect effect)
        {
            if (effect.clips == null || effect.clips.Length == 0)
                return;

            // Select random clip
            AudioClip clip = effect.clips[Random.Range(0, effect.clips.Length)];

            if (clip != null && oneShotAudioSource != null)
            {
                oneShotAudioSource.PlayOneShot(clip, effect.volume * masterVolume);

                effect.hasTriggeredThisHour = true;
                effect.currentRepeatCount++;
                effect.nextRepeatTime = Time.time + effect.repeatDelay;
            }
        }

        #endregion

        #region Random Sounds

        private void CheckRandomSounds()
        {
            if (!enableRandomSounds) return;
            if (Time.time < nextRandomSoundTime) return;

            // Get appropriate sound array
            AudioClip[] sounds = currentPhase == DayPhase.Night || currentPhase == DayPhase.Dusk
                ? randomNightSounds
                : randomDaySounds;

            if (sounds != null && sounds.Length > 0)
            {
                AudioClip clip = sounds[Random.Range(0, sounds.Length)];
                if (clip != null && oneShotAudioSource != null)
                {
                    oneShotAudioSource.PlayOneShot(clip, randomSoundVolume * masterVolume);
                }
            }

            // Schedule next random sound
            nextRandomSoundTime = Time.time + Random.Range(randomSoundMinInterval, randomSoundMaxInterval);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set the master volume for all ambient audio.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Get the current master volume.
        /// </summary>
        public float GetMasterVolume()
        {
            return masterVolume;
        }

        /// <summary>
        /// Play a custom sound effect.
        /// </summary>
        public void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip != null && oneShotAudioSource != null)
            {
                oneShotAudioSource.PlayOneShot(clip, volume * masterVolume);
            }
        }

        /// <summary>
        /// Stop all ambient audio.
        /// </summary>
        public void StopAllAmbient()
        {
            foreach (var layer in ambientLayers)
            {
                if (layer.audioSource != null)
                {
                    layer.audioSource.Stop();
                    layer.isPlaying = false;
                    layer.currentVolume = 0f;
                    layer.targetVolume = 0f;
                }
            }
        }

        /// <summary>
        /// Resume ambient audio based on current phase.
        /// </summary>
        public void ResumeAmbient()
        {
            if (cycleManager == null) return;

            currentPhase = cycleManager.CurrentPhase;

            foreach (var layer in ambientLayers)
            {
                if (IsLayerActiveForPhase(layer, currentPhase))
                {
                    layer.targetVolume = layer.maxVolume;

                    if (!layer.isPlaying && layer.audioSource != null)
                    {
                        layer.audioSource.Play();
                        layer.isPlaying = true;
                    }
                }
            }
        }

        /// <summary>
        /// Get a specific ambient layer by name.
        /// </summary>
        public AmbientAudioLayer GetLayer(string layerName)
        {
            foreach (var layer in ambientLayers)
            {
                if (layer.layerName == layerName)
                    return layer;
            }
            return null;
        }

        /// <summary>
        /// Set volume for a specific layer.
        /// </summary>
        public void SetLayerVolume(string layerName, float volume)
        {
            var layer = GetLayer(layerName);
            if (layer != null)
            {
                layer.maxVolume = Mathf.Clamp01(volume);
                if (IsLayerActiveForPhase(layer, currentPhase))
                {
                    layer.targetVolume = layer.maxVolume;
                }
            }
        }

        #endregion

        #region Editor Debug

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugInfo || cycleManager == null) return;

            GUILayout.BeginArea(new Rect(10, 220, 300, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label("=== Ambient Audio Debug ===");
            GUILayout.Label($"Master Volume: {masterVolume:P0}");
            GUILayout.Label($"Current Phase: {currentPhase}");

            GUILayout.Space(5);
            GUILayout.Label("Active Layers:");

            foreach (var layer in ambientLayers)
            {
                string status = layer.isPlaying ? "Playing" : "Stopped";
                GUILayout.Label($"  {layer.layerName}: {layer.currentVolume:F2}/{layer.maxVolume:F2} ({status})");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}
