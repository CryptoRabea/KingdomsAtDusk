using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RTS.Core;



namespace RTS.FogOfWar
{


    public class RTS_FogOfWar : MonoBehaviour
    {

        [System.Serializable]
        public class LevelData
        {
            public void AddColumn(LevelColumn levelColumn)
            {
                levelRow.Add(levelColumn);
            }

            // Indexer definition
            public LevelColumn this[int index]
            {
                get
                {
                    if (index >= 0 && index < levelRow.Count)
                    {
                        return levelRow[index];
                    }
                    else
                    {

                        return null;
                    }
                }
                set
                {
                    if (index >= 0 && index < levelRow.Count)
                    {
                        levelRow[index] = value;
                    }
                    else
                    {

                        return;
                    }
                }
            }

            // Grid dimensions (calculated from PlayAreaBounds)
            public int levelDimensionX = 0;
            public int levelDimensionY = 0;

            [SerializeField]
            private List<LevelColumn> levelRow = new List<LevelColumn>();
        }



        [System.Serializable]
        public class LevelColumn
        {
            public LevelColumn(IEnumerable<ETileState> stateTiles)
            {
                levelColumn = new List<ETileState>(stateTiles);
            }

            // If I create a separate Tile class, it will impact the size of the save file (but enums will be saved as int)
            public enum ETileState
            {
                Empty,
                Obstacle
            }

            // Indexer definition
            public ETileState this[int index]
            {
                get
                {
                    if (index >= 0 && index < levelColumn.Count)
                    {
                        return levelColumn[index];
                    }
                    else
                    {

                        return ETileState.Empty;
                    }
                }
                set
                {
                    if (index >= 0 && index < levelColumn.Count)
                    {
                        levelColumn[index] = value;
                    }
                    else
                    {

                        return;
                    }
                }
            }

            [SerializeField]
            private List<ETileState> levelColumn = new List<ETileState>();
        }



        [System.Serializable]
        public class FogRevealer
        {
            public FogRevealer(Transform revealerTransform, int sightRange, bool updateOnlyOnMove)
            {
                this.revealerTransform = revealerTransform;
                this.sightRange = sightRange;
                this.updateOnlyOnMove = updateOnlyOnMove;
            }

            public Vector2Int GetCurrentLevelCoordinates(RTS_FogOfWar fogWar)
            {
                // Return last known position if transform was destroyed
                if (revealerTransform == null)
                {
                    return currentLevelCoordinates;
                }

                // Use WorldToLevel for proper coordinate conversion
                currentLevelCoordinates = fogWar.WorldToLevel(revealerTransform.position);

                return currentLevelCoordinates;
            }

            // To be assigned manually by the user
            [SerializeField]
            private Transform revealerTransform = null;
            // These are called expression-bodied properties btw, being stricter here because these are not pure data containers
            public Transform _RevealerTransform => revealerTransform;

            [SerializeField]
            private int sightRange = 0;
            public int _SightRange => sightRange;

            [SerializeField]
            private bool updateOnlyOnMove = true;
            public bool _UpdateOnlyOnMove => updateOnlyOnMove;

            private Vector2Int currentLevelCoordinates = new Vector2Int();
            public Vector2Int _CurrentLevelCoordinates
            {
                get
                {
                    lastSeenAt = currentLevelCoordinates;

                    return currentLevelCoordinates;
                }
            }

            [Header("Debug")]
            [SerializeField]
            private Vector2Int lastSeenAt = new Vector2Int(Int32.MaxValue, Int32.MaxValue);
            public Vector2Int _LastSeenAt => lastSeenAt;

            // Check if revealer is still valid (not destroyed)
            public bool IsValid => revealerTransform != null;
        }



        [BigHeader("Basic Properties")]
        [SerializeField]
        private List<FogRevealer> fogRevealers = null;
        public List<FogRevealer> _FogRevealers => fogRevealers;
        [SerializeField]
        [Range(1, 30)]
        private float FogRefreshRate = 10;

