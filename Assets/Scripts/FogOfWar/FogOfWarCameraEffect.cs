using UnityEngine;

namespace KingdomsAtDusk.FogOfWar
{
    /// <summary>
    /// Camera-based fog of war effect that dims unexplored and explored areas
    /// This attaches to the camera and renders a full-screen overlay
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class FogOfWarCameraEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FogOfWarManager fogManager;
        [SerializeField] private Material fogEffectMaterial;

        [Header("Settings")]
        [SerializeField] private bool enableEffect = true;
        [SerializeField, Range(0f, 1f)] private float dimStrength = 0.7f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private bool visualizeDepth = false;
        [SerializeField] private bool visualizeFogTexture = false;

        private Camera cam;
        private Texture2D fogTexture;
        private Color[] texturePixels;
        private bool isInitialized;
        private int updateFrameCount = 0;

        private void Awake()
        {
            cam = GetComponent<Camera>();

            // Ensure depth texture is enabled for world position reconstruction
            cam.depthTextureMode = DepthTextureMode.Depth;
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;

            if (enableDebugLogging)
                Debug.Log("[FogOfWarCameraEffect] Starting initialization...");

            // Check camera
            if (cam == null)
            {
                Debug.LogError("[FogOfWarCameraEffect] Camera component is null!");
                return;
            }

            if (enableDebugLogging)
                Debug.Log($"[FogOfWarCameraEffect] Camera depth mode: {cam.depthTextureMode}");

            // Check material
            if (fogEffectMaterial == null)
            {
                Debug.LogError("[FogOfWarCameraEffect] Fog Effect Material is not assigned! Please assign a material with the FogOfWarCameraEffect shader.");
                return;
            }

            if (enableDebugLogging)
                Debug.Log($"[FogOfWarCameraEffect] Material: {fogEffectMaterial.name}, Shader: {fogEffectMaterial.shader.name}");

            // Find fog manager
            if (fogManager == null)
            {
                fogManager = FogOfWarManager.Instance;
                if (fogManager == null)
                {
                    Debug.LogError("[FogOfWarCameraEffect] No FogOfWarManager found in scene! Please create one first.");
                    return;
                }
            }

            if (enableDebugLogging)
                Debug.Log($"[FogOfWarCameraEffect] FogOfWarManager found: {fogManager.name}");

            // Wait for fog manager to initialize
            if (fogManager.Grid == null)
            {
                if (enableDebugLogging)
                    Debug.LogWarning("[FogOfWarCameraEffect] FogOfWarManager grid not initialized yet, will retry...");
                return;
            }

            CreateFogTexture();
            isInitialized = true;

            Debug.Log($"[FogOfWarCameraEffect] ✓ Initialization complete! Grid: {fogManager.Grid.Width}x{fogManager.Grid.Height}, Texture: {fogTexture.width}x{fogTexture.height}");
        }

        private void CreateFogTexture()
        {
            if (fogManager == null || fogManager.Grid == null) return;

            int width = fogManager.Grid.Width;
            int height = fogManager.Grid.Height;

            fogTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            texturePixels = new Color[width * height];

            // Initialize as unexplored
            for (int i = 0; i < texturePixels.Length; i++)
            {
                texturePixels[i] = Color.black;
            }

            fogTexture.SetPixels(texturePixels);
            fogTexture.Apply();

            if (fogEffectMaterial != null)
            {
                fogEffectMaterial.SetTexture("_FogTex", fogTexture);
            }
        }

        private void Update()
        {
            if (!isInitialized)
            {
                Initialize();
                return;
            }

            UpdateFogTexture();
        }

        private void UpdateFogTexture()
        {
            if (fogManager == null || fogManager.Grid == null || fogTexture == null) return;

            bool needsUpdate = false;

            // Update texture based on grid state
            for (int x = 0; x < fogManager.Grid.Width; x++)
            {
                for (int y = 0; y < fogManager.Grid.Height; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    VisionState state = fogManager.Grid.GetState(cell);
                    float alpha = fogManager.Grid.GetVisibilityAlpha(cell);

                    Color targetColor;

                    switch (state)
                    {
                        case VisionState.Unexplored:
                            // Fully black (100% dim)
                            targetColor = new Color(0, 0, 0, 1f * dimStrength);
                            break;
                        case VisionState.Explored:
                            // Semi-transparent (60% dim by default)
                            targetColor = new Color(0, 0, 0, 0.6f * dimStrength);
                            break;
                        case VisionState.Visible:
                            // Fully transparent (no dim)
                            targetColor = new Color(0, 0, 0, 0f);
                            break;
                        default:
                            targetColor = Color.black;
                            break;
                    }

                    int pixelIndex = y * fogManager.Grid.Width + x;

                    if (texturePixels[pixelIndex] != targetColor)
                    {
                        texturePixels[pixelIndex] = Color.Lerp(texturePixels[pixelIndex], targetColor, Time.deltaTime * 5f);
                        needsUpdate = true;
                    }
                }
            }

            if (needsUpdate)
            {
                fogTexture.SetPixels(texturePixels);
                fogTexture.Apply();
            }
        }

