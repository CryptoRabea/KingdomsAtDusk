using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RTS.Core;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace RTS.FogOfWar
{
    public class RTS_FogOfWar : MonoBehaviour
    {
        [Header("Play Area")]
        [Tooltip("Reference to PlayAreaBounds. If set, replaces levelMidPoint.")]
        [SerializeField] private PlayAreaBounds playAreaBounds;

        [Header("Grid (Auto-calculated from Play Area)")]
        [Tooltip("Grid resolution. Size is calculated from PlayAreaBounds.")]
        public int levelDimensionX = 128;
        public int levelDimensionY = 128;
        public float unitScale = 1f;

        [Header("Revealers")]
        [SerializeField] private List<FogRevealer> fogRevealers = new();

        [Header("Terrain Binding")]
        [SerializeField] private TerrainFogBinder terrainFogBinder;

        [Header("Fog Properties")]
        [SerializeField] [Range(0, 100)] private float fogPlaneHeight = 1;
        [SerializeField] private Material fogPlaneMaterial = null;
        [SerializeField] private Color fogColor = new Color32(5, 15, 25, 255);
        [SerializeField] [Range(0, 1)] private float fogPlaneAlpha = 1;
        [SerializeField] [Range(1, 5)] private float fogLerpSpeed = 2.5f;
        public bool keepRevealedTiles = false;
        [Range(0, 1)] public float revealedTileOpacity = 0.5f;

        [Header("Performance")]
        [SerializeField] [Range(1, 60)] private float FogRefreshRate = 15f;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = false;

        // Runtime references
        private GameObject fogPlane;
        private Texture2D fogPlaneTextureLerpTarget;
        private Texture2D fogPlaneTextureLerpBuffer;
        private float FogRefreshRateTimer = 0f;

        // Shadowcaster module
        public Shadowcaster shadowcaster { get; private set; } = new Shadowcaster();

        // Level data for obstacle mapping
        public LevelData levelData { get; private set; }

        // Fixed position for fog (from PlayAreaBounds center)
        private Vector3 fixedLevelMidPoint;

        // ================================
        // UNITY LIFECYCLE
        // ================================

        private void Awake()
        {
            // Find PlayAreaBounds if not assigned
            if (playAreaBounds == null)
            {
                playAreaBounds = PlayAreaBounds.Instance;
            }
        }

        private void Start()
        {
            InitializeFromPlayArea();
            InitializeLevelData();
            InitializeFog();
            shadowcaster.Initialize(this);

            if (terrainFogBinder != null)
            {
                terrainFogBinder.SetPlayAreaBounds(playAreaBounds);
            }
        }

        private void Update()
        {
            UpdateFog();
        }

        // ================================
        // INITIALIZATION
        // ================================

        private void InitializeFromPlayArea()
        {
            if (playAreaBounds != null)
            {
                fixedLevelMidPoint = playAreaBounds.Center;

                // Calculate unit scale from play area size and grid resolution
                float maxSize = Mathf.Max(playAreaBounds.Size.x, playAreaBounds.Size.y);
                unitScale = maxSize / Mathf.Max(levelDimensionX, levelDimensionY);
            }
            else
            {
                fixedLevelMidPoint = Vector3.zero;
                Debug.LogWarning("[RTS_FogOfWar] No PlayAreaBounds found. Using origin.");
            }
        }

        private void InitializeLevelData()
        {
            // Create empty level data (no obstacles by default)
            levelData = new LevelData(levelDimensionX, levelDimensionY, unitScale, 0.25f);
        }

        private void InitializeFog()
        {
            if (fogPlaneMaterial == null)
            {
                Debug.LogError("[RTS_FogOfWar] Fog plane material not assigned!");
                return;
            }

            fogPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            fogPlane.name = "[RUNTIME] Fog_Plane";

            fogPlane.transform.position = new Vector3(
                fixedLevelMidPoint.x,
                fixedLevelMidPoint.y + fogPlaneHeight,
                fixedLevelMidPoint.z);

            fogPlane.transform.localScale = new Vector3(
                (levelDimensionX * unitScale) / 10.0f,
                1,
                (levelDimensionY * unitScale) / 10.0f);

            fogPlaneTextureLerpTarget = new Texture2D(levelDimensionX, levelDimensionY);
            fogPlaneTextureLerpBuffer = new Texture2D(levelDimensionX, levelDimensionY);
            fogPlaneTextureLerpBuffer.wrapMode = TextureWrapMode.Clamp;
            fogPlaneTextureLerpBuffer.filterMode = FilterMode.Bilinear;

            fogPlane.GetComponent<MeshRenderer>().material = new Material(fogPlaneMaterial);
            fogPlane.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", fogPlaneTextureLerpBuffer);
            fogPlane.GetComponent<MeshCollider>().enabled = false;
        }

        // ================================
        // FOG UPDATE
        // ================================

        private void UpdateFog()
        {
            FogRefreshRateTimer += Time.deltaTime;

            if (FogRefreshRateTimer < 1f / FogRefreshRate)
            {
                UpdateFogPlaneTextureBuffer();
                return;
            }

            FogRefreshRateTimer -= 1f / FogRefreshRate;

            // Check if any revealer has moved
            bool needsUpdate = false;
            foreach (var fogRevealer in fogRevealers)
            {
                if (!fogRevealer.IsValid) continue;

                if (!fogRevealer._UpdateOnlyOnMove)
                {
                    needsUpdate = true;
                    break;
                }

                Vector2Int currentCoords = fogRevealer.GetCurrentLevelCoordinates(this);
                if (currentCoords != fogRevealer._LastSeenAt)
                {
                    needsUpdate = true;
                    break;
                }
            }

            if (!needsUpdate && fogRevealers.Count > 0) return;

            UpdateFogField();
            UpdateFogPlaneTextureBuffer();
        }

        private void UpdateFogField()
        {
            shadowcaster.ResetTileVisibility();

            foreach (var fogRevealer in fogRevealers)
            {
                if (!fogRevealer.IsValid) continue;

                fogRevealer.GetCurrentLevelCoordinates(this);
                shadowcaster.ProcessLevelData(
                    fogRevealer._CurrentLevelCoordinates,
                    Mathf.RoundToInt(fogRevealer._SightRange / unitScale));
            }

            UpdateFogPlaneTextureTarget();
        }

        private void UpdateFogPlaneTextureBuffer()
        {
            if (fogPlaneTextureLerpBuffer == null || fogPlaneTextureLerpTarget == null) return;

            Color32[] bufferPixels = fogPlaneTextureLerpBuffer.GetPixels32();
            Color32[] targetPixels = fogPlaneTextureLerpTarget.GetPixels32();

            if (bufferPixels.Length != targetPixels.Length) return;

            for (int i = 0; i < bufferPixels.Length; i++)
            {
                bufferPixels[i] = Color.Lerp(bufferPixels[i], targetPixels[i], fogLerpSpeed * Time.deltaTime);
            }

            fogPlaneTextureLerpBuffer.SetPixels32(bufferPixels);
            fogPlaneTextureLerpBuffer.Apply();
        }

        private void UpdateFogPlaneTextureTarget()
        {
            if (fogPlane == null) return;

            fogPlane.GetComponent<MeshRenderer>().material.SetColor("_Color", fogColor);
            fogPlaneTextureLerpTarget.SetPixels32(shadowcaster.fogField.GetColors(fogPlaneAlpha, this));
            fogPlaneTextureLerpTarget.Apply();
        }

        // ================================
        // COORDINATE CONVERSION
        // ================================

        public Vector2Int WorldToLevel(Vector3 worldCoordinates)
        {
            Vector2Int unitCoords = GetUnitVector(worldCoordinates);
            return new Vector2Int(
                unitCoords.x + (levelDimensionX / 2),
                unitCoords.y + (levelDimensionY / 2));
        }

        public Vector3 GetWorldVector(Vector2Int levelCoordinates)
        {
            return new Vector3(
                GetWorldX(levelCoordinates.x),
                0,
                GetWorldY(levelCoordinates.y));
        }

        public Vector2Int GetUnitVector(Vector3 worldCoordinates)
        {
            return new Vector2Int(
                GetUnitX(worldCoordinates.x),
                GetUnitY(worldCoordinates.z));
        }

        public float GetWorldX(int xValue)
        {
            return fixedLevelMidPoint.x + (xValue - levelDimensionX / 2f) * unitScale;
        }

        public float GetWorldY(int yValue)
        {
            return fixedLevelMidPoint.z + (yValue - levelDimensionY / 2f) * unitScale;
        }

        public int GetUnitX(float xValue)
        {
            return Mathf.RoundToInt((xValue - fixedLevelMidPoint.x) / unitScale);
        }

        public int GetUnitY(float yValue)
        {
            return Mathf.RoundToInt((yValue - fixedLevelMidPoint.z) / unitScale);
        }

        // ================================
        // VISIBILITY API
        // ================================

        public bool CheckLevelGridRange(Vector2Int levelCoordinates)
        {
            return levelCoordinates.x >= 0 &&
                   levelCoordinates.x < levelDimensionX &&
                   levelCoordinates.y >= 0 &&
                   levelCoordinates.y < levelDimensionY;
        }

        public bool CheckWorldGridRange(Vector3 worldCoordinates)
        {
            return CheckLevelGridRange(WorldToLevel(worldCoordinates));
        }

        public bool CheckVisibility(Vector3 worldCoordinates, int additionalRadius = 0)
        {
            Vector2Int levelCoords = WorldToLevel(worldCoordinates);

            if (!CheckLevelGridRange(levelCoords)) return false;

            if (additionalRadius == 0)
            {
                return shadowcaster.fogField[levelCoords.x][levelCoords.y] ==
                    Shadowcaster.LevelColumn.ETileVisibility.Revealed;
            }

            for (int x = -additionalRadius; x <= additionalRadius; x++)
            {
                for (int y = -additionalRadius; y <= additionalRadius; y++)
                {
                    Vector2Int checkCoords = new Vector2Int(levelCoords.x + x, levelCoords.y + y);
                    if (CheckLevelGridRange(checkCoords))
                    {
                        if (shadowcaster.fogField[checkCoords.x][checkCoords.y] ==
                            Shadowcaster.LevelColumn.ETileVisibility.Revealed)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        // ================================
        // REVEALER MANAGEMENT
        // ================================

        public int AddFogRevealer(FogRevealer fogRevealer)
        {
            fogRevealers.Add(fogRevealer);
            return fogRevealers.Count - 1;
        }

        public void RemoveFogRevealer(int revealerIndex)
        {
            if (revealerIndex >= 0 && revealerIndex < fogRevealers.Count)
            {
                fogRevealers.RemoveAt(revealerIndex);
            }
        }

        public void ReplaceFogRevealerList(List<FogRevealer> newList)
        {
            fogRevealers = newList;
        }

        // ================================
        // LEVEL DATA CLASS
        // ================================

        [System.Serializable]
        public class LevelData
        {
            public int levelDimensionX;
            public int levelDimensionY;
            public float unitScale;
            public float scanSpacingPerUnit;
            private List<LevelColumn> columns = new List<LevelColumn>();

            public LevelData(int dimX, int dimY, float scale, float spacing)
            {
                levelDimensionX = dimX;
                levelDimensionY = dimY;
                unitScale = scale;
                scanSpacingPerUnit = spacing;

                for (int x = 0; x < dimX; x++)
                {
                    columns.Add(new LevelColumn(dimY));
                }
            }

            public LevelColumn this[int x]
            {
                get => (x >= 0 && x < columns.Count) ? columns[x] : null;
            }
        }

        [System.Serializable]
        public class LevelColumn
        {
            public enum ETileState { Empty, Obstacle }
            private List<ETileState> tiles = new List<ETileState>();

            public LevelColumn(int height)
            {
                for (int i = 0; i < height; i++)
                    tiles.Add(ETileState.Empty);
            }

            public ETileState this[int y]
            {
                get => (y >= 0 && y < tiles.Count) ? tiles[y] : ETileState.Empty;
                set { if (y >= 0 && y < tiles.Count) tiles[y] = value; }
            }
        }

        // ================================
        // FOG REVEALER CLASS
        // ================================

        [System.Serializable]
        public class FogRevealer
        {
            public Transform _RevealerTransform;
            public int _SightRange;
            public bool _UpdateOnlyOnMove;
            public Vector2Int _CurrentLevelCoordinates;
            public Vector2Int _LastSeenAt;

            public bool IsValid => _RevealerTransform != null;

            public FogRevealer(Transform transform, int sightRange, bool updateOnlyOnMove = true)
            {
                _RevealerTransform = transform;
                _SightRange = sightRange;
                _UpdateOnlyOnMove = updateOnlyOnMove;
                _CurrentLevelCoordinates = Vector2Int.zero;
                _LastSeenAt = Vector2Int.zero;
            }

            public Vector2Int GetCurrentLevelCoordinates(RTS_FogOfWar fogWar)
            {
                _LastSeenAt = _CurrentLevelCoordinates;
                _CurrentLevelCoordinates = fogWar.WorldToLevel(_RevealerTransform.position);
                return _CurrentLevelCoordinates;
            }
        }

        // ================================
        // GIZMOS
        // ================================

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !drawGizmos) return;

            for (int x = 0; x < levelDimensionX; x++)
            {
                for (int y = 0; y < levelDimensionY; y++)
                {
                    Vector3 pos = new Vector3(GetWorldX(x), fixedLevelMidPoint.y, GetWorldY(y));

                    if (shadowcaster.fogField[x][y] == Shadowcaster.LevelColumn.ETileVisibility.Revealed)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawSphere(pos, unitScale / 3f);
                    }
                }
            }
        }
#endif
    }

    // ================================
    // CUSTOM ATTRIBUTES
    // ================================

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public string _BaseCondition { get; private set; }
        public ShowIfAttribute(string baseCondition) { _BaseCondition = baseCondition; }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class BigHeaderAttribute : PropertyAttribute
    {
        public string _Text { get; private set; }
        public BigHeaderAttribute(string text) { _Text = text; }
    }
}
