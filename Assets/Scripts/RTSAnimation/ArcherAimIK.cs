using UnityEngine;

namespace RTS.Units.Animation
{
    /// <summary>
    /// Advanced IK system for archer aiming.
    /// Smoothly aims upper body and head at targets while maintaining natural poses.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class ArcherAimIK : MonoBehaviour
    {
        [Header("IK Settings")]
        [SerializeField] private bool enableIK = true;
        [SerializeField] private Transform aimTarget;

        [Header("IK Weights")]
        [Range(0f, 1f)]
        [SerializeField] private float bodyWeight = 0.3f;

        [Range(0f, 1f)]
        [SerializeField] private float headWeight = 0.8f;

        [Range(0f, 1f)]
        [SerializeField] private float eyesWeight = 0.9f;

        [Range(0f, 1f)]
        [SerializeField] private float clampWeight = 0.5f;

        [Header("Smoothing")]
        [SerializeField] private float smoothTime = 0.2f;
        [SerializeField] private bool onlyAimWhenInCombat = true;

        [Header("Constraints")]
        [SerializeField] private float maxAimAngle = 80f;
        [SerializeField] private float minAimAngle = -40f;
        [SerializeField] private float maxAimDistance = 50f;

        private Animator animator;
        private UnitCombat combat;
        private ArcherAnimationController archerController;

        private float currentIKWeight = 0f;
        private float targetIKWeight = 0f;
        private float ikVelocity = 0f;

        private Vector3 currentLookPosition;
        private Vector3 lookVelocity;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            combat = GetComponent<UnitCombat>();
            archerController = GetComponent<ArcherAnimationController>();
        }

        private void Update()
        {
            UpdateAimTarget();
            UpdateIKWeight();
        }

        private void UpdateAimTarget()
        {
            // Determine if we should aim
            bool shouldAim = enableIK;

            if (onlyAimWhenInCombat)
            {
                shouldAim &= combat != null && combat.CurrentTarget != null;
            }

            if (shouldAim && combat != null && combat.CurrentTarget != null)
            {
                targetIKWeight = 1f;

                // Smooth look position
                Vector3 targetPosition = combat.CurrentTarget.position;
                currentLookPosition = Vector3.SmoothDamp(
                    currentLookPosition,
                    targetPosition,
                    ref lookVelocity,
                    smoothTime
                );
            }
            else
            {
                targetIKWeight = 0f;
            }
        }

        private void UpdateIKWeight()
        {
            currentIKWeight = Mathf.SmoothDamp(
                currentIKWeight,
                targetIKWeight,
                ref ikVelocity,
                smoothTime
            );
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!enableIK || animator == null || currentIKWeight <= 0.01f)
            {
                // Disable IK
                animator.SetLookAtWeight(0);
                return;
            }

            // Check if target is within valid aim cone
            if (!IsValidAimTarget(currentLookPosition))
            {
                animator.SetLookAtWeight(0);
                return;
            }

            // Apply look-at IK
            animator.SetLookAtWeight(
                currentIKWeight,
                bodyWeight * currentIKWeight,
                headWeight * currentIKWeight,
                eyesWeight * currentIKWeight,
                clampWeight * currentIKWeight
            );

            animator.SetLookAtPosition(currentLookPosition);
        }

        private bool IsValidAimTarget(Vector3 targetPosition)
        {
            Vector3 directionToTarget = targetPosition - transform.position;

            // Check distance
            if (directionToTarget.magnitude > maxAimDistance)
                return false;

            // Check angle
            float angle = Vector3.Angle(transform.forward, directionToTarget);
            if (angle > maxAimAngle || angle < minAimAngle)
                return false;

            return true;
        }

        #region Public API

        public void SetIKEnabled(bool enabled)
        {
            enableIK = enabled;
        }

        public void SetAimTarget(Transform target)
        {
            aimTarget = target;
        }

        public void SetIKWeight(float weight)
        {
            targetIKWeight = Mathf.Clamp01(weight);
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (!enableIK) return;

            // Draw aim cone
            Gizmos.color = Color.yellow;

            Vector3 forward = transform.forward * maxAimDistance;
            Gizmos.DrawRay(transform.position, forward);

            // Draw current look target
            if (currentIKWeight > 0.1f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, currentLookPosition);
                Gizmos.DrawWireSphere(currentLookPosition, 0.5f);
            }
        }
    }
}
