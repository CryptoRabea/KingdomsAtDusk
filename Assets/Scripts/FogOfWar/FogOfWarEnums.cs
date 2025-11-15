using UnityEngine;

namespace KingdomsAtDusk.FogOfWar
{
    /// <summary>
    /// Represents the visibility state of a fog of war cell
    /// </summary>
    public enum VisionState : byte
    {
        Unexplored = 0,  // Never seen before (black)
        Explored = 1,    // Previously seen but not currently visible (dark/grey)
        Visible = 2      // Currently visible (full color)
    }

    /// <summary>
    /// Configuration for fog of war system
    /// </summary>
    [System.Serializable]
    public class FogOfWarConfig
    {
        [Header("Grid Settings")]
        [Tooltip("Size of each grid cell in world units")]
        public float cellSize = 2f;

        [Tooltip("World bounds for the fog of war grid")]
        public Bounds worldBounds = new Bounds(Vector3.zero, new Vector3(2000f, 100f, 2000f));

        [Header("Vision Settings")]
        [Tooltip("Default vision radius for units without specific vision range")]
        public float defaultVisionRadius = 15f;

        [Tooltip("Building vision radius multiplier")]
        public float buildingVisionMultiplier = 1.5f;

        [Tooltip("How often to update fog of war (in seconds)")]
        public float updateInterval = 0.1f;

        [Header("Visual Settings")]
        [Tooltip("Color for unexplored areas")]
        public Color unexploredColor = new Color(0f, 0f, 0f, 1f);

        [Tooltip("Color for explored but not visible areas")]
        public Color exploredColor = new Color(0f, 0f, 0f, 0.6f);

        [Tooltip("Fade speed when transitioning between states")]
        public float fadeSpeed = 2f;

        [Header("Performance")]
        [Tooltip("Maximum number of cells to update per frame")]
        public int maxCellUpdatesPerFrame = 500;

        [Tooltip("Enable debug visualization")]
        public bool enableDebugVisualization = false;
    }
}
