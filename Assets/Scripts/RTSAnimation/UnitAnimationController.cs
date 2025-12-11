using UnityEngine;
using RTS.Core.Events;

namespace RTS.Units.Animation
{
    /// <summary>
    /// Modular animation controller for units.
    /// Automatically syncs with unit state, movement, and combat.
    /// Uses Unity's Animator component for animation playback.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class UnitAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Animation Settings")]
        #pragma warning disable CS0414 // Field is assigned but never used - reserved for future movement threshold feature
        [SerializeField] private float movementThreshold = 0.1f; // Min speed to trigger walk
        #pragma warning restore CS0414
        [SerializeField] private bool useRootMotion = false;
        [SerializeField] private float animationTransitionSpeed = 0.1f;

        [Header("Animation Events")]
        [SerializeField] private bool enableAttackEvents = true;
        [SerializeField] private bool enableFootstepEvents = true;

        // Component references
        private UnitMovement movement;
        private UnitCombat combat;
        private UnitHealth health;
        private AI.UnitAIController aiController;

        // Optional modular system components
        private UnitPersonalityController personalityController;
        private UnitAnimatorProfileLoader profileLoader;

        // Animation state tracking
        private AnimationState currentAnimState = AnimationState.Idle;
        private bool isDead = false;

        // Animation parameter hashes (for performance)
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int DeathTriggerHash = Animator.StringToHash("Death");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
        private static readonly int HitTriggerHash = Animator.StringToHash("Hit");
        private static readonly int IdleTriggerHash = Animator.StringToHash("Idle");

        // Personality parameters (new modular system)
        private static readonly int DoIdleActionHash = Animator.StringToHash("DoIdleAction");
        private static readonly int IdleVariantHash = Animator.StringToHash("IdleVariant");
        private static readonly int VictoryHash = Animator.StringToHash("Victory");
        private static readonly int RetreatHash = Animator.StringToHash("Retreat");
        private static readonly int LookWeightHash = Animator.StringToHash("LookWeight");

        public AnimationState CurrentState => currentAnimState;
        public Animator Animator => animator;

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

        private void Update()
        {
            if (isDead) return;

            UpdateAnimationState();
        }

        #region Initialization

        private void InitializeComponents()
        {
            // Get Animator
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                enabled = false;
                return;
            }

            // Configure animator
            animator.applyRootMotion = useRootMotion;

            // Get unit components
            movement = GetComponent<UnitMovement>();
            combat = GetComponent<UnitCombat>();
            health = GetComponent<UnitHealth>();
            aiController = GetComponent<AI.UnitAIController>();

            // Get optional modular system components
            personalityController = GetComponent<UnitPersonalityController>();
            profileLoader = GetComponent<UnitAnimatorProfileLoader>();

