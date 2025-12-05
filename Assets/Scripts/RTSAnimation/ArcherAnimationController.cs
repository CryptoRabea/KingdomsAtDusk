using UnityEngine;
using RTS.Core.Events;

namespace RTS.Units.Animation
{
    /// <summary>
    /// High-performance directional animation controller for archers.
    /// Handles 100+ animations with draw-aim-release combat sequences and 8-way movement.
    /// Optimized for multiple units with LOD and culling.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class ArcherAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Combat Settings")]
        [SerializeField] private float drawDuration = 0.5f;
        [SerializeField] private float aimDuration = 0.3f;
        [SerializeField] private float releaseDuration = 0.4f;
        [SerializeField] private bool allowAimWhileMoving = true;

        [Header("Movement Settings")]
        [SerializeField] private float directionSmoothTime = 0.1f;
        [SerializeField] private float speedSmoothTime = 0.1f;
        [SerializeField] private bool use8WayMovement = true;

        [Header("Performance Optimization")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float lodDistance1 = 30f; // Full detail
        [SerializeField] private float lodDistance2 = 60f; // Reduced detail
        [SerializeField] private float lodDistance3 = 100f; // Minimal detail
        [SerializeField] private bool cullWhenNotVisible = true;

        [Header("Animation Layering")]
        [SerializeField] private bool useUpperBodyLayer = true;
        [SerializeField] private int upperBodyLayerIndex = 1;
        [SerializeField] private float upperBodyLayerWeight = 1f;

        // Component references
        private UnitMovement movement;
        private UnitCombat combat;
        private UnitHealth health;
        private Camera mainCamera;
        private Renderer[] renderers;

        // Animation state
        private ArcherCombatState combatState = ArcherCombatState.Idle;
        private ArcherMovementState movementState = ArcherMovementState.Idle;
        private float currentAttackTime = 0f;
        private Vector3 lastMoveDirection;
        private bool isDead = false;

        // Performance tracking
        private int currentLODLevel = 0;
        private bool isVisible = true;
        private float distanceToCamera = 0f;

        // Directional movement
        private float currentDirectionX = 0f;
        private float currentDirectionY = 0f;
        private float currentSpeed = 0f;
        private Vector3 smoothVelocity;

        // Animation parameter hashes
        private static readonly int DirectionXHash = Animator.StringToHash("DirectionX");
        private static readonly int DirectionYHash = Animator.StringToHash("DirectionY");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int CombatStateHash = Animator.StringToHash("CombatState");
        private static readonly int DrawTriggerHash = Animator.StringToHash("Draw");
        private static readonly int AimTriggerHash = Animator.StringToHash("Aim");
        private static readonly int ReleaseTriggerHash = Animator.StringToHash("Release");
        private static readonly int DeathTriggerHash = Animator.StringToHash("Death");
        private static readonly int HitTriggerHash = Animator.StringToHash("Hit");
        private static readonly int LODLevelHash = Animator.StringToHash("LODLevel");

        public ArcherCombatState CombatState => combatState;
        public ArcherMovementState MovementState => movementState;
        public int LODLevel => currentLODLevel;

        #region Initialization

        private void Awake()
        {
            InitializeComponents();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeComponents()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                Debug.LogError($"[ArcherAnimation] Animator not found on {gameObject.name}!");
                enabled = false;
                return;
            }

            // Get components
            movement = GetComponent<UnitMovement>();
            combat = GetComponent<UnitCombat>();
            health = GetComponent<UnitHealth>();
            mainCamera = Camera.main;
            renderers = GetComponentsInChildren<Renderer>();

            // Configure animator
            animator.applyRootMotion = false;

            // Set up animation layers
            if (useUpperBodyLayer && animator.layerCount > upperBodyLayerIndex)
            {
                animator.SetLayerWeight(upperBodyLayerIndex, upperBodyLayerWeight);
            }
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<UnitHealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<UnitHealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            if (isDead) return;

            UpdatePerformanceOptimization();

            if (!isVisible && cullWhenNotVisible)
            {
                // Skip animation updates when not visible
                return;
            }

            UpdateMovementAnimation();
            UpdateCombatAnimation();
            UpdateAnimatorParameters();
        }

        #endregion

        #region Movement Animation

