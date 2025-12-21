using System.Collections.Generic;
using UnityEngine;

namespace RTS.FogOfWar
{
    public class RTS_FogOfWar : MonoBehaviour
    {
        [Header("Grid")]
        public int levelDimensionX = 128;
        public int levelDimensionY = 128;
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

            shadowcaster.Initialize(levelDimensionX, levelDimensionY);
            CreateFogTexture();

            terrainFogBinder.fogTexture = fogTexture;
            terrainFogBinder.ApplyFog();
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

        public Vector2Int WorldToLevel(Vector3 world)
        {
            return new Vector2Int(
                Mathf.FloorToInt(world.x / unitScale + levelDimensionX * 0.5f),
                Mathf.FloorToInt(world.z / unitScale + levelDimensionY * 0.5f)
            );
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
