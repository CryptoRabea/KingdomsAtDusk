using UnityEngine;

namespace RTS.Units.Animation
{
    /// <summary>
    /// Advanced animation features: IK, look-at, animation layers.
    /// Optional component for units that need more sophisticated animation.
    /// </summary>
    [RequireComponent(typeof(UnitAnimationController))]
    [RequireComponent(typeof(Animator))]
    public class UnitAnimationAdvanced : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnitAnimationController animController;
        [SerializeField] private Animator animator;

        [Header("Look At (IK)")]
        [SerializeField] private bool enableLookAt = false;
        [SerializeField] private Transform lookAtTarget;
        [SerializeField] private float lookAtWeight = 1f;
        [SerializeField] private float lookAtBodyWeight = 0.3f;
        [SerializeField] private float lookAtHeadWeight = 0.7f;
        [SerializeField] private float lookAtTransitionSpeed = 2f;

        [Header("Hand IK")]
        [SerializeField] private bool enableHandIK = false;
        [SerializeField] private Transform rightHandTarget;
        [SerializeField] private Transform leftHandTarget;
        [SerializeField] private float handIKWeight = 1f;

        [Header("Animation Layers")]
        [SerializeField] private bool useUpperBodyLayer = false;
        [SerializeField] private int upperBodyLayerIndex = 1;
        [SerializeField] private float upperBodyLayerWeight = 1f;

        // Component references
        private UnitCombat combat;
        private UnitMovement movement;

        // Current IK weights (for smooth transitions)
        private float currentLookAtWeight = 0f;
        private float currentHandIKWeight = 0f;

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (animController == null)
            {
                animController = GetComponent<UnitAnimationController>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            combat = GetComponent<UnitCombat>();
            movement = GetComponent<UnitMovement>();

            // Set up animation layers
            if (useUpperBodyLayer && animator != null)
            {
                animator.SetLayerWeight(upperBodyLayerIndex, upperBodyLayerWeight);
            }
        }

        private void Update()
        {
            UpdateLookAtTarget();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null) return;

            // Apply Look At IK
            if (enableLookAt)
            {
                ApplyLookAtIK();
            }

            // Apply Hand IK
            if (enableHandIK)
            {
                ApplyHandIK();
            }
        }

        #region Look At IK

        private void UpdateLookAtTarget()
        {
            if (!enableLookAt) return;

            // Automatically track combat target
            if (combat != null && combat.CurrentTarget != null)
            {
                lookAtTarget = combat.CurrentTarget;
            }
            else
            {
                lookAtTarget = null;
            }
        }

        private void ApplyLookAtIK()
        {
            if (animator == null) return;

            // Smoothly transition look at weight
            float targetWeight = (lookAtTarget != null) ? lookAtWeight : 0f;
            currentLookAtWeight = Mathf.Lerp(
                currentLookAtWeight,
                targetWeight,
                Time.deltaTime * lookAtTransitionSpeed
            );

            if (lookAtTarget != null && currentLookAtWeight > 0.01f)
            {
                animator.SetLookAtWeight(
                    currentLookAtWeight,
                    lookAtBodyWeight,
                    lookAtHeadWeight,
                    1f, // eyes weight
                    0.5f // clamp weight
                );

                animator.SetLookAtPosition(lookAtTarget.position);
            }
            else
            {
                animator.SetLookAtWeight(0);
            }
        }

        #endregion

        #region Hand IK

        private void ApplyHandIK()
        {
            if (animator == null) return;

            // Smooth transition
            float targetWeight = (rightHandTarget != null || leftHandTarget != null) ? handIKWeight : 0f;
            currentHandIKWeight = Mathf.Lerp(
                currentHandIKWeight,
                targetWeight,
                Time.deltaTime * lookAtTransitionSpeed
            );

            // Right hand
            if (rightHandTarget != null && currentHandIKWeight > 0.01f)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, currentHandIKWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, currentHandIKWeight);
                animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
            }

            // Left hand
            if (leftHandTarget != null && currentHandIKWeight > 0.01f)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, currentHandIKWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, currentHandIKWeight);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
            }
        }

        #endregion

        #region Animation Layers

        /// <summary>
        /// Set weight of a specific animation layer.
        /// </summary>
        public void SetLayerWeight(int layerIndex, float weight)
        {
            if (animator == null) return;

            animator.SetLayerWeight(layerIndex, Mathf.Clamp01(weight));
        }

        /// <summary>
        /// Play animation on a specific layer.
        /// </summary>
        public void PlayOnLayer(string stateName, int layerIndex)
        {
            if (animator == null) return;

            animator.Play(stateName, layerIndex);
        }

        #endregion

        #region Public API

        public void SetLookAtTarget(Transform target)
        {
            lookAtTarget = target;
        }

        public void SetHandIKTarget(Transform target, bool isRightHand)
        {
            if (isRightHand)
            {
                rightHandTarget = target;
            }
            else
            {
                leftHandTarget = target;
            }
        }

        public void EnableLookAt(bool enable)
        {
            enableLookAt = enable;
        }

        public void EnableHandIK(bool enable)
        {
            enableHandIK = enable;
        }

        #endregion
    }
}
