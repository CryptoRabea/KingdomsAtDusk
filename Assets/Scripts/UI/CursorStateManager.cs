using RTS.Units;
using RTS.Buildings.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KingdomsAtDusk.UI
{
    /// <summary>
    /// Manages cursor state and appearance based on hover context and selected units
    /// Shows attack cursor for enemy units and buildings
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
        [SerializeField] private Texture2D baseScrollArrowCursor;
        [Tooltip("If true, rotates the base arrow cursor. If false, uses individual textures.")]
        [SerializeField] private bool useRotatedArrow = true;

        // Individual cursor textures (used when useRotatedArrow is false)
        [SerializeField] private Texture2D scrollUpCursor;
        [SerializeField] private Texture2D scrollDownCursor;
        [SerializeField] private Texture2D scrollLeftCursor;
        [SerializeField] private Texture2D scrollRightCursor;
        [SerializeField] private Texture2D scrollUpLeftCursor;
        [SerializeField] private Texture2D scrollUpRightCursor;
        [SerializeField] private Texture2D scrollDownLeftCursor;
        [SerializeField] private Texture2D scrollDownRightCursor;

        // Cached rotated cursors (generated at runtime if useRotatedArrow is true)
        private Texture2D cachedScrollUpCursor;
        private Texture2D cachedScrollDownCursor;
        private Texture2D cachedScrollLeftCursor;
        private Texture2D cachedScrollRightCursor;
        private Texture2D cachedScrollUpLeftCursor;
        private Texture2D cachedScrollUpRightCursor;
        private Texture2D cachedScrollDownLeftCursor;
        private Texture2D cachedScrollDownRightCursor;

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
        [SerializeField] private LayerMask buildingLayer;

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

            // Generate rotated cursors if using rotated arrow mode
            if (useRotatedArrow && baseScrollArrowCursor != null)
            {
                GenerateRotatedCursors();
            }
        }

        private void GenerateRotatedCursors()
        {
            // Assume base arrow points UP (0 degrees)
            cachedScrollUpCursor = baseScrollArrowCursor; // 0 degrees
            cachedScrollRightCursor = RotateTexture(baseScrollArrowCursor, 90); // 90 degrees clockwise
            cachedScrollDownCursor = RotateTexture(baseScrollArrowCursor, -180); // 180 degrees
            cachedScrollLeftCursor = RotateTexture(baseScrollArrowCursor, -90); // 270 degrees clockwise

            // Diagonals
            cachedScrollUpRightCursor = RotateTexture(baseScrollArrowCursor, 45); // 45 degrees
            cachedScrollDownRightCursor = RotateTexture(baseScrollArrowCursor, 135); // 135 degrees
            cachedScrollDownLeftCursor = RotateTexture(baseScrollArrowCursor, 225); // 225 degrees
            cachedScrollUpLeftCursor = RotateTexture(baseScrollArrowCursor, 315); // 315 degrees
        }

        private Texture2D RotateTexture(Texture2D source, float angleDegrees)
        {
            if (source == null) return null;

            int width = source.width;
            int height = source.height;

            // Create new texture with same dimensions
            Texture2D rotated = new Texture2D(width, height, source.format, false);

            // Get pixel data from source
            Color[] sourcePixels = source.GetPixels();
            Color32[] rotatedPixels = new Color32[width * height];

            // Calculate rotation
            float angleRad = angleDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);

            // Center of rotation
            float centerX = width / 2f;
            float centerY = height / 2f;

            // Rotate pixels
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Translate to origin
                    float dx = x - centerX;
                    float dy = y - centerY;

                    // Rotate
                    float rotatedX = dx * cos - dy * sin + centerX;
                    float rotatedY = dx * sin + dy * cos + centerY;

                    // Sample source pixel (with bounds check)
                    if (rotatedX >= 0 && rotatedX < width && rotatedY >= 0 && rotatedY < height)
                    {
                        int sourceX = Mathf.RoundToInt(rotatedX);
                        int sourceY = Mathf.RoundToInt(rotatedY);
                        rotatedPixels[y * width + x] = sourcePixels[sourceY * width + sourceX];
                    }
                    else
                    {
                        rotatedPixels[y * width + x] = Color.clear;
                    }
                }
            }

            rotated.SetPixels32(rotatedPixels);
            rotated.Apply();

            return rotated;
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
                if (hitObject.TryGetComponent<RTS.Buildings.BuildingSelectable>(out var buildingSelectable))
                {
                    SetCursor(CursorState.SelectBuilding);
                    return;
                }

                // Check for UnitSelectable
                if (hitObject.TryGetComponent<RTS.Units.UnitSelectable>(out var unitSelectable))
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

            // Priority 2.5: Check for enemy buildings (for attack commands)
            if (Physics.Raycast(ray, out RaycastHit buildingHit, raycastDistance, buildingLayer))
            {
                GameObject hitObject = buildingHit.collider.gameObject;

                // Check if it's an enemy building
                if (IsEnemy(hitObject))
                {
                    // Check if building has health (is attackable)
                    var buildingHealth = hitObject.GetComponent<BuildingHealth>();
                    if (buildingHealth != null && !buildingHealth.IsDead)
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
            // Don't show edge scroll cursor if mouse is outside game view (especially in Unity Editor)
            if (mousePosition.x < 0 || mousePosition.x > Screen.width ||
                mousePosition.y < 0 || mousePosition.y > Screen.height)
            {
                return CursorState.Normal;
            }

            // Calculate viewport boundaries in screen space
            float viewportBottomY = Screen.height * viewportYOffset;
            float viewportTopY = Screen.height * (viewportYOffset + viewportHeight);

            // Only show edge scroll cursors if mouse is within viewport Y bounds
            if (mousePosition.y < viewportBottomY || mousePosition.y > viewportTopY)
            {
                return CursorState.Normal;
            }

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
                    texture = useRotatedArrow ? cachedScrollUpCursor : scrollUpCursor;
                    hotspot = scrollHotspot;
                    break;

                case CursorState.ScrollDown:
                    texture = useRotatedArrow ? cachedScrollDownCursor : scrollDownCursor;
                    hotspot = scrollHotspot;
                    break;

                case CursorState.ScrollLeft:
                    texture = useRotatedArrow ? cachedScrollLeftCursor : scrollLeftCursor;
                    hotspot = scrollHotspot;
                    break;

                case CursorState.ScrollRight:
                    texture = useRotatedArrow ? cachedScrollRightCursor : scrollRightCursor;
                    hotspot = scrollHotspot;
                    break;

                case CursorState.ScrollUpLeft:
                    texture = useRotatedArrow ? cachedScrollUpLeftCursor : scrollUpLeftCursor;
                    hotspot = scrollHotspot;
                    break;

                case CursorState.ScrollUpRight:
                    texture = useRotatedArrow ? cachedScrollUpRightCursor : scrollUpRightCursor;
                    hotspot = scrollHotspot;
                    break;

                case CursorState.ScrollDownLeft:
                    texture = useRotatedArrow ? cachedScrollDownLeftCursor : scrollDownLeftCursor;
                    hotspot = scrollHotspot;
                    break;

                case CursorState.ScrollDownRight:
                    texture = useRotatedArrow ? cachedScrollDownRightCursor : scrollDownRightCursor;
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
