using UnityEngine;
using System.Collections.Generic;

namespace RTS.Units.Animation
{
    /// <summary>
    /// Loads animation profile into Animator using AnimatorOverrideController.
    /// This allows runtime swapping of animations while keeping the same state machine.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class UnitAnimatorProfileLoader : MonoBehaviour
    {
        [Header("Profile")]
        [SerializeField] private UnitAnimationProfile animationProfile;

        [Header("Auto-Load Settings")]
        [SerializeField] private bool loadOnAwake = true;
        [SerializeField] private bool createRuntimeCopy = true;

        private Animator animator;
        private AnimatorOverrideController overrideController;
        private RuntimeAnimatorController originalController;

        public UnitAnimationProfile Profile => animationProfile;
        public AnimatorOverrideController OverrideController => overrideController;

        private void Awake()
        {
            animator = GetComponent<Animator>();

            if (animator == null)
            {
                Debug.LogError($"[UnitAnimatorProfileLoader] No Animator found on {gameObject.name}!");
                enabled = false;
                return;
            }

            if (loadOnAwake && animationProfile != null)
            {
                LoadProfile(animationProfile);
            }
        }

        /// <summary>
        /// Load an animation profile into the Animator.
        /// Creates an AnimatorOverrideController to swap animations.
        /// </summary>
        public void LoadProfile(UnitAnimationProfile profile)
        {
            if (profile == null)
            {
                Debug.LogError($"[UnitAnimatorProfileLoader] Cannot load null profile on {gameObject.name}!");
                return;
            }

            if (animator == null)
            {
                Debug.LogError($"[UnitAnimatorProfileLoader] Animator not initialized on {gameObject.name}!");
                return;
            }

            animationProfile = profile;

            // Validate profile
            profile.ValidateProfile();

            // Store original controller if not already stored
            if (originalController == null)
            {
                originalController = animator.runtimeAnimatorController;
            }

            // Create override controller if needed
            if (overrideController == null)
            {
                if (createRuntimeCopy)
                {
                    // Create a runtime copy to avoid modifying the original
                    overrideController = new AnimatorOverrideController(originalController);
                    animator.runtimeAnimatorController = overrideController;
                }
                else
                {
                    // Use the original controller (requires it to already be an AnimatorOverrideController)
                    if (originalController is AnimatorOverrideController existingOverride)
                    {
                        overrideController = existingOverride;
                    }
                    else
                    {
                        Debug.LogError($"[UnitAnimatorProfileLoader] Original controller is not an AnimatorOverrideController! Creating runtime copy instead.");
                        overrideController = new AnimatorOverrideController(originalController);
                        animator.runtimeAnimatorController = overrideController;
                    }
                }
            }

            // Apply animation overrides
            ApplyAnimationOverrides();

            // Apply animation speed
            animator.speed = profile.animationSpeedMultiplier;

            Debug.Log($"[UnitAnimatorProfileLoader] Loaded profile '{profile.name}' on {gameObject.name}");
        }

        /// <summary>
        /// Apply animation overrides from the profile to the AnimatorOverrideController.
        /// </summary>
        private void ApplyAnimationOverrides()
        {
            if (overrideController == null || animationProfile == null)
                return;

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();

            // Get all current overrides
            overrideController.GetOverrides(overrides);

            // Build a dictionary for easier mapping
            var overrideDict = new Dictionary<string, AnimationClip>();

            // Locomotion
            if (animationProfile.idleAnimation != null)
                overrideDict["Idle"] = animationProfile.idleAnimation;

            if (animationProfile.walkAnimation != null)
            {
                overrideDict["Walk"] = animationProfile.walkAnimation;
                overrideDict["Walking"] = animationProfile.walkAnimation;
            }

            if (animationProfile.runAnimation != null)
            {
                overrideDict["Run"] = animationProfile.runAnimation;
                overrideDict["Running"] = animationProfile.runAnimation;
            }
            else if (animationProfile.walkAnimation != null)
            {
                // Fallback: use walk for run
                overrideDict["Run"] = animationProfile.walkAnimation;
                overrideDict["Running"] = animationProfile.walkAnimation;
            }

            // Combat
            if (animationProfile.attackAnimation != null)
                overrideDict["Attack"] = animationProfile.attackAnimation;

            if (animationProfile.hitAnimation != null)
            {
                overrideDict["Hit"] = animationProfile.hitAnimation;
                overrideDict["GetHit"] = animationProfile.hitAnimation;
                overrideDict["Damaged"] = animationProfile.hitAnimation;
            }

            if (animationProfile.deathAnimation != null)
            {
                overrideDict["Death"] = animationProfile.deathAnimation;
                overrideDict["Die"] = animationProfile.deathAnimation;
            }

            // Personality
            if (animationProfile.victoryAnimation != null)
            {
                overrideDict["Victory"] = animationProfile.victoryAnimation;
                overrideDict["Celebrate"] = animationProfile.victoryAnimation;
            }

            if (animationProfile.retreatAnimation != null)
            {
                overrideDict["Retreat"] = animationProfile.retreatAnimation;
                overrideDict["Fear"] = animationProfile.retreatAnimation;
            }

            // Idle Variants
            for (int i = 0; i < animationProfile.idleVariants.Length; i++)
            {
                if (animationProfile.idleVariants[i] != null)
                {
                    overrideDict[$"IdleVariant{i}"] = animationProfile.idleVariants[i];
                    overrideDict[$"Idle{i}"] = animationProfile.idleVariants[i];
                    overrideDict[$"IdleAction{i}"] = animationProfile.idleVariants[i];
                }
            }

            // Apply overrides by matching clip names
            for (int i = 0; i < overrides.Count; i++)
            {
                var pair = overrides[i];
                string clipName = pair.Key.name;

                if (overrideDict.ContainsKey(clipName))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, overrideDict[clipName]);
                    Debug.Log($"[UnitAnimatorProfileLoader] Override: {clipName} -> {overrideDict[clipName].name}");
                }
            }