        /// <summary>
        /// Render the fog overlay using OnRenderImage
        /// </summary>
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // Debug logging (only once per second to avoid spam)
            if (enableDebugLogging && updateFrameCount % 60 == 0)
            {
                Debug.Log($"[FogOfWarCameraEffect] OnRenderImage called - Effect:{enableEffect}, Material:{fogEffectMaterial != null}, Texture:{fogTexture != null}, Manager:{fogManager != null}, Initialized:{isInitialized}");
            }
            updateFrameCount++;

            // Validation checks
            if (!enableEffect)
            {
                if (enableDebugLogging && updateFrameCount == 1)
                    Debug.LogWarning("[FogOfWarCameraEffect] Effect is disabled!");
                Graphics.Blit(source, destination);
                return;
            }

            if (fogEffectMaterial == null)
            {
                if (enableDebugLogging && updateFrameCount % 120 == 0)
                    Debug.LogError("[FogOfWarCameraEffect] No material assigned!");
                Graphics.Blit(source, destination);
                return;
            }

            if (fogTexture == null || fogManager == null || !isInitialized)
            {
                if (enableDebugLogging && updateFrameCount % 120 == 0)
                    Debug.LogWarning($"[FogOfWarCameraEffect] Not ready - Texture:{fogTexture != null}, Manager:{fogManager != null}, Init:{isInitialized}");
                Graphics.Blit(source, destination);
                return;
            }

            // Debug visualization modes
            if (visualizeDepth)
            {
                // Just show depth buffer
                Graphics.Blit(source, destination);
                return;
            }

            if (visualizeFogTexture)
            {
                // Show fog texture directly
                Graphics.Blit(fogTexture, destination);
                return;
            }

            // Calculate world bounds in shader uniforms
            Bounds worldBounds = fogManager.Config.worldBounds;
            fogEffectMaterial.SetVector("_WorldBoundsMin", new Vector4(worldBounds.min.x, worldBounds.min.y, worldBounds.min.z, 0));
            fogEffectMaterial.SetVector("_WorldBoundsMax", new Vector4(worldBounds.max.x, worldBounds.max.y, worldBounds.max.z, 0));
            fogEffectMaterial.SetTexture("_FogTex", fogTexture);
            fogEffectMaterial.SetFloat("_DimStrength", dimStrength);

            // Apply the fog effect
            Graphics.Blit(source, destination, fogEffectMaterial);
        }

        /// <summary>
        /// Enable or disable the fog effect
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            enableEffect = enabled;
        }

        /// <summary>
        /// Set the dim strength (0 = no dimming, 1 = full dimming)
        /// </summary>
        public void SetDimStrength(float strength)
        {
            dimStrength = Mathf.Clamp01(strength);
        }

        /// <summary>
        /// Debug: Print diagnostic information
        /// </summary>
        [ContextMenu("Debug: Print Status")]
        public void DebugPrintStatus()
        {
            Debug.Log("=== FOG OF WAR CAMERA EFFECT STATUS ===");
            Debug.Log($"Initialized: {isInitialized}");
            Debug.Log($"Enable Effect: {enableEffect}");
            Debug.Log($"Dim Strength: {dimStrength}");
            Debug.Log($"Camera: {(cam != null ? cam.name : "NULL")}");
            Debug.Log($"Camera Depth Mode: {(cam != null ? cam.depthTextureMode.ToString() : "NULL")}");
            Debug.Log($"Material: {(fogEffectMaterial != null ? fogEffectMaterial.name : "NULL")}");
            Debug.Log($"Material Shader: {(fogEffectMaterial != null ? fogEffectMaterial.shader.name : "NULL")}");
            Debug.Log($"Fog Texture: {(fogTexture != null ? $"{fogTexture.width}x{fogTexture.height}" : "NULL")}");
            Debug.Log($"Fog Manager: {(fogManager != null ? fogManager.name : "NULL")}");

            if (fogManager != null && fogManager.Grid != null)
            {
                Debug.Log($"Grid Size: {fogManager.Grid.Width}x{fogManager.Grid.Height}");
                Debug.Log($"World Bounds: {fogManager.Config.worldBounds}");
            }
            else
            {
                Debug.LogWarning("Grid is NULL - FogOfWarManager may not be initialized");
            }

            Debug.Log($"Update Frame Count: {updateFrameCount}");
            Debug.Log("======================================");
        }

        /// <summary>
        /// Debug: Force reinitialization
        /// </summary>
        [ContextMenu("Debug: Force Reinitialize")]
        public void DebugForceReinitialize()
        {
            isInitialized = false;
            Initialize();
        }

        /// <summary>
        /// Debug: Test with full visibility
        /// </summary>
        [ContextMenu("Debug: Set Full Visibility")]
        public void DebugSetFullVisibility()
        {
            if (fogManager != null)
            {
                fogManager.RevealAll();
                Debug.Log("[FogOfWarCameraEffect] Set entire map to visible");
            }
        }

        /// <summary>
        /// Debug: Test with no visibility
        /// </summary>
        [ContextMenu("Debug: Set No Visibility")]
        public void DebugSetNoVisibility()
        {
            if (fogManager != null)
            {
                fogManager.HideAll();
                Debug.Log("[FogOfWarCameraEffect] Hidden entire map");
            }
        }

        private void OnDestroy()
        {
            if (fogTexture != null)
            {
                Destroy(fogTexture);
            }
        }

        private void OnDrawGizmos()
        {
            if (fogManager != null && fogManager.Config != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(fogManager.Config.worldBounds.center, fogManager.Config.worldBounds.size);
            }
        }
    }
}
