using UnityEngine;
using System.Collections.Generic;
using RTS.Core.Services;

namespace RTS.Managers
{
    /// <summary>
    /// Manages peasant workforce allocation across different systems.
    /// This is a convenience service that wraps IPopulationService for specific use cases.
    /// </summary>
    public class PeasantWorkforceManager : MonoBehaviour, IPeasantWorkforceService
    {
        private Dictionary<GameObject, WorkerAssignment> assignments = new Dictionary<GameObject, WorkerAssignment>();

        private class WorkerAssignment
        {
            public string WorkType;
            public int Amount;
        }

        #region IPeasantWorkforceService Implementation

        public bool RequestWorkers(string workType, int amount, GameObject requester)
        {
            if (amount <= 0) return true;
            if (requester == null) return false;

            var populationService = ServiceLocator.TryGet<IPopulationService>();
            if (populationService == null)
            {
                return false;
            }

            // Try to assign peasants
            if (!populationService.TryAssignPeasants(amount, workType, requester))
            {
                return false;
            }

            // Track the assignment
            if (assignments.ContainsKey(requester))
            {
                assignments[requester].Amount += amount;
            }
            else
            {
                assignments[requester] = new WorkerAssignment
                {
                    WorkType = workType,
                    Amount = amount
                };
            }

            return true;
        }

        public void ReleaseWorkers(string workType, int amount, GameObject requester)
        {
            if (amount <= 0) return;
            if (requester == null) return;

            var populationService = ServiceLocator.TryGet<IPopulationService>();
            if (populationService == null) return;

            // Release peasants
            populationService.ReleasePeasants(amount, workType, requester);

            // Update tracking
            if (assignments.ContainsKey(requester))
            {
                assignments[requester].Amount = Mathf.Max(0, assignments[requester].Amount - amount);

                if (assignments[requester].Amount == 0)
                {
                    assignments.Remove(requester);
                }
            }
        }

        public int GetAssignedWorkers(GameObject requester)
        {
            if (requester == null) return 0;
            return assignments.ContainsKey(requester) ? assignments[requester].Amount : 0;
        }

        public bool CanAssignWorkers(int amount)
        {
            var populationService = ServiceLocator.TryGet<IPopulationService>();
            if (populationService == null) return false;

            return populationService.AvailablePeasants >= amount;
        }

        #endregion

        private void OnDestroy()
        {
            // Clean up all assignments when manager is destroyed
            var populationService = ServiceLocator.TryGet<IPopulationService>();
            if (populationService != null)
            {
                foreach (var assignment in assignments)
                {
                    populationService.ReleasePeasants(assignment.Value.Amount, assignment.Value.WorkType, assignment.Key);
                }
            }
            assignments.Clear();
        }
    }
}
