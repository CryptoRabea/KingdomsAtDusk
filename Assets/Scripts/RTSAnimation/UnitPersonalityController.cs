using UnityEngine;
using UnityEngine.Animations.Rigging;
using RTS.Core.Events;
using System.Collections;

namespace RTS.Units.Animation
{
    /// <summary>
    /// Controls unit personality behaviors including idle variants, victory, retreat, and look-at.
    /// Works alongside UnitAnimationController for personality layer animations.
    /// Event-driven and performance-optimized.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class UnitPersonalityController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private UnitAnimatorProfileLoader profileLoader;

        [Header("Look-At / Aim Settings")]
        [SerializeField] private Rig lookAtRig;
        [SerializeField] private Transform lookAtTarget;
        [SerializeField] private bool enableLookAt = false;

        [Header("Idle Action Settings")]
        [SerializeField] private bool enableIdleActions = true;
        [SerializeField] private bool randomizeFirstIdleTime = true;

        [Header("Personality Override Settings")]
        [SerializeField] private bool enableVictoryAnimation = true;
        [SerializeField] private bool enableRetreatAnimation = true;

        // Component references
        private UnitAnimationController animationController;
        private UnitHealth health;
        private UnitMovement movement;
        private AI.UnitAIController aiController;

        // State tracking
        private float idleTimer = 0f;
        private float nextIdleActionTime = 10f;
        private bool isIdle = false;
        private bool isDead = false;
        private bool isRetreating = false;
        private bool isVictorious = false;

        // Animation parameter hashes
        private static readonly int DoIdleActionHash = Animator.StringToHash("DoIdleAction");
        private static readonly int IdleVariantHash = Animator.StringToHash("IdleVariant");
        private static readonly int VictoryHash = Animator.StringToHash("Victory");
        private static readonly int RetreatHash = Animator.StringToHash("Retreat");
        private static readonly int LookWeightHash = Animator.StringToHash("LookWeight");

        // Look-at state
        private float currentLookWeight = 0f;
        private float targetLookWeight = 0f;

        private void Awake()
        {
            InitializeComponents();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
            ResetIdleTimer();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (isDead) return;

            UpdateIdleActions();
            UpdateLookAtRig();
        }

        #region Initialization

        private void InitializeComponents()
        {
            // Get animator
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            // Get profile loader
            if (profileLoader == null)
            {
                profileLoader = GetComponent<UnitAnimatorProfileLoader>();
            }

            // Get other components
            animationController = GetComponent<UnitAnimationController>();
            health = GetComponent<UnitHealth>();
            movement = GetComponent<UnitMovement>();
            aiController = GetComponent<AI.UnitAIController>();

            // Initialize look-at from profile if available
            if (profileLoader != null && profileLoader.Profile != null)
            {
                var profile = profileLoader.Profile;
                enableLookAt = profile.enableLookAt;
                targetLookWeight = profile.lookWeight;
                currentLookWeight = 0f; // Start at 0 and blend in
            }

            // Randomize first idle time to avoid synchronized actions in groups
            if (randomizeFirstIdleTime)
            {
                ResetIdleTimer();
            }
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<UnitStateChangedEvent>(OnUnitStateChanged);
            EventBus.Subscribe<UnitHealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<UnitStateChangedEvent>(OnUnitStateChanged);
            EventBus.Unsubscribe<UnitHealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
        }

        #endregion

        #region Idle Actions

        private void UpdateIdleActions()
        {
            if (!enableIdleActions || profileLoader == null || profileLoader.Profile == null)
                return;

            // Check if unit is currently idle
            bool currentlyIdle = IsUnitIdle();

            if (currentlyIdle && !isIdle)
            {
                // Just became idle
                isIdle = true;
                ResetIdleTimer();
            }
            else if (!currentlyIdle && isIdle)
            {
                // No longer idle
                isIdle = false;
                idleTimer = 0f;
            }

            // Update idle timer if idle
            if (isIdle)
            {
                idleTimer += Time.deltaTime;

                if (idleTimer >= nextIdleActionTime)
                {
                    TriggerIdleAction();
                    ResetIdleTimer();
                }
            }
        }

        private bool IsUnitIdle()
        {
            // Unit is idle if not moving, not attacking, and not dead
            if (isDead) return false;

            bool notMoving = movement == null || !movement.IsMoving;
            bool notAttacking = animationController == null ||
                               (animationController.CurrentState != AnimationState.Attack);

            return notMoving && notAttacking;
        }

        private void TriggerIdleAction()
        {
            if (profileLoader == null || profileLoader.Profile == null)
                return;

            var profile = profileLoader.Profile;

            // Check probability
            if (Random.value > profile.idleActionProbability)
                return;

            // Select random idle variant
            int variantIndex = Random.Range(0, 4);

            if (animator != null)
            {
                animator.SetInteger(IdleVariantHash, variantIndex);
                animator.SetTrigger(DoIdleActionHash);

                Debug.Log($"[{gameObject.name}] Triggered idle action: variant {variantIndex}");
            }
        }

        private void ResetIdleTimer()
        {
            if (profileLoader != null && profileLoader.Profile != null)
            {
                var profile = profileLoader.Profile;
                nextIdleActionTime = Random.Range(profile.minIdleTime, profile.maxIdleTime);
            }
            else
            {
                nextIdleActionTime = Random.Range(5f, 15f);
            }

            idleTimer = 0f;
        }

        #endregion

        #region Victory & Retreat

        /// <summary>
        /// Trigger victory/celebration animation.
        /// </summary>
        public void TriggerVictory()
        {
            if (!enableVictoryAnimation || isDead || isVictorious)
                return;

            if (profileLoader != null && profileLoader.Profile != null &&
                profileLoader.Profile.victoryAnimation != null)
            {
                isVictorious = true;
                animator?.SetTrigger(VictoryHash);

                Debug.Log($"[{gameObject.name}] Victory animation triggered!");

                // Reset after animation duration
                StartCoroutine(ResetVictoryState());
            }
        }

