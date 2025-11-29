using UnityEngine;

namespace RTS.Units.AI
{
    public class RetreatState : UnitState
    {
        private Vector3 retreatPosition;
        private const float RETREAT_DISTANCE = 10f;

        public RetreatState(UnitAIController aiController) : base(aiController) { }

        public override UnitStateType GetStateType() => UnitStateType.Retreating;

        public override void OnEnter()
        {
            CalculateRetreatPosition();
            controller.Movement?.SetDestination(retreatPosition);
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

            if (!controller.ShouldRetreat())
            {
                controller.ChangeState(new IdleState(controller));
                return;
            }

            if (controller.Movement != null && controller.Movement.HasReachedDestination)
            {
                controller.ChangeState(new IdleState(controller));
            }
        }

        private void CalculateRetreatPosition()
        {
            Transform target = controller.CurrentTarget;

            if (target != null)
            {
                Vector3 direction = (controller.transform.position - target.position).normalized;
                retreatPosition = controller.transform.position + direction * RETREAT_DISTANCE;
            }
            else
            {
                retreatPosition = controller.transform.position - controller.transform.forward * RETREAT_DISTANCE;
            }
        }
    }
}