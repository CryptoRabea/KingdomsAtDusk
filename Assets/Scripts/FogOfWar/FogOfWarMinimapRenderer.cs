using UnityEngine;
using UnityEngine.UI;

namespace KingdomsAtDusk.FogOfWar
{
    /// <summary>
    /// Renders fog of war on the minimap using a texture overlay
    /// </summary>
    public class FogOfWarMinimapRenderer : MonoBehaviour
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

            // Initialize all pixels as unexplored (black)
            for (int i = 0; i < texturePixels.Length; i++)
            {
                texturePixels[i] = manager.Config.unexploredColor;
            }

            fogTexture.SetPixels(texturePixels);
            fogTexture.Apply();
        }

        public void OnVisionUpdated()
        {
            isDirty = true;
        }

        private void Update()
        {
            if (!enableMinimapFog || !isDirty) return;

            UpdateMinimapTexture();
        }

        private void UpdateMinimapTexture()
        {
            if (manager == null || manager.Grid == null) return;

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
                            targetColor = new Color(0, 0, 0, 1f); // Black
                            break;
                        case VisionState.Explored:
                            targetColor = new Color(0, 0, 0, 0.5f); // Semi-transparent
                            break;
                        case VisionState.Visible:
                            targetColor = new Color(0, 0, 0, 0f); // Fully transparent
                            break;
                        default:
                            targetColor = Color.black;
                            break;
                    }

                    int pixelIndex = ty * textureSize + tx;

                    if (texturePixels[pixelIndex] != targetColor)
                    {
                        texturePixels[pixelIndex] = Color.Lerp(
                            texturePixels[pixelIndex],
                            targetColor,
                            Time.deltaTime * manager.Config.fadeSpeed * 2f
                        );
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
}
