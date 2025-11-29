using UnityEngine;

namespace RTS.Units.AI
{
    public class IdleState : UnitState
    {
        private float scanTimer;
        private float scanInterval = 0.5f;

        public IdleState(UnitAIController aiController) : base(aiController) { }

        public override UnitStateType GetStateType() => UnitStateType.Idle;

        public override void OnEnter()
        {
            controller.Movement?.Stop();
            scanTimer = 0f;
        }

        public override void OnUpdate()
        {
            // Check if unit should be moving (formation change, forced move, etc.)
            if (controller.Movement != null && controller.Movement.IsMoving)
            {
                // Unit is moving, transition to MovingState
                controller.ChangeState(new MovingState(controller));
                return;
            }

            // Clear forced move flag if we've reached the destination
            if (controller.IsOnForcedMove && controller.HasReachedForcedMoveDestination())
            {
                // Reached destination, resume normal aggro behavior immediately
                controller.SetForcedMove(false);
            }

            scanTimer += Time.deltaTime;

            if (scanTimer >= scanInterval)
            {
                scanTimer = 0f;

                // Don't auto-target if player has issued a forced move command
                if (!controller.IsOnForcedMove)
                {
                    Transform target = controller.FindTarget();
                    if (target != null)
                    {
                        controller.SetTarget(target);
                        controller.ChangeState(new MovingState(controller));
                    }
                }
            }
        }
    }
}