        [BigHeader("Fog Properties")]
        [SerializeField]
        [Range(0, 100)]
        private float fogPlaneHeight = 20;
        [SerializeField]
        private Material fogPlaneMaterial = null;
        [SerializeField]
        private Color fogColor = new Color32(5, 15, 25, 255);
        [SerializeField]
        [Range(0, 1)]
        private float fogPlaneAlpha = 1;
        [SerializeField]
        [Range(1, 5)]
        private float fogLerpSpeed = 2.5f;
        public bool keepRevealedTiles = false;
        [ShowIf("keepRevealedTiles")]
        [Range(0, 1)]
        public float revealedTileOpacity = 0.5f;
        [Header("Debug")]
        [SerializeField]
        private Texture2D fogPlaneTextureLerpTarget = null;
        [SerializeField]
        private Texture2D fogPlaneTextureLerpBuffer = null;

        [BigHeader("Grid Settings")]
        [SerializeField]
        [Range(32, 256)]
        [Tooltip("Resolution of the fog grid. Higher = more detailed but slower. This is the number of cells along each axis.")]
        private int gridResolution = 128;

        [BigHeader("Obstacle Scan Settings")]
        [SerializeField]
        private float rayStartHeight = 5;
        [SerializeField]
        private float rayMaxDistance = 10;
        [SerializeField]
        private LayerMask obstacleLayers = new LayerMask();
        [SerializeField]
        private bool ignoreTriggers = true;

        [BigHeader("Debug Options")]
        [SerializeField]
        private bool drawGizmos = false;
        [SerializeField]
        private bool LogOutOfRange = false;

        // External shadowcaster module

        public Shadowcaster shadowcaster { get; private set; } = new Shadowcaster();

        public LevelData levelData { get; private set; } = new LevelData();

        // The primitive plane which will act as a mesh for rendering the fog with
        private GameObject fogPlane = null;

        private float FogRefreshRateTimer = 0;

        // PlayAreaBounds reference (required)
        private PlayAreaBounds playAreaBounds;

        // Calculated values from PlayAreaBounds
        private Vector3 boundsCenter;
        private Vector2 boundsSize;
        private Vector2 worldBoundsMin;
        private Vector2 worldBoundsMax;

        // Grid dimensions (always square for circular reveals)
        private int levelDimensionX;
        private int levelDimensionY;

        // World units per grid cell (same for both axes for circular reveals)
        private float cellSize;
        public float CellSize => cellSize;

        // Cached references for performance
        private MeshRenderer fogPlaneMeshRenderer;



        // --- --- ---



        private void Start()
        {
            CheckProperties();

            InitializeFromPlayAreaBounds();

            ScanLevel();

            InitializeFog();

            // This part passes the needed references to the shadowcaster
            shadowcaster.Initialize(this);

            // This is needed because we do not update the fog when there's no unit-scale movement of each fogRevealer
            ForceUpdateFog();
        }



        private void Update()
        {
            UpdateFog();
        }



        // --- --- ---



        private void CheckProperties()
        {
            // Ensure fogRevealers list is initialized
            if (fogRevealers == null)
            {
                fogRevealers = new List<FogRevealer>();
            }

            foreach (FogRevealer fogRevealer in fogRevealers)
            {
                if (fogRevealer._RevealerTransform == null)
                {
                }
            }

            if (fogPlaneMaterial == null)
            {
                Debug.LogError("RTS_FogOfWar: fogPlaneMaterial is not assigned!");
            }
        }



