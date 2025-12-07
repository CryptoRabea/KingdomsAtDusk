
using UnityEngine;

namespace RTS.Units.AI
{
    public class DeadState : UnitState
    {
        public DeadState(UnitAIController aiController) : base(aiController) { }

        public override UnitStateType GetStateType() => UnitStateType.Dead;

        public override void OnEnter()
        {
            controller.Movement?.SetEnabled(false);
            controller.Movement?.Stop();
            controller.Combat?.SetCanAttack(false);

            if (controller.TryGetComponent<Collider>(out var collider)) collider.enabled = false;

            Object.Destroy(controller.gameObject, 2f);
        }
    }
}
