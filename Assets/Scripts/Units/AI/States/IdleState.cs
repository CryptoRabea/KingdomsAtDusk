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
            // Don't stop if we're on a forced move (formation change)
            if (!controller.IsOnForcedMove)
            {
                controller.Movement?.Stop();
            }
            scanTimer = 0f;
        }

        public override void OnUpdate()
        {
            // Check if we're on a forced move and still moving
            if (controller.IsOnForcedMove)
            {
                // Check if we've reached the destination
                if (controller.HasReachedForcedMoveDestination() ||
                    (controller.Movement != null && controller.Movement.HasReachedDestination))
                {
                    // Reached destination, clear forced move and resume normal behavior
                    controller.SetForcedMove(false);
                }
                else
                {
                    // Still moving to forced destination, stay in idle (don't auto-aggro)
                    return;
                }
            }

            scanTimer += Time.deltaTime;

            if (scanTimer >= scanInterval)
            {
                scanTimer = 0f;

                // Only scan for targets if not on a forced move
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