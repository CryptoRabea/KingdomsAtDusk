using UnityEngine;
using RTS.Core.Events;

namespace RTS.Debug.EnemySpawner
{
    /// <summary>
    /// Manages enemy spawner selection via raycasting.
    /// Attach this to an empty GameObject in the scene (e.g., "EnemySpawnerManager").
    ///
    /// This is a standalone manager that doesn't interfere with other selection systems.
    ///
    /// TO REMOVE: Delete the entire Assets/Scripts/Debug folder
    /// </summary>
    public class EnemySpawnerManager : MonoBehaviour
    {
        [Header("Selection Settings")]
        [SerializeField] private LayerMask spawnerLayerMask = -1;
        [SerializeField] private KeyCode selectKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode deselectKey = KeyCode.Escape;

        [Header("Debug")]
        [SerializeField] private bool showDebugRays = false;

        private EnemySpawnerBuilding currentlySelectedSpawner;
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            // Handle deselection
            if (Input.GetKeyDown(deselectKey) && currentlySelectedSpawner != null)
            {
                DeselectCurrentSpawner();
                return;
            }

            // Handle selection via click
            if (Input.GetKeyDown(selectKey))
            {
                TrySelectSpawner();
            }
        }

        private void TrySelectSpawner()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (showDebugRays)
            {
                UnityEngine.Debug.DrawRay(ray.origin, ray.direction * 100f, Color.yellow, 1f);
            }

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, spawnerLayerMask))
            {
                // Check if we hit an enemy spawner
                EnemySpawnerBuilding spawner = hit.collider.GetComponent<EnemySpawnerBuilding>();

                if (spawner == null)
                {
                    spawner = hit.collider.GetComponentInParent<EnemySpawnerBuilding>();
                }

                if (spawner != null)
                {
                    SelectSpawner(spawner);
                }
                else if (currentlySelectedSpawner != null)
                {
                    // Clicked on something else, could set rally point or deselect
                    // For now, check if shift is held for rally point
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        currentlySelectedSpawner.SetRallyPoint(hit.point);
                    }
                }
            }
        }

        private void SelectSpawner(EnemySpawnerBuilding spawner)
        {
            // Deselect previous
            if (currentlySelectedSpawner != null && currentlySelectedSpawner != spawner)
            {
                currentlySelectedSpawner.Deselect();
            }

            currentlySelectedSpawner = spawner;
            spawner.Select();
        }

        private void DeselectCurrentSpawner()
        {
            if (currentlySelectedSpawner != null)
            {
                currentlySelectedSpawner.Deselect();
                currentlySelectedSpawner = null;
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemySpawnerDeselectedEvent>(OnSpawnerDeselected);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemySpawnerDeselectedEvent>(OnSpawnerDeselected);
        }

        private void OnSpawnerDeselected(EnemySpawnerDeselectedEvent evt)
        {
            if (currentlySelectedSpawner == evt.Spawner)
            {
                currentlySelectedSpawner = null;
            }
        }
    }
}
