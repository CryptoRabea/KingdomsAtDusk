using UnityEngine;

namespace RTS.UI
{
    /// <summary>
    /// Manages custom cursor textures for different UI states
    /// </summary>
    public class CustomCursorController : MonoBehaviour
    {
        [Header("Cursor Textures")]
        [SerializeField] private Texture2D defaultCursor;
        [SerializeField] private Texture2D hoverCursor;
        [SerializeField] private Texture2D selectCursor;
        [SerializeField] private Texture2D deselectCursor;
        [SerializeField] private Vector2 cursorHotspot = new Vector2(0, 0);

        private static CustomCursorController instance;
        public static CustomCursorController Instance => instance;

        private CursorState currentState = CursorState.Default;

        public enum CursorState
        {
            Default,
            Hover,
            Select,
            Deselect
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void Start()
        {
            SetCursorState(CursorState.Default);
        }

        public void SetCursorState(CursorState state)
        {
            currentState = state;

            Texture2D cursorTexture = GetCursorTexture(state);
            if (cursorTexture != null)
            {
                Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }

        private Texture2D GetCursorTexture(CursorState state)
        {
            switch (state)
            {
                case CursorState.Hover:
                    return hoverCursor;
                case CursorState.Select:
                    return selectCursor;
                case CursorState.Deselect:
                    return deselectCursor;
                default:
                    return defaultCursor;
            }
        }

        public void ResetCursor()
        {
            SetCursorState(CursorState.Default);
        }

        private void OnDestroy()
        {
            // Reset cursor when this is destroyed
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