            // Set initial state
            SetAnimationState(AnimationState.Idle);
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<UnitHealthChangedEvent>(OnHealthChanged);
           // EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<UnitStateChangedEvent>(OnUnitStateChanged);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<UnitHealthChangedEvent>(OnHealthChanged);
          //  EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<UnitStateChangedEvent>(OnUnitStateChanged);
        }

        #endregion

        #region Animation State Management

        private void UpdateAnimationState()
        {
            // Determine current animation state based on unit components
            AnimationState newState = DetermineAnimationState();

            if (newState != currentAnimState)
            {
                SetAnimationState(newState);
            }

            // Update continuous parameters
            UpdateAnimatorParameters();
        }

        private AnimationState DetermineAnimationState()
        {
            // Priority order: Death > Attack > Move > Idle

            if (health != null && health.IsDead)
                return AnimationState.Death;

            if (combat != null && combat.CurrentTarget != null && combat.IsInAttackRange)
                return AnimationState.Attack;

            // Check movement - animation follows actual velocity naturally
            // No explicit stuck checks - if unit stops moving, animation will transition to idle
            if (movement != null && movement.IsMoving)
                return AnimationState.Walk;

            return AnimationState.Idle;
        }

        private void SetAnimationState(AnimationState newState)
        {
            currentAnimState = newState;

            // Set animator parameters based on state
            switch (newState)
            {
                case AnimationState.Idle:
                    SetIdle();
                    break;

                case AnimationState.Walk:
                    SetWalking();
                    break;

                case AnimationState.Attack:
                    TriggerAttack();
                    break;

                case AnimationState.Death:
                    TriggerDeath();
                    break;
            }
        }

        private void UpdateAnimatorParameters()
        {
            if (animator == null) return;

            // Update speed parameter
            float speed = 0f;
            bool isMoving = false;

            if (movement != null)
            {
                isMoving = movement.IsMoving; // Use IsMoving property which handles intent

                // If we have movement intent, use the configured speed even if velocity hasn't updated yet
                if (isMoving)
                {
                    speed = Mathf.Max(movement.Velocity.magnitude, movement.Speed);
                }
                else
                {
                    speed = movement.Velocity.magnitude;
                }
                speed = movement.Velocity.magnitude;
                isMoving = movement.IsMoving; // Use IsMoving property which handles intent
            }

            animator.SetFloat(SpeedHash, speed, animationTransitionSpeed, Time.deltaTime);
            animator.SetBool(IsMovingHash, isMoving);
        }

        #endregion

        #region Animation Triggers

        private void SetIdle()
        {
            if (animator == null) return;

            animator.SetBool(IsMovingHash, false);
            animator.SetFloat(SpeedHash, 0f);
            
            // Optionally trigger idle if you have multiple idle variations
            // animator.SetTrigger(IdleTriggerHash);
        }

        private void SetWalking()
        {
            if (animator == null) return;

            animator.SetBool(IsMovingHash, true);
        }

        private void TriggerAttack()
        {
            if (animator == null) return;

            animator.SetTrigger(AttackTriggerHash);
        }

        private void TriggerDeath()
        {
            if (animator == null) return;

            isDead = true;
            animator.SetTrigger(DeathTriggerHash);
            animator.SetBool(IsDeadHash, true);

            // Disable animator after death animation completes
            StartCoroutine(DisableAnimatorAfterDeath());
        }

        private void TriggerHit()
        {
            if (animator == null) return;

            animator.SetTrigger(HitTriggerHash);
        }

        #endregion

        #region Event Handlers

        private void OnHealthChanged(UnitHealthChangedEvent evt)
        {
            // Only respond to this unit's health changes
            if (evt.Unit != gameObject) return;

            // Trigger hit animation on damage
            if (evt.Delta < 0 && !isDead)
            {
                TriggerHit();
            }
        }

       /* private void OnUnitDied(UnitDiedEvent evt)
        {
            if (evt.Unit != gameObject) return;

            TriggerDeath();
        }*/

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            // If this unit attacked, ensure attack animation plays
            // Use safe null checking for Unity GameObjects
            if (evt.Attacker != null && !ReferenceEquals(evt.Attacker, null) &&
                evt.Attacker == gameObject && !isDead)
            {
                TriggerAttack();
            }
        }

        private void OnUnitStateChanged(UnitStateChangedEvent evt)
        {
            if (evt.Unit != gameObject) return;

            // React to AI state changes if needed
            AI.UnitStateType newStateType = (AI.UnitStateType)evt.NewState;

            // Could add special animations for specific states here
            // For example, special "retreat" animation or "celebrating" animation
        }

        #endregion

        #region Animation Events (Called from Animation Clips)

        /// <summary>
        /// Called from animation event at the moment attack should deal damage.
        /// </summary>
        public void OnAttackHit()
        {
            if (!enableAttackEvents) return;

            // This is where the actual attack damage is applied
            // The animation determines the timing
            combat?.TryAttack();

        }

        /// <summary>
        /// Called when attack animation completes.
        /// </summary>
        public void OnAttackComplete()
        {
            if (!enableAttackEvents) return;

        }

        /// <summary>
        /// Called from animation event for footstep sounds.
        /// </summary>
        public void OnFootstep()
        {
            if (!enableFootstepEvents) return;

            // Play footstep sound here
            // AudioManager.Instance?.PlayFootstep(transform.position);
            
        }

        /// <summary>
        /// Called when death animation completes.
        /// </summary>
        public void OnDeathComplete()
        {
            
            // Could trigger corpse spawn, item drops, etc.
            // For now, the unit is destroyed by DeadState
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually trigger an attack animation.
        /// </summary>
        public void PlayAttack()
        {
            TriggerAttack();
        }

        /// <summary>
        /// Manually play a custom animation state.
        /// </summary>
        public void PlayCustomAnimation(string stateName)
        {
            if (animator == null) return;

            animator.Play(stateName);
        }

        /// <summary>
        /// Set animation speed multiplier.
        /// </summary>
        public void SetAnimationSpeed(float speed)
        {
            if (animator == null) return;

            animator.speed = Mathf.Max(0, speed);
        }

        /// <summary>
        /// Check if a specific animation state is currently playing.
        /// </summary>
        public bool IsPlayingState(string stateName)
        {
            if (animator == null) return false;

            return animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
        }

        /// <summary>
        /// Get normalized time of current animation (0-1).
        /// </summary>
        public float GetCurrentAnimationTime()
        {
            if (animator == null) return 0f;

            return animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        }

        /// <summary>
        /// Check if the modular personality system is enabled on this unit.
        /// </summary>
        public bool HasPersonalityController()
        {
            return personalityController != null;
        }

        /// <summary>
        /// Check if the modular profile loader is enabled on this unit.
        /// </summary>
        public bool HasProfileLoader()
        {
            return profileLoader != null;
        }

        /// <summary>
        /// Get the personality controller (if available).
        /// </summary>
        public UnitPersonalityController GetPersonalityController()
        {
            return personalityController;
        }

        /// <summary>
        /// Get the profile loader (if available).
        /// </summary>
        public UnitAnimatorProfileLoader GetProfileLoader()
        {
            return profileLoader;
        }

        #endregion

        #region Coroutines

        private System.Collections.IEnumerator DisableAnimatorAfterDeath()
        {
            // Wait for death animation to complete
            yield return new WaitForSeconds(2f);

            if (animator != null)
            {
                animator.enabled = false;
            }
        }

        #endregion

        #region Callbacks

        private void OnDestroy()
        {
            // Critical: Stop all coroutines to prevent memory leaks
            StopAllCoroutines();

            // Critical: Ensure events are unsubscribed even if OnDisable wasn't called
            UnsubscribeFromEvents();
        }

      /*  private void OnUnitDied()
        {
            TriggerDeath();
        }*/

        #endregion
    }

    /// <summary>
    /// Animation state enumeration.
    /// </summary>
    public enum AnimationState
    {
        Idle,
        Walk,
        Attack,
        Death,
        Hit,
        Custom
    }
}