        private void InitializeFromPlayAreaBounds()
        {
            // Find PlayAreaBounds - this is now required
            playAreaBounds = FindFirstObjectByType<PlayAreaBounds>();
            if (playAreaBounds == null)
            {
                Debug.LogError("RTS_FogOfWar: No PlayAreaBounds found in scene! This is required for fog of war to work.");
                enabled = false;
                return;
            }

            // Get bounds data from PlayAreaBounds
            boundsCenter = playAreaBounds.Center;
            boundsSize = playAreaBounds.Size;
            worldBoundsMin = playAreaBounds.WorldMin;
            worldBoundsMax = playAreaBounds.WorldMax;

            // Use square grid for circular reveals - both dimensions use the same cell count
            // This ensures sight range works the same in all directions
            levelDimensionX = gridResolution;
            levelDimensionY = gridResolution;

            // Calculate cell size based on the larger dimension to ensure full coverage
            // Using the same cell size for both axes ensures circular reveals
            float maxWorldSize = Mathf.Max(boundsSize.x, boundsSize.y);
            cellSize = maxWorldSize / gridResolution;

            Debug.Log($"RTS_FogOfWar: Initialized from PlayAreaBounds - Center: {boundsCenter}, Size: {boundsSize}, Grid: {levelDimensionX}x{levelDimensionY}, CellSize: {cellSize:F3}");

            // This is for faster development iteration purposes
            if (obstacleLayers.value == 0)
            {
                obstacleLayers = LayerMask.GetMask("Default");
            }
        }



        private void InitializeFog()
        {
            fogPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);

            fogPlane.name = "[RUNTIME] Fog_Plane";

            // Position fog plane at bounds center, raised by fogPlaneHeight
            // The center is calculated from PlayAreaBounds, not from the fog object's position
            fogPlane.transform.position = new Vector3(
                boundsCenter.x,
                boundsCenter.y + fogPlaneHeight,
                boundsCenter.z);

            // Unity's primitive plane is 10x10 units
            // Scale to match the larger dimension (since we use a square grid)
            float maxWorldSize = Mathf.Max(boundsSize.x, boundsSize.y);
            float planeScale = maxWorldSize / 10.0f;
            fogPlane.transform.localScale = new Vector3(planeScale, 1, planeScale);

            fogPlaneTextureLerpTarget = new Texture2D(levelDimensionX, levelDimensionY);
            fogPlaneTextureLerpBuffer = new Texture2D(levelDimensionX, levelDimensionY);

            fogPlaneTextureLerpBuffer.wrapMode = TextureWrapMode.Clamp;

            fogPlaneTextureLerpBuffer.filterMode = FilterMode.Bilinear;

            // Cache the mesh renderer for performance
            fogPlaneMeshRenderer = fogPlane.GetComponent<MeshRenderer>();
            fogPlaneMeshRenderer.material = new Material(fogPlaneMaterial);
            fogPlaneMeshRenderer.material.SetTexture("_MainTex", fogPlaneTextureLerpBuffer);

            // Disable the collider - not needed for fog
            fogPlane.GetComponent<MeshCollider>().enabled = false;
        }



        private void ForceUpdateFog()
        {
            UpdateFogField();

            // Safety check - ensure textures are initialized before copying
            if (fogPlaneTextureLerpTarget != null && fogPlaneTextureLerpBuffer != null)
            {
                Graphics.CopyTexture(fogPlaneTextureLerpTarget, fogPlaneTextureLerpBuffer);
            }
        }



        private void UpdateFog()
        {
            // Fog plane stays at fixed world position - do not update it every frame

            FogRefreshRateTimer += Time.deltaTime;

            if (FogRefreshRateTimer < 1 / FogRefreshRate)
            {
                UpdateFogPlaneTextureBuffer();

                return;
            }
            else
            {
                // This is to cancel out minor excess values
                FogRefreshRateTimer -= 1 / FogRefreshRate;
            }

            foreach (FogRevealer fogRevealer in fogRevealers)
            {
                // Skip destroyed revealers
                if (!fogRevealer.IsValid)
                {
                    continue;
                }

                if (fogRevealer._UpdateOnlyOnMove == false)
                {
                    break;
                }

                Vector2Int currentLevelCoordinates = fogRevealer.GetCurrentLevelCoordinates(this);

                if (currentLevelCoordinates != fogRevealer._LastSeenAt)
                {
                    break;
                }

                if (fogRevealer == fogRevealers.Last())
                {
                    return;
                }
            }

            UpdateFogField();

            UpdateFogPlaneTextureBuffer();
        }



