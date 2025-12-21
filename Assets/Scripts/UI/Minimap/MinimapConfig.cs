using UnityEngine;

namespace RTS.UI.Minimap
{
    /// <summary>
    /// ScriptableObject configuration for the minimap system.
    /// Provides centralized settings for performance tuning and visual customization.
    ///
    /// NOTE: If a PlayAreaBounds component is present in the scene, it will override
    /// the World Bounds settings below. Use PlayAreaBounds for a visual gizmo-based
    /// approach to defining the play area.
    /// </summary>
    [CreateAssetMenu(fileName = "MinimapConfig", menuName = "RTS/UI/Minimap Config")]
    public class MinimapConfig : ScriptableObject
    {
        [Header("World Bounds (Fallback)")]
        [Tooltip("Minimum world coordinates (X, Z). Overridden by PlayAreaBounds if present.")]
        public Vector2 worldMin = new Vector2(-1000f, -1000f);

        [Tooltip("Maximum world coordinates (X, Z). Overridden by PlayAreaBounds if present.")]
        public Vector2 worldMax = new Vector2(1000f, 1000f);

        [Header("Render Settings")]
        [Tooltip("Enable real-time world rendering")]
        public bool renderWorldMap = true;

        [Tooltip("Resolution of the minimap render texture")]
        public int renderTextureSize = 512;

        [Tooltip("Height of the minimap camera above world center")]
        public float minimapCameraHeight = 500f;

        [Tooltip("Layers to render on the minimap")]
        public LayerMask minimapLayers = -1;

        [Tooltip("Background color of the minimap")]
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);

        [Header("Camera Movement")]
        [Tooltip("Speed multiplier for camera movement")]
        [Range(0.1f, 10f)]
        public float cameraMoveSpeed = 2f;

        [Tooltip("Use smooth camera movement")]
        public bool useSmoothing = true;

        [Tooltip("Movement animation curve")]
        public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("Minimum movement duration in seconds")]
        [Range(0.1f, 1f)]
        public float minMoveDuration = 0.3f;

        [Tooltip("Maximum movement duration in seconds")]
        [Range(0.5f, 5f)]
        public float maxMoveDuration = 2f;

        [Header("Viewport Indicator")]
        [Tooltip("Color of the camera viewport indicator")]
        public Color viewportColor = new Color(1f, 1f, 1f, 0.3f);

        [Tooltip("Border width of the viewport indicator")]
        [Range(1f, 5f)]
        public float viewportBorderWidth = 2f;

        [Header("Building Markers")]
        [Tooltip("Color for friendly building markers")]
        public Color friendlyBuildingColor = Color.blue;

        [Tooltip("Color for enemy building markers")]
        public Color enemyBuildingColor = Color.red;

        [Tooltip("Size of building markers in pixels")]
        [Range(2f, 20f)]
        public float buildingMarkerSize = 5f;

        [Tooltip("Initial pool size for building markers")]
        public int buildingMarkerPoolSize = 50;

        [Header("Unit Markers")]
        [Tooltip("Color for friendly unit markers")]
        public Color friendlyUnitColor = Color.green;

        [Tooltip("Color for enemy unit markers")]
        public Color enemyUnitColor = Color.red;

        [Tooltip("Size of unit markers in pixels")]
        [Range(1f, 10f)]
        public float unitMarkerSize = 3f;

        [Tooltip("Initial pool size for unit markers")]
        public int unitMarkerPoolSize = 200;

        [Header("Performance Settings")]
        [Tooltip("Update markers every N frames (1 = every frame, 2 = every other frame)")]
        [Range(1, 10)]
        public int markerUpdateInterval = 2;

        [Tooltip("Update viewport indicator every N frames")]
        [Range(1, 5)]
        public int viewportUpdateInterval = 1;

        [Tooltip("Maximum markers to update per frame (0 = unlimited)")]
        [Range(0, 500)]
        public int maxMarkersPerFrame = 100;

        [Tooltip("Enable marker culling for off-screen markers")]
        public bool enableMarkerCulling = true;

        [Tooltip("Margin for marker culling (normalized 0-1)")]
        [Range(0f, 0.2f)]
        public float cullingMargin = 0.05f;

        [Header("Click Handling")]
        [Tooltip("Enable click-to-move on minimap")]
        public bool enableClickToMove = true;

        [Tooltip("Clamp click positions to world bounds")]
        public bool clampClickPositions = true;

        // Computed properties
        public Vector2 WorldSize => worldMax - worldMin;
        public Vector3 WorldCenter => new Vector3(
            (worldMin.x + worldMax.x) / 2f,
            0f,
            (worldMin.y + worldMax.y) / 2f
        );
    }
}
