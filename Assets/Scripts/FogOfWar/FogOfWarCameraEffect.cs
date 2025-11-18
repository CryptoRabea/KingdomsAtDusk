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

        private Camera cam;
        private Texture2D fogTexture;
        private Color[] texturePixels;
        private bool isInitialized;

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

            if (fogManager == null)
            {
                fogManager = FogOfWarManager.Instance;
                if (fogManager == null)
                {
                    Debug.LogWarning("[FogOfWarCameraEffect] No FogOfWarManager found!");
                    return;
                }
            }

            CreateFogTexture();
            isInitialized = true;
            Debug.Log("[FogOfWarCameraEffect] Initialized camera fog effect");
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
            if (!enableEffect || fogEffectMaterial == null || fogTexture == null || fogManager == null)
            {
                Graphics.Blit(source, destination);
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
