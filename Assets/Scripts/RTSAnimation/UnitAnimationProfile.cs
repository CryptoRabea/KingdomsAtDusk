using UnityEngine;

namespace RTS.Units.Animation
{
    /// <summary>
    /// ScriptableObject profile that defines all animations for a unit type.
    /// Supports locomotion, combat, personality, and look-at animations.
    /// </summary>
    [CreateAssetMenu(fileName = "New Unit Animation Profile", menuName = "RTS/Animation/Unit Animation Profile")]
    public class UnitAnimationProfile : ScriptableObject
    {
        [Header("Profile Info")]
        [SerializeField] private string profileName = "Default";
        [TextArea(2, 4)]
        [SerializeField] private string description = "Animation profile for unit";

        [Header("Locomotion Animations")]
        [Tooltip("Idle animation clip")]
        public AnimationClip idleAnimation;

        [Tooltip("Walk animation clip")]
        public AnimationClip walkAnimation;

        [Tooltip("Run animation clip (optional, falls back to walk if not set)")]
        public AnimationClip runAnimation;

        [Header("Combat Animations")]
        [Tooltip("Primary attack animation")]
        public AnimationClip attackAnimation;

        [Tooltip("Additional attack variations (randomly selected)")]
        public AnimationClip[] attackVariations;

        [Tooltip("Hit/damage reaction animation")]
        public AnimationClip hitAnimation;

        [Tooltip("Death animation")]
        public AnimationClip deathAnimation;

        [Header("Personality Animations")]
        [Tooltip("Idle variation animations (0-3 for IdleVariant parameter)")]
        public AnimationClip[] idleVariants = new AnimationClip[4];

        [Tooltip("Victory/celebration animation")]
        public AnimationClip victoryAnimation;

        [Tooltip("Retreat/fear animation")]
        public AnimationClip retreatAnimation;

        [Tooltip("Special idle actions (yawn, stretch, look around, etc.)")]
        public AnimationClip[] specialIdleActions;

        [Header("Personality Settings")]
        [Tooltip("Minimum time before triggering an idle action")]
        [Range(3f, 30f)]
        public float minIdleTime = 5f;

        [Tooltip("Maximum time before triggering an idle action")]
        [Range(5f, 60f)]
        public float maxIdleTime = 15f;

        [Tooltip("Probability of playing an idle action when timer expires (0-1)")]
        [Range(0f, 1f)]
        public float idleActionProbability = 0.7f;

        [Header("Look-At / Aim Settings")]
        [Tooltip("Default look weight (0 = no look, 1 = full look)")]
        [Range(0f, 1f)]
        public float lookWeight = 0.5f;

        [Tooltip("Look transition speed (higher = faster)")]
        [Range(0.1f, 10f)]
        public float lookSpeed = 2f;

        [Tooltip("Enable look-at/aim rig for this unit")]
        public bool enableLookAt = false;

        [Header("Animation Speeds")]
        [Tooltip("Movement speed multiplier for animations")]
        [Range(0.5f, 2f)]
        public float animationSpeedMultiplier = 1f;

        [Tooltip("Attack speed multiplier")]
        [Range(0.5f, 3f)]
        public float attackSpeedMultiplier = 1f;

        [Header("Blend Settings")]
        [Tooltip("Transition time between animations")]
        [Range(0.05f, 0.5f)]
        public float transitionDuration = 0.1f;

        /// <summary>
        /// Get a random attack animation from available variations
        /// </summary>
        public AnimationClip GetRandomAttackAnimation()
        {
            if (attackVariations != null && attackVariations.Length > 0)
            {
                int randomIndex = Random.Range(0, attackVariations.Length + 1);
                if (randomIndex == 0)
                    return attackAnimation;
                return attackVariations[randomIndex - 1];
            }
            return attackAnimation;
        }

        /// <summary>
        /// Get a random idle variant animation
        /// </summary>
        public AnimationClip GetRandomIdleVariant()
        {
            if (idleVariants != null && idleVariants.Length > 0)
            {
                var validVariants = System.Array.FindAll(idleVariants, clip => clip != null);
                if (validVariants.Length > 0)
                    return validVariants[Random.Range(0, validVariants.Length)];
            }
            return idleAnimation;
        }

        /// <summary>
        /// Get a random special idle action
        /// </summary>
        public AnimationClip GetRandomSpecialIdleAction()
        {
            if (specialIdleActions != null && specialIdleActions.Length > 0)
            {
                var validActions = System.Array.FindAll(specialIdleActions, clip => clip != null);
                if (validActions.Length > 0)
                    return validActions[Random.Range(0, validActions.Length)];
            }
            return null;
        }

        /// <summary>
        /// Get idle variant by index (0-3)
        /// </summary>
        public AnimationClip GetIdleVariant(int index)
        {
            if (idleVariants != null && index >= 0 && index < idleVariants.Length)
            {
                return idleVariants[index];
            }
            return idleAnimation;
        }

        /// <summary>
        /// Validate the profile and log warnings for missing animations
        /// </summary>
      

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure min/max idle times are valid
            if (minIdleTime > maxIdleTime)
            {
                minIdleTime = maxIdleTime - 1f;
            }
        }
#endif
    }
}
