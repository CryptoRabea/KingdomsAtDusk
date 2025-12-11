using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RTS.Buildings
{
    /// <summary>
    /// Handles automatic gate opening/closing when friendly units approach.
    /// Uses layer-based detection to identify friendly units.
    /// </summary>
    public class GateAutoOpenController : MonoBehaviour
    {
        private Gate gate;
        private GateDataSO gateData;
        private bool isEnabled = true;
        private Coroutine detectionCoroutine;

        // Track units in range
        private HashSet<Collider> unitsInOpenRange = new HashSet<Collider>();
        private HashSet<Collider> unitsInCloseRange = new HashSet<Collider>();

        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                isEnabled = value;
                if (!isEnabled && detectionCoroutine != null)
                {
                    StopCoroutine(detectionCoroutine);
                    detectionCoroutine = null;
                }
                else if (isEnabled && detectionCoroutine == null)
                {
                    detectionCoroutine = StartCoroutine(DetectionLoop());
                }
            }
        }

        /// <summary>
        /// Initialize the auto-open controller.
        /// </summary>
        public void Initialize(Gate gateComponent, GateDataSO data)
        {
            gate = gateComponent;
            gateData = data;

            if (gateData.enableAutoOpen)
            {
                detectionCoroutine = StartCoroutine(DetectionLoop());
            }
        }

        private void OnDestroy()
        {
            if (detectionCoroutine != null)
            {
                StopCoroutine(detectionCoroutine);
            }
        }

        private IEnumerator DetectionLoop()
        {
            while (true)
            {
                if (isEnabled && gateData != null)
                {
                    CheckForNearbyUnits();
                }

                yield return new WaitForSeconds(gateData?.detectionInterval ?? 0.5f);
            }
        }

        private void CheckForNearbyUnits()
        {
            if (gate == null || gateData == null) return;

            Vector3 gatePosition = transform.position;

            // Check for units in open range
            Collider[] openRangeHits = Physics.OverlapSphere(
                gatePosition,
                gateData.autoOpenRange,
                gateData.friendlyLayers
            );

            // Check for units in close range
            Collider[] closeRangeHits = Physics.OverlapSphere(
                gatePosition,
                gateData.autoCloseRange,
                gateData.friendlyLayers
            );

            // Update tracking sets
            unitsInOpenRange.Clear();
            unitsInCloseRange.Clear();

            foreach (var hit in openRangeHits)
            {
                if (hit != null && IsValidUnit(hit))
                {
                    unitsInOpenRange.Add(hit);
                }
            }

            foreach (var hit in closeRangeHits)
            {
                if (hit != null && IsValidUnit(hit))
                {
                    unitsInCloseRange.Add(hit);
                }
            }

            // Decide whether to open or close
            if (unitsInOpenRange.Count > 0 && !gate.IsOpen)
            {
                // Units are near, open the gate
                gate.Open();
            }
            else if (unitsInCloseRange.Count == 0 && gate.IsOpen)
            {
                // No units nearby, close the gate
                gate.Close();
            }
        }

        private bool IsValidUnit(Collider col)
        {
            if (col == null) return false;
            if (col.gameObject == gameObject) return false; // Ignore self

            // Check if the collider belongs to a valid unit
            // You can add more validation here (e.g., check for UnitHealth, UnitMovement components)
            if (col.TryGetComponent<RTS.Units.UnitHealth>(out var unitHealth) && unitHealth.IsDead)
            {
                return false; // Ignore dead units
            }

            // Check if it's a building (we want units, not buildings)
            if (col.TryGetComponent<Building>(out var building))
            {
                return false; // Ignore buildings
            }

            return true;
        }

        #region Debug Visualization

        private void OnDrawGizmosSelected()
        {
            if (gateData == null) return;

            // Draw open range
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, gateData.autoOpenRange);

            // Draw close range
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, gateData.autoCloseRange);

            // Draw units in range
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                foreach (var unit in unitsInOpenRange)
                {
                    if (unit != null)
                    {
                        Gizmos.DrawLine(transform.position, unit.transform.position);
                    }
                }
            }
        }

        [ContextMenu("Print Units in Range")]
        private void DebugPrintUnitsInRange()
        {

            foreach (var unit in unitsInOpenRange)
            {
                if (unit != null)
                {
                }
            }
        }

        [ContextMenu("Toggle Auto-Open")]
        private void DebugToggleAutoOpen()
        {
            IsEnabled = !IsEnabled;
        }

        #endregion
    }
}
