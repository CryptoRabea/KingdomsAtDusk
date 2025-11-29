using UnityEngine;
using RTS.Core.Events;

namespace RTS.Core.Conditions
{
    /// <summary>
    /// Victory condition: Survive a specified number of waves
    /// </summary>
    public class SurviveWavesVictory : VictoryCondition
    {
        [Header("Wave Settings")]
        [SerializeField] private int targetWaves = 10;

        private int currentWave = 0;
        private EventBus.EventSubscription<WaveCompletedEvent> waveSubscription;

        public override bool IsCompleted => currentWave >= targetWaves;
        public override float Progress => Mathf.Clamp01((float)currentWave / targetWaves);

        public override void Initialize()
        {
            currentWave = 0;
            waveSubscription = EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        }

        public override void Cleanup()
        {
            EventBus.Unsubscribe(waveSubscription);
        }

        public override string GetStatusText()
        {
            return $"Waves Survived: {currentWave}/{targetWaves}";
        }

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            currentWave = evt.WaveNumber;
            Debug.Log($"Victory Progress: {GetStatusText()}");
        }
    }
}
