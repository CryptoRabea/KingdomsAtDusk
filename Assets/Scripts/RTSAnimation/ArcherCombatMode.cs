using UnityEngine;

namespace RTS.Units.Animation
{
    /// <summary>
    /// Controls archer combat behavior - can shoot while moving or must stand still.
    /// Provides flexible combat modes for different archer types or gameplay situations.
    /// </summary>
    public class ArcherCombatMode : MonoBehaviour
    {
        [Header("Combat Mode Settings")]
        [SerializeField] private CombatMovementMode movementMode = CombatMovementMode.CanShootWhileMoving;

        [Header("Stationary Combat Settings")]
        [Tooltip("When must stand still: stop this distance from target")]
        [SerializeField] private float stationaryAttackRange = 8f;

        [Tooltip("How long to wait after stopping before shooting (seconds)")]
        [SerializeField] private float aimSettleTime = 0.2f;

        [Tooltip("Force unit to face target when stationary")]
        [SerializeField] private bool autoFaceTarget = true;

        [Header("Moving Combat Settings")]
        [Tooltip("When shooting while moving: reduce movement speed")]
        [SerializeField] private bool reduceSpeedWhileShooting = true;

        [Tooltip("Movement speed multiplier while drawing/aiming (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float combatSpeedMultiplier = 0.5f;

        [Header("Animation Blending")]
        [Tooltip("Use standing animations even while moving (requires blend setup)")]
        [SerializeField] private bool useStandingAnimationsWhileMoving = false;

        [Tooltip("Blend weight between moving and standing combat animations")]
        [Range(0f, 1f)]
        [SerializeField] private float standingAnimationWeight = 1f;

        [Header("Runtime Control")]
        [Tooltip("Allow mode to be changed at runtime (for abilities, etc.)")]
        [SerializeField] private bool allowRuntimeModeChange = true;

        // Component references
        private UnitMovement movement;
        private UnitCombat combat;
        private ArcherAnimationController animController;

        // State tracking
        private float timeSinceStoppedMoving = 0f;
        private bool isSettled = false;
        private Vector3 lastPosition;
        private float originalSpeed;

        public CombatMovementMode CurrentMode => movementMode;
        public bool IsSettled => isSettled;
        public bool CanShootNow => DetermineCanShoot();

        private void Awake()
        {
            movement = GetComponent<UnitMovement>();
            combat = GetComponent<UnitCombat>();
            animController = GetComponent<ArcherAnimationController>();

            if (movement != null)
            {
                originalSpeed = movement.Speed;
            }

            lastPosition = transform.position;
        }

        private void Update()
        {
            UpdateCombatMode();
        }

        private void UpdateCombatMode()
        {
            if (movement == null || combat == null) return;

            switch (movementMode)
            {
                case CombatMovementMode.MustStandStill:
                    UpdateStationaryCombat();
                    break;

                case CombatMovementMode.CanShootWhileMoving:
                    UpdateMovingCombat();
                    break;

                case CombatMovementMode.Adaptive:
                    UpdateAdaptiveCombat();
                    break;
            }

            // Update animator parameters
            UpdateAnimatorParameters();
        }

        #region Stationary Combat Mode

        private void UpdateStationaryCombat()
        {
            bool isMoving = movement.IsMoving;
            bool hasTarget = combat.CurrentTarget != null;

            // Track if we've stopped moving
            if (!isMoving)
            {
                timeSinceStoppedMoving += Time.deltaTime;
                isSettled = timeSinceStoppedMoving >= aimSettleTime;
            }
            else
            {
                timeSinceStoppedMoving = 0f;
                isSettled = false;
            }

            // If in combat and settled, face target
            if (hasTarget && isSettled && autoFaceTarget)
            {
                FaceTarget(combat.CurrentTarget);
            }

            // Force stop if trying to attack
            if (hasTarget && combat.IsInAttackRange && isMoving)
            {
                // Stop movement to allow shooting
                movement.Stop();
            }
        }

        #endregion

        #region Moving Combat Mode

        private void UpdateMovingCombat()
        {
            bool inCombat = combat != null && combat.CurrentTarget != null;
            bool isAttacking = animController != null && animController.IsInCombat();

            if (inCombat && isAttacking && reduceSpeedWhileShooting)
            {
                // Reduce movement speed while shooting
                if (movement != null)
                {
                    movement.SetSpeedMultiplier(combatSpeedMultiplier);
                }
            }
            else
            {
                // Restore normal speed
                if (movement != null)
                {
                    movement.SetSpeedMultiplier(1f);
                }
            }

            // Always settled when can shoot while moving
            isSettled = true;
        }

        #endregion

        #region Adaptive Combat Mode

        private void UpdateAdaptiveCombat()
        {
            // Adaptive: Use stationary if standing, moving if moving
            bool isMoving = movement != null && movement.IsMoving;

            if (isMoving)
            {
                UpdateMovingCombat();
            }
            else
            {
                UpdateStationaryCombat();
            }
        }

        #endregion

        #region Helper Methods

        private bool DetermineCanShoot()
        {
            switch (movementMode)
            {
                case CombatMovementMode.MustStandStill:
                    return !movement.IsMoving && isSettled;

                case CombatMovementMode.CanShootWhileMoving:
                    return true;

                case CombatMovementMode.Adaptive:
                    return true; // Can shoot in either state

                default:
                    return true;
            }
        }

        private void FaceTarget(Transform target)
        {
            if (target == null) return;

            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0; // Keep on horizontal plane

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * 10f
                );
            }
        }

