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
        }

        #region Animation Event Callbacks

        /// <summary>
        /// Called from animation event on footstep frames.
        /// </summary>
        public void OnFootstep()
        {
            PlayRandomSound(footstepSounds, footstepVolume);
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
            Debug.Log($"{gameObject.name}: Attack Hit Frame");
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
            Debug.Log($"{gameObject.name}: Death Complete");
        }

        #endregion

        #region Audio Playback

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
            var particleSystem = effect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
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
