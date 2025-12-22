using System;                       // Convert
using System.Collections.Generic;   // List
using System.IO;                    // Directory
using System.Linq;                  // Enumerable
using UnityEditor;                  // Handles
using UnityEngine;                  // Monobehaviour
using RTS.Core;                     // PlayAreaBounds



namespace RTS.FogOfWar
{


    public class RTS_FogOfWar : MonoBehaviour
    {
        /// A class for storing the base level data.
        ///
        /// This class is later serialized into Json format.\n
        /// Empty spaces are stored as 0, while the obstacles are stored as 1.\n
        /// If a level is loaded instead of being scanned,
        /// the level dimension properties of csFogWar will be replaced by the level data.
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

            // Adding private getter / setters are not allowed for serialization
            public int levelDimensionX = 0;
            public int levelDimensionY = 0;

            public float unitScale = 0;

            public float scanSpacingPerUnit = 0;

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

                currentLevelCoordinates = new Vector2Int(
                    fogWar.GetUnitX(revealerTransform.position.x),
                    fogWar.GetUnitY(revealerTransform.position.z));

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
        private Transform levelMidPoint = null;
        public Transform _LevelMidPoint => levelMidPoint;
        [SerializeField]
        [Range(1, 30)]
        private float FogRefreshRate = 10;

        [BigHeader("Fog Properties")]
        [SerializeField]
        [Range(0, 100)]
        private float fogPlaneHeight = 1;
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

        [BigHeader("Level Data")]
        [SerializeField]
        private TextAsset LevelDataToLoad = null;
        [SerializeField]
        private bool saveDataOnScan = true;
        [ShowIf("saveDataOnScan")]
        [SerializeField]
        private string levelNameToSave = "Default";

        [BigHeader("Scan Properties")]
        [SerializeField]
        [Tooltip("When enabled, the fog plane will automatically size itself to match the PlayAreaBounds in the scene.")]
        private bool usePlayAreaBounds = true;
        [SerializeField]
        [Range(1, 128)]
        [Tooltip("If you need more than 128 units, consider using raycasting-based fog modules instead. Ignored when usePlayAreaBounds is enabled.")]
        private int levelDimensionX = 11;
        [SerializeField]
        [Range(1, 128)]
        [Tooltip("If you need more than 128 units, consider using raycasting-based fog modules instead. Ignored when usePlayAreaBounds is enabled.")]
        private int levelDimensionY = 11;
        [SerializeField]
        [Tooltip("Scale of each fog grid cell in world units. Ignored when usePlayAreaBounds is enabled.")]
        private float unitScale = 1;
        public float _UnitScale => unitScale;
        [SerializeField]
        private float scanSpacingPerUnit = 0.25f;
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

        private const string levelScanDataPath = "/LevelData";

        // Store the initial fixed position of levelMidPoint to keep fog in world space
        private Vector3 fixedLevelMidPoint;

        // Cached play area size for proper fog plane scaling (used when usePlayAreaBounds is enabled)
        private Vector2 cachedPlayAreaSize;



        // --- --- ---



        private void Start()
        {
            CheckProperties();

            InitializeVariables();

            if (LevelDataToLoad == null)
            {
                ScanLevel();

                if (saveDataOnScan == true)
                {
                    // Preprocessor definitions are used because the save function code will be stripped out on build
#if UNITY_EDITOR
                    SaveScanAsLevelData();
#endif
                }
            }
            else
            {
                LoadLevelData();
            }

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
            foreach (FogRevealer fogRevealer in fogRevealers)
            {
                if (fogRevealer._RevealerTransform == null)
                {
                }
            }

            if (unitScale <= 0)
            {
            }

            if (scanSpacingPerUnit <= 0)
            {
            }

            if (levelMidPoint == null)
            {
            }

            if (fogPlaneMaterial == null)
            {
            }
        }



