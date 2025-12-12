using UnityEngine;

namespace RTS.Units.Animation
{
    /// <summary>
    /// Handles animation events for audio, particles, and other effects.
    /// Attach this to the root GameObject with the Animator.
    /// </summary>
    public class UnitAnimationEvents : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] footstepSounds;
        [SerializeField] private AudioClip[] attackSounds;
        [SerializeField] private AudioClip[] hitSounds;
        [SerializeField] private AudioClip deathSound;

        [Header("Volume Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float footstepVolume = 0.5f;
        [Range(0f, 1f)]
        [SerializeField] private float attackVolume = 0.7f;
        [Range(0f, 1f)]
        [SerializeField] private float hitVolume = 0.6f;
        [Range(0f, 1f)]
        [SerializeField] private float deathVolume = 0.8f;

        [Header("Particle Effects")]
        [SerializeField] private GameObject attackEffectPrefab;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private Transform effectSpawnPoint;

        [Header("Settings")]
        [SerializeField] private bool randomizePitch = true;
        [SerializeField] private float pitchVariation = 0.1f;

        [Header("Performance Optimization")]
        [SerializeField] private bool enableDistanceCulling = true;
        [SerializeField] private float maxFootstepDistance = 50f;
        [SerializeField] private float minFootstepInterval = 0.2f;
        [SerializeField] private int maxConcurrentFootsteps = 20;

        private static int currentFootstepCount = 0;
        private static readonly object footstepLock = new object();
        private float lastFootstepTime = 0f;
        private Camera mainCamera;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();

                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 1f; // 3D sound
                }
            }

            mainCamera = Camera.main;
        }

        #region Animation Event Callbacks

        /// <summary>
        /// Called from animation event on footstep frames.
        /// </summary>
        public void OnFootstep()
        {
            PlayFootstepSound();
        }

        /// <summary>
        /// Called from animation event for right foot step.
        /// </summary>
        public void RightFoot()
        {
            PlayFootstepSound();
        }

        /// <summary>
        /// Called from animation event for left foot step.
        /// </summary>
        public void LeftFoot()
        {
            PlayFootstepSound();
        }

        /// <summary>
        /// Called from animation event for right foot step (alternate name).
        /// </summary>
        public void RightFootStep()
        {
            PlayFootstepSound();
        }

        /// <summary>
        /// Called from animation event for left foot step (alternate name).
        /// </summary>
        public void LeftFootStep()
        {
            PlayFootstepSound();
        }

        /// <summary>
        /// Called from animation event when attack starts.
        /// </summary>
        public void OnAttackStart()
        {
            PlayRandomSound(attackSounds, attackVolume);
            SpawnEffect(attackEffectPrefab);
        }

        /// <summary>
        /// Called from animation event at the moment attack hits.
        /// This is when damage should be applied.
        /// </summary>
        public void OnAttackHit()
        {
            // The UnitAnimationController will handle actual damage
            // This is just for effects
        }

        /// <summary>
        /// Called from animation event when unit takes damage.
        /// </summary>
        public void OnHit()
        {
            PlayRandomSound(hitSounds, hitVolume);
            SpawnEffect(hitEffectPrefab);
        }

        /// <summary>
        /// Called from animation event when death animation starts.
        /// </summary>
        public void OnDeath()
        {
            PlaySound(deathSound, deathVolume);
        }

        /// <summary>
        /// Called from animation when death animation completes.
        /// </summary>
        public void OnDeathComplete()
        {
        }

        #endregion

        #region Audio Playback

        /// <summary>
        /// Plays a footstep sound with performance optimizations.
        /// </summary>
        private void PlayFootstepSound()
        {
            // Performance optimization: Check minimum interval between footsteps
            float timeSinceLastFootstep = Time.time - lastFootstepTime;
            if (timeSinceLastFootstep < minFootstepInterval)
            {
                return;
            }

            // Performance optimization: Distance culling
            if (enableDistanceCulling && mainCamera != null)
            {
                float distanceToCamera = Vector3.Distance(transform.position, mainCamera.transform.position);
                if (distanceToCamera > maxFootstepDistance)
                {
                    return;
                }
            }

            // Performance optimization: Limit concurrent footsteps globally
            lock (footstepLock)
            {
                if (currentFootstepCount >= maxConcurrentFootsteps)
                {
                    return;
                }
                currentFootstepCount++;
            }

            lastFootstepTime = Time.time;
            PlayRandomSound(footstepSounds, footstepVolume);

            // Decrement counter after sound duration
            if (footstepSounds != null && footstepSounds.Length > 0 && footstepSounds[0] != null)
            {
                float soundDuration = footstepSounds[0].length;
                Invoke(nameof(DecrementFootstepCount), soundDuration);
            }
            else
            {
                Invoke(nameof(DecrementFootstepCount), 0.5f); // Default duration
            }
        }

        /// <summary>
        /// Decrements the global footstep counter.
        /// </summary>
        private void DecrementFootstepCount()
        {
            lock (footstepLock)
            {
                currentFootstepCount = Mathf.Max(0, currentFootstepCount - 1);
            }
        }

        private void PlayRandomSound(AudioClip[] clips, float volume)
        {
            if (clips == null || clips.Length == 0) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            PlaySound(clip, volume);
        }

        private void PlaySound(AudioClip clip, float volume)
        {
            if (clip == null || audioSource == null) return;

            // Randomize pitch
            if (randomizePitch)
            {
                audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            }
            else
            {
                audioSource.pitch = 1f;
            }

            audioSource.PlayOneShot(clip, volume);
        }

        #endregion

        #region Effect Spawning

        private void SpawnEffect(GameObject effectPrefab)
        {
            if (effectPrefab == null) return;

            Vector3 spawnPos = effectSpawnPoint != null 
                ? effectSpawnPoint.position 
                : transform.position;

            GameObject effect = Instantiate(effectPrefab, spawnPos, Quaternion.identity);
            
            // Auto-destroy particle effects
            if (effect.TryGetComponent<ParticleSystem>(out var particleSystem))
            {
                Destroy(effect, particleSystem.main.duration + particleSystem.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(effect, 2f);
            }
        }

        #endregion

        #region Public API

        public void PlayFootstep()
        {
            OnFootstep();
        }

        public void PlayAttackSound()
        {
            PlayRandomSound(attackSounds, attackVolume);
        }

        public void PlayHitSound()
        {
            PlayRandomSound(hitSounds, hitVolume);
        }

        public void PlayDeathSound()
        {
            PlaySound(deathSound, deathVolume);
        }

        #endregion
    }
}
