using RTS.Units;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KingdomsAtDusk.UI
{
    /// <summary>
    /// Manages cursor state and appearance based on hover context and selected units
    /// </summary>
    public class CursorStateManager : MonoBehaviour
    {
        [Header("Cursor Textures")]
        [SerializeField] private Texture2D normalCursor;
        [SerializeField] private Texture2D moveCursor;
        [SerializeField] private Texture2D attackCursor;
        [SerializeField] private Texture2D invalidCursor;
        [SerializeField] private Texture2D selectUnitCursor;
        [SerializeField] private Texture2D selectBuildingCursor;

        [Header("Edge Scroll Cursors")]
        [SerializeField] private Texture2D scrollUpCursor;
        [SerializeField] private Texture2D scrollDownCursor;
        [SerializeField] private Texture2D scrollLeftCursor;
        [SerializeField] private Texture2D scrollRightCursor;
        [SerializeField] private Texture2D scrollUpLeftCursor;
        [SerializeField] private Texture2D scrollUpRightCursor;
        [SerializeField] private Texture2D scrollDownLeftCursor;
        [SerializeField] private Texture2D scrollDownRightCursor;

        [Header("Cursor Hotspots (pixel offset from top-left)")]
        [SerializeField] private Vector2 normalHotspot = Vector2.zero;
        [SerializeField] private Vector2 moveHotspot = new Vector2(16, 16);
        [SerializeField] private Vector2 attackHotspot = new Vector2(16, 16);
        [SerializeField] private Vector2 invalidHotspot = new Vector2(16, 16);
        [SerializeField] private Vector2 selectUnitHotspot = new Vector2(16, 16);
        [SerializeField] private Vector2 selectBuildingHotspot = new Vector2(16, 16);
        [SerializeField] private Vector2 scrollHotspot = new Vector2(16, 16);

        [Header("References")]
        [SerializeField] private UnitSelectionManager selectionManager;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask unitLayer;

        [Header("Settings")]
        [SerializeField] private float raycastDistance = 1000f;
        [SerializeField] private float edgeScrollBorderThickness = 10f;
        [Tooltip("Camera viewport height (0-1). If viewport is smaller than screen, UI below viewport counts as edge.")]
        [SerializeField] private float viewportHeight = 0.8f;
        [Tooltip("Camera viewport Y offset (0-1). Bottom of viewport where edge scrolling starts.")]
        [SerializeField] private float viewportYOffset = 0.2f;

        private CursorState currentState = CursorState.Normal;
        private Mouse mouse;

        public enum CursorState
        {
            Normal,
            Move,
            Attack,
            Invalid,
            SelectUnit,
            SelectBuilding,
            ScrollUp,
            ScrollDown,
            ScrollLeft,
            ScrollRight,
            ScrollUpLeft,
            ScrollUpRight,
            ScrollDownLeft,
            ScrollDownRight
        }

        private void Start()
        {
            // Get input device
            mouse = Mouse.current;

            // Set default cursor
            SetCursor(CursorState.Normal);

            // Auto-find references if not set
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (selectionManager == null)
            {
                selectionManager = Object.FindAnyObjectByType<UnitSelectionManager>();
            }
        }

        private void Update()
        {
            if (mouse == null || selectionManager == null || mainCamera == null)
                return;

            UpdateCursorState();
        }

        private void UpdateCursorState()
        {
            // Check what's under the cursor
            Vector2 mousePosition = mouse.position.ReadValue();

            // Priority 0: Check for edge scrolling (highest priority)
            CursorState edgeScrollState = CheckEdgeScrolling(mousePosition);
            if (edgeScrollState != CursorState.Normal)
            {
                SetCursor(edgeScrollState);
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(mousePosition);

            // Priority 1: Check for buildings (BuildingSelectable component)
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
            {
                GameObject hitObject = hit.collider.gameObject;

                // Check for BuildingSelectable (highest priority)
                var buildingSelectable = hitObject.GetComponent<RTS.Buildings.BuildingSelectable>();
                if (buildingSelectable != null)
                {
                    SetCursor(CursorState.SelectBuilding);
                    return;
                }

                // Check for UnitSelectable
                var unitSelectable = hitObject.GetComponent<RTS.Units.UnitSelectable>();
                if (unitSelectable != null)
                {
                    SetCursor(CursorState.SelectUnit);
                    return;
                }
            }

            // If no units selected, use normal cursor for everything else
            if (selectionManager.SelectionCount == 0)
            {
                SetCursor(CursorState.Normal);
                return;
            }

            // Priority 2: Check for units (for attack commands)
            if (Physics.Raycast(ray, out RaycastHit unitHit, raycastDistance, unitLayer))
            {
                GameObject hitObject = unitHit.collider.gameObject;

                // Check if it's an enemy
                if (IsEnemy(hitObject))
                {
                    // Check if any selected unit can attack
                    if (AnySelectedUnitCanAttack())
                    {
                        SetCursor(CursorState.Attack);
                        return;
                    }
                    else
                    {
                        SetCursor(CursorState.Invalid);
                        return;
                    }
                }
            }

            // Priority 3: Check for ground (move command)
            if (Physics.Raycast(ray, out RaycastHit groundHit, raycastDistance, groundLayer))
            {
                // Hovering over ground with units selected = move cursor
                SetCursor(CursorState.Move);
                return;
            }

            // Nothing under cursor
            SetCursor(CursorState.Normal);
        }

        private CursorState CheckEdgeScrolling(Vector2 mousePosition)
        {
            // Calculate viewport boundaries in screen space
            float viewportBottomY = Screen.height * viewportYOffset;
            float viewportTopY = Screen.height * (viewportYOffset + viewportHeight);

            bool isAtTop = mousePosition.y >= viewportTopY - edgeScrollBorderThickness;
            bool isAtBottom = mousePosition.y <= viewportBottomY + edgeScrollBorderThickness;
            bool isAtLeft = mousePosition.x <= edgeScrollBorderThickness;
            bool isAtRight = mousePosition.x >= Screen.width - edgeScrollBorderThickness;

            // Check diagonal edges first
            if (isAtTop && isAtLeft) return CursorState.ScrollUpLeft;
            if (isAtTop && isAtRight) return CursorState.ScrollUpRight;
            if (isAtBottom && isAtLeft) return CursorState.ScrollDownLeft;
            if (isAtBottom && isAtRight) return CursorState.ScrollDownRight;

            // Check cardinal edges
            if (isAtTop) return CursorState.ScrollUp;
            if (isAtBottom) return CursorState.ScrollDown;
            if (isAtLeft) return CursorState.ScrollLeft;
            if (isAtRight) return CursorState.ScrollRight;

            return CursorState.Normal;
        }

        private bool IsEnemy(GameObject obj)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            return obj.layer == enemyLayer;
        }

        private bool AnySelectedUnitCanAttack()
        {
            foreach (var unit in selectionManager.SelectedUnits)
            {
                if (unit == null) continue;

                // Check if unit has combat component
                var combat = unit.GetComponent<UnitCombat>();
                if (combat != null && combat.enabled)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetCursor(CursorState newState)
        {
            if (currentState == newState)
                return;

            currentState = newState;

            Texture2D texture;
            Vector2 hotspot;

            switch (newState)
            {
                case CursorState.Move:
                    texture = moveCursor;
                    hotspot = moveHotspot;
                    break;

                case CursorState.Attack:
                    texture = attackCursor;
                    hotspot = attackHotspot;
                    break;

                case CursorState.Invalid:
                    texture = invalidCursor;
                    hotspot = invalidHotspot;
                    break;

                case CursorState.SelectUnit:
                    texture = selectUnitCursor;
                    hotspot = selectUnitHotspot;
                    break;

                case CursorState.SelectBuilding:
                    texture = selectBuildingCursor;
                    hotspot = selectBuildingHotspot;
                    break;

                case CursorState.ScrollUp:
                    texture = scrollUpCursor;
                    hotspot = scrollHotspot;
                    break;

                case CursorState.ScrollDown:
                    texture = scrollDownCursor;
                    hotspot = scrollHotspot;
                    break;

                case CursorState.ScrollLeft:
                    texture = scrollLeftCursor;
                    hotspot = scrollHotspot;
                    break;

                case CursorState.ScrollRight:
                    texture = scrollRightCursor;
                    hotspot = scrollHotspot;
                    break;

                case CursorState.ScrollUpLeft:
                    texture = scrollUpLeftCursor;
                    hotspot = scrollHotspot;
                    break;

                case CursorState.ScrollUpRight:
                    texture = scrollUpRightCursor;
                    hotspot = scrollHotspot;
                    break;

                case CursorState.ScrollDownLeft:
                    texture = scrollDownLeftCursor;
                    hotspot = scrollHotspot;
                    break;

                case CursorState.ScrollDownRight:
                    texture = scrollDownRightCursor;
                    hotspot = scrollHotspot;
                    break;

                case CursorState.Normal:
                default:
                    texture = normalCursor;
                    hotspot = normalHotspot;
                    break;
            }

            // If texture is null, use hardware cursor
            if (texture == null)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
            }
        }

        private void OnDisable()
        {
            // Reset to default cursor when disabled
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        private void OnDestroy()
        {
            // Reset to default cursor when destroyed
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
