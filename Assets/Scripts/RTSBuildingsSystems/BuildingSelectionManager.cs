using UnityEngine;
using UnityEngine.InputSystem;
using RTS.Core.Events;

namespace RTS.Buildings
{
    /// <summary>
    /// Manages building selection using modern Input System.
    /// ADD THIS TO ONE GAMEOBJECT IN YOUR SCENE (like a "GameManager").
    /// Allows players to click on buildings to select them and show training UI.
    /// </summary>
    public class BuildingSelectionManager : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private InputActionReference clickAction;
        [SerializeField] private InputActionReference positionAction;

        [Header("Selection Settings")]
        [SerializeField] private LayerMask buildingLayer;
        [SerializeField] private Camera mainCamera;

        private BuildingSelectable currentlySelected;

        public BuildingSelectable CurrentlySelectedBuilding => currentlySelected;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (clickAction != null)
            {
                clickAction.action.Enable();
                clickAction.action.performed += OnClick;
            }

            if (positionAction != null)
            {
                positionAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (clickAction != null)
            {
                clickAction.action.Disable();
                clickAction.action.performed -= OnClick;
            }

            if (positionAction != null)
            {
                positionAction.action.Disable();
            }
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            Vector2 mousePosition = positionAction.action.ReadValue<Vector2>();
            TrySelectBuilding(mousePosition);
        }

        private void TrySelectBuilding(Vector2 screenPosition)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, buildingLayer))
            {
                var selectable = hit.collider.GetComponent<BuildingSelectable>();
                if (selectable != null)
                {
                    SelectBuilding(selectable);
                    return;
                }
            }

            // Clicked empty space - deselect current building
            DeselectCurrentBuilding();
        }

        private void SelectBuilding(BuildingSelectable building)
        {
            // Deselect previous building if different
            if (currentlySelected != null && currentlySelected != building)
            {
                currentlySelected.Deselect();
            }

            currentlySelected = building;
            building.Select();
        }

        private void DeselectCurrentBuilding()
        {
            if (currentlySelected != null)
            {
                currentlySelected.Deselect();
                currentlySelected = null;
            }
        }

        public void DeselectBuilding()
        {
            DeselectCurrentBuilding();
        }
    }
}
