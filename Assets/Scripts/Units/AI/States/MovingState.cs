using UnityEngine;

namespace RTS.Units.AI
{
    public class MovingState : UnitState
    {
        private float pathUpdateTimer;
        private const float PATH_UPDATE_INTERVAL = 0.5f;

        public MovingState(UnitAIController aiController) : base(aiController) { }

        public override UnitStateType GetStateType() => UnitStateType.Moving;

        public override void OnEnter()
        {
            pathUpdateTimer = 0f;
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
        }
    }
}