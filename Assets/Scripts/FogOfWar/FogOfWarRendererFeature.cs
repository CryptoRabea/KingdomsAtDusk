using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KingdomsAtDusk.FogOfWar
{
    /// <summary>
    /// URP Renderer Feature for fog of war camera dimming effect
    /// Add this to your URP Renderer asset
    /// </summary>
    public class FogOfWarRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public Material fogMaterial = null;
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            [Range(0f, 1f)] public float dimStrength = 0.7f;
        }

        public Settings settings = new Settings();
        private FogOfWarRenderPass renderPass;

        public override void Create()
        {
            renderPass = new FogOfWarRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.fogMaterial == null)
            {
                Debug.LogWarning("[FogOfWarRendererFeature] Fog material is not assigned!");
                return;
            }

            renderPass.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(renderPass);
        }
    }

    /// <summary>
    /// The actual render pass that applies the fog effect
    /// </summary>
    public class FogOfWarRenderPass : ScriptableRenderPass
    {
        private FogOfWarRendererFeature.Settings settings;
        private RenderTargetIdentifier source;
        private RenderTargetHandle tempTexture;
        private FogOfWarManager fogManager;
        private Texture2D fogTexture;
        private Color[] texturePixels;
        private bool isInitialized;
        private int frameCount;

        public FogOfWarRenderPass(FogOfWarRendererFeature.Settings settings)
        {
            this.settings = settings;
            this.renderPassEvent = settings.renderPassEvent;
            tempTexture.Init("_TempFogOfWarTexture");
        }

        public void Setup(RenderTargetIdentifier source)
        {
            this.source = source;

            // Initialize on first setup
            if (!isInitialized)
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            if (isInitialized) return;

            // Find fog manager
            fogManager = FogOfWarManager.Instance;
            if (fogManager == null || fogManager.Grid == null)
            {
                Debug.LogWarning("[FogOfWarRenderPass] FogOfWarManager not found or not initialized");
                return;
            }

            // Create fog texture
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

            isInitialized = true;
            Debug.Log($"[FogOfWarRenderPass] Initialized! Grid: {width}x{height}");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings.fogMaterial == null)
                return;

            // Re-initialize if needed
            if (!isInitialized || fogManager == null)
            {
                Initialize();
                if (!isInitialized)
                    return;
            }

            // Update fog texture
            UpdateFogTexture();

            // Get command buffer
            CommandBuffer cmd = CommandBufferPool.Get("FogOfWarEffect");

            // Get render texture descriptor
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;

            // Get temporary render texture
            cmd.GetTemporaryRT(tempTexture.id, descriptor);

            // Set shader parameters
            if (fogManager != null && fogManager.Config != null)
            {
                Bounds worldBounds = fogManager.Config.worldBounds;
                settings.fogMaterial.SetVector("_WorldBoundsMin", new Vector4(worldBounds.min.x, worldBounds.min.y, worldBounds.min.z, 0));
                settings.fogMaterial.SetVector("_WorldBoundsMax", new Vector4(worldBounds.max.x, worldBounds.max.y, worldBounds.max.z, 0));
                settings.fogMaterial.SetTexture("_FogTex", fogTexture);
                settings.fogMaterial.SetFloat("_DimStrength", settings.dimStrength);
            }

            // Blit with fog material
            cmd.Blit(source, tempTexture.Identifier(), settings.fogMaterial, 0);
            cmd.Blit(tempTexture.Identifier(), source);

            // Execute and cleanup
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // Release temporary RT
            cmd.ReleaseTemporaryRT(tempTexture.id);
            CommandBufferPool.Release(cmd);

            frameCount++;
        }

        private void UpdateFogTexture()
        {
            if (fogManager == null || fogManager.Grid == null || fogTexture == null)
                return;

            bool needsUpdate = false;

            // Update texture based on grid state
            for (int x = 0; x < fogManager.Grid.Width; x++)
            {
                for (int y = 0; y < fogManager.Grid.Height; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    VisionState state = fogManager.Grid.GetState(cell);

                    Color targetColor;

                    switch (state)
                    {
                        case VisionState.Unexplored:
                            targetColor = new Color(0, 0, 0, 1f);
                            break;
                        case VisionState.Explored:
                            targetColor = new Color(0, 0, 0, 0.6f);
                            break;
                        case VisionState.Visible:
                            targetColor = new Color(0, 0, 0, 0f);
                            break;
                        default:
                            targetColor = Color.black;
                            break;
                    }

                    int pixelIndex = y * fogManager.Grid.Width + x;

                    if (texturePixels[pixelIndex] != targetColor)
                    {
                        texturePixels[pixelIndex] = Color.Lerp(texturePixels[pixelIndex], targetColor, 0.1f);
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

        public override void FrameCleanup(CommandBuffer cmd)
        {
            // Cleanup if needed
        }
    }
}
