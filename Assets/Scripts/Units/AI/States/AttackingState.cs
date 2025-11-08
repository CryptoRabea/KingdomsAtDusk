using UnityEngine;

namespace RTS.Units.AI
{
    public class AttackingState : UnitState
    {
        public AttackingState(UnitAIController aiController) : base(aiController) { }

        public override UnitStateType GetStateType() => UnitStateType.Attacking;

        public override void OnEnter()
        {
            controller.Movement?.Stop();
        }

        public override void OnUpdate()
        {
            Transform target = controller.CurrentTarget;

            if (target == null)
            {
                controller.ChangeState(new IdleState(controller));
                return;
            }

            if (controller.ShouldRetreat())
            {
                controller.ChangeState(new RetreatState(controller));
                return;
            }

            controller.Movement?.LookAt(target.position);

            if (!controller.Combat.IsTargetInRange(target))
            {
                controller.ChangeState(new MovingState(controller));
                return;
            }

            controller.Combat?.TryAttack();
        }
    }
}