        /// <summary>
        /// Trigger retreat/fear animation.
        /// </summary>
        public void TriggerRetreat(bool retreating)
        {
            if (!enableRetreatAnimation || isDead)
                return;

            if (profileLoader != null && profileLoader.Profile != null &&
                profileLoader.Profile.retreatAnimation != null)
            {
                isRetreating = retreating;
                animator?.SetBool(RetreatHash, retreating);

                Debug.Log($"[{gameObject.name}] Retreat state: {retreating}");
            }
        }

        private IEnumerator ResetVictoryState()
        {
            // Wait for victory animation to complete
            yield return new WaitForSeconds(3f);
            isVictorious = false;
        }

        #endregion

        #region Look-At Control

        private void UpdateLookAtRig()
        {
            if (!enableLookAt || lookAtRig == null)
                return;

            // Smoothly blend look weight
            currentLookWeight = Mathf.Lerp(
                currentLookWeight,
                targetLookWeight,
                Time.deltaTime * GetLookSpeed()
            );

            lookAtRig.weight = currentLookWeight;

            // Update animator parameter if it exists
            if (animator != null)
            {
                animator.SetFloat(LookWeightHash, currentLookWeight);
            }
        }

        /// <summary>
        /// Set look-at target and weight.
        /// </summary>
        public void SetLookAtTarget(Transform target, float weight = 1f)
        {
            if (!enableLookAt) return;

            lookAtTarget = target;
            targetLookWeight = Mathf.Clamp01(weight);
        }

        /// <summary>
        /// Clear look-at target.
        /// </summary>
        public void ClearLookAtTarget()
        {
            lookAtTarget = null;
            targetLookWeight = 0f;
        }

        /// <summary>
        /// Set look weight directly.
        /// </summary>
        public void SetLookWeight(float weight)
        {
            if (!enableLookAt) return;
            targetLookWeight = Mathf.Clamp01(weight);
        }

        private float GetLookSpeed()
        {
            if (profileLoader != null && profileLoader.Profile != null)
            {
                return profileLoader.Profile.lookSpeed;
            }
            return 2f;
        }

        #endregion

        #region Event Handlers

        private void OnUnitStateChanged(UnitStateChangedEvent evt)
        {
            if (evt.Unit != gameObject) return;

            AI.UnitStateType newState = (AI.UnitStateType)evt.NewState;

            // Trigger retreat animation when fleeing
            if (newState == AI.UnitStateType.Flee)
            {
                TriggerRetreat(true);
            }
            else if (isRetreating)
            {
                TriggerRetreat(false);
            }
        }

        private void OnHealthChanged(UnitHealthChangedEvent evt)
        {
            if (evt.Unit != gameObject) return;

            // Trigger retreat if health is critically low
            if (health != null && !isDead)
            {
                float healthPercent = health.CurrentHealth / health.MaxHealth;
                if (healthPercent < 0.25f && !isRetreating)
                {
                    // Could trigger automatic retreat at low health
                    // TriggerRetreat(true);
                }
            }
        }

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            // Don't trigger victory if this unit is dead
            if (isDead) return;

            // Random chance to celebrate (not all units at once)
            if (Random.value < 0.3f)
            {
                // Stagger celebration times
                StartCoroutine(DelayedVictory(Random.Range(0f, 2f)));
            }
        }

        private IEnumerator DelayedVictory(float delay)
        {
            yield return new WaitForSeconds(delay);
            TriggerVictory();
        }

        #endregion

        #region Group Behavior Integration

        /// <summary>
        /// Trigger a group victory celebration (called by GroupAnimationManager).
        /// </summary>
        public void OnGroupVictory()
        {
            if (!isDead && Random.value < 0.8f) // 80% chance to join group celebration
            {
                StartCoroutine(DelayedVictory(Random.Range(0f, 1.5f)));
            }
        }

        /// <summary>
        /// Trigger a group scan/look-around (called by GroupAnimationManager).
        /// </summary>
        public void OnGroupScan()
        {
            if (!isDead && enableLookAt && Random.value < 0.5f)
            {
                // Random look direction
                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = 0;

                if (lookAtTarget == null)
                {
                    lookAtTarget = new GameObject($"{gameObject.name}_LookTarget").transform;
                }

                lookAtTarget.position = transform.position + randomDirection.normalized * 5f;
                SetLookWeight(0.8f);

                // Clear after a few seconds
                StartCoroutine(ClearLookAfterDelay(Random.Range(2f, 4f)));
            }
        }

        private IEnumerator ClearLookAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ClearLookAtTarget();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Manually trigger an idle action.
        /// </summary>
        public void ForceIdleAction(int variantIndex = -1)
        {
            if (animator == null) return;

            if (variantIndex < 0)
            {
                variantIndex = Random.Range(0, 4);
            }

            animator.SetInteger(IdleVariantHash, variantIndex);
            animator.SetTrigger(DoIdleActionHash);
        }

        /// <summary>
        /// Enable or disable personality features.
        /// </summary>
        public void SetPersonalityEnabled(bool enabled)
        {
            enableIdleActions = enabled;
            enableVictoryAnimation = enabled;
            enableRetreatAnimation = enabled;
        }

        #endregion

        #region Callbacks

        private void OnDestroy()
        {
            StopAllCoroutines();
            UnsubscribeFromEvents();

            // Clean up temporary look target if created
            if (lookAtTarget != null && lookAtTarget.name.Contains("_LookTarget"))
            {
                Destroy(lookAtTarget.gameObject);
            }
        }

        #endregion
    }
}
