using UnityEngine;
using System;
using System.Collections.Generic;
using RTS.Buildings;

namespace RTSBuildingsSystems.ConstructionVisuals
{
    /// <summary>
    /// Manages building LOD progression during construction using MeshRenderer.forceMeshLod.
    /// Transitions from LOD 7 (base) to LOD 0 (complete building) based on construction time.
    /// Includes particle effects, audio, floating numbers, and progress UI.
    /// </summary>
    public class BuildingLODProgression : BaseConstructionVisual
    {
        [Header("LOD Configuration")]
        [Tooltip("the mesh renderer component on the building")]
        [SerializeField] private MeshRenderer lodGrouptargetRenderer;


        [Tooltip("Total number of LOD levels in the mesh (e.g., 8 for LOD 0-7)")]
        [SerializeField] private int lodLevels = 8;

        [Tooltip("Start from highest LOD number (7) to lowest (0)")]
        [SerializeField] private bool reverseDirection = true;

        [Header("Construction Effects")]
        [SerializeField] private ConstructionParticleEffect[] particleEffects;
        [SerializeField] private ConstructionAudioEffect[] audioEffects;

        [Header("Floating Numbers")]
        [Tooltip("Enable floating progress numbers during construction")]
        [SerializeField] private bool showFloatingNumbers = true;

        [Tooltip("Interval between floating number spawns (in seconds)")]
        [SerializeField] private float floatingNumberInterval = 1f;

        [Tooltip("Prefab for floating numbers (should have FloatingText component)")]
        [SerializeField] private GameObject floatingNumberPrefab;

        [Tooltip("Offset position for floating numbers")]
        [SerializeField] private Vector3 floatingNumberOffset = new Vector3(0, 2, 0);

        [Header("Progress Bar Settings")]
        [Tooltip("Show construction progress bar")]
        [SerializeField] private bool showConstructionProgressBar = true;

        [Tooltip("Construction progress bar color")]
        [SerializeField] private Color constructionBarColor = new Color(0.2f, 0.5f, 1f, 0.8f); // Blue

        [Tooltip("Show HP bar after construction")]
        [SerializeField] private bool showHealthBar = true;

        [Tooltip("Health bar color")]
        [SerializeField] private Color healthBarColor = new Color(0f, 1f, 0f, 0.8f); // Green

        [Tooltip("Low health bar color (below 30%)")]
        [SerializeField] private Color lowHealthBarColor = new Color(1f, 0f, 0f, 0.8f); // Red

        [Tooltip("Bar fills from right to left during construction")]
        [SerializeField] private bool fillRightToLeft = true;

        [Header("UI References")]
        [SerializeField] private BuildingProgressUI progressUI;
        [SerializeField] private GameObject worldSpaceCanvas;
        [SerializeField] private UnityEngine.UI.Image progressBarImage;
        [SerializeField] private TMPro.TextMeshProUGUI progressText;

        // Private state
        private int currentLOD = 7; // Start with base (LOD 7)
        private Dictionary<int, bool> activeParticles = new Dictionary<int, bool>();
        private Dictionary<int, bool> activeAudio = new Dictionary<int, bool>();
        private float lastFloatingNumberTime;
        private BuildingHealth buildingHealth;
        private bool isConstructionComplete = false;

        [Serializable]
        public class ConstructionParticleEffect
        {
            [Tooltip("Name for identification")]
            public string name = "Particle Effect";

            [Tooltip("Particle system to play")]
            public ParticleSystem particleSystem;

            [Tooltip("Time to start effect (0-1 of construction time, or absolute seconds if useAbsoluteTime is true)")]
            [Range(0f, 1f)]
            public float startTime = 0f;

            [Tooltip("Duration of effect in seconds (0 = play once)")]
            public float duration = 0f;

            [Tooltip("Use absolute time in seconds instead of normalized 0-1")]
            public bool useAbsoluteTime = false;

            [Tooltip("Loop the particle system")]
            public bool loop = false;

