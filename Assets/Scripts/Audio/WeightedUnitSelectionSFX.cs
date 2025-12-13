using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RTS.Core.Events;

namespace RTS.Audio
{
    /// <summary>
    /// Plays weighted audio clips when multiple units are selected via selection box.
    /// Supports different clips based on unit types selected with configurable weights.
    /// </summary>
    public class WeightedUnitSelectionSFX : MonoBehaviour
    {
        [System.Serializable]
        public class WeightedClipSet
        {
            [Tooltip("Name for this clip set (e.g., 'Infantry', 'Cavalry', 'Mixed')")]
            public string name = "Clip Set";

            [Tooltip("Weighted audio clips to choose from")]
            public WeightedAudioClip[] clips;

            [Tooltip("Minimum number of units selected to trigger this set")]
            public int minUnits = 2;

            [Tooltip("Optional: Unit tags required to use this set (leave empty for any)")]
            public string[] requiredTags;

            [Tooltip("Optional: Percentage of selected units that must have required tags (0-1)")]
            [Range(0f, 1f)]
            public float requiredTagPercentage = 0.5f;

            [Tooltip("Enable this clip set")]
            public bool enabled = true;
        }

        [System.Serializable]
        public class WeightedAudioClip
        {
            [Tooltip("Audio clip to play")]
            public AudioClip clip;

            [Tooltip("Weight (higher = more likely to be selected)")]
            [Range(0.1f, 100f)]
            public float weight = 1f;

            [Tooltip("Enable this clip")]
            public bool enabled = true;
        }

        [Header("Weighted Clip Sets")]
        [Tooltip("Array of weighted clip sets for different unit compositions")]
        [SerializeField] private WeightedClipSet[] clipSets;

        [Header("Fallback")]
        [Tooltip("Fallback clips if no set matches (equal weight)")]
        [SerializeField] private AudioClip[] fallbackClips;

