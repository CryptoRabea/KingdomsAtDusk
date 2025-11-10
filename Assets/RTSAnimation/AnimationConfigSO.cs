using UnityEngine;

namespace RTS.Units.Animation
{
    /// <summary>
    /// ScriptableObject containing animation configuration.
    /// Allows designers to customize animation behavior per unit type.
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationConfig", menuName = "RTS/AnimationConfig")]
    public class AnimationConfigSO : ScriptableObject
    {
        [Header("Animation Clips")]
        [Tooltip("Reference to the Animator Controller for this unit")]
        public RuntimeAnimatorController animatorController;

        [Header("Transition Settings")]
        [Range(0f, 1f)]
        [Tooltip("How quickly animations blend together")]
        public float transitionDuration = 0.15f;

        [Header("Movement")]
        [Tooltip("Speed threshold to trigger walk animation")]
        public float walkThreshold = 0.1f;
        
        [Tooltip("Speed for run animation (if available)")]
        public float runThreshold = 5f;

        [Range(0f, 2f)]
        [Tooltip("Animation speed multiplier for walk")]
        public float walkSpeedMultiplier = 1f;

        [Header("Combat")]
        [Range(0f, 2f)]
        [Tooltip("Attack animation speed multiplier")]
        public float attackSpeedMultiplier = 1f;

        [Tooltip("Frame in attack animation where damage is dealt")]
        public float attackHitFrame = 0.5f;

        [Header("Root Motion")]
        [Tooltip("Use animation root motion for movement")]
        public bool useRootMotion = false;

        [Header("Audio")]
        [Tooltip("Play footstep sounds from animation events")]
        public bool enableFootsteps = true;
        
        [Tooltip("Play attack sounds from animation events")]
        public bool enableAttackSounds = true;

        [Header("Advanced")]
        [Tooltip("Layer mask for IK targets (if using IK)")]
        public LayerMask ikTargetLayers;

        [Tooltip("Enable look-at IK for aiming")]
        public bool enableLookAtIK = false;
    }
}