        private void InitializeVariables()
        {
            // Check if we should use PlayAreaBounds for automatic sizing
            if (usePlayAreaBounds)
            {
                PlayAreaBounds playAreaBounds = PlayAreaBounds.Instance;
                if (playAreaBounds != null)
                {
                    // Use PlayAreaBounds center as the fog center
                    fixedLevelMidPoint = playAreaBounds.Center;

                    // Cache the play area size for fog plane scaling
                    cachedPlayAreaSize = playAreaBounds.Size;

                    // Calculate unitScale to keep dimensions within 128 limit
                    // while covering the entire play area
                    float maxDimension = Mathf.Max(cachedPlayAreaSize.x, cachedPlayAreaSize.y);

                    // Calculate unitScale so that the larger dimension fits within 128 grid cells
                    // Using a minimum unitScale of 1 to avoid too fine granularity
                    unitScale = Mathf.Max(1f, Mathf.Ceil(maxDimension / 128f));

                    // Calculate grid dimensions based on play area size and unitScale
                    levelDimensionX = Mathf.CeilToInt(cachedPlayAreaSize.x / unitScale);
                    levelDimensionY = Mathf.CeilToInt(cachedPlayAreaSize.y / unitScale);

                    // Clamp to valid range (1-128)
                    levelDimensionX = Mathf.Clamp(levelDimensionX, 1, 128);
                    levelDimensionY = Mathf.Clamp(levelDimensionY, 1, 128);

                    Debug.Log($"RTS_FogOfWar: Using PlayAreaBounds - Size: {cachedPlayAreaSize}, Grid: {levelDimensionX}x{levelDimensionY}, UnitScale: {unitScale}");
                }
                else
                {
                    Debug.LogWarning("RTS_FogOfWar: usePlayAreaBounds is enabled but no PlayAreaBounds found in scene. Using manual settings.");
                    cachedPlayAreaSize = Vector2.zero;
                    InitializeFallbackMidPoint();
                }
            }
            else
            {
                cachedPlayAreaSize = Vector2.zero;
                InitializeFallbackMidPoint();
            }

            // This is for faster development iteration purposes
            if (obstacleLayers.value == 0)
            {
                obstacleLayers = LayerMask.GetMask("Default");
            }

            // This is also for faster development iteration purposes
            if (levelNameToSave == String.Empty)
            {
                levelNameToSave = "Default";
            }
        }

        private void InitializeFallbackMidPoint()
        {
            // Store the initial fixed position to keep fog in world space
            // Use the transform's position as fallback if levelMidPoint is not assigned
            if (levelMidPoint != null)
            {
                fixedLevelMidPoint = levelMidPoint.position;
            }
            else
            {
                fixedLevelMidPoint = transform.position;
                Debug.LogWarning("RTS_FogOfWar: levelMidPoint not assigned, using this object's position as the fog center.");
            }
        }



        private void InitializeFog()
        {
            fogPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);

            fogPlane.name = "[RUNTIME] Fog_Plane";

            fogPlane.transform.position = new Vector3(
                fixedLevelMidPoint.x,
                fixedLevelMidPoint.y + fogPlaneHeight,
                fixedLevelMidPoint.z);

            // Calculate fog plane scale
            // Unity's primitive plane is 10x10 units, so we divide by 10 to get the correct scale
            Vector3 fogPlaneScale;
            if (usePlayAreaBounds && cachedPlayAreaSize != Vector2.zero)
            {
                // Use the exact play area size for precise matching with PlayAreaBounds
                fogPlaneScale = new Vector3(
                    cachedPlayAreaSize.x / 10.0f,
                    1,
                    cachedPlayAreaSize.y / 10.0f);
            }
            else
            {
                // Use the traditional formula based on level dimensions and unit scale
                fogPlaneScale = new Vector3(
                    (levelDimensionX * unitScale) / 10.0f,
                    1,
                    (levelDimensionY * unitScale) / 10.0f);
            }
            fogPlane.transform.localScale = fogPlaneScale;

            fogPlaneTextureLerpTarget = new Texture2D(levelDimensionX, levelDimensionY);
            fogPlaneTextureLerpBuffer = new Texture2D(levelDimensionX, levelDimensionY);

