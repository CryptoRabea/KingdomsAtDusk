using KingdomsAtDusk.FogOfWar;
using UnityEngine;
using UnityEngine.UI;


    /// <summary>
    /// Renders fog of war on the minimap using a texture overlay
    /// </summary>
    public class FogOfWarMinimapRenderer : MonoBehaviour, IFogRenderer
    {
        [Header("References")]
        [SerializeField] private RawImage fogOverlay;

        [Header("Settings")]
        [SerializeField] private bool enableMinimapFog = true;
        [SerializeField] private int textureSize = 512;

        private FogOfWarManager manager;
        private Texture2D fogTexture;
        private Color[] texturePixels;
        private bool isDirty;
        private bool isInitialized;

        // IFogRenderer implementation
        public bool IsInitialized => isInitialized;
        public bool IsEnabled
        {
            get => enableMinimapFog;
            set => SetEnabled(value);
        }

        public void Initialize(FogOfWarManager manager)
        {
            this.manager = manager;

            if (!enableMinimapFog)
            {
                if (fogOverlay != null)
                {
                    fogOverlay.gameObject.SetActive(false);
                }
                return;
            }

            CreateMinimapFogTexture();

            if (fogOverlay != null)
            {
                fogOverlay.texture = fogTexture;
                fogOverlay.gameObject.SetActive(true);
            }

            isInitialized = true;
            Debug.Log("[FogOfWarMinimapRenderer] Initialized");
        }

        private void CreateMinimapFogTexture()
        {
            fogTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            texturePixels = new Color[textureSize * textureSize];

            // Initialize all pixels as unexplored (slightly less opaque so minimap is still somewhat usable)
            Color initialColor = new Color(0, 0, 0, 1f);
            for (int i = 0; i < texturePixels.Length; i++)
            {
                texturePixels[i] = initialColor;
            }

            fogTexture.SetPixels(texturePixels);
            fogTexture.Apply();
        }

        public void OnVisionUpdated()
        {
            isDirty = true;
        }

        public void UpdateRenderer()
        {
            if (!enableMinimapFog || !isDirty) return;

            UpdateMinimapTexture();
        }

        private void Update()
        {
            UpdateRenderer();
        }

        private void UpdateMinimapTexture()
        {
            if (manager == null || manager.Grid == null)
            {
                // If fog of war isn't working, make the overlay fully transparent so the minimap is usable
                if (fogOverlay != null && fogOverlay.color.a > 0.1f)
                {
                    fogOverlay.color = new Color(1, 1, 1, 0f);
                }
                return;
            }

            // Ensure overlay is fully opaque when fog is active
            if (fogOverlay != null && fogOverlay.color.a < 0.9f)
            {
                fogOverlay.color = new Color(1, 1, 1, 1f);
            }

            // Map fog of war grid to minimap texture
            float scaleX = (float)textureSize / manager.Grid.Width;
            float scaleY = (float)textureSize / manager.Grid.Height;

            bool needsApply = false;

            for (int tx = 0; tx < textureSize; tx++)
            {
                for (int ty = 0; ty < textureSize; ty++)
                {
                    // Map texture pixel to grid cell
                    int gridX = Mathf.FloorToInt(tx / scaleX);
                    int gridY = Mathf.FloorToInt(ty / scaleY);

                    Vector2Int cell = new Vector2Int(gridX, gridY);
                    VisionState state = manager.Grid.GetState(cell);

                    Color targetColor;

                    switch (state)
                    {
                        case VisionState.Unexplored:
                            targetColor = manager.Config.unexploredColor;
                            break;
                        case VisionState.Explored:
                            targetColor = manager.Config.exploredColor;
                            break;
                        case VisionState.Visible:
                            targetColor = manager.Config.visibleColor;
                            break;
                        default:
                            targetColor = new Color(0, 0, 0, 1f);
                            break;
                    }

                    int pixelIndex = ty * textureSize + tx;

                    Color currentColor = texturePixels[pixelIndex];
                    Color newColor = Color.Lerp(
                        currentColor,
                        targetColor,
                        Time.deltaTime * manager.Config.fadeSpeed * 2f
                    );

                    if (currentColor != newColor)
                    {
                        texturePixels[pixelIndex] = newColor;
                        needsApply = true;
                    }

                }
            }

            if (needsApply)
            {
                fogTexture.SetPixels(texturePixels);
                fogTexture.Apply();
            }

            isDirty = false;
        }

        /// <summary>
        /// Enable or disable minimap fog of war
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            enableMinimapFog = enabled;

            if (fogOverlay != null)
            {
                fogOverlay.gameObject.SetActive(enabled);
            }
        }

        private void OnDestroy()
        {
            if (fogTexture != null)
            {
                Destroy(fogTexture);
            }
        }
    }

