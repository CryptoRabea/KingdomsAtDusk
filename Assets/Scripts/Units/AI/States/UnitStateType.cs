namespace RTS.Units.AI
{
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
        Patrolling,
        ReturningToOrigin
    }
}