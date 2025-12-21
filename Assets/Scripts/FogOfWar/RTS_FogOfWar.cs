using System.Collections.Generic;
using UnityEngine;
using RTS.Core;

namespace RTS.FogOfWar
{
    public class RTS_FogOfWar : MonoBehaviour
    {
        [Header("Play Area")]
        [Tooltip("Reference to PlayAreaBounds. If not set, will try to find one in the scene.")]
        [SerializeField] private PlayAreaBounds playAreaBounds;

        [Header("Grid")]
        [Tooltip("Resolution of the fog grid. Higher = more detailed fog but more processing.")]
        public int gridResolution = 128;
        [Tooltip("World units per fog grid cell. Calculated automatically if usePlayAreaBounds is true.")]
        public float unitScale = 1f;

        [Header("Revealers")]
        [SerializeField] private List<FogRevealer> fogRevealers = new();

        [Header("Terrain Binding")]
        [SerializeField] private TerrainFogBinder terrainFogBinder;

        [Header("Fog")]
        public bool keepRevealedTiles = false;

        public Shadowcaster shadowcaster { get; private set; } = new Shadowcaster();

        private Texture2D fogTexture;
        private byte[] fogData;

        // Cached play area values
        private Vector3 playAreaCenter;
        private Vector2 playAreaSize;
        private int levelDimensionX;
        private int levelDimensionY;

        // Public accessors for other systems
        public PlayAreaBounds PlayArea => playAreaBounds;
        public Vector3 PlayAreaCenter => playAreaCenter;
        public Vector2 PlayAreaSize => playAreaSize;
        public int GridWidth => levelDimensionX;
        public int GridHeight => levelDimensionY;

        // ================================
        // UNITY
        // ================================

        void Awake()
        {
            if (terrainFogBinder == null)
            {
                Debug.LogError("[RTS_FogOfWar] TerrainFogBinder not assigned");
                enabled = false;
                return;
            }

            // Find PlayAreaBounds if not assigned
            if (playAreaBounds == null)
            {
                playAreaBounds = PlayAreaBounds.Instance;
            }

            // Initialize grid dimensions based on play area
            InitializeGridFromPlayArea();

            shadowcaster.Initialize(levelDimensionX, levelDimensionY);
            CreateFogTexture();

            // Pass play area info to terrain binder
            terrainFogBinder.SetPlayAreaBounds(playAreaBounds);
            terrainFogBinder.fogTexture = fogTexture;
            terrainFogBinder.ApplyFog();
        }

        private void InitializeGridFromPlayArea()
        {
            if (playAreaBounds != null)
            {
                playAreaCenter = playAreaBounds.Center;
                playAreaSize = playAreaBounds.Size;

                // Calculate unit scale to fit grid resolution to play area
                float maxDimension = Mathf.Max(playAreaSize.x, playAreaSize.y);
                unitScale = maxDimension / gridResolution;

                // Use square grid based on resolution
                levelDimensionX = gridResolution;
                levelDimensionY = gridResolution;
            }
            else
            {
                // Fallback: assume center at origin, use grid resolution
                playAreaCenter = Vector3.zero;
                playAreaSize = new Vector2(gridResolution * unitScale, gridResolution * unitScale);
                levelDimensionX = gridResolution;
                levelDimensionY = gridResolution;
                Debug.LogWarning("[RTS_FogOfWar] No PlayAreaBounds found. Using default grid centered at origin.");
            }
        }

        void Update()
        {
            UpdateFog();
        }

        // ================================
        // FOG CORE
        // ================================

        void CreateFogTexture()
        {
            fogTexture = new Texture2D(
                levelDimensionX,
                levelDimensionY,
                TextureFormat.R8,
                false,
                true
            );

            fogTexture.wrapMode = TextureWrapMode.Clamp;
            fogTexture.filterMode = FilterMode.Point;

            fogData = new byte[levelDimensionX * levelDimensionY];
            System.Array.Fill(fogData, (byte)0);

            fogTexture.LoadRawTextureData(fogData);
            fogTexture.Apply(false, false);
        }

        void UpdateFog()
        {
            shadowcaster.Reset(keepRevealedTiles);

            foreach (var r in fogRevealers)
            {
                if (!r.IsValid) continue;

                Vector2Int cell = WorldToLevel(r._RevealerTransform.position);
                int radius = Mathf.RoundToInt(r._SightRange / unitScale);

                shadowcaster.Reveal(cell, radius);
            }

            WriteFogTexture();
            terrainFogBinder.ApplyFog();
        }

        void WriteFogTexture()
        {
            int w = levelDimensionX;
            int h = levelDimensionY;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    fogData[y * w + x] =
                        shadowcaster.fogField[x, y] == Shadowcaster.TileVisibility.Revealed
                        ? (byte)255
                        : (byte)0;
                }
            }

            fogTexture.LoadRawTextureData(fogData);
            fogTexture.Apply(false, false);
        }

        // ================================
        // GRID CONVERSION
        // ================================

        /// <summary>
        /// Convert world position to fog grid cell coordinates.
        /// Grid is centered on the PlayAreaBounds center.
        /// </summary>
        public Vector2Int WorldToLevel(Vector3 world)
        {
            // Calculate offset from play area center
            float offsetX = world.x - playAreaCenter.x;
            float offsetZ = world.z - playAreaCenter.z;

            // Convert to grid coordinates (center of grid is at levelDimension/2)
            return new Vector2Int(
                Mathf.FloorToInt(offsetX / unitScale + levelDimensionX * 0.5f),
                Mathf.FloorToInt(offsetZ / unitScale + levelDimensionY * 0.5f)
            );
        }

        /// <summary>
        /// Convert fog grid cell to world position.
        /// </summary>
        public Vector3 LevelToWorld(Vector2Int cell)
        {
            float worldX = (cell.x - levelDimensionX * 0.5f) * unitScale + playAreaCenter.x;
            float worldZ = (cell.y - levelDimensionY * 0.5f) * unitScale + playAreaCenter.z;
            return new Vector3(worldX, 0, worldZ);
        }

        public bool CheckLevelGridRange(Vector2Int p)
        {
            return p.x >= 0 && p.y >= 0 &&
                   p.x < levelDimensionX &&
                   p.y < levelDimensionY;
        }

        // ================================
        // VISIBILITY API (CANONICAL)
        // ================================

        public bool CheckVisibility(Vector3 worldPos)
        {
            Vector2Int p = WorldToLevel(worldPos);

            if (!CheckLevelGridRange(p))
                return false;

            return shadowcaster.IsVisible(p);
        }

        // ================================
        // COMPATIBILITY OVERLOADS
        // ================================

        // Used by HP bars, buildings, walls, etc.
        public bool CheckVisibility(Vector3 worldPos, int unused)
        {
            return CheckVisibility(worldPos);
        }

        public bool CheckWorldGridRange(Vector3 worldPos)
        {
            Vector2Int p = WorldToLevel(worldPos);
            return CheckLevelGridRange(p);
        }

        // ================================
        // REVEALERS
        // ================================

        public int AddFogRevealer(FogRevealer r)
        {
            fogRevealers.Add(r);
            return fogRevealers.Count - 1;
        }

        public void RemoveFogRevealer(int index)
        {
            if (index >= 0 && index < fogRevealers.Count)
                fogRevealers.RemoveAt(index);
        }

        [System.Serializable]
        public class FogRevealer
        {
            public Transform _RevealerTransform;
            public int _SightRange;
            public bool IsValid => _RevealerTransform != null;

            public FogRevealer(Transform t, int r)
            {
                _RevealerTransform = t;
                _SightRange = r;
            }
        }
    }
}
