using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RTS.UI
{
    /// <summary>
    /// Adds zoom in/out functionality to a ScrollRect using mouse wheel
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ZoomableScrollRect : MonoBehaviour, IScrollHandler
    {
        [Header("Zoom Settings")]
        [SerializeField] private RectTransform contentToZoom;
        [SerializeField] private float zoomSpeed = 0.1f;
        [SerializeField] private float minZoom = 0.5f;
        [SerializeField] private float maxZoom = 3f;
        [SerializeField] private float currentZoom = 1f;

        [Header("Optional: Show Zoom Level")]
        [SerializeField] private TMPro.TextMeshProUGUI zoomLevelText;

        private ScrollRect scrollRect;
        private Vector3 originalScale;

        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();

            if (contentToZoom == null)
                contentToZoom = scrollRect.content;

            if (contentToZoom != null)
                originalScale = contentToZoom.localScale;
        }

        private void Update()
        {
            // Handle zoom with mouse wheel when mouse is over the panel
            if (RectTransformUtility.RectangleContainsScreenPoint(GetComponent<RectTransform>(), Input.mousePosition))
            {
                float scroll = Input.mouseScrollDelta.y;
                if (scroll != 0)
                {
                    ZoomBy(scroll * zoomSpeed);
                }
            }

            // Optional: Keyboard zoom
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals))
                {
                    ZoomBy(zoomSpeed * 2f);
                }
                if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    ZoomBy(-zoomSpeed * 2f);
                }
            }

            UpdateZoomLevelDisplay();
        }

        public void ZoomBy(float delta)
        {
            currentZoom = Mathf.Clamp(currentZoom + delta, minZoom, maxZoom);
            ApplyZoom();
        }

        public void SetZoom(float zoom)
        {
            currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            ApplyZoom();
        }

        public void ResetZoom()
        {
            currentZoom = 1f;
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (contentToZoom != null)
            {
                contentToZoom.localScale = originalScale * currentZoom;
            }
        }

        private void UpdateZoomLevelDisplay()
        {
            if (zoomLevelText != null)
            {
                zoomLevelText.text = $"Zoom: {(currentZoom * 100f):F0}%";
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            // This is called by the event system, but we're handling it in Update() instead
        }

        // Public API for UI buttons
        public void ZoomIn()
        {
            ZoomBy(zoomSpeed * 2f);
        }

        public void ZoomOut()
        {
            ZoomBy(-zoomSpeed * 2f);
        }
    }
}
