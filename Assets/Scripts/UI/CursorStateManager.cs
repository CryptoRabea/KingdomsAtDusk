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

        [Header("Cursor Hotspots (pixel offset from top-left)")]
        [SerializeField] private Vector2 normalHotspot = Vector2.zero;
        [SerializeField] private Vector2 moveHotspot = new Vector2(16, 16);
        [SerializeField] private Vector2 attackHotspot = new Vector2(16, 16);
        [SerializeField] private Vector2 invalidHotspot = new Vector2(16, 16);

        [Header("References")]
        [SerializeField] private UnitSelectionManager selectionManager;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask unitLayer;

        [Header("Settings")]
        [SerializeField] private float raycastDistance = 1000f;

        private CursorState currentState = CursorState.Normal;
        private Mouse mouse;

        public enum CursorState
        {
            Normal,
            Move,
            Attack,
            Invalid
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
                selectionManager = FindObjectOfType<UnitSelectionManager>();
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
            // If no units selected, use normal cursor
            if (selectionManager.SelectionCount == 0)
            {
                SetCursor(CursorState.Normal);
                return;
            }

            // Check what's under the cursor
            Vector2 mousePosition = mouse.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);

            // First check for units (higher priority)
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
                else
                {
                    // Hovering over friendly/neutral unit
                    SetCursor(CursorState.Normal);
                    return;
                }
            }

            // Then check for ground (lower priority)
            if (Physics.Raycast(ray, out RaycastHit groundHit, raycastDistance, groundLayer))
            {
                // Hovering over ground with units selected = move cursor
                SetCursor(CursorState.Move);
                return;
            }

            // Nothing under cursor
            SetCursor(CursorState.Normal);
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
