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
        private EventBus.EventSubscription<UnitDiedEvent> unitDiedSubscription;

        public override bool IsCompleted => bossDefeated;
        public override float Progress => bossDefeated ? 1f : 0f;

        public override void Initialize()
        {
            bossDefeated = false;
            unitDiedSubscription = EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
        }

        public override void Cleanup()
        {
            EventBus.Unsubscribe(unitDiedSubscription);
        }

        public override string GetStatusText()
        {
            return bossDefeated ? "Boss Defeated!" : "Defeat the Boss";
        }

        private void OnUnitDied(UnitDiedEvent evt)
        {
            if (evt.Unit == null) return;

            // Check if the dead unit was a boss
            var bossAI = evt.Unit.GetComponent<BossAI>();
            if (bossAI != null || evt.Unit.CompareTag(bossTag))
            {
                bossDefeated = true;
                Debug.Log("Victory Condition: Boss Defeated!");
            }
        }
    }
}