        private void UpdateFogField()
        {
            shadowcaster.ResetTileVisibility();

            foreach (FogRevealer fogRevealer in fogRevealers)
            {
                // Skip destroyed revealers
                if (!fogRevealer.IsValid)
                {
                    continue;
                }

                fogRevealer.GetCurrentLevelCoordinates(this);

                // Convert sight range from world units to grid cells
                int sightRangeInCells = Mathf.RoundToInt(fogRevealer._SightRange / cellSize);

                shadowcaster.ProcessLevelData(
                    fogRevealer._CurrentLevelCoordinates,
                    sightRangeInCells);
            }

            UpdateFogPlaneTextureTarget();
        }



        // Optimized texture buffer update - reuses cached arrays to avoid GC allocation
        private void UpdateFogPlaneTextureBuffer()
        {
            // Safety check - ensure textures are initialized
            if (fogPlaneTextureLerpBuffer == null || fogPlaneTextureLerpTarget == null)
            {
                return;
            }

            // Get pixel arrays (Unity allocates these, but we reuse the references)
            Color32[] bufferPixels = fogPlaneTextureLerpBuffer.GetPixels32();
            Color32[] targetPixels = fogPlaneTextureLerpTarget.GetPixels32();

            if (bufferPixels.Length != targetPixels.Length)
            {
                return;
            }

            float lerpFactor = fogLerpSpeed * Time.deltaTime;

            // Optimized loop - avoid function call overhead by inlining lerp
            for (int i = 0; i < bufferPixels.Length; i++)
            {
                Color32 buffer = bufferPixels[i];
                Color32 target = targetPixels[i];

                bufferPixels[i] = new Color32(
                    (byte)(buffer.r + (target.r - buffer.r) * lerpFactor),
                    (byte)(buffer.g + (target.g - buffer.g) * lerpFactor),
                    (byte)(buffer.b + (target.b - buffer.b) * lerpFactor),
                    (byte)(buffer.a + (target.a - buffer.a) * lerpFactor)
                );
            }

            fogPlaneTextureLerpBuffer.SetPixels32(bufferPixels);
            fogPlaneTextureLerpBuffer.Apply();
        }



        private void UpdateFogPlaneTextureTarget()
        {
            // Safety check - ensure fog plane and textures are initialized
            if (fogPlane == null || fogPlaneTextureLerpTarget == null || fogPlaneMeshRenderer == null)
            {
                return;
            }

            // Use cached mesh renderer reference
            fogPlaneMeshRenderer.material.SetColor("_Color", fogColor);

            fogPlaneTextureLerpTarget.SetPixels32(shadowcaster.fogField.GetColors(fogPlaneAlpha, this));

            fogPlaneTextureLerpTarget.Apply();
        }



        private void ScanLevel()
        {
            // Store grid dimensions in levelData for reference
            levelData.levelDimensionX = levelDimensionX;
            levelData.levelDimensionY = levelDimensionY;

            // Scan spacing based on cell size
            float scanSpacing = cellSize * 0.25f;

            for (int xIterator = 0; xIterator < levelDimensionX; xIterator++)
            {
                // Adding a new list for column (y axis) for each unit in row (x axis)
                levelData.AddColumn(new LevelColumn(Enumerable.Repeat(LevelColumn.ETileState.Empty, levelDimensionY)));

                for (int yIterator = 0; yIterator < levelDimensionY; yIterator++)
                {
                    Vector3 worldPos = GetWorldVector(new Vector2Int(xIterator, yIterator));

                    bool isObstacleHit = Physics.BoxCast(
                        new Vector3(
                            worldPos.x,
                            boundsCenter.y + rayStartHeight,
                            worldPos.z),
                        new Vector3(
                            (cellSize - scanSpacing) / 2.0f,
                            cellSize / 2.0f,
                            (cellSize - scanSpacing) / 2.0f),
                        Vector3.down,
                        Quaternion.identity,
                        rayMaxDistance,
                        obstacleLayers,
                        (QueryTriggerInteraction)(2 - Convert.ToInt32(ignoreTriggers)));

                    if (isObstacleHit == true)
                    {
                        levelData[xIterator][yIterator] = LevelColumn.ETileState.Obstacle;
                    }
                }
            }
        }