            [Tooltip("Attach to specific LOD (optional, -1 for none)")]
            [Range(-1, 7)]
            public int attachToLOD = -1;
        }

        [Serializable]
        public class ConstructionAudioEffect
        {
            [Tooltip("Name for identification")]
            public string name = "Audio Effect";

            [Tooltip("Audio clip to play")]
            public AudioClip audioClip;

            [Tooltip("Audio source to use (optional, will create one if null)")]
            public AudioSource audioSource;

            [Tooltip("Time to start audio (0-1 of construction time, or absolute seconds if useAbsoluteTime is true)")]
            [Range(0f, 1f)]
            public float startTime = 0f;

            [Tooltip("Duration to play audio in seconds (0 = play full clip)")]
            public float duration = 0f;

            [Tooltip("Use absolute time in seconds instead of normalized 0-1")]
            public bool useAbsoluteTime = false;

            [Tooltip("Loop the audio")]
            public bool loop = false;

            [Tooltip("Volume (0-1)")]
            [Range(0f, 1f)]
            public float volume = 1f;

            [Tooltip("Spatial blend (0 = 2D, 1 = 3D)")]
            [Range(0f, 1f)]
            public float spatialBlend = 1f;
        }

        protected override void Initialize()
        {
            // Find MeshRenderer if not assigned
            if (lodGrouptargetRenderer == null)
            {
                lodGrouptargetRenderer = GetComponentInChildren<MeshRenderer>();
                if (lodGrouptargetRenderer == null)
                {
                    lodGrouptargetRenderer = GetComponent<MeshRenderer>();
                }
            }

            buildingHealth = GetComponentInParent<BuildingHealth>();

            // Initialize particle and audio tracking
            for (int i = 0; i < particleEffects.Length; i++)
            {
                activeParticles[i] = false;
            }
            for (int i = 0; i < audioEffects.Length; i++)
            {
                activeAudio[i] = false;
            }

            // Setup UI
            if (progressUI == null)
            {
                progressUI = GetComponentInChildren<BuildingProgressUI>();
            }

            if (worldSpaceCanvas != null)
            {
                worldSpaceCanvas.SetActive(showConstructionProgressBar);
            }

            lastFloatingNumberTime = Time.time;
            isConstructionComplete = false;

            // Set initial LOD to highest (LOD 7 = base)
            if (lodGrouptargetRenderer != null && reverseDirection)
            {
                SetLODLevel(7);
            }
        }

        protected override void UpdateVisual(float progress)
        {
            if (isConstructionComplete)
            {
                UpdateHealthBar();
                return;
            }

            // Calculate target LOD based on progress (0-1)
            // LOD 7 at 0% -> LOD 0 at 100%
            float lodProgress = progress * (lodLevels - 1);
            int targetLOD = reverseDirection ? (lodLevels - 1) - Mathf.FloorToInt(lodProgress) : Mathf.FloorToInt(lodProgress);
            targetLOD = Mathf.Clamp(targetLOD, 0, lodLevels - 1);

            // Update LOD if changed
            if (targetLOD != currentLOD)
            {
                currentLOD = targetLOD;
                SetLODLevel(currentLOD);
            }

            // Update particle effects
            UpdateParticleEffects(progress);

            // Update audio effects
            UpdateAudioEffects(progress);

            // Update floating numbers
            if (showFloatingNumbers && Time.time - lastFloatingNumberTime >= floatingNumberInterval)
            {
                SpawnFloatingNumber(Mathf.RoundToInt(progress * 100));
                lastFloatingNumberTime = Time.time;
            }

            // Update progress bar
            UpdateProgressBar(progress);

            // Check if construction is complete
            if (progress >= 1f && !isConstructionComplete)
            {
                OnConstructionComplete();
            }
        }

