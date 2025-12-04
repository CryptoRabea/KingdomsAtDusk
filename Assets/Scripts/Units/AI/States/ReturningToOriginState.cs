using UnityEngine;

namespace RTS.Units.AI
{
    /// <summary>
    /// State where unit returns to its aggro origin position after losing aggro.
    /// Transitions to Idle when reaching the origin position.
    /// </summary>
    public class ReturningToOriginState : UnitState
    {
        private const float ARRIVAL_THRESHOLD = 1f; // How close to origin before considering "arrived"

        public ReturningToOriginState(UnitAIController aiController) : base(aiController) { }

        public override UnitStateType GetStateType() => UnitStateType.ReturningToOrigin;

        public override void OnEnter()
        {
            if (!controller.AggroOriginPosition.HasValue)
            {
                // No origin position, just go idle
                controller.ChangeState(new IdleState(controller));
                return;
            }

            // Start moving to origin
            controller.Movement?.SetDestination(controller.AggroOriginPosition.Value);
        }

        public override void OnUpdate()
        {
            // Check if player issued a forced move (e.g., formation change)
            if (controller.IsOnForcedMove)
            {
                controller.ClearTarget();
                controller.ClearAggroOrigin();
                controller.ChangeState(new MovingState(controller));
                return;
            }

            if (!controller.AggroOriginPosition.HasValue)
            {
                // Lost origin somehow, go idle
                controller.ChangeState(new IdleState(controller));
                return;
            }

            // Check if we've arrived at origin
            float distanceToOrigin = Vector3.Distance(
                controller.transform.position,
                controller.AggroOriginPosition.Value
            );

            if (distanceToOrigin <= ARRIVAL_THRESHOLD)
            {
                // Reached origin, clear it and go idle
                controller.ClearAggroOrigin();
                controller.ChangeState(new IdleState(controller));
                return;
            }

            // Keep updating path to origin
            controller.Movement?.SetDestination(controller.AggroOriginPosition.Value);
        }

        public override void OnExit()
        {
            // Nothing to clean up
        }
    }
}