        private void UpdateAnimatorParameters()
        {
            if (animController == null || animController.Animator == null) return;

            // Set parameter for using standing animations while moving
            int useStandingHash = Animator.StringToHash("UseStandingCombat");
            if (HasParameter(animController.Animator, "UseStandingCombat"))
            {
                animController.Animator.SetBool(useStandingHash, useStandingAnimationsWhileMoving);
            }

            // Set combat mode parameter
            int combatModeHash = Animator.StringToHash("CombatMode");
            if (HasParameter(animController.Animator, "CombatMode"))
            {
                animController.Animator.SetInteger(combatModeHash, (int)movementMode);
            }

            // Set standing animation weight
            int standingWeightHash = Animator.StringToHash("StandingWeight");
            if (HasParameter(animController.Animator, "StandingWeight"))
            {
                animController.Animator.SetFloat(standingWeightHash, standingAnimationWeight);
            }
        }

        private bool HasParameter(Animator anim, string paramName)
        {
            foreach (AnimatorControllerParameter param in anim.parameters)
            {
                if (param.name == paramName)
                    return true;
            }
            return false;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Change combat mode at runtime
        /// </summary>
        public void SetCombatMode(CombatMovementMode mode)
        {
            if (!allowRuntimeModeChange)
            {
                Debug.LogWarning($"[ArcherCombatMode] Runtime mode change disabled on {gameObject.name}");
                return;
            }

            movementMode = mode;

            // Reset state
            timeSinceStoppedMoving = 0f;
            isSettled = false;

            Debug.Log($"[ArcherCombatMode] Changed to {mode} mode");
        }

        /// <summary>
        /// Toggle between stationary and moving modes
        /// </summary>
        public void ToggleCombatMode()
        {
            if (movementMode == CombatMovementMode.MustStandStill)
            {
                SetCombatMode(CombatMovementMode.CanShootWhileMoving);
            }
            else
            {
                SetCombatMode(CombatMovementMode.MustStandStill);
            }
        }

        /// <summary>
        /// Enable/disable using standing animations while moving
        /// </summary>
        public void SetUseStandingAnimations(bool useStanding)
        {
            useStandingAnimationsWhileMoving = useStanding;
        }

        /// <summary>
        /// Set the blend weight for standing animations
        /// </summary>
        public void SetStandingAnimationWeight(float weight)
        {
            standingAnimationWeight = Mathf.Clamp01(weight);
        }

        /// <summary>
        /// Temporarily force stationary mode (for abilities, etc.)
        /// </summary>
        public void ForceStationaryMode(float duration)
        {
            StartCoroutine(TemporaryModeChange(CombatMovementMode.MustStandStill, duration));
        }

        /// <summary>
        /// Temporarily force moving mode
        /// </summary>
        public void ForceMovingMode(float duration)
        {
            StartCoroutine(TemporaryModeChange(CombatMovementMode.CanShootWhileMoving, duration));
        }

        private System.Collections.IEnumerator TemporaryModeChange(CombatMovementMode tempMode, float duration)
        {
            CombatMovementMode originalMode = movementMode;
            SetCombatMode(tempMode);

            yield return new WaitForSeconds(duration);

            SetCombatMode(originalMode);
        }

        /// <summary>
        /// Check if archer should be allowed to attack based on current mode
        /// </summary>
        public bool ShouldAllowAttack()
        {
            return CanShootNow;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            // Draw stationary attack range
            if (movementMode == CombatMovementMode.MustStandStill)
            {
                Gizmos.color = isSettled ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(transform.position, stationaryAttackRange);
            }

            // Draw movement speed indicator
            if (movement != null && movement.IsMoving)
            {
                Gizmos.color = reduceSpeedWhileShooting && animController != null && animController.IsInCombat()
                    ? Color.red
                    : Color.blue;

                Vector3 velocity = movement.Velocity;
                Gizmos.DrawRay(transform.position, velocity * 2f);
            }
        }

        #endregion
    }

    /// <summary>
    /// Combat movement modes for archer behavior
    /// </summary>
    public enum CombatMovementMode
    {
        /// <summary>
        /// Archer must stop moving completely to shoot
        /// More realistic, requires positioning
        /// </summary>
        MustStandStill = 0,

        /// <summary>
        /// Archer can shoot while moving (kiting)
        /// More arcade-style, better for micro-management
        /// </summary>
        CanShootWhileMoving = 1,

        /// <summary>
        /// Adaptive: Uses standing animations when still, moving when moving
        /// Best of both worlds
        /// </summary>
        Adaptive = 2
    }
}