        private void UpdateMovementAnimation()
        {
            if (movement == null)
            {
                movementState = ArcherMovementState.Idle;
                return;
            }

            bool isMoving = movement.IsMoving;
            Vector3 velocity = movement.Velocity;
            float speed = velocity.magnitude;

            // Determine movement state
            if (!isMoving || speed < 0.1f)
            {
                movementState = ArcherMovementState.Idle;
                currentSpeed = Mathf.SmoothDamp(currentSpeed, 0f, ref smoothVelocity.x, speedSmoothTime);
            }
            else
            {
                movementState = ArcherMovementState.Walking;
                currentSpeed = Mathf.SmoothDamp(currentSpeed, speed, ref smoothVelocity.x, speedSmoothTime);

                // Calculate directional movement
                UpdateDirectionalMovement(velocity);
            }
        }

        private void UpdateDirectionalMovement(Vector3 velocity)
        {
            if (velocity.sqrMagnitude < 0.01f) return;

            // Get local space direction relative to character facing
            Vector3 localVelocity = transform.InverseTransformDirection(velocity.normalized);

            // Smooth direction changes
            float targetX = localVelocity.x;
            float targetY = localVelocity.z;

            currentDirectionX = Mathf.SmoothDamp(currentDirectionX, targetX, ref smoothVelocity.y, directionSmoothTime);
            currentDirectionY = Mathf.SmoothDamp(currentDirectionY, targetY, ref smoothVelocity.z, directionSmoothTime);

            // Normalize for 8-way movement if enabled
            if (use8WayMovement)
            {
                SnapTo8WayDirection(ref currentDirectionX, ref currentDirectionY);
            }
        }

        private void SnapTo8WayDirection(ref float x, ref float y)
        {
            // Snap to 8 cardinal directions for cleaner animation blending
            float angle = Mathf.Atan2(x, y) * Mathf.Rad2Deg;

            // Snap to nearest 45 degrees
            float snappedAngle = Mathf.Round(angle / 45f) * 45f;
            float rad = snappedAngle * Mathf.Deg2Rad;

            x = Mathf.Sin(rad);
            y = Mathf.Cos(rad);
        }

        #endregion

        #region Combat Animation

        private void UpdateCombatAnimation()
        {
            if (combat == null || health == null) return;

            // Check if we should be in combat
            bool hasTarget = combat.CurrentTarget != null;
            bool inRange = combat.IsInAttackRange;

            if (!hasTarget || !inRange)
            {
                // Return to idle if no target
                if (combatState != ArcherCombatState.Idle)
                {
                    ResetCombatState();
                }
                return;
            }

            // Update combat state machine
            switch (combatState)
            {
                case ArcherCombatState.Idle:
                    if (hasTarget && inRange)
                    {
                        StartDrawSequence();
                    }
                    break;

                case ArcherCombatState.Drawing:
                    currentAttackTime += Time.deltaTime;
                    if (currentAttackTime >= drawDuration)
                    {
                        TransitionToAim();
                    }
                    break;

                case ArcherCombatState.Aiming:
                    currentAttackTime += Time.deltaTime;
                    if (currentAttackTime >= aimDuration)
                    {
                        TransitionToRelease();
                    }
                    break;

                case ArcherCombatState.Releasing:
                    currentAttackTime += Time.deltaTime;
                    if (currentAttackTime >= releaseDuration)
                    {
                        CompleteCombatSequence();
                    }
                    break;
            }
        }

        private void StartDrawSequence()
        {
            combatState = ArcherCombatState.Drawing;
            currentAttackTime = 0f;
            animator.SetTrigger(DrawTriggerHash);
        }

        private void TransitionToAim()
        {
            combatState = ArcherCombatState.Aiming;
            currentAttackTime = 0f;
            animator.SetTrigger(AimTriggerHash);
        }

        private void TransitionToRelease()
        {
            combatState = ArcherCombatState.Releasing;
            currentAttackTime = 0f;
            animator.SetTrigger(ReleaseTriggerHash);

            // This is when the arrow is actually fired
            combat?.TryAttack();
        }

        private void CompleteCombatSequence()
        {
            // After release, return to idle or start new sequence if still in combat
            if (combat.CurrentTarget != null && combat.IsInAttackRange)
            {
                StartDrawSequence();
            }
            else
            {
                ResetCombatState();
            }
        }

        private void ResetCombatState()
        {
            combatState = ArcherCombatState.Idle;
            currentAttackTime = 0f;
        }

        #endregion

        #region Animator Parameter Updates

        private void UpdateAnimatorParameters()
        {
            if (animator == null) return;

            // Movement parameters
            animator.SetFloat(DirectionXHash, currentDirectionX);
            animator.SetFloat(DirectionYHash, currentDirectionY);
            animator.SetFloat(SpeedHash, currentSpeed);
            animator.SetBool(IsMovingHash, movementState == ArcherMovementState.Walking);

            // Combat state (as integer for state machine)
            animator.SetInteger(CombatStateHash, (int)combatState);

            // LOD level
            if (enableLOD)
            {
                animator.SetInteger(LODLevelHash, currentLODLevel);
            }
        }

