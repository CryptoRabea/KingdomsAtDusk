
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

        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnExit() { }
        public abstract UnitStateType GetStateType();
    }
}
