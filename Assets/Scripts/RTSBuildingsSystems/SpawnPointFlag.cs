using UnityEngine;
using RTS.Core.Events;

namespace RTS.Buildings
{
    /// <summary>
    /// Manages the spawn point flag visualization for buildings.
    /// The flag is only visible when the building is selected.
    /// </summary>
    public class SpawnPointFlag : MonoBehaviour
    {
        [Header("Flag Settings")]
        [SerializeField] private GameObject flagVisual;
        [SerializeField] private Color flagColor = Color.green;
        [SerializeField] private float flagHeight = 2f;
        [SerializeField] private float flagPoleRadius = 0.05f;
        [SerializeField] private float flagSize = 0.5f;

        [Header("References")]
        [SerializeField] private Transform spawnPoint;

        [Header("Auto-Create Flag")]
        [SerializeField] private bool autoCreateFlag = true;

        private BuildingSelectable buildingSelectable;
        private UnitTrainingQueue trainingQueue;
        private bool isVisible = false;

        private void Awake()
        {
            buildingSelectable = GetComponent<BuildingSelectable>();
            trainingQueue = GetComponent<UnitTrainingQueue>();

            // Get spawn point from training queue if not set
            if (spawnPoint == null && trainingQueue != null)
            {
                spawnPoint = trainingQueue.GetSpawnPoint();
            }

            // Auto-create flag visual if not assigned
            if (flagVisual == null && autoCreateFlag)
            {
                CreateFlagVisual();
            }

            // Hide flag initially
            if (flagVisual != null)
            {
                flagVisual.SetActive(false);
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<BuildingSelectedEvent>(OnBuildingSelected);
            EventBus.Subscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BuildingSelectedEvent>(OnBuildingSelected);
            EventBus.Unsubscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
        }

        private void OnBuildingSelected(BuildingSelectedEvent evt)
        {
            if (evt.Building == gameObject)
            {
                ShowFlag();
            }
        }

        private void OnBuildingDeselected(BuildingDeselectedEvent evt)
        {
            if (evt.Building == gameObject)
            {
                HideFlag();
            }
        }

        /// <summary>
        /// Show the spawn point flag
        /// </summary>
        public void ShowFlag()
        {
            if (flagVisual != null && spawnPoint != null)
            {
                isVisible = true;
                flagVisual.SetActive(true);
                UpdateFlagPosition();
            }
        }

        /// <summary>
        /// Hide the spawn point flag
        /// </summary>
        public void HideFlag()
        {
            if (flagVisual != null)
            {
                isVisible = false;
                flagVisual.SetActive(false);
            }
        }

        /// <summary>
        /// Update flag position to match spawn point
        /// </summary>
        public void UpdateFlagPosition()
        {
            if (flagVisual != null && spawnPoint != null)
            {
                flagVisual.transform.position = spawnPoint.position;
            }
        }

        /// <summary>
        /// Set the spawn point transform
        /// </summary>
        public void SetSpawnPoint(Transform newSpawnPoint)
        {
            spawnPoint = newSpawnPoint;
            if (isVisible)
            {
                UpdateFlagPosition();
            }
        }

        /// <summary>
        /// Update spawn point position
        /// </summary>
        public void SetSpawnPointPosition(Vector3 position)
        {
            if (spawnPoint != null)
            {
                spawnPoint.position = position;
                UpdateFlagPosition();
            }
        }

        private void Update()
        {
            // Keep flag position synchronized with spawn point
            if (isVisible && spawnPoint != null)
            {
                UpdateFlagPosition();
            }
        }

        /// <summary>
        /// Creates a simple flag visual using Unity primitives
        /// </summary>
        private void CreateFlagVisual()
        {
            // Create parent object
            GameObject flagParent = new GameObject("FlagVisual");
            flagParent.transform.SetParent(transform);

            // Create pole (cylinder)
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "FlagPole";
            pole.transform.SetParent(flagParent.transform);
            pole.transform.localPosition = Vector3.up * (flagHeight / 2f);
            pole.transform.localScale = new Vector3(flagPoleRadius, flagHeight / 2f, flagPoleRadius);

            // Remove collider from pole (we don't want it to interfere with clicks)
            Collider poleCollider = pole.GetComponent<Collider>();
            if (poleCollider != null)
                Destroy(poleCollider);

            // Set pole color to dark gray
            Renderer poleRenderer = pole.GetComponent<Renderer>();
            if (poleRenderer != null)
            {
                poleRenderer.material.color = new Color(0.3f, 0.3f, 0.3f);
            }

            // Create flag (cube stretched and rotated)
            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag";
            flag.transform.SetParent(flagParent.transform);
            flag.transform.localPosition = new Vector3(flagSize / 2f, flagHeight * 0.85f, 0);
            flag.transform.localScale = new Vector3(flagSize, flagSize * 0.6f, 0.05f);

            // Remove collider from flag
            Collider flagCollider = flag.GetComponent<Collider>();
            if (flagCollider != null)
                Destroy(flagCollider);

            // Set flag color
            Renderer flagRenderer = flag.GetComponent<Renderer>();
            if (flagRenderer != null)
            {
                flagRenderer.material.color = flagColor;
            }

            flagVisual = flagParent;
        }

        private void OnDrawGizmosSelected()
        {
            if (spawnPoint != null)
            {
                // Draw a green sphere at spawn point location
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPoint.position, 0.3f);

                // Draw line from building to spawn point
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, spawnPoint.position);
            }
        }
    }
}
