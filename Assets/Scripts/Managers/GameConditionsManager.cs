using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RTS.Core.Services;
using static RTS.Core.Services.GameState;

namespace RTS.Core
{
    /// <summary>
    /// Manages all victory and defeat conditions for the game.
    /// Checks conditions each frame and triggers game over when appropriate.
    /// </summary>
    public class GameConditionsManager : MonoBehaviour
    {
        [Header("Condition Settings")]
        [SerializeField] private ConditionCheckMode checkMode = ConditionCheckMode.AnyVictory;
        [SerializeField] private float checkInterval = 1f; // Check conditions every second

        [Header("Condition Components")]
        [SerializeField] private List<VictoryCondition> victoryConditions = new List<VictoryCondition>();
        [SerializeField] private List<DefeatCondition> defeatConditions = new List<DefeatCondition>();

        private float checkTimer = 0f;
        private IGameStateService gameStateService;
        private bool gameEnded = false;

        // Public accessors
        public List<VictoryCondition> VictoryConditions => victoryConditions;
        public List<DefeatCondition> DefeatConditions => defeatConditions;
        public bool GameEnded => gameEnded;

        private void Start()
        {
            gameStateService = ServiceLocator.Get<IGameStateService>();
            InitializeConditions();
        }

        private void Update()
        {
            if (gameEnded) return;

            checkTimer += Time.deltaTime;
            if (checkTimer >= checkInterval)
            {
                checkTimer = 0f;
                CheckConditions();
            }
        }

        private void InitializeConditions()
        {
            // Auto-discover conditions attached to this GameObject or children
            if (victoryConditions.Count == 0)
            {
                victoryConditions.AddRange(GetComponentsInChildren<VictoryCondition>());
            }

            if (defeatConditions.Count == 0)
            {
                defeatConditions.AddRange(GetComponentsInChildren<DefeatCondition>());
            }

            // Initialize all conditions
            foreach (var condition in victoryConditions)
            {
                condition.Initialize();
                Debug.Log($"Initialized Victory Condition: {condition.ConditionName}");
            }

            foreach (var condition in defeatConditions)
            {
                condition.Initialize();
                Debug.Log($"Initialized Defeat Condition: {condition.ConditionName}");
            }

            Debug.Log($"Game Conditions Manager initialized with {victoryConditions.Count} victory and {defeatConditions.Count} defeat conditions.");
        }

        private void CheckConditions()
        {
            // Check defeat conditions first (they take priority)
            foreach (var condition in defeatConditions)
            {
                if (condition.IsFailed)
                {
                    TriggerDefeat(condition);
                    return;
                }
            }

            // Check victory conditions
            bool victoryAchieved = checkMode switch
            {
                ConditionCheckMode.AnyVictory => victoryConditions.Any(c => c.IsCompleted),
                ConditionCheckMode.AllVictory => victoryConditions.All(c => c.IsCompleted),
                _ => false
            };

            if (victoryAchieved)
            {
                TriggerVictory();
            }
        }

        private void TriggerVictory()
        {
            if (gameEnded) return;

            gameEnded = true;
            Debug.Log("=== VICTORY ACHIEVED ===");

            // Log all completed conditions
            foreach (var condition in victoryConditions.Where(c => c.IsCompleted))
            {
                Debug.Log($"OK {condition.ConditionName}: {condition.GetStatusText()}");
            }

            // Trigger game over with victory state
            if (gameStateService != null)
            {
                gameStateService.ChangeState(GameState.Victory);
            }
        }

        private void TriggerDefeat(DefeatCondition failedCondition)
        {
            if (gameEnded) return;

            gameEnded = true;
            Debug.Log("=== DEFEAT ===");
            Debug.Log($"X {failedCondition.ConditionName}: {failedCondition.GetStatusText()}");

            // Trigger game over with defeat state
            if (gameStateService != null)
            {
                gameStateService.ChangeState(GameState.GameOver);
            }
        }

        /// <summary>
        /// Get summary of all conditions for UI display
        /// </summary>
        public string GetConditionsSummary()
        {
            string summary = "=== OBJECTIVES ===\n";

            summary += "\nVictory Conditions:\n";
            foreach (var condition in victoryConditions)
            {
                string status = condition.IsCompleted ? "OK" : "-";
                summary += $"{status} {condition.GetStatusText()} ({condition.Progress * 100:F0}%)\n";
            }

            summary += "\nDefeat Conditions:\n";
            foreach (var condition in defeatConditions)
            {
                string status = condition.IsFailed ? "X" : "OK";
                summary += $"{status} {condition.GetStatusText()}\n";
            }

            return summary;
        }

        /// <summary>
        /// Reset all conditions (for new game)
        /// </summary>
        public void ResetConditions()
        {
            gameEnded = false;
            checkTimer = 0f;

            foreach (var condition in victoryConditions)
            {
                condition.Cleanup();
                condition.Initialize();
            }

            foreach (var condition in defeatConditions)
            {
                condition.Cleanup();
                condition.Initialize();
            }

            Debug.Log("All game conditions reset.");
        }

        private void OnDestroy()
        {
            // Cleanup all conditions
            foreach (var condition in victoryConditions)
            {
                condition?.Cleanup();
            }

            foreach (var condition in defeatConditions)
            {
                condition?.Cleanup();
            }
        }
    }

    public enum ConditionCheckMode
    {
        AnyVictory,  // Any single victory condition triggers win
        AllVictory   // All victory conditions must be met
    }
}
