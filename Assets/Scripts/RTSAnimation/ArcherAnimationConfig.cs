using UnityEngine;

namespace RTS.Units.Animation
{
    /// <summary>
    /// Configuration for archer animation system.
    /// Optimized for 100+ animations with performance settings.
    /// </summary>
    [CreateAssetMenu(fileName = "ArcherAnimationConfig", menuName = "RTS/Animation/Archer Config")]
    public class ArcherAnimationConfig : ScriptableObject
    {
        [Header("Animator Controller")]
        [Tooltip("Main animator controller with all archer animations")]
        public RuntimeAnimatorController animatorController;

        [Header("Combat Timing")]
        [Tooltip("Time to draw bow (seconds)")]
        [Range(0.1f, 2f)]
        public float drawDuration = 0.5f;

        [Tooltip("Time to aim at target (seconds)")]
        [Range(0.1f, 2f)]
        public float aimDuration = 0.3f;

        [Tooltip("Time for arrow release animation (seconds)")]
        [Range(0.1f, 2f)]
        public float releaseDuration = 0.4f;

        [Tooltip("Can archer aim while moving?")]
        public bool allowAimWhileMoving = false;

        [Header("Combat Movement Mode")]
        [Tooltip("Combat movement mode: MustStandStill, CanShootWhileMoving, or Adaptive")]
        public CombatMovementMode defaultCombatMode = CombatMovementMode.CanShootWhileMoving;

        [Tooltip("Use standing combat animations even while moving")]
        public bool useStandingAnimationsWhileMoving = false;

        [Tooltip("Reduce movement speed while in combat")]
        public bool reduceSpeedWhileShooting = true;

        [Tooltip("Movement speed multiplier during combat (0-1)")]
        [Range(0f, 1f)]
        public float combatSpeedMultiplier = 0.5f;

        [Header("Movement")]
        [Tooltip("Use 8-way directional movement (vs free blend)")]
        public bool use8WayMovement = true;

        [Tooltip("Smoothing time for direction changes")]
        [Range(0.01f, 0.5f)]
        public float directionSmoothTime = 0.1f;

        [Tooltip("Smoothing time for speed changes")]
        [Range(0.01f, 0.5f)]
        public float speedSmoothTime = 0.1f;

        [Header("Animation Layers")]
        [Tooltip("Use separate upper body layer for aiming while moving")]
        public bool useUpperBodyLayer = true;

        [Tooltip("Upper body layer weight (0-1)")]
        [Range(0f, 1f)]
        public float upperBodyLayerWeight = 1f;

        [Header("Performance - LOD System")]
        [Tooltip("Enable Level of Detail optimization")]
        public bool enableLOD = true;

        [Tooltip("Distance for LOD 0 (full quality)")]
        public float lodDistance1 = 30f;

        [Tooltip("Distance for LOD 1 (good quality)")]
        public float lodDistance2 = 60f;

        [Tooltip("Distance for LOD 2 (reduced quality)")]
        public float lodDistance3 = 100f;

        [Header("Performance - Culling")]
        [Tooltip("Disable animations when unit is not visible")]
        public bool cullWhenNotVisible = true;

        [Tooltip("Update animations every N frames when far away")]
        [Range(1, 10)]
        public int farUpdateInterval = 3;

        [Header("Animation Quality")]
        [Tooltip("Max bones to update per frame at LOD 2")]
        [Range(10, 100)]
        public int reducedBoneCount = 30;

        [Tooltip("Animation update rate at different LODs (updates per second)")]
        public LODUpdateRates lodUpdateRates = new LODUpdateRates
        {
            lod0 = 60,
            lod1 = 30,
            lod2 = 15,
            lod3 = 5
        };

        [Header("Transition Settings")]
        [Tooltip("Blend time between animations")]
        [Range(0f, 1f)]
        public float transitionDuration = 0.15f;

        [Tooltip("Blend time for combat state transitions")]
        [Range(0f, 0.5f)]
        public float combatTransitionDuration = 0.1f;

        [Header("Audio")]
        [Tooltip("Enable footstep sounds")]
        public bool enableFootsteps = true;

        [Tooltip("Enable bow draw/release sounds")]
        public bool enableCombatSounds = true;

        [Header("Advanced")]
        [Tooltip("Enable IK for aiming at targets")]
        public bool enableAimIK = true;

        [Tooltip("IK weight when aiming")]
        [Range(0f, 1f)]
        public float aimIKWeight = 0.8f;

        [Tooltip("Smooth IK weight transitions")]
        [Range(0.01f, 0.5f)]
        public float ikSmoothTime = 0.2f;
    }

    [System.Serializable]
    public struct LODUpdateRates
    {
        public int lod0; // Full quality - 60 fps
        public int lod1; // Good quality - 30 fps
        public int lod2; // Reduced quality - 15 fps
        public int lod3; // Minimal quality - 5 fps
    }
}