        [Header("Audio Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        [Range(0f, 0.5f)]
        [SerializeField] private float pitchVariation = 0.1f;

        [Range(0f, 1f)]
        [SerializeField] private float spatialBlend = 0.5f;

        [Range(0, 256)]
        [SerializeField] private int priority = 100;

        [Header("Selection Settings")]
        [Tooltip("Minimum units to trigger multi-selection SFX")]
        [SerializeField] private int minSelectionCount = 2;

        [Tooltip("Cooldown between plays (seconds)")]
        [SerializeField] private float cooldown = 0.5f;

        [Header("3D Sound Settings")]
        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        private AudioSource audioSource;
        private float lastPlayTime = -999f;
        private RTS.Units.UnitSelectionManager selectionManager;

        private void Awake()
        {
            // Create audio source
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
            audioSource.spatialBlend = spatialBlend;
            audioSource.priority = priority;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.rolloffMode = rolloffMode;

            // Find selection manager
            selectionManager = Object.FindAnyObjectByType<RTS.Units.UnitSelectionManager>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SelectionChangedEvent>(OnSelectionChanged);
        }

        private void OnSelectionChanged(SelectionChangedEvent evt)
        {
            // Only trigger for multi-selection
            if (evt.SelectionCount < minSelectionCount)
                return;

            // Check cooldown
            if (Time.time - lastPlayTime < cooldown)
            {
                if (showDebugLogs)
                    Debug.Log($"[WeightedUnitSelectionSFX] On cooldown");
                return;
            }

            PlaySelectionSound(evt.SelectionCount);
        }

        private void PlaySelectionSound(int selectionCount)
        {
            if (selectionManager == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[WeightedUnitSelectionSFX] No selection manager found");
                return;
            }

            // Get selected units
            List<GameObject> selectedUnits = new List<GameObject>(selectionManager.SelectedUnits);

            // Find matching clip set
            WeightedClipSet matchingSet = FindMatchingClipSet(selectedUnits, selectionCount);

            AudioClip clipToPlay = null;

            if (matchingSet != null && matchingSet.enabled)
            {
                // Use weighted selection
                clipToPlay = SelectWeightedClip(matchingSet.clips);

                if (showDebugLogs && clipToPlay != null)
                    Debug.Log($"[WeightedUnitSelectionSFX] Playing weighted clip '{clipToPlay.name}' from set '{matchingSet.name}'");
            }
            else
            {
                // Use fallback
                if (fallbackClips != null && fallbackClips.Length > 0)
                {
                    var validFallbacks = fallbackClips.Where(c => c != null).ToArray();
                    if (validFallbacks.Length > 0)
                    {
                        clipToPlay = validFallbacks[Random.Range(0, validFallbacks.Length)];

                        if (showDebugLogs && clipToPlay != null)
                            Debug.Log($"[WeightedUnitSelectionSFX] Playing fallback clip '{clipToPlay.name}'");
                    }
                }
            }

            if (clipToPlay != null)
            {
                PlayClip(clipToPlay);
            }
            else if (showDebugLogs)
            {
                Debug.LogWarning("[WeightedUnitSelectionSFX] No valid clip found to play");
            }
        }

        private WeightedClipSet FindMatchingClipSet(List<GameObject> selectedUnits, int selectionCount)
        {
            if (clipSets == null || clipSets.Length == 0)
                return null;

            foreach (var set in clipSets)
            {
                if (!set.enabled)
                    continue;

                // Check minimum units
                if (selectionCount < set.minUnits)
                    continue;

                // Check required tags
                if (set.requiredTags != null && set.requiredTags.Length > 0)
                {
                    int matchingUnits = 0;

                    foreach (var unit in selectedUnits)
                    {
                        if (unit == null) continue;

                        // Check if unit has any of the required tags
                        bool hasTag = false;
                        foreach (var tag in set.requiredTags)
                        {
                            if (unit.CompareTag(tag))
                            {
                                hasTag = true;
                                break;
                            }
                        }

                        if (hasTag)
                            matchingUnits++;
                    }

                    float actualPercentage = (float)matchingUnits / selectionCount;

                    if (actualPercentage >= set.requiredTagPercentage)
                    {
                        if (showDebugLogs)
                            Debug.Log($"[WeightedUnitSelectionSFX] Matched set '{set.name}' ({matchingUnits}/{selectionCount} = {actualPercentage:P0})");

                        return set;
                    }
                }
                else
                {
                    // No tag requirements, use this set
                    if (showDebugLogs)
                        Debug.Log($"[WeightedUnitSelectionSFX] Matched set '{set.name}' (no tag requirements)");

                    return set;
                }
            }

            return null;
        }

        private AudioClip SelectWeightedClip(WeightedAudioClip[] weightedClips)
        {
            if (weightedClips == null || weightedClips.Length == 0)
                return null;

            // Filter enabled clips with valid audio
            var validClips = weightedClips.Where(wc => wc.enabled && wc.clip != null && wc.weight > 0).ToArray();

            if (validClips.Length == 0)
                return null;

            // Calculate total weight
            float totalWeight = validClips.Sum(wc => wc.weight);

            if (totalWeight <= 0)
                return validClips[0].clip; // Fallback to first clip

            // Select random value
            float randomValue = Random.Range(0f, totalWeight);

            // Find clip based on weight
            float cumulativeWeight = 0f;
            foreach (var weightedClip in validClips)
            {
                cumulativeWeight += weightedClip.weight;
                if (randomValue <= cumulativeWeight)
                {
                    return weightedClip.clip;
                }
            }

            // Fallback (shouldn't reach here)
            return validClips[validClips.Length - 1].clip;
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource == null || clip == null)
                return;

            // Update audio source position to center of selected units
            if (selectionManager != null && selectionManager.SelectionCount > 0)
            {
                Vector3 centerPosition = CalculateSelectionCenter();
                audioSource.transform.position = centerPosition;
            }

            audioSource.clip = clip;
            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.Play();

            lastPlayTime = Time.time;
        }

        private Vector3 CalculateSelectionCenter()
        {
            if (selectionManager == null || selectionManager.SelectionCount == 0)
                return transform.position;

            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (var unit in selectionManager.SelectedUnits)
            {
                if (unit != null)
                {
                    sum += unit.transform.position;
                    count++;
                }
            }

            return count > 0 ? sum / count : transform.position;
        }

        #region Public Methods

        /// <summary>
        /// Manually trigger selection sound (for testing)
        /// </summary>
        public void TestPlay()
        {
            if (selectionManager != null)
            {
                PlaySelectionSound(selectionManager.SelectionCount);
            }
        }

        /// <summary>
        /// Add a new clip set at runtime
        /// </summary>
        public void AddClipSet(WeightedClipSet clipSet)
        {
            var list = new List<WeightedClipSet>(clipSets);
            list.Add(clipSet);
            clipSets = list.ToArray();
        }

        #endregion
    }
}
