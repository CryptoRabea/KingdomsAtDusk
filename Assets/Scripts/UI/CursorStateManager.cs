using RTS.Units;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KingdomsAtDusk.UI
{
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
        [SerializeField] private bool useRotatedArrow = true;

        [SerializeField] private Texture2D scrollUpCursor;
        [SerializeField] private Texture2D scrollDownCursor;
        [SerializeField] private Texture2D scrollLeftCursor;
        [SerializeField] private Texture2D scrollRightCursor;
        [SerializeField] private Texture2D scrollUpLeftCursor;
        [SerializeField] private Texture2D scrollUpRightCursor;
        [SerializeField] private Texture2D scrollDownLeftCursor;
        [SerializeField] private Texture2D scrollDownRightCursor;

        // Cached rotated cursors
        private Texture2D cachedScrollUpCursor;
        private Texture2D cachedScrollDownCursor;
        private Texture2D cachedScrollLeftCursor;
        private Texture2D cachedScrollRightCursor;
        private Texture2D cachedScrollUpLeftCursor;
        private Texture2D cachedScrollUpRightCursor;
        private Texture2D cachedScrollDownLeftCursor;
        private Texture2D cachedScrollDownRightCursor;

        [Header("Cursor Hotspots")]
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

        [Tooltip("Viewport height (0-1). You set 0.76")]
        [SerializeField] private float viewportHeight = 0.76f;

        [Tooltip("Viewport Y offset (0-1). You set 0.24")]
        [SerializeField] private float viewportYOffset = 0.24f;

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
            mouse = Mouse.current;
            SetCursor(CursorState.Normal);

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (selectionManager == null)
                selectionManager = Object.FindAnyObjectByType<UnitSelectionManager>();

            if (useRotatedArrow && baseScrollArrowCursor != null)
                GenerateRotatedCursors();
        }

        private void GenerateRotatedCursors()
        {
            cachedScrollUpCursor = baseScrollArrowCursor;
            cachedScrollRightCursor = RotateTexture(baseScrollArrowCursor, 90);
            cachedScrollDownCursor = RotateTexture(baseScrollArrowCursor, 180);
            cachedScrollLeftCursor = RotateTexture(baseScrollArrowCursor, 270);

            cachedScrollUpRightCursor = RotateTexture(baseScrollArrowCursor, 45);
            cachedScrollDownRightCursor = RotateTexture(baseScrollArrowCursor, 135);
            cachedScrollDownLeftCursor = RotateTexture(baseScrollArrowCursor, 225);
            cachedScrollUpLeftCursor = RotateTexture(baseScrollArrowCursor, 315);
        }

        private Texture2D RotateTexture(Texture2D source, float angleDegrees)
        {
            if (source == null)
                return null;

            int width = source.width;
            int height = source.height;

            Texture2D rotated = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color[] src = source.GetPixels();
            Color32[] dst = new Color32[src.Length];

            float angleRad = angleDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);

            float cx = width * 0.5f;
            float cy = height * 0.5f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;

                    float rx = dx * cos - dy * sin + cx;
                    float ry = dx * sin + dy * cos + cy;

                    int sx = Mathf.Clamp(Mathf.RoundToInt(rx), 0, width - 1);
                    int sy = Mathf.Clamp(Mathf.RoundToInt(ry), 0, height - 1);

                    dst[y * width + x] = src[sy * width + sx];
                }
            }

            rotated.SetPixels32(dst);
            rotated.Apply();
            return rotated;
        }

        private void Update()
        {
            if (mouse == null)
                return;

            UpdateCursorState();
        }

        private void UpdateCursorState()
        {
            Vector2 pos = mouse.position.ReadValue();

            // Edge scrolling first
            CursorState edgeState = CheckEdgeScrolling(pos);
            if (edgeState != CursorState.Normal)
            {
                SetCursor(edgeState);
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(pos);

            // Buildings
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
            {
                if (hit.collider.TryGetComponent<RTS.Buildings.BuildingSelectable>(out _))
                {
                    SetCursor(CursorState.SelectBuilding);
                    return;
                }

                if (hit.collider.TryGetComponent<UnitSelectable>(out _))
                {
                    SetCursor(CursorState.SelectUnit);
                    return;
                }
            }

            // No unit selected → normal
            if (selectionManager.SelectionCount == 0)
            {
                SetCursor(CursorState.Normal);
                return;
            }

            // Attackable unit
            if (Physics.Raycast(ray, out RaycastHit unitHit, raycastDistance, unitLayer))
            {
                if (IsEnemy(unitHit.collider.gameObject))
                {
                    if (AnySelectedUnitCanAttack())
                        SetCursor(CursorState.Attack);
                    else
                        SetCursor(CursorState.Invalid);

                    return;
                }
            }

            // Ground = move
            if (Physics.Raycast(ray, out RaycastHit groundHit, raycastDistance, groundLayer))
            {
                SetCursor(CursorState.Move);
                return;
            }

            SetCursor(CursorState.Normal);
        }

        private CursorState CheckEdgeScrolling(Vector2 pos)
        {
            if (pos.x < 0 || pos.x > Screen.width ||
                pos.y < 0 || pos.y > Screen.height)
                return CursorState.Normal;

            float viewBottom = Screen.height * viewportYOffset;
            float viewTop = Screen.height * (viewportYOffset + viewportHeight);

            // Only apply scrolling inside the viewport area
            if (pos.y < viewBottom || pos.y > viewTop)
                return CursorState.Normal;

            bool top = pos.y >= viewTop - edgeScrollBorderThickness;
            bool bottom = pos.y <= viewBottom + edgeScrollBorderThickness;
            bool left = pos.x <= edgeScrollBorderThickness;
            bool right = pos.x >= Screen.width - edgeScrollBorderThickness;

            if (top && left) return CursorState.ScrollUpLeft;
            if (top && right) return CursorState.ScrollUpRight;
            if (bottom && left) return CursorState.ScrollDownLeft;
            if (bottom && right) return CursorState.ScrollDownRight;

            if (top) return CursorState.ScrollUp;
            if (bottom) return CursorState.ScrollDown;
            if (left) return CursorState.ScrollLeft;
            if (right) return CursorState.ScrollRight;

            return CursorState.Normal;
        }

        private bool IsEnemy(GameObject obj)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            return obj.layer == enemyLayer;
        }

        private bool AnySelectedUnitCanAttack()
        {
            foreach (var u in selectionManager.SelectedUnits)
            {
                if (u == null) continue;

                if (u.TryGetComponent<UnitCombat>(out var combat) && combat.enabled)
                    return true;
            }
            return false;
        }

        private void SetCursor(CursorState newState)
        {
            if (currentState == newState)
                return;

            currentState = newState;

            Texture2D tex;
            Vector2 hot;

            switch (newState)
            {
                case CursorState.Move: tex = moveCursor; hot = moveHotspot; break;
                case CursorState.Attack: tex = attackCursor; hot = attackHotspot; break;
                case CursorState.Invalid: tex = invalidCursor; hot = invalidHotspot; break;
                case CursorState.SelectUnit: tex = selectUnitCursor; hot = selectUnitHotspot; break;
                case CursorState.SelectBuilding: tex = selectBuildingCursor; hot = selectBuildingHotspot; break;

                case CursorState.ScrollUp:
                    tex = useRotatedArrow ? cachedScrollUpCursor : scrollUpCursor; hot = scrollHotspot; break;
                case CursorState.ScrollDown:
                    tex = useRotatedArrow ? cachedScrollDownCursor : scrollDownCursor; hot = scrollHotspot; break;
                case CursorState.ScrollLeft:
                    tex = useRotatedArrow ? cachedScrollLeftCursor : scrollLeftCursor; hot = scrollHotspot; break;
                case CursorState.ScrollRight:
                    tex = useRotatedArrow ? cachedScrollRightCursor : scrollRightCursor; hot = scrollHotspot; break;

                case CursorState.ScrollUpLeft:
                    tex = useRotatedArrow ? cachedScrollUpLeftCursor : scrollUpLeftCursor; hot = scrollHotspot; break;
                case CursorState.ScrollUpRight:
                    tex = useRotatedArrow ? cachedScrollUpRightCursor : scrollUpRightCursor; hot = scrollHotspot; break;
                case CursorState.ScrollDownLeft:
                    tex = useRotatedArrow ? cachedScrollDownLeftCursor : scrollDownLeftCursor; hot = scrollHotspot; break;
                case CursorState.ScrollDownRight:
                    tex = useRotatedArrow ? cachedScrollDownRightCursor : scrollDownRightCursor; hot = scrollHotspot; break;

                default:
                    tex = normalCursor;
                    hot = normalHotspot;
                    break;
            }

            if (tex == null)
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            else
                Cursor.SetCursor(tex, hot, CursorMode.Auto);
        }

        private void OnDisable() => Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        private void OnDestroy() => Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
