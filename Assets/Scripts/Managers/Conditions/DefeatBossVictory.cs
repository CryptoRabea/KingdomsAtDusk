using System;
using UnityEngine;
using RTS.Core.Events;
using RTS.Units.AI;

namespace RTS.Core.Conditions
{
    /// <summary>
    /// Victory condition: Defeat a specific boss enemy
    /// </summary>
    public class DefeatBossVictory : VictoryCondition
    {
        [Header("Boss Settings")]
        [SerializeField] private string bossTag = "Boss";

        private bool bossDefeated = false;
        private Action<UnitDiedEvent> unitDiedHandler;

        public override bool IsCompleted => bossDefeated;
        public override float Progress => bossDefeated ? 1f : 0f;

        public override void Initialize()
        {
            bossDefeated = false;
            unitDiedHandler = OnUnitDied;
            EventBus.Subscribe(unitDiedHandler);
        }

        public override void Cleanup()
        {
            if (unitDiedHandler != null)
            {
                EventBus.Unsubscribe(unitDiedHandler);
            }
        }

        public override string GetStatusText()
        {
            return bossDefeated ? "Boss Defeated!" : "Defeat the Boss";
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            if (evt.Unit == null) return;

            // Check if the dead unit was a boss
            if (evt.Unit.TryGetComponent<BossAI>(out var bossAI) || evt.Unit.CompareTag(bossTag))
            {
                bossDefeated = true;
            }
        }
    }
}