        #endregion

        #region Performance Optimization

        private void UpdatePerformanceOptimization()
        {
            if (!enableLOD && !cullWhenNotVisible) return;

            // Check visibility
            UpdateVisibility();

            // Update LOD based on distance
            if (enableLOD && mainCamera != null)
            {
                distanceToCamera = Vector3.Distance(transform.position, mainCamera.transform.position);
                UpdateLODLevel();
            }
        }

        private void UpdateVisibility()
        {
            if (renderers == null || renderers.Length == 0) return;

            // Check if any renderer is visible
            bool wasVisible = isVisible;
            isVisible = false;

            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.isVisible)
                {
                    isVisible = true;
                    break;
                }
            }

            // Disable animator when not visible for performance
            if (cullWhenNotVisible && animator != null)
            {
                if (wasVisible != isVisible)
                {
                    animator.enabled = isVisible;
                }
            }
        }

        private void UpdateLODLevel()
        {
            int newLODLevel = 0;

            if (distanceToCamera > lodDistance3)
            {
                newLODLevel = 3; // Disable or minimal animations
            }
            else if (distanceToCamera > lodDistance2)
            {
                newLODLevel = 2; // Reduced animation quality
            }
            else if (distanceToCamera > lodDistance1)
            {
                newLODLevel = 1; // Medium quality
            }
            else
            {
                newLODLevel = 0; // Full quality
            }

            if (newLODLevel != currentLODLevel)
            {
                currentLODLevel = newLODLevel;
                ApplyLODLevel(currentLODLevel);
            }
        }

        private void ApplyLODLevel(int lodLevel)
        {
            if (animator == null) return;

            switch (lodLevel)
            {
                case 0: // Full quality
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.updateMode = AnimatorUpdateMode.Normal;
                    break;

                case 1: // Medium quality
                    animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                    animator.updateMode = AnimatorUpdateMode.Normal;
                    break;

                case 2: // Low quality
                    animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                    animator.updateMode = AnimatorUpdateMode.Normal;
                    break;

                case 3: // Minimal - very far away
                    animator.cullingMode = AnimatorCullingMode.CullCompletely;
                    animator.updateMode = AnimatorUpdateMode.Normal;
                    break;
            }
        }

        #endregion

        #region Event Handlers

        private void OnHealthChanged(UnitHealthChangedEvent evt)
        {
            if (evt.Unit != gameObject) return;

            if (evt.Delta < 0 && !isDead)
            {
                animator.SetTrigger(HitTriggerHash);
            }
        }

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            // Attack animation is handled by combat state machine
        }

        public void OnDeath()
        {
            if (isDead) return;

            isDead = true;
            animator.SetTrigger(DeathTriggerHash);

            // Disable optimization features on death
            enableLOD = false;
            cullWhenNotVisible = false;
        }

        #endregion

        #region Animation Events (Called from Animation Clips)

        /// <summary>
        /// Called when draw animation completes
        /// </summary>
        public void OnDrawComplete()
        {
            Debug.Log($"[ArcherAnimation] Draw complete");
        }

        /// <summary>
        /// Called when aim animation completes
        /// </summary>
        public void OnAimComplete()
        {
            Debug.Log($"[ArcherAnimation] Aim complete");
        }

        /// <summary>
        /// Called at the exact moment arrow should be released
        /// </summary>
        public void OnArrowRelease()
        {
            Debug.Log($"[ArcherAnimation] Arrow released!");
            // Spawn arrow projectile here if using projectile system
        }

        /// <summary>
        /// Called when release animation completes
        /// </summary>
        public void OnReleaseComplete()
        {
            Debug.Log($"[ArcherAnimation] Release complete");
        }

        public void OnFootstep()
        {
            // Play footstep sound
        }

        #endregion

        #region Public API

        public void ForceDrawAttack()
        {
            StartDrawSequence();
        }

        public void CancelAttack()
        {
            ResetCombatState();
        }

        public void SetAnimationSpeed(float speed)
        {
            if (animator != null)
            {
                animator.speed = Mathf.Max(0, speed);
            }
        }

        public bool IsInCombat()
        {
            return combatState != ArcherCombatState.Idle;
        }

        #endregion

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
    }

    /// <summary>
    /// Archer combat state machine states
    /// </summary>
    public enum ArcherCombatState
    {
        Idle = 0,
        Drawing = 1,
        Aiming = 2,
        Releasing = 3
    }

    /// <summary>
    /// Movement states
    /// </summary>
    public enum ArcherMovementState
    {
        Idle,
        Walking,
        Running
    }
}