        private void SetLODLevel(int lodLevel)
        {
            if (lodGrouptargetRenderer == null) return;

            // Force the mesh renderer to use a specific LOD index
            // LOD 7 = index 7 (base), LOD 0 = index 0 (complete)
            lodGrouptargetRenderer.forceMeshLod = (short)lodLevel;
        }

        private void UpdateParticleEffects(float progress)
        {
            if (parentBuilding == null) return;

            float constructionTime = parentBuilding.Data.constructionTime;
            float elapsedTime = progress * constructionTime;

            for (int i = 0; i < particleEffects.Length; i++)
            {
                var effect = particleEffects[i];
                if (effect.particleSystem == null) continue;

                float startTime = effect.useAbsoluteTime ? effect.startTime : effect.startTime * constructionTime;
                float endTime = effect.duration > 0 ? startTime + effect.duration : constructionTime;

                bool shouldBeActive = elapsedTime >= startTime && elapsedTime <= endTime;

                // Check LOD attachment
                if (shouldBeActive && effect.attachToLOD >= 0)
                {
                    shouldBeActive = currentLOD == effect.attachToLOD;
                }

                if (shouldBeActive && !activeParticles[i])
                {
                    effect.particleSystem.Play();
                    activeParticles[i] = true;
                }
                else if (!shouldBeActive && activeParticles[i])
                {
                    effect.particleSystem.Stop();
                    activeParticles[i] = false;
                }
            }
        }

        private void UpdateAudioEffects(float progress)
        {
            if (parentBuilding == null) return;

            // Don't play audio if building is in preview/placement mode (not yet placed)
            // Only play sounds after construction has actually started (building is placed)
            if (!parentBuilding.enabled || !parentBuilding.gameObject.activeInHierarchy)
                return;

            // Check if we're still in placement preview - construction progress only updates after placement
            if (progress <= 0f)
                return;

            float constructionTime = parentBuilding.Data.constructionTime;
            float elapsedTime = progress * constructionTime;

            for (int i = 0; i < audioEffects.Length; i++)
            {
                var effect = audioEffects[i];
                if (effect.audioClip == null) continue;

                // Create audio source if needed
                if (effect.audioSource == null)
                {
                    GameObject audioObj = new GameObject($"Audio_{effect.name}");
                    audioObj.transform.SetParent(transform);
                    audioObj.transform.localPosition = Vector3.zero;
                    effect.audioSource = audioObj.AddComponent<AudioSource>();
                    effect.audioSource.clip = effect.audioClip;
                    effect.audioSource.loop = effect.loop;
                    effect.audioSource.volume = effect.volume;
                    effect.audioSource.spatialBlend = effect.spatialBlend;
                    effect.audioSource.playOnAwake = false;
                    effect.audioSource.priority = 0; // Highest priority
                    effect.audioSource.minDistance = 1f;
                    effect.audioSource.maxDistance = 500f;
                    effect.audioSource.rolloffMode = AudioRolloffMode.Linear;
                }

                float startTime = effect.useAbsoluteTime ? effect.startTime : effect.startTime * constructionTime;
                float endTime = effect.duration > 0 ? startTime + effect.duration : constructionTime;

                bool shouldBeActive = elapsedTime >= startTime && elapsedTime <= endTime;

                if (shouldBeActive && !activeAudio[i])
                {
                    effect.audioSource.Play();
                    activeAudio[i] = true;
                }
                else if (!shouldBeActive && activeAudio[i])
                {
                    effect.audioSource.Stop();
                    activeAudio[i] = false;
                }
            }
        }

        private void SpawnFloatingNumber(int percentage)
        {
            if (floatingNumberPrefab == null) return;

            Vector3 spawnPos = transform.position + floatingNumberOffset;
            GameObject floatingObj = Instantiate(floatingNumberPrefab, spawnPos, Quaternion.identity);

            // Try to set the text
            var textMesh = floatingObj.GetComponent<TMPro.TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = $"{percentage}%";
                textMesh.color = constructionBarColor;
            }