        /// Adds a new FogRevealer instance to the list and returns its index
        public int AddFogRevealer(FogRevealer fogRevealer)
        {
            // Ensure list is initialized
            if (fogRevealers == null)
            {
                fogRevealers = new List<FogRevealer>();
            }

            fogRevealers.Add(fogRevealer);

            return fogRevealers.Count - 1;
        }



        /// Removes a FogRevealer instance from the list with index
        public void RemoveFogRevealer(int revealerIndex)
        {
            if (fogRevealers.Count > revealerIndex && revealerIndex > -1)
            {
                fogRevealers.RemoveAt(revealerIndex);
            }
            else
            {
            }
        }



        /// Replaces the FogRevealer list with the given one
        public void ReplaceFogRevealerList(List<FogRevealer> fogRevealers)
        {
            this.fogRevealers = fogRevealers;
        }



        /// Checks if the given level coordinates are within level dimension range.
        public bool CheckLevelGridRange(Vector2Int levelCoordinates)
        {
            bool result =
                levelCoordinates.x >= 0 &&
                levelCoordinates.x < levelDimensionX &&
                levelCoordinates.y >= 0 &&
                levelCoordinates.y < levelDimensionY;

            if (result == false && LogOutOfRange == true)
            {
            }

            return result;
        }



        /// Checks if the given world coordinates are within level dimension range.
        public bool CheckWorldGridRange(Vector3 worldCoordinates)
        {
            Vector2Int levelCoordinates = WorldToLevel(worldCoordinates);

            return CheckLevelGridRange(levelCoordinates);
        }



        /// Checks if the given pair of world coordinates and additionalRadius is visible by FogRevealers.
        public bool CheckVisibility(Vector3 worldCoordinates, int additionalRadius)
        {
            Vector2Int levelCoordinates = WorldToLevel(worldCoordinates);

            if (additionalRadius == 0)
            {
                return shadowcaster.fogField[levelCoordinates.x][levelCoordinates.y] ==
                    Shadowcaster.LevelColumn.ETileVisibility.Revealed;
            }


            // Iterate through a square region around the central levelCoordinates
            for (int xIterator = -additionalRadius; xIterator <= additionalRadius; xIterator++)
            {
                for (int yIterator = -additionalRadius; yIterator <= additionalRadius; yIterator++)
                {
                    Vector2Int currentCheckCoordinates = new Vector2Int(
                        levelCoordinates.x + xIterator,
                        levelCoordinates.y + yIterator);

                    // Only check visibility for cells that are within the level grid range
                    if (CheckLevelGridRange(currentCheckCoordinates))
                    {
                        if (shadowcaster.fogField[currentCheckCoordinates.x][currentCheckCoordinates.y] == Shadowcaster.LevelColumn.ETileVisibility.Revealed)
                        {
                            return true; // Found at least one revealed tile in the additional radius
                        }
                    }
                }
            }

            return false; // No revealed tiles found in the additional radius
        }



        /// Converts world coordinates to level (grid) coordinates.
        /// Uses PlayAreaBounds center and uniform cell size for accurate circular reveals.
        public Vector2Int WorldToLevel(Vector3 worldCoordinates)
        {
            // Calculate offset from the center of the grid (which corresponds to bounds center)
            // The grid is square and centered on boundsCenter
            float maxWorldSize = Mathf.Max(boundsSize.x, boundsSize.y);
            float halfGridWorldSize = maxWorldSize / 2f;

            // Convert world position to grid position
            // Grid cell (0,0) is at boundsCenter - halfGridWorldSize
            float gridMinX = boundsCenter.x - halfGridWorldSize;
            float gridMinZ = boundsCenter.z - halfGridWorldSize;

            float localX = worldCoordinates.x - gridMinX;
            float localZ = worldCoordinates.z - gridMinZ;

            int gridX = Mathf.Clamp(Mathf.FloorToInt(localX / cellSize), 0, levelDimensionX - 1);
            int gridZ = Mathf.Clamp(Mathf.FloorToInt(localZ / cellSize), 0, levelDimensionY - 1);

            return new Vector2Int(gridX, gridZ);
        }



