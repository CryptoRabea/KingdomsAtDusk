using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KingdomsAtDusk.FogOfWar
{
    /// <summary>
    /// URP Renderer Feature for fog of war vision dimming effect
    /// 
    /// NOTE: This is for RTS fog of war (unexplored/explored/visible areas), NOT environmental fog.
    /// This system is fully compatible with Unity's standard fog - they can be used together.
    /// 
    /// Add this to your URP Renderer asset to enable fog of war in the game view.
    /// </summary>
    public class FogOfWarRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Material with the FogOfWarVisionDimming shader (for RTS fog of war, not environmental fog)")]
            public Material fogOfWarMaterial = null;

            [Tooltip("When to inject the fog of war rendering")]
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            [Tooltip("Strength of the vision dimming effect (0 = no dimming, 1 = full dimming)")]
            [Range(0f, 1f)]
            public float visionDimStrength = 0.7f;

            [Tooltip("Enable debug logging")]
            public bool enableDebugLogging = false;
        }

        public Settings settings = new Settings();
        private FogOfWarRenderPass renderPass;

        public override void Create()
        {
            if (settings.fogOfWarMaterial == null)
            {
                Debug.LogError("[FogOfWarRendererFeature] Fog of war material is not assigned! Please assign a material with the FogOfWarVisionDimming shader.");
                return;
            }

            renderPass = new FogOfWarRenderPass(settings);

            if (settings.enableDebugLogging)
            {
                Debug.Log("[FogOfWarRendererFeature] Created fog of war render pass");
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.fogOfWarMaterial == null)
            {
                return;
            }

            if (renderPass == null)
            {
                Create();
            }

            if (renderPass != null)
            {
                renderPass.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(renderPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            renderPass?.Dispose();
        }
    }

    /// <summary>
    /// The actual render pass that applies the fog of war vision dimming effect
    /// </summary>
    public class FogOfWarRenderPass : ScriptableRenderPass
    {
        private FogOfWarRendererFeature.Settings settings;
        private FogOfWarManager fogManager;
        private Texture2D fogOfWarTexture;
        private Color32[] texturePixels;
        private bool isInitialized;
        private int frameCount;

        private const string PROFILER_TAG = "FogOfWarVisionDimming";
        private static readonly int FogOfWarTexID = Shader.PropertyToID("_FogOfWarTex");
        private static readonly int VisionDimStrengthID = Shader.PropertyToID("_FogOfWarDimStrength");
        private static readonly int WorldBoundsMinID = Shader.PropertyToID("_WorldBoundsMin");
        private static readonly int WorldBoundsMaxID = Shader.PropertyToID("_WorldBoundsMax");

        // RenderGraph class for passing data
        private class PassData
        {
            internal Material material;
            internal Texture2D fogTexture;
            internal Bounds worldBounds;
            internal float dimStrength;
        }

        public FogOfWarRenderPass(FogOfWarRendererFeature.Settings settings)
        {
            this.settings = settings;
            this.renderPassEvent = settings.renderPassEvent;
        }

        private void Initialize()
        {
            if (isInitialized) return;

            // Find fog manager
            fogManager = FogOfWarManager.Instance;

            // If manager exists but grid isn't ready yet, just wait for next frame
            if (fogManager != null && fogManager.Grid == null)
            {
                // Grid not ready yet, will retry next frame
                return;
            }

            if (fogManager == null || fogManager.Grid == null)
            {
                // Only log warning occasionally to avoid spam
                if (frameCount % 300 == 0 && settings.enableDebugLogging)
                {
                    Debug.LogWarning("[FogOfWarRenderPass] FogOfWarManager not found or grid not initialized. Make sure FogOfWarManager is in the scene and active.");
                }
                return;
            }

            // Create fog of war vision texture
            int width = fogManager.Grid.Width;
            int height = fogManager.Grid.Height;

            fogOfWarTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            texturePixels = new Color32[width * height];

            // Initialize as unexplored (fully black)
            for (int i = 0; i < texturePixels.Length; i++)
            {
                texturePixels[i] = Color.black;
            }

            fogOfWarTexture.SetPixels32(texturePixels);
            fogOfWarTexture.Apply();

            isInitialized = true;

            Debug.Log($"[FogOfWarRenderPass] ✓ Successfully initialized! Grid: {width}x{height}");
        }

        // Modern RenderGraph API implementation
        public override void RecordRenderGraph(UnityEngine.Rendering.RenderGraphModule.RenderGraph renderGraph,
            UnityEngine.Rendering.ContextContainer frameData)
        {
            frameCount++; // Increment for warning throttling

            if (settings.fogOfWarMaterial == null)
                return;

            // Initialize if needed
            if (!isInitialized || fogManager == null)
            {
                Initialize();
                if (!isInitialized)
                    return;
            }

            // Update fog texture
            UpdateFogOfWarTexture();

            // Get camera data from frame data
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // Add render graph pass
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG, out var passData))
            {
                // Setup pass data
                passData.material = settings.fogOfWarMaterial;
                passData.fogTexture = fogOfWarTexture;
                passData.worldBounds = fogManager.Config.worldBounds;
                passData.dimStrength = settings.visionDimStrength;

                // Import camera color texture
                var cameraColorHandle = resourceData.activeColorTexture;
                builder.UseTexture(cameraColorHandle, UnityEngine.Rendering.RenderGraphModule.AccessFlags.ReadWrite);

                // Set render function
                builder.SetRenderFunc((PassData data, UnityEngine.Rendering.RenderGraphModule.RasterGraphContext context) =>
                {
                    // Set shader parameters
                    data.material.SetVector(WorldBoundsMinID, new Vector4(
                        data.worldBounds.min.x,
                        data.worldBounds.min.y,
                        data.worldBounds.min.z,
                        0
                    ));
                    data.material.SetVector(WorldBoundsMaxID, new Vector4(
                        data.worldBounds.max.x,
                        data.worldBounds.max.y,
                        data.worldBounds.max.z,
                        0
                    ));
                    data.material.SetTexture(FogOfWarTexID, data.fogTexture);
                    data.material.SetFloat(VisionDimStrengthID, data.dimStrength);

                    // Blit with fog material
                    Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }
        }

        // Legacy Execute method for compatibility mode
        [System.Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.")]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            frameCount++; // Increment for warning throttling

            // This is kept for compatibility mode, but won't be called when RenderGraph is enabled
            if (settings.fogOfWarMaterial == null)
                return;

            // Re-initialize if needed
            if (!isInitialized || fogManager == null)
            {
                Initialize();
                if (!isInitialized)
                    return;
            }

            // Update fog of war texture from grid state
            UpdateFogOfWarTexture();

            // Get command buffer
            CommandBuffer cmd = CommandBufferPool.Get(PROFILER_TAG);

            // Get the camera color target
#pragma warning disable CS0618 // Type or member is obsolete
            RTHandle cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
#pragma warning restore CS0618 // Type or member is obsolete

            // Set shader parameters
            if (fogManager != null && fogManager.Config != null)
            {
                Bounds worldBounds = fogManager.Config.worldBounds;
                settings.fogOfWarMaterial.SetVector(WorldBoundsMinID, new Vector4(
                    worldBounds.min.x,
                    worldBounds.min.y,
                    worldBounds.min.z,
                    0
                ));
                settings.fogOfWarMaterial.SetVector(WorldBoundsMaxID, new Vector4(
                    worldBounds.max.x,
                    worldBounds.max.y,
                    worldBounds.max.z,
                    0
                ));
                settings.fogOfWarMaterial.SetTexture(FogOfWarTexID, fogOfWarTexture);
                settings.fogOfWarMaterial.SetFloat(VisionDimStrengthID, settings.visionDimStrength);
            }

            // Apply the fog of war effect using Blitter
            Blitter.BlitCameraTexture(cmd, cameraColorTarget, cameraColorTarget, settings.fogOfWarMaterial, 0);

            // Execute and cleanup
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void UpdateFogOfWarTexture()
        {
            if (fogManager == null || fogManager.Grid == null || fogOfWarTexture == null)
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
                            // Fully black (100% dim)
                            targetColor = new Color(0, 0, 0, 1f);
                            break;

                        case VisionState.Explored:
                            // Semi-transparent (60% dim)
                            targetColor = new Color(0, 0, 0, 0.6f);
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

                    // Smooth transition between states
                    Color currentColor = texturePixels[pixelIndex];
                    Color newColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * 5f);

                    if (currentColor != newColor)
                    {
                        texturePixels[pixelIndex] = newColor;
                        needsUpdate = true;
                    }
                }
            }

            if (needsUpdate)
            {
                fogOfWarTexture.SetPixels32(texturePixels);
                fogOfWarTexture.Apply();
            }
        }

        public void Dispose()
        {
            if (fogOfWarTexture != null)
            {
                Object.Destroy(fogOfWarTexture);
                fogOfWarTexture = null;
            }

            texturePixels = null;
        }
    }
}