            var textMeshUI = floatingObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textMeshUI != null)
            {
                textMeshUI.text = $"{percentage}%";
                textMeshUI.color = constructionBarColor;
            }

            // Try to use FloatingText component if available
            var floatingText = floatingObj.GetComponent<UI.FloatingText>();
            if (floatingText != null)
            {
                floatingText.SetText($"{percentage}%");
                floatingText.SetColor(constructionBarColor);
            }
        }

        private void UpdateProgressBar(float progress)
        {
            if (!showConstructionProgressBar) return;

            if (progressBarImage != null)
            {
                progressBarImage.color = constructionBarColor;

                if (fillRightToLeft)
                {
                    progressBarImage.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Right;
                    progressBarImage.fillAmount = progress;
                }
                else
                {
                    progressBarImage.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
                    progressBarImage.fillAmount = progress;
                }
            }

            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            }

            if (progressUI != null)
            {
                progressUI.UpdateProgress(progress, true);
            }
        }

        private void UpdateHealthBar()
        {
            if (!showHealthBar || buildingHealth == null) return;

            float healthPercent = buildingHealth.HealthPercent;

            if (progressBarImage != null)
            {
                progressBarImage.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
                progressBarImage.fillAmount = healthPercent;
                progressBarImage.color = healthPercent < 0.3f ? lowHealthBarColor : healthBarColor;
            }

            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(buildingHealth.CurrentHealth)}/{Mathf.RoundToInt(buildingHealth.MaxHealth)}";
            }

            if (progressUI != null)
            {
                progressUI.UpdateHealth(healthPercent);
            }
        }

        private void OnConstructionComplete()
        {
            isConstructionComplete = true;

            // Stop all active effects
            foreach (var effect in particleEffects)
            {
                if (effect.particleSystem != null && effect.particleSystem.isPlaying)
                {
                    effect.particleSystem.Stop();
                }
            }

            foreach (var effect in audioEffects)
            {
                if (effect.audioSource != null && effect.audioSource.isPlaying)
                {
                    effect.audioSource.Stop();
                }
            }

            // Ensure final LOD is active (LOD 0 - complete building)
            currentLOD = 0;
            SetLODLevel(0);

            // Release forced LOD to allow normal LOD behavior based on distance
            if (lodGrouptargetRenderer != null)
            {
                lodGrouptargetRenderer.forceMeshLod = -1; // -1 releases the forced LOD
            }

            // Switch to health bar if enabled
            if (showHealthBar && worldSpaceCanvas != null)
            {
                worldSpaceCanvas.SetActive(true);
                UpdateHealthBar();
            }
            else if (worldSpaceCanvas != null)
            {
                worldSpaceCanvas.SetActive(false);
            }

            // Spawn completion floating number
            if (showFloatingNumbers)
            {
                SpawnFloatingNumber(100);
            }
        }

        protected override void Cleanup()
        {
            // Cleanup audio sources
            foreach (var effect in audioEffects)
            {
                if (effect.audioSource != null && effect.audioSource.gameObject.name.StartsWith("Audio_"))
                {
                    Destroy(effect.audioSource.gameObject);
                }
            }

            // Release forced LOD
            if (lodGrouptargetRenderer != null)
            {
                lodGrouptargetRenderer.forceMeshLod = -1;
            }
        }
        // Public methods for external control
        public void EnableHealthBar(bool enable)
        {
            showHealthBar = enable;
            if (isConstructionComplete && worldSpaceCanvas != null)
            {
                worldSpaceCanvas.SetActive(enable);
            }
        }

        public void EnableProgressBar(bool enable)
        {
            showConstructionProgressBar = enable;
            if (!isConstructionComplete && worldSpaceCanvas != null)
            {
                worldSpaceCanvas.SetActive(enable);
            }
        }

        public bool IsConstructionComplete => isConstructionComplete;
        public int CurrentLOD => currentLOD;
        public float ConstructionProgress => parentBuilding != null ? parentBuilding.ConstructionProgress : 0f;
    }
}
