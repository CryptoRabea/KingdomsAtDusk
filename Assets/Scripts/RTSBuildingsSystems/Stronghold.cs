using System;
using UnityEngine;
using RTS.Core.Events;

namespace RTS.Buildings
{
    /// <summary>
    /// Stronghold - The player's main base building.
    /// Critical building that triggers defeat when destroyed.
    /// Provides vision, housing, and serves as a rally point.
    /// </summary>
    [RequireComponent(typeof(Building))]
    [RequireComponent(typeof(BuildingHealth))]
    public class Stronghold : MonoBehaviour
    {
        [Header("Stronghold Settings")]
        [SerializeField] private float visionRange = 30f;
        #pragma warning disable CS0414 // Field is assigned but never used - reserved for future housing system
        [SerializeField] private int housingBonus = 20;
        #pragma warning restore CS0414
        #pragma warning disable CS0414 // Field is assigned but never used - reserved for future happiness system
        [SerializeField] private float happinessBonus = 10f;
        #pragma warning restore CS0414

        [Header("Rally Point")]
        [SerializeField] private Transform rallyPoint;
        [SerializeField] private GameObject rallyPointMarker;

        private Building building;
        private BuildingHealth health;
        private Action<BuildingDamagedEvent> damageHandler;

        private void Awake()
        {
            building = GetComponent<Building>();
            health = GetComponent<BuildingHealth>();
        }

        private void Start()
        {
            // Listen for damage events to this stronghold
            damageHandler = OnBuildingDamaged;
            EventBus.Subscribe(damageHandler);

            // Show rally point marker if available
            if (rallyPointMarker != null)
            {
                rallyPointMarker.SetActive(true);
            }

            Debug.Log($"Stronghold initialized with {health.MaxHealth} HP");
        }

        private void OnBuildingDamaged(BuildingDamagedEvent evt)
        {
            // Check if this event is for our stronghold
            if (evt.Building != gameObject) return;

            // Log stronghold damage for dramatic effect
            if (evt.Delta < 0)
            {
                float healthPercent = evt.CurrentHealth / evt.MaxHealth * 100f;
                Debug.LogWarning($"[WARNING] STRONGHOLD UNDER ATTACK! Health: {healthPercent:F0}%");

                // Critical health warning
                if (healthPercent <= 25f && healthPercent > 0f)
                {
                    Debug.LogError($"[ALERT] CRITICAL! Stronghold health at {healthPercent:F0}%!");
                }
            }
        }

        /// <summary>
        /// Set the rally point for units trained/spawned at the stronghold
        /// </summary>
        public void SetRallyPoint(Vector3 position)
        {
            if (rallyPoint != null)
            {
                rallyPoint.position = position;
            }

            if (rallyPointMarker != null)
            {
                rallyPointMarker.transform.position = position;
            }
        }

        public Vector3 GetRallyPoint()
        {
            return rallyPoint != null ? rallyPoint.position : transform.position;
        }

        private void OnDrawGizmos()
        {
            // Draw vision range
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, visionRange);

            // Draw rally point connection
            if (rallyPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, rallyPoint.position);
                Gizmos.DrawWireSphere(rallyPoint.position, 1f);
            }
        }

        private void OnDestroy()
        {
            if (damageHandler != null)
            {
                EventBus.Unsubscribe(damageHandler);
            }
        }
    }
}