        /// Converts level (grid) coordinates to world coordinates.
        public Vector3 GetWorldVector(Vector2Int levelCoordinates)
        {
            return new Vector3(
                GetWorldX(levelCoordinates.x),
                boundsCenter.y,
                GetWorldZ(levelCoordinates.y));
        }



        /// Converts level/grid X coordinate to world X coordinate.
        public float GetWorldX(int xValue)
        {
            float maxWorldSize = Mathf.Max(boundsSize.x, boundsSize.y);
            float halfGridWorldSize = maxWorldSize / 2f;
            float gridMinX = boundsCenter.x - halfGridWorldSize;

            // Return center of the grid cell
            return gridMinX + (xValue + 0.5f) * cellSize;
        }



        /// Converts level/grid Y coordinate to world Z coordinate.
        public float GetWorldZ(int yValue)
        {
            float maxWorldSize = Mathf.Max(boundsSize.x, boundsSize.y);
            float halfGridWorldSize = maxWorldSize / 2f;
            float gridMinZ = boundsCenter.z - halfGridWorldSize;

            // Return center of the grid cell
            return gridMinZ + (yValue + 0.5f) * cellSize;
        }


        // Legacy method name for compatibility
        public float GetWorldY(int yValue) => GetWorldZ(yValue);



#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying == false)
            {
                return;
            }

            if (drawGizmos == false)
            {
                return;
            }

            Handles.color = Color.yellow;

            for (int xIterator = 0; xIterator < levelDimensionX; xIterator++)
            {
                for (int yIterator = 0; yIterator < levelDimensionY; yIterator++)
                {
                    if (levelData[xIterator][yIterator] == LevelColumn.ETileState.Obstacle)
                    {
                        if (shadowcaster.fogField[xIterator][yIterator] == Shadowcaster.LevelColumn.ETileVisibility.Revealed)
                        {
                            Handles.color = Color.green;
                        }
                        else
                        {
                            Handles.color = Color.red;
                        }

                        Handles.DrawWireCube(
                            new Vector3(
                                GetWorldX(xIterator),
                                boundsCenter.y,
                                GetWorldZ(yIterator)),
                            new Vector3(
                                cellSize * 0.9f,
                                cellSize,
                                cellSize * 0.9f));
                    }
                    else
                    {
                        Gizmos.color = Color.yellow;

                        Gizmos.DrawSphere(
                            new Vector3(
                                GetWorldX(xIterator),
                                boundsCenter.y,
                                GetWorldZ(yIterator)),
                            cellSize / 5.0f);
                    }

                    if (shadowcaster.fogField[xIterator][yIterator] == Shadowcaster.LevelColumn.ETileVisibility.Revealed)
                    {
                        Gizmos.color = Color.green;

                        Gizmos.DrawSphere(
                            new Vector3(
                                GetWorldX(xIterator),
                                boundsCenter.y,
                                GetWorldZ(yIterator)),
                            cellSize / 3.0f);
                    }
                }
            }
        }
#endif
    }



    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public string _BaseCondition
        {
            get { return mBaseCondition; }
        }

        private string mBaseCondition = String.Empty;

        public ShowIfAttribute(string baseCondition)
        {
            mBaseCondition = baseCondition;
        }
    }



    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class BigHeaderAttribute : PropertyAttribute
    {
        public string _Text
        {
            get { return mText; }
        }

        private string mText = String.Empty;

        public BigHeaderAttribute(string text)
        {
            mText = text;
        }
    }



}
