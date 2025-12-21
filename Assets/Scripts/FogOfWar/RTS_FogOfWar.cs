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
        [Range(1, 128)]
        [Tooltip("If you need more than 128 units, consider using raycasting-based fog modules instead.")]
        private int levelDimensionX = 11;
        [SerializeField]
        [Range(1, 128)]
        [Tooltip("If you need more than 128 units, consider using raycasting-based fog modules instead.")]
        private int levelDimensionY = 11;
        [SerializeField]
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

        private Texture2D fogTexture;
        private byte[] fogData;

        // Store the initial fixed position of levelMidPoint to keep fog in world space
        private Vector3 fixedLevelMidPoint;



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

            // Find PlayAreaBounds if not assigned
            if (playAreaBounds == null)
            {
                playAreaBounds = PlayAreaBounds.Instance;
            }

            // Initialize grid dimensions based on play area
            InitializeGridFromPlayArea();

            InitializeFog();

            // This part passes the needed references to the shadowcaster
            shadowcaster.Initialize(this);

            terrainFogBinder.fogTexture = fogTexture;
            terrainFogBinder.ApplyFog();
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
            // Store the initial fixed position to keep fog in world space
            fixedLevelMidPoint = levelMidPoint.position;

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



        private void InitializeFog()
        {
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



        private void ForceUpdateFog()
        {
            UpdateFogField();

            Graphics.CopyTexture(fogPlaneTextureLerpTarget, fogPlaneTextureLerpBuffer);
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
            fogPlane.GetComponent<MeshRenderer>().material.SetColor("_Color", fogColor);

            fogPlaneTextureLerpTarget.SetPixels32(shadowcaster.fogField.GetColors(fogPlaneAlpha, this));

            fogPlaneTextureLerpTarget.Apply();
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