using UnityEngine;
using System.Collections.Generic;
using RTS.Core.Events;
using RTS.Units;

namespace RTS.Audio
{
    /// <summary>
    /// Plays voice lines when a unit receives commands.
    /// Supports move commands, attack unit commands, and attack building commands.
    /// Each command type has its own array of clips with individual configuration.
    /// </summary>
    public class UnitCommandSFX : MonoBehaviour
    {
        [System.Serializable]
        public class CommandSFXSettings
        {
            [Header("Audio Clips")]
            [Tooltip("Array of clips to randomly choose from")]
            public AudioClip[] clips;

            [Header("Audio Settings")]
            [Tooltip("Volume for this command type (0-1)")]
            [Range(0f, 1f)]
            public float volume = 1f;

            [Tooltip("Random pitch variation (+/-)")]
            [Range(0f, 0.5f)]
            public float pitchVariation = 0.1f;

            [Tooltip("Spatial blend (0 = 2D, 1 = 3D)")]
            [Range(0f, 1f)]
            public float spatialBlend = 1f;

            [Tooltip("Audio priority (0 = highest, 256 = lowest)")]
            [Range(0, 256)]
            public int priority = 128;

            [Tooltip("Minimum time between plays (seconds) to prevent spam")]
            public float cooldown = 0.2f;

            [Tooltip("Enable this command type")]
            public bool enabled = true;

            [HideInInspector] public float lastPlayTime = -999f;
        }

        [Header("Move Command")]
        [Tooltip("Voice lines when unit receives move command")]
        public CommandSFXSettings moveCommand = new CommandSFXSettings();

        [Header("Attack Unit Command")]
        [Tooltip("Voice lines when unit receives attack unit command")]
        public CommandSFXSettings attackUnitCommand = new CommandSFXSettings();

        [Header("Attack Building Command")]
        [Tooltip("Voice lines when unit receives attack building command")]
        public CommandSFXSettings attackBuildingCommand = new CommandSFXSettings();

        [Header("3D Sound Settings")]
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 50f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        private AudioSource audioSource;
        private readonly List<AudioClip> validClipsCache = new();

        private void Awake()
        {
            // Create audio source for this unit
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.rolloffMode = rolloffMode;
        }

        private void OnEnable()
        {
            // Subscribe to command events
            EventBus.Subscribe<RTS.Units.UnitMoveCommandEvent>(OnMoveCommand);
            EventBus.Subscribe<RTS.Units.UnitAttackCommandEvent>(OnAttackCommand);
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            EventBus.Unsubscribe<RTS.Units.UnitMoveCommandEvent>(OnMoveCommand);
            EventBus.Unsubscribe<RTS.Units.UnitAttackCommandEvent>(OnAttackCommand);
        }

        private void OnMoveCommand(RTS.Units.UnitMoveCommandEvent evt)
        {
            // Only respond to commands for this unit
            if (evt.Unit != gameObject)
                return;

            if (moveCommand.enabled)
            {
                PlayCommandSound(moveCommand, "Move");
            }
        }

        private void OnAttackCommand(RTS.Units.UnitAttackCommandEvent evt)
        {
            // Only respond to commands for this unit
            if (evt.Unit != gameObject)
                return;

            // Choose appropriate command settings based on target type (Unit or Building)
            CommandSFXSettings settings = evt.TargetType == AttackTargetType.Unit
                ? attackUnitCommand
                : attackBuildingCommand;

            string commandName = evt.TargetType == AttackTargetType.Unit
                ? "Attack Unit"
                : "Attack Building";

            if (settings.enabled)
            {
                PlayCommandSound(settings, commandName);
            }
        }

        private void PlayCommandSound(CommandSFXSettings settings, string commandName)
        {
            // Check if settings are valid
            if (settings.clips == null || settings.clips.Length == 0)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[UnitCommandSFX] No clips assigned for {commandName} command on {gameObject.name}");
                return;
            }

            // Check cooldown
            if (Time.time - settings.lastPlayTime < settings.cooldown)
            {
                if (showDebugLogs)
                    Debug.Log($"[UnitCommandSFX] {commandName} command on cooldown for {gameObject.name}");
                return;
            }

            // Filter out null clips (reuse cached list to avoid allocations)
            validClipsCache.Clear();
            foreach (var clip in settings.clips)
            {
                if (clip != null)
                    validClipsCache.Add(clip);
            }

            if (validClipsCache.Count == 0)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[UnitCommandSFX] All clips are null for {commandName} command on {gameObject.name}");
                return;
            }

            // Select random clip
            AudioClip clipToPlay = validClipsCache[Random.Range(0, validClipsCache.Count)];

            // Configure audio source
            audioSource.clip = clipToPlay;
            audioSource.volume = settings.volume;
            audioSource.spatialBlend = settings.spatialBlend;
            audioSource.priority = settings.priority;

            // Apply random pitch variation
            audioSource.pitch = 1f + Random.Range(-settings.pitchVariation, settings.pitchVariation);

            // Play the clip
            audioSource.Play();
            settings.lastPlayTime = Time.time;

            if (showDebugLogs)
                Debug.Log($"[UnitCommandSFX] Playing {commandName} clip '{clipToPlay.name}' on {gameObject.name}");
        }

        #region Public Methods

        /// <summary>
        /// Manually trigger a move command sound (for testing)
        /// </summary>
        public void PlayMoveSound()
        {
            PlayCommandSound(moveCommand, "Move");
        }

        /// <summary>
        /// Manually trigger an attack unit command sound (for testing)
        /// </summary>
        public void PlayAttackUnitSound()
        {
            PlayCommandSound(attackUnitCommand, "Attack Unit");
        }

        /// <summary>
        /// Manually trigger an attack building command sound (for testing)
        /// </summary>
        public void PlayAttackBuildingSound()
        {
            PlayCommandSound(attackBuildingCommand, "Attack Building");
        }

        /// <summary>
        /// Set move command clips at runtime
        /// </summary>
        public void SetMoveClips(AudioClip[] clips)
        {
            moveCommand.clips = clips;
        }

        /// <summary>
        /// Set attack unit command clips at runtime
        /// </summary>
        public void SetAttackUnitClips(AudioClip[] clips)
        {
            attackUnitCommand.clips = clips;
        }

        /// <summary>
        /// Set attack building command clips at runtime
        /// </summary>
        public void SetAttackBuildingClips(AudioClip[] clips)
        {
            attackBuildingCommand.clips = clips;
        }

        /// <summary>
        /// Enable/disable a specific command type
        /// </summary>
        public void SetCommandEnabled(string commandType, bool enabled)
        {
            switch (commandType.ToLower())
            {
                case "move":
                    moveCommand.enabled = enabled;
                    break;
                case "attackunit":
                    attackUnitCommand.enabled = enabled;
                    break;
                case "attackbuilding":
                    attackBuildingCommand.enabled = enabled;
                    break;
                default:
                    Debug.LogWarning($"[UnitCommandSFX] Unknown command type: {commandType}");
                    break;
            }
        }

        #endregion
    }
}
