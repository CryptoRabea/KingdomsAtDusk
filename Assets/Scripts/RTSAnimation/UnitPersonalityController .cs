using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace RTS.Units.Animation
{
    public class UnitPersonalityController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnitAnimationController animController;
        [SerializeField] private Animator animator;
        [SerializeField] private Rig lookAtRig;

        [Header("Idle Personality Settings")]
        public float minIdleTime = 3f;
        public float maxIdleTime = 10f;
        public int idleVariants = 3;

        [Header("Look Settings")]
        public Transform headTarget;
        public float lookBlendSpeed = 4f;

        private float idleTimer;
        private Transform currentTarget;

        private bool inCombat;

        private void Awake()
        {
            if (animController == null)
                animController = GetComponent<UnitAnimationController>();

            if (animator == null)
                animator = animController.Animator;

            ResetIdleTimer();

            // Listen to combat + AI state events
            RTS.Core.Events.EventBus.Subscribe<UnitStateChangedEvent>(OnStateChanged);
        }

        private void OnDestroy()
        {
            RTS.Core.Events.EventBus.Unsubscribe<UnitStateChangedEvent>(OnStateChanged);
        }

        private void Update()
        {
            if (inCombat) return;       // No random idles during combat
            if (currentTarget != null) return;   // Look-at overrides random idles

            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0)
            {
                PlayRandomIdle();
                ResetIdleTimer();
            }
        }

        #region Idle Actions

        private void ResetIdleTimer()
        {
            idleTimer = Random.Range(minIdleTime, maxIdleTime);
        }

        private void PlayRandomIdle()
        {
            int randomIdle = Random.Range(0, idleVariants);
            animator.SetInteger("IdleVariant", randomIdle);
            animator.SetTrigger("DoIdleAction");
        }

        #endregion

        #region Combat + Victory/Retreat Reactions

        private void OnStateChanged(UnitStateChangedEvent evt)
        {
            if (evt.Unit != gameObject) return;

            var newState = (AI.UnitStateType)evt.NewState;

            switch (newState)
            {
                case AI.UnitStateType.Combat:
                    EnterCombat();
                    break;

                case AI.UnitStateType.Retreat:
                    animator.SetBool("Retreat", true);
                    break;

                case AI.UnitStateType.Idle:
                    ExitCombat();
                    break;

                case AI.UnitStateType.Victory:
                    animator.SetTrigger("Victory");
                    break;
            }
        }

        private void EnterCombat()
        {
            inCombat = true;
            currentTarget = null;
            lookAtRig.weight = 0;
        }

        private void ExitCombat()
        {
            inCombat = false;
            animator.SetBool("Retreat", false);
            ResetIdleTimer();
        }

        #endregion

        #region Target Look-At

        public void SetLookTarget(Transform target)
        {
            currentTarget = target;

            if (target == null)
            {
                StartCoroutine(FadeLookOff());
            }
            else
            {
                StopAllCoroutines();
                StartCoroutine(FadeLookOn());
            }
        }

        private IEnumerator FadeLookOn()
        {
            while (lookAtRig.weight < 1f)
            {
                lookAtRig.weight += Time.deltaTime * lookBlendSpeed;
                yield return null;
            }
        }

        private IEnumerator FadeLookOff()
        {
            while (lookAtRig.weight > 0f)
            {
                lookAtRig.weight -= Time.deltaTime * lookBlendSpeed;
                yield return null;
            }
        }

        #endregion
    }
}
