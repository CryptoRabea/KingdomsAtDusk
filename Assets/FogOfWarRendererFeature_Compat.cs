using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KingdomsAtDusk.FogOfWar
{
    /// <summary>
    /// COMPATIBILITY MODE: URP Renderer Feature for fog of war
    /// Use this if you cannot enable Render Graph in your project
    /// For Unity 6 with Render Graph, use FogOfWarRendererFeature.cs instead
    /// </summary>
    public class FogOfWarRendererFeature_Compat : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public Material fogMaterial = null;
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            [Range(0f, 1f)] public float dimStrength = 0.7f;
        }

        public Settings settings = new Settings();
        private FogOfWarRenderPass_Compat renderPass;

        public override void Create()
        {
            renderPass = new FogOfWarRenderPass_Compat(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.fogMaterial == null)
            {
                Debug.LogWarning("[FogOfWarRendererFeature_Compat] Fog material is not assigned!");
                return;
            }

            renderPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(renderPass);
        }

        protected override void Dispose(bool disposing)
        {
            renderPass?.Dispose();
        }
    }

    /// <summary>
    /// Compatibility render pass - suppresses obsolete warnings
    /// </summary>
    public class FogOfWarRenderPass_Compat : ScriptableRenderPass
    {
        private FogOfWarRendererFeature_Compat.Settings settings;
        private RTHandle tempRTHandle;
        private FogOfWarManager fogManager;
        private Texture2D fogTexture;
        private Color[] texturePixels;
        private bool isInitialized;
        private int frameCount;

        private const string PROFILER_TAG = "FogOfWarEffect";
        private ProfilingSampler profilingSampler;

        public FogOfWarRenderPass_Compat(FogOfWarRendererFeature_Compat.Settings settings)
        {
            this.settings = settings;
            this.renderPassEvent = settings.renderPassEvent;
            profilingSampler = new ProfilingSampler(PROFILER_TAG);
        }

        [System.Obsolete("This rendering path is for compatibility mode only")]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Initialize on first setup
            if (!isInitialized)
            {
                Initialize();
            }

            // Allocate temporary render texture if needed
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;

#pragma warning disable CS0618 // Type or member is obsolete
            RenderingUtils.ReAllocateHandleIfNeeded(
                ref tempRTHandle,
                descriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_TempFogOfWarTexture"
            );
#pragma warning restore CS0618
        }

        private void Initialize()
        {
            if (isInitialized) return;

            // Find fog manager using new API
#pragma warning disable CS0618 // Type or member is obsolete
            fogManager = Object.FindFirstObjectByType<FogOfWarManager>();
#pragma warning restore CS0618
            
            if (fogManager == null || fogManager.Grid == null)
            {
                Debug.LogWarning("[FogOfWarRenderPass_Compat] FogOfWarManager not found or not initialized");
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
            Debug.Log($"[FogOfWarRenderPass_Compat] Initialized! Grid: {width}x{height}");
        }

        [System.Obsolete("This rendering path is for compatibility mode only")]
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
            CommandBuffer cmd = CommandBufferPool.Get(PROFILER_TAG);

            // Get the camera color target (compatibility mode)
#pragma warning disable CS0618 // Type or member is obsolete
            RTHandle cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
#pragma warning restore CS0618

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
            using (new ProfilingScope(cmd, profilingSampler))
            {
                Blitter.BlitCameraTexture(cmd, cameraColorTarget, tempRTHandle, settings.fogMaterial, 0);
                Blitter.BlitCameraTexture(cmd, tempRTHandle, cameraColorTarget);
            }

            // Execute and cleanup
            context.ExecuteCommandBuffer(cmd);
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
                            targetColor = new Color(0, 0, 0, 1f); // Fully black (not visible)
                            break;
                        case VisionState.Explored:
                            targetColor = new Color(0, 0, 0, 0.6f); // Dimmed (visible but grayed)
                            break;
                        case VisionState.Visible:
                            targetColor = new Color(0, 0, 0, 0f); // Fully transparent (fully visible)
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

        public void Dispose()
        {
            tempRTHandle?.Release();

            if (fogTexture != null)
            {
                Object.Destroy(fogTexture);
                fogTexture = null;
            }
        }
    }
}
