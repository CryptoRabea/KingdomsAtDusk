using UnityEngine;

namespace RTS.Units.AI
{
    /// <summary>
    /// Base class for all unit AI states.
    /// Implements the State pattern for clean, maintainable AI behavior.
    /// </summary>
    public abstract class UnitState
    {
        protected UnitAIController controller;

        public UnitState(UnitAIController aiController)
        {
            controller = aiController;
        }

        /// <summary>
        /// Called when entering this state.
        /// </summary>
        public virtual void OnEnter() { }

        /// <summary>
        /// Called every frame while in this state.
        /// </summary>
        public virtual void OnUpdate() { }

        /// <summary>
        /// Called when exiting this state.
        /// </summary>
        public virtual void OnExit() { }

        /// <summary>
        /// Get the state type enum for this state.
        /// </summary>
        public abstract UnitStateType GetStateType();
    }

    /// <summary>
    /// State type enumeration for easy identification.
    /// </summary>
    public enum UnitStateType
    {
        Idle,
        Moving,
        Attacking,
        Retreating,
        Dead,
        Healing,
        Patrolling
    }

    // ==================== IDLE STATE ====================
    
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
                
                // Look for targets
                Transform target = controller.FindTarget();
                if (target != null)
                {
                    controller.SetTarget(target);
                    controller.ChangeState(new MovingState(controller));
                }
            }
        }
    }

    // ==================== MOVING STATE ====================
    
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

            // Check if should retreat
            if (controller.ShouldRetreat())
            {
                controller.ChangeState(new RetreatState(controller));
                return;
            }

            // Update path periodically
            pathUpdateTimer += Time.deltaTime;
            if (pathUpdateTimer >= PATH_UPDATE_INTERVAL)
            {
                pathUpdateTimer = 0f;
                controller.Movement?.SetDestination(target.position);
            }

            // Check if in attack range
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

    // ==================== ATTACKING STATE ====================
    
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

            // Check if should retreat
            if (controller.ShouldRetreat())
            {
                controller.ChangeState(new RetreatState(controller));
                return;
            }

            // Face target
            controller.Movement?.LookAt(target.position);

            // Check if still in range
            if (!controller.Combat.IsTargetInRange(target))
            {
                controller.ChangeState(new MovingState(controller));
                return;
            }

            // Try to attack
            controller.Combat?.TryAttack();
        }
    }

    // ==================== RETREAT STATE ====================
    
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
            // If health recovered above threshold, go back to idle
            if (!controller.ShouldRetreat())
            {
                controller.ChangeState(new IdleState(controller));
                return;
            }

            // Keep retreating until destination reached
            if (controller.Movement != null && controller.Movement.HasReachedDestination)
            {
                // Reached retreat point, now idle and heal
                controller.ChangeState(new IdleState(controller));
            }
        }

        private void CalculateRetreatPosition()
        {
            Transform target = controller.CurrentTarget;
            
            if (target != null)
            {
                // Retreat away from target
                Vector3 direction = (controller.transform.position - target.position).normalized;
                retreatPosition = controller.transform.position + direction * RETREAT_DISTANCE;
            }
            else
            {
                // No target, just move back a bit
                retreatPosition = controller.transform.position - controller.transform.forward * RETREAT_DISTANCE;
            }
        }
    }

    // ==================== DEAD STATE ====================
    
    public class DeadState : UnitState
    {
        public DeadState(UnitAIController aiController) : base(aiController) { }

        public override UnitStateType GetStateType() => UnitStateType.Dead;

        public override void OnEnter()
        {
            controller.Movement?.SetEnabled(false);
            controller.Movement?.Stop();
            controller.Combat?.SetCanAttack(false);

            // Disable collider if present
            var collider = controller.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            // Schedule destruction
            Object.Destroy(controller.gameObject, 2f);
        }
    }

    // ==================== HEALING STATE (for healer units) ====================
    
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
            Transform target = controller.CurrentTarget;

            if (target == null)
            {
                controller.ChangeState(new IdleState(controller));
                return;
            }

            // Face target
            controller.Movement?.LookAt(target.position);

            // Check if still in range
            if (controller.Combat != null && !controller.Combat.IsTargetInRange(target))
            {
                controller.ChangeState(new MovingState(controller));
                return;
            }

            // Try to heal (healers use "attack" to heal)
            controller.Combat?.TryAttack();
        }
    }
}
