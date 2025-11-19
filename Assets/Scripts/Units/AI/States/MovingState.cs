using UnityEngine;

namespace RTS.Units.AI
{
    public class MovingState : UnitState
    {
        private float pathUpdateTimer;
        private const float PATH_UPDATE_INTERVAL = 0.5f;

        // Chase tracking
        private float outOfRangeTimer = 0f;
        private bool targetWasInRangeOnce = false;

        public MovingState(UnitAIController aiController) : base(aiController) { }

        public override UnitStateType GetStateType() => UnitStateType.Moving;

        public override void OnEnter()
        {
            pathUpdateTimer = 0f;
            outOfRangeTimer = 0f;
            targetWasInRangeOnce = false;
        }

        public override void OnUpdate()
        {
            Transform target = controller.CurrentTarget;

            if (target == null)
            {
                // Lost target, return to origin if configured
                if (controller.Config != null &&
                    controller.Config.returnToOriginAfterAggro &&
                    controller.AggroOriginPosition.HasValue)
                {
                    controller.ClearTarget();
                    controller.ChangeState(new ReturningToOriginState(controller));
                }
                else
                {
                    controller.ChangeState(new IdleState(controller));
                }
                return;
            }

            if (controller.ShouldRetreat())
            {
                controller.ChangeState(new RetreatState(controller));
                return;
            }

            // Check if exceeded max chase distance from origin
            if (controller.HasExceededChaseDistance())
            {
                // Too far from origin, abandon chase
                if (controller.Config != null && controller.Config.returnToOriginAfterAggro)
                {
                    controller.ClearTarget();
                    controller.ChangeState(new ReturningToOriginState(controller));
                }
                else
                {
                    controller.ClearTarget();
                    controller.ChangeState(new IdleState(controller));
                }
                return;
            }

            // Check if target is in detection range
            bool targetInDetectionRange = false;
            if (controller.Config != null)
            {
                float distanceToTarget = Vector3.Distance(controller.transform.position, target.position);
                targetInDetectionRange = distanceToTarget <= controller.Config.detectionRange;

                // Track if target was ever in range
                if (targetInDetectionRange)
                {
                    targetWasInRangeOnce = true;
                    outOfRangeTimer = 0f;
                }
                else if (targetWasInRangeOnce)
                {
                    // Target went out of range after being in range
                    outOfRangeTimer += Time.deltaTime;

                    // Check if chase timeout exceeded
                    if (outOfRangeTimer >= controller.Config.chaseTimeout)
                    {
                        // Stop chasing, return to origin
                        if (controller.Config.returnToOriginAfterAggro && controller.AggroOriginPosition.HasValue)
                        {
                            controller.ClearTarget();
                            controller.ChangeState(new ReturningToOriginState(controller));
                        }
                        else
                        {
                            controller.ClearTarget();
                            controller.ChangeState(new IdleState(controller));
                        }
                        return;
                    }
                }
            }

            // Check if unit reached forced move destination
            if (controller.IsOnForcedMove && controller.HasReachedForcedMoveDestination())
            {
                // Reached destination, clear forced move and allow aggro
                controller.SetForcedMove(false);
            }

            pathUpdateTimer += Time.deltaTime;
            if (pathUpdateTimer >= PATH_UPDATE_INTERVAL)
            {
                pathUpdateTimer = 0f;
                controller.Movement?.SetDestination(target.position);
            }

            if (controller.Combat != null && controller.Combat.IsTargetInRange(target))
            {
                controller.ChangeState(new AttackingState(controller));
            }
        }

        public override void OnExit()
        {
            pathUpdateTimer = 0f;
            outOfRangeTimer = 0f;
            targetWasInRangeOnce = false;
        }
    }
}