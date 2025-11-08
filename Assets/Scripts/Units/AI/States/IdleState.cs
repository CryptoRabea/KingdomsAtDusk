using UnityEngine;

namespace RTS.Units.AI
{
    public class IdleState : UnitState
    {
        private float idleTimer;
        private float scanInterval = 0.5f;

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

            if (idleTimer >= scanInterval)
            {
                idleTimer = 0f;

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