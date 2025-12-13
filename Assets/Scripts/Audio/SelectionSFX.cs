using UnityEngine;
using System.Collections.Generic;
using RTS.Core.Events;

using RTS.Units;
namespace RTS.Audio
{
    /// <summary>
    /// Plays audio when a building or unit is selected.
    /// Supports single clip (plays each time) or random selection from array.
    /// </summary>
    public class SelectionSFX : MonoBehaviour
    {
        [Header("Single Selection Clip")]
        [Tooltip("Optional: Plays this clip every time the object is selected")]
        [SerializeField] private AudioClip singleSelectionClip;

        [Header("Random Selection Clips")]
        [Tooltip("Optional: Randomly picks one of these clips when selected")]
        [SerializeField] private AudioClip[] randomSelectionClips;

        [Header("Audio Settings")]
        [Tooltip("Volume for selection sounds (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        [Tooltip("Pitch variation for random clips (0 = no variation)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float pitchVariation = 0.1f;

        [Tooltip("Spatial blend (0 = 2D, 1 = 3D)")]
        [Range(0f, 1f)]
        [SerializeField] private float spatialBlend = 1f;

        [Tooltip("Priority (0 = highest, 256 = lowest)")]
        [Range(0, 256)]
        [SerializeField] private int priority = 128;

        [Tooltip("Minimum time between plays to avoid spam (seconds)")]
        [SerializeField] private float cooldownTime = 0.1f;

        [Header("3D Sound Settings")]
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 50f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

        private AudioSource audioSource;
        private float lastPlayTime = -999f;
        private bool isBuilding = false;
        private bool isUnit = false;

        private void Awake()
        {
            // Check if this is on a building or unit
            isBuilding = GetComponent<Buildings.Building>() != null || GetComponent<Buildings.BuildingSelectable>() != null;
            isUnit = GetComponent<RTS.Units.UnitController>() != null || GetComponent<RTS.Units.UnitSelectable>() != null;
            isBuilding = TryGetComponent<Buildings.Building>(out _) || TryGetComponent<Buildings.BuildingSelectable>(out _);
            isUnit = TryGetComponent<UnitSelectable>(out _);

            // Create audio source
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
            audioSource.spatialBlend = spatialBlend;
            audioSource.priority = priority;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.rolloffMode = rolloffMode;
        }

        private void OnEnable()
        {
            // Subscribe to appropriate selection events
            if (isBuilding)
            {
                EventBus.Subscribe<BuildingSelectedEvent>(OnBuildingSelected);
            }
            if (isUnit)
            {
                EventBus.Subscribe<UnitSelectedEvent>(OnUnitSelected);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (isBuilding)
            {
                EventBus.Unsubscribe<BuildingSelectedEvent>(OnBuildingSelected);
            }
            if (isUnit)
            {
                EventBus.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
            }
        }

        private void OnBuildingSelected(BuildingSelectedEvent evt)
        {
            if (evt.Building == gameObject)
            {
                PlaySelectionSound();
            }
        }

        private void OnUnitSelected(UnitSelectedEvent evt)
        {
            if (evt.Unit == gameObject)
            {
                PlaySelectionSound();
            }
        }

        private void PlaySelectionSound()
        {
            // Check cooldown
            if (Time.time - lastPlayTime < cooldownTime)
                return;

            AudioClip clipToPlay = null;

            // Priority: single clip > random clips
            if (singleSelectionClip != null)
            {
                clipToPlay = singleSelectionClip;
            }
            else if (randomSelectionClips != null && randomSelectionClips.Length > 0)
            {
                // Filter out null clips
                List<AudioClip> validClips = new List<AudioClip>();
                foreach (var clip in randomSelectionClips)
                {
                    if (clip != null)
                        validClips.Add(clip);
                }

                if (validClips.Count > 0)
                {
                    clipToPlay = validClips[Random.Range(0, validClips.Count)];
                }
            }

            // Play the clip
            if (clipToPlay != null && audioSource != null)
            {
                audioSource.clip = clipToPlay;

                // Apply pitch variation for random clips
                if (randomSelectionClips != null && randomSelectionClips.Length > 1)
                {
                    audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
                }
                else
                {
                    audioSource.pitch = 1f;
                }

                audioSource.Play();
                lastPlayTime = Time.time;
            }
        }

        /// <summary>
        /// Manually trigger selection sound (for testing or external use)
        /// </summary>
        public void PlaySound()
        {
            PlaySelectionSound();
        }

        /// <summary>
        /// Set a new single selection clip at runtime
        /// </summary>
        public void SetSingleClip(AudioClip clip)
        {
            singleSelectionClip = clip;
        }

        /// <summary>
        /// Set new random selection clips at runtime
        /// </summary>
        public void SetRandomClips(AudioClip[] clips)
        {
            randomSelectionClips = clips;
        }

        /// <summary>
        /// Set volume at runtime
        /// </summary>
        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
            if (audioSource != null)
                audioSource.volume = volume;
        }
    }
}