            // Set all overrides back
            overrideController.ApplyOverrides(overrides);
        }

        /// <summary>
        /// Swap to a different profile at runtime.
        /// </summary>
        public void SwapProfile(UnitAnimationProfile newProfile)
        {
            if (newProfile == null)
            {
                Debug.LogWarning($"[UnitAnimatorProfileLoader] Cannot swap to null profile on {gameObject.name}");
                return;
            }

            LoadProfile(newProfile);
        }

        /// <summary>
        /// Restore the original animator controller (remove overrides).
        /// </summary>
        public void RestoreOriginalController()
        {
            if (animator != null && originalController != null)
            {
                animator.runtimeAnimatorController = originalController;
                overrideController = null;
                Debug.Log($"[UnitAnimatorProfileLoader] Restored original controller on {gameObject.name}");
            }
        }

        /// <summary>
        /// Get a specific animation clip by name from the profile.
        /// </summary>
        public AnimationClip GetClipByName(string clipName)
        {
            if (animationProfile == null)
                return null;

            return clipName.ToLower() switch
            {
                "idle" => animationProfile.idleAnimation,
                "walk" or "walking" => animationProfile.walkAnimation,
                "run" or "running" => animationProfile.runAnimation ?? animationProfile.walkAnimation,
                "attack" => animationProfile.attackAnimation,
                "hit" or "gethit" or "damaged" => animationProfile.hitAnimation,
                "death" or "die" => animationProfile.deathAnimation,
                "victory" or "celebrate" => animationProfile.victoryAnimation,
                "retreat" or "fear" => animationProfile.retreatAnimation,
                _ => null
            };
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor utility: Reload profile (useful for testing).
        /// </summary>
        [ContextMenu("Reload Profile")]
        private void ReloadProfile()
        {
            if (animationProfile != null)
            {
                LoadProfile(animationProfile);
            }
            else
            {
                Debug.LogWarning("[UnitAnimatorProfileLoader] No profile assigned to reload!");
            }
        }

        /// <summary>
        /// Editor utility: List all clips in the current override controller.
        /// </summary>
        [ContextMenu("Debug: List Override Clips")]
        private void DebugListOverrides()
        {
            if (overrideController == null)
            {
                Debug.Log("[UnitAnimatorProfileLoader] No override controller active.");
                return;
            }

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(overrides);

            Debug.Log($"[UnitAnimatorProfileLoader] {overrides.Count} animation overrides:");
            foreach (var pair in overrides)
            {
                string original = pair.Key != null ? pair.Key.name : "null";
                string replacement = pair.Value != null ? pair.Value.name : "null";
                Debug.Log($"  {original} -> {replacement}");
            }
        }
#endif
    }
}
