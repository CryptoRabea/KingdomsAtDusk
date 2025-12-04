using UnityEngine;

namespace RTS.Units.AI
{
    public class HealingState : UnitState
    {
        public HealingState(UnitAIController aiController) : base(aiController) { }

        public override UnitStateType GetStateType() => UnitStateType.Healing;

        public override void OnEnter()
        {
            controller.Movement?.Stop();
        }

        public override void OnUpdate()
        {
            // Check if player issued a forced move (e.g., formation change)
            if (controller.IsOnForcedMove)
            {
                controller.ClearTarget();
                controller.ChangeState(new MovingState(controller));
                return;
            }

            Transform target = controller.CurrentTarget;

            if (target == null)
            {
                controller.ChangeState(new IdleState(controller));
                return;
            }

            controller.Movement?.LookAt(target.position);

            if (controller.Combat != null && !controller.Combat.IsTargetInRange(target))
            {
                controller.ChangeState(new MovingState(controller));
                return;
            }

            controller.Combat?.TryAttack();
        }
    }
}