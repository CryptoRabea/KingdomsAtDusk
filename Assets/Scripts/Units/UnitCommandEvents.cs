using UnityEngine;

namespace RTS.Units
{
    /// <summary>
    /// Type of target for attack commands
    /// </summary>
    public enum AttackTargetType
    {
        Unit,
        Building
    }

    /// <summary>
    /// Event published when a unit receives a move command
    /// </summary>
    public struct UnitMoveCommandEvent
    {
        public GameObject Unit;
        public Vector3 TargetPosition;

        public UnitMoveCommandEvent(GameObject unit, Vector3 targetPosition)
        {
            Unit = unit;
            TargetPosition = targetPosition;
        }
    }

    /// <summary>
    /// Event published when a unit receives an attack command
    /// </summary>
    public struct UnitAttackCommandEvent
    {
        public GameObject Unit;
        public GameObject Target;
        public AttackTargetType TargetType;

        public UnitAttackCommandEvent(GameObject unit, GameObject target, AttackTargetType targetType)
        {
            Unit = unit;
            Target = target;
            TargetType = targetType;
        }
    }
}