            fogPlaneTextureLerpBuffer.wrapMode = TextureWrapMode.Clamp;

            fogPlaneTextureLerpBuffer.filterMode = FilterMode.Bilinear;

            fogPlane.GetComponent<MeshRenderer>().material = new Material(fogPlaneMaterial);

            fogPlane.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", fogPlaneTextureLerpBuffer);

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

                shadowcaster.ProcessLevelData(
                    fogRevealer._CurrentLevelCoordinates,
                    Mathf.RoundToInt(fogRevealer._SightRange / unitScale));
            }

            UpdateFogPlaneTextureTarget();
        }



        // Doing shader business on the script, if we pull this out as a shader pass, same operations must be repeated
        private void UpdateFogPlaneTextureBuffer()
        {
            // Safety check - ensure textures are initialized
            if (fogPlaneTextureLerpBuffer == null || fogPlaneTextureLerpTarget == null)
            {
                return;
            }

            Color32[] bufferPixels = fogPlaneTextureLerpBuffer.GetPixels32();
            Color32[] targetPixels = fogPlaneTextureLerpTarget.GetPixels32();

            if (bufferPixels.Length != targetPixels.Length)
            {
                return;
            }

            for (int i = 0; i < bufferPixels.Length; i++)
            {
                bufferPixels[i] = Color.Lerp(bufferPixels[i], targetPixels[i], fogLerpSpeed * Time.deltaTime);
            }

            fogPlaneTextureLerpBuffer.SetPixels32(bufferPixels);

            fogPlaneTextureLerpBuffer.Apply();
        }



        private void UpdateFogPlaneTextureTarget()
        {
            // Safety check - ensure fog plane and textures are initialized
            if (fogPlane == null || fogPlaneTextureLerpTarget == null)
            {
                return;
            }

            fogPlane.GetComponent<MeshRenderer>().material.SetColor("_Color", fogColor);

            fogPlaneTextureLerpTarget.SetPixels32(shadowcaster.fogField.GetColors(fogPlaneAlpha, this));

            fogPlaneTextureLerpTarget.Apply();
        }



        private void ScanLevel()
        {

            // These operations have no real computational meaning, but it will bring consistency to the data
            levelData.levelDimensionX = levelDimensionX;
            levelData.levelDimensionY = levelDimensionY;
            levelData.unitScale = unitScale;
            levelData.scanSpacingPerUnit = scanSpacingPerUnit;

            for (int xIterator = 0; xIterator < levelDimensionX; xIterator++)
            {
                // Adding a new list for column (y axis) for each unit in row (x axis)
                levelData.AddColumn(new LevelColumn(Enumerable.Repeat(LevelColumn.ETileState.Empty, levelDimensionY)));

                for (int yIterator = 0; yIterator < levelDimensionY; yIterator++)
                {
                    bool isObstacleHit = Physics.BoxCast(
                        new Vector3(
                            GetWorldX(xIterator),
                            fixedLevelMidPoint.y + rayStartHeight,
                            GetWorldY(yIterator)),
                        new Vector3(
                            (unitScale - scanSpacingPerUnit) / 2.0f,
                            unitScale / 2.0f,
                            (unitScale - scanSpacingPerUnit) / 2.0f),
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



        // We intend to use Application.dataPath only for accessing project files directory (only in unity editor)
#if UNITY_EDITOR
        private void SaveScanAsLevelData()
        {
            string fullPath = Application.dataPath + levelScanDataPath + "/" + levelNameToSave + ".json";

            if (Directory.Exists(Application.dataPath + levelScanDataPath) == false)
            {
                Directory.CreateDirectory(Application.dataPath + levelScanDataPath);

            }

            if (File.Exists(fullPath) == true)
            {
            }

            string levelJson = JsonUtility.ToJson(levelData);

            File.WriteAllText(fullPath, levelJson);

        }
#endif



        private void LoadLevelData()
        {

            // Exception check is indirectly performed through branching on the upper part of the code
            string levelJson = LevelDataToLoad.ToString();

            levelData = JsonUtility.FromJson<LevelData>(levelJson);

            levelDimensionX = levelData.levelDimensionX;
            levelDimensionY = levelData.levelDimensionY;
            unitScale = levelData.unitScale;
            scanSpacingPerUnit = levelData.scanSpacingPerUnit;

        }



        /// Adds a new FogRevealer instance to the list and returns its index
        public int AddFogRevealer(FogRevealer fogRevealer)
        {
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
                levelCoordinates.x < levelData.levelDimensionX &&
                levelCoordinates.y >= 0 &&
                levelCoordinates.y < levelData.levelDimensionY;

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

            int scanResult = 0;

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



        /// Converts unit (divided by unitScale, then rounded) world coordinates to level coordinates.
        public Vector2Int WorldToLevel(Vector3 worldCoordinates)
        {
            Vector2Int unitWorldCoordinates = GetUnitVector(worldCoordinates);

            return new Vector2Int(
                unitWorldCoordinates.x + (levelDimensionX / 2),
                unitWorldCoordinates.y + (levelDimensionY / 2));
        }



        /// Converts level coordinates into world coordinates.
        public Vector3 GetWorldVector(Vector2Int worldCoordinates)
        {
            return new Vector3(
                GetWorldX(worldCoordinates.x + (levelDimensionX / 2)),
                0,
                GetWorldY(worldCoordinates.y + (levelDimensionY / 2)));
        }



        /// Converts "pure" world coordinates into unit world coordinates.
        public Vector2Int GetUnitVector(Vector3 worldCoordinates)
        {
            return new Vector2Int(GetUnitX(worldCoordinates.x), GetUnitY(worldCoordinates.z));
        }



        /// Converts level coordinate to corresponding unit world coordinates.
        public float GetWorldX(int xValue)
        {
            if (levelData.levelDimensionX % 2 == 0)
            {
                return (fixedLevelMidPoint.x - ((levelDimensionX / 2.0f) - xValue) * unitScale);
            }

            return (fixedLevelMidPoint.x - ((levelDimensionX / 2.0f) - (xValue + 0.5f)) * unitScale);
        }



        /// Converts world coordinate to unit world coordinates.
        public int GetUnitX(float xValue)
        {
            return Mathf.RoundToInt((xValue - fixedLevelMidPoint.x) / unitScale);
        }



        /// Converts level coordinate to corresponding unit world coordinates.
        public float GetWorldY(int yValue)
        {
            if (levelData.levelDimensionY % 2 == 0)
            {
                return (fixedLevelMidPoint.z - ((levelDimensionY / 2.0f) - yValue) * unitScale);
            }

            return (fixedLevelMidPoint.z - ((levelDimensionY / 2.0f) - (yValue + 0.5f)) * unitScale);
        }



        /// Converts world coordinate to unit world coordinates.
        public int GetUnitY(float yValue)
        {
            return Mathf.RoundToInt((yValue - fixedLevelMidPoint.z) / unitScale);
        }



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
                                fixedLevelMidPoint.y,
                                GetWorldY(yIterator)),
                            new Vector3(
                                unitScale - scanSpacingPerUnit,
                                unitScale,
                                unitScale - scanSpacingPerUnit));
                    }
                    else
                    {
                        Gizmos.color = Color.yellow;

                        Gizmos.DrawSphere(
                            new Vector3(
                                GetWorldX(xIterator),
                                fixedLevelMidPoint.y,
                                GetWorldY(yIterator)),
                            unitScale / 5.0f);
                    }

                    if (shadowcaster.fogField[xIterator][yIterator] == Shadowcaster.LevelColumn.ETileVisibility.Revealed)
                    {
                        Gizmos.color = Color.green;

                        Gizmos.DrawSphere(
                            new Vector3(
                                GetWorldX(xIterator),
                                fixedLevelMidPoint.y,
                                GetWorldY(yIterator)),
                            unitScale / 3.0f);
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
