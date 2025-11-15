using UnityEngine;
using RTS.Core.Events;

namespace RTS.Buildings
{
    /// <summary>
    /// Manages the rally point flag visualization for buildings.
    /// The flag is only visible when the building is selected.
    /// </summary>
    public class RallyPointFlag : MonoBehaviour
    {
        [Header("Flag Settings")]
        [SerializeField] private GameObject flagVisual;
        [SerializeField] private Color flagColor = Color.green;
        [SerializeField] private float flagHeight = 2f;
        [SerializeField] private float flagPoleRadius = 0.05f;
        [SerializeField] private float flagSize = 0.5f;

        [Header("References")]
        [SerializeField] private Transform rallyPoint;

        [Header("Auto-Create Flag")]
        [SerializeField] private bool autoCreateFlag = true;

        private BuildingSelectable buildingSelectable;
        private UnitTrainingQueue trainingQueue;
        private bool isVisible = false;

        private void Awake()
        {
            buildingSelectable = GetComponent<BuildingSelectable>();
            trainingQueue = GetComponent<UnitTrainingQueue>();

            // Get rally point from training queue if not set
            if (rallyPoint == null && trainingQueue != null)
            {
                rallyPoint = trainingQueue.GetRallyPoint();
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

        private void OnDestroy()
        {
            // Clean up flag visual when component is destroyed
            if (flagVisual != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(flagVisual);
                }
                else
                {
                    DestroyImmediate(flagVisual);
                }
            }
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
        /// Show the rally point flag
        /// </summary>
        public void ShowFlag()
        {
            // Get the latest rally point reference from training queue
            if (trainingQueue != null)
            {
                rallyPoint = trainingQueue.GetRallyPoint();
            }

            if (flagVisual != null && rallyPoint != null)
            {
                isVisible = true;
                flagVisual.SetActive(true);
                UpdateFlagPosition();
                Debug.Log($"✅ RallyPointFlag: Showing flag for {gameObject.name} at {rallyPoint.position}");
            }
            else
            {
                if (flagVisual == null)
                    Debug.LogWarning($"⚠️ RallyPointFlag: Cannot show flag for {gameObject.name}: flagVisual is null");
                if (rallyPoint == null)
                    Debug.LogWarning($"⚠️ RallyPointFlag: Cannot show flag for {gameObject.name}: rallyPoint is null (training queue has no rally point set)");
            }
        }

        /// <summary>
        /// Hide the rally point flag
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
        /// Update flag position to match rally point
        /// </summary>
        public void UpdateFlagPosition()
        {
            if (flagVisual != null && rallyPoint != null)
            {
                flagVisual.transform.position = rallyPoint.position;
            }
        }

        /// <summary>
        /// Set the rally point transform
        /// </summary>
        public void SetRallyPoint(Transform newRallyPoint)
        {
            rallyPoint = newRallyPoint;
            if (isVisible)
            {
                UpdateFlagPosition();
            }
        }

        /// <summary>
        /// Update rally point position
        /// </summary>
        public void SetRallyPointPosition(Vector3 position)
        {
            // Get the latest rally point reference from training queue
            // (it might have been created after Awake)
            if (trainingQueue != null)
            {
                rallyPoint = trainingQueue.GetRallyPoint();
            }

            if (rallyPoint != null)
            {
                rallyPoint.position = position;
                UpdateFlagPosition();
                Debug.Log($"✅ RallyPointFlag: Updated rally point position to {position} for {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ RallyPointFlag: Cannot set rally point position for {gameObject.name}: rallyPoint is still null even after getting from training queue");
            }
        }

        private void Update()
        {
            // Keep flag position synchronized with rally point
            if (isVisible && rallyPoint != null)
            {
                UpdateFlagPosition();
            }
        }

        /// <summary>
        /// Creates a simple flag visual using Unity primitives
        /// </summary>
        private void CreateFlagVisual()
        {
            // Create parent object - DON'T parent to building to avoid transform issues
            GameObject flagParent = new GameObject($"RallyFlag_{gameObject.name}");

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
            Debug.Log($"Created rally point flag visual for {gameObject.name}");
        }

        private void OnDrawGizmosSelected()
        {
            if (rallyPoint != null)
            {
                // Draw a green sphere at rally point location
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(rallyPoint.position, 0.3f);

                // Draw line from building to rally point
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, rallyPoint.position);
            }
        }
    }
}
