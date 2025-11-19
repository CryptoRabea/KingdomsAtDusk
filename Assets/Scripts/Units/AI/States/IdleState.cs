using UnityEngine;

namespace RTS.Units.AI
{
    public class IdleState : UnitState
    {
        private float idleTimer;
        private float scanInterval = 0.5f;
        private const float FORCED_MOVE_CLEAR_TIME = 2f; // Clear forced move after 2 seconds of idle

        public IdleState(UnitAIController aiController) : base(aiController) { }

        public override UnitStateType GetStateType() => UnitStateType.Idle;

        public override void OnEnter()
        {
            controller.Movement?.Stop();
            idleTimer = 0f;
        }

        public override void OnUpdate()
        {
            idleTimer += Time.deltaTime;

            // Clear forced move flag after being idle for a while
            if (controller.IsOnForcedMove && idleTimer >= FORCED_MOVE_CLEAR_TIME)
            {
                controller.SetForcedMove(false);
            }

            if (idleTimer >= scanInterval)
            {
                idleTimer = 0f;

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