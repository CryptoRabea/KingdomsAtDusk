using UnityEngine;
using UnityEngine.EventSystems;

namespace RTS.UI.Minimap
{
    /// <summary>
    /// Handles dragging on the minimap to move the camera.
    /// Attach this to the MiniMap root GameObject or viewport indicator.
    /// Supports both clicking and dragging for camera movement.
    /// </summary>
    public class MinimapDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [Header("References")]
        [SerializeField] private MiniMapControllerPro minimapController;
        [SerializeField] private RectTransform minimapRect;

        [Header("Drag Settings")]
        [Tooltip("Enable dragging to move camera")]
        [SerializeField] private bool enableDrag = true;

        [Tooltip("Minimum drag distance before camera starts moving (prevents accidental drags)")]
        [SerializeField] private float dragThreshold = 5f;

        [Tooltip("Update camera continuously while dragging")]
        [SerializeField] private bool continuousDrag = true;

        #pragma warning disable CS0414 // Field is assigned but never used - reserved for future visual feedback feature
        [Tooltip("Visual feedback while dragging")]
        [SerializeField] private bool showDragFeedback = true;
        #pragma warning restore CS0414

        [Tooltip("Cursor texture while dragging (optional)")]
        [SerializeField] private Texture2D dragCursor;

        private bool isDragging = false;
        private Vector2 dragStartPosition;
        private float totalDragDistance = 0f;
        private Camera eventCamera;

        private void Awake()
        {
            // Auto-find references if not assigned
            if (minimapController == null)
            {
                minimapController = GetComponent<MiniMapControllerPro>();
                if (minimapController == null)
                {
                    minimapController = GetComponentInParent<MiniMapControllerPro>();
                }
            }

            if (minimapRect == null)
            {
                minimapRect = GetComponent<RectTransform>();
            }

            if (minimapController == null)
            {
                enabled = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Store the camera for coordinate conversion
            eventCamera = eventData.pressEventCamera;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!enableDrag) return;

            isDragging = true;
            dragStartPosition = eventData.position;
            totalDragDistance = 0f;

            // Change cursor if specified
            if (dragCursor != null)
            {
                Cursor.SetCursor(dragCursor, Vector2.zero, CursorMode.Auto);
            }

        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!enableDrag || !isDragging) return;

            // Calculate drag distance
            totalDragDistance += eventData.delta.magnitude;

            // Only start moving camera after threshold
            if (totalDragDistance < dragThreshold) return;

            // Move camera to drag position
            if (continuousDrag)
            {
                MoveCamera(eventData.position, eventData.pressEventCamera);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!enableDrag) return;

            // Move camera to final position if not using continuous drag
            if (!continuousDrag && totalDragDistance >= dragThreshold)
            {
                MoveCamera(eventData.position, eventData.pressEventCamera);
            }

            isDragging = false;
            totalDragDistance = 0f;

            // Reset cursor
            if (dragCursor != null)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }

        }

        private void MoveCamera(Vector2 screenPosition, Camera eventCamera)
        {
            if (minimapController == null) return;

            // Convert screen position to world position
            Vector3 worldPos = minimapController.ScreenToWorldPosition(screenPosition, eventCamera);

            if (worldPos != Vector3.zero)
            {
                minimapController.MoveCameraTo(worldPos);
            }
        }

        /// <summary>
        /// Enable or disable dragging at runtime.
        /// </summary>
        public void SetDragEnabled(bool enabled)
        {
            enableDrag = enabled;
        }

        /// <summary>
        /// Check if currently dragging.
        /// </summary>
        public bool IsDragging()
        {
            return isDragging;
        }
    }
}
