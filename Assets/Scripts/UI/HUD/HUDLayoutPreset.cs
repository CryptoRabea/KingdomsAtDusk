using UnityEngine;

namespace RTS.UI.HUD
{
    /// <summary>
    /// Defines a layout preset for the HUD (positioning, anchoring, sizing).
    /// Examples: Classic Warcraft 3 style, Modern RTS, Age of Empires style, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "HUDLayoutPreset", menuName = "RTS/UI/HUD Layout Preset", order = 2)]
    public class HUDLayoutPreset : ScriptableObject
    {
        [Header("Preset Info")]
        public string presetName = "Default Layout";
        [TextArea(2, 4)]
        public string description = "Default HUD layout";

        [Header("Minimap")]
        public AnchorPosition minimapAnchor = AnchorPosition.BottomLeft;
        public Vector2 minimapSize = new Vector2(200, 200);
        public Vector2 minimapOffset = new Vector2(10, 10);

        [Header("Unit Details")]
        public AnchorPosition unitDetailsAnchor = AnchorPosition.BottomCenter;
        public Vector2 unitDetailsSize = new Vector2(400, 150);
        public Vector2 unitDetailsOffset = new Vector2(0, 10);

        [Header("Building Details")]
        public AnchorPosition buildingDetailsAnchor = AnchorPosition.BottomCenter;
        public Vector2 buildingDetailsSize = new Vector2(400, 200);
        public Vector2 buildingDetailsOffset = new Vector2(0, 10);

        [Header("Building HUD")]
        public AnchorPosition buildingHUDAnchor = AnchorPosition.BottomRight;
        public Vector2 buildingHUDSize = new Vector2(300, 250);
        public Vector2 buildingHUDOffset = new Vector2(-10, 10);

        [Header("Inventory")]
        public AnchorPosition inventoryAnchor = AnchorPosition.BottomRight;
        public Vector2 inventorySize = new Vector2(200, 150);
        public Vector2 inventoryOffset = new Vector2(-10, 10);

        [Header("Top Bar")]
        public float topBarHeight = 50;
        public Vector2 topBarOffset = new Vector2(0, 0);

        [Header("Resource Panel (Standalone)")]
        public AnchorPosition resourcePanelAnchor = AnchorPosition.TopCenter;
        public Vector2 resourcePanelSize = new Vector2(400, 60);
        public Vector2 resourcePanelOffset = new Vector2(0, -10);

        [Header("Notifications")]
        public AnchorPosition notificationsAnchor = AnchorPosition.TopCenter;
        public Vector2 notificationsOffset = new Vector2(0, -80);

        /// <summary>
        /// Anchor positions for UI elements
        /// </summary>
        public enum AnchorPosition
        {
            TopLeft,
            TopCenter,
            TopRight,
            MiddleLeft,
            MiddleCenter,
            MiddleRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }

        /// <summary>
        /// Converts anchor position to Unity anchor min/max values
        /// </summary>
        public static void ApplyAnchor(RectTransform rectTransform, AnchorPosition position)
        {
            switch (position)
            {
                case AnchorPosition.TopLeft:
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    rectTransform.pivot = new Vector2(0, 1);
                    break;
                case AnchorPosition.TopCenter:
                    rectTransform.anchorMin = new Vector2(0.5f, 1);
                    rectTransform.anchorMax = new Vector2(0.5f, 1);
                    rectTransform.pivot = new Vector2(0.5f, 1);
                    break;
                case AnchorPosition.TopRight:
                    rectTransform.anchorMin = new Vector2(1, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(1, 1);
                    break;
                case AnchorPosition.MiddleLeft:
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(0, 0.5f);
                    rectTransform.pivot = new Vector2(0, 0.5f);
                    break;
                case AnchorPosition.MiddleCenter:
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case AnchorPosition.MiddleRight:
                    rectTransform.anchorMin = new Vector2(1, 0.5f);
                    rectTransform.anchorMax = new Vector2(1, 0.5f);
                    rectTransform.pivot = new Vector2(1, 0.5f);
                    break;
                case AnchorPosition.BottomLeft:
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0, 0);
                    rectTransform.pivot = new Vector2(0, 0);
                    break;
                case AnchorPosition.BottomCenter:
                    rectTransform.anchorMin = new Vector2(0.5f, 0);
                    rectTransform.anchorMax = new Vector2(0.5f, 0);
                    rectTransform.pivot = new Vector2(0.5f, 0);
                    break;
                case AnchorPosition.BottomRight:
                    rectTransform.anchorMin = new Vector2(1, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    rectTransform.pivot = new Vector2(1, 0);
                    break;
            }
        }
    }
}
