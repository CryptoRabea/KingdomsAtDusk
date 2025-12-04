using UnityEngine;
using System.Collections;

namespace KAD.UI.FloatingNumbers
{
    /// <summary>
    /// Blood decal that appears on the ground and fades out over time.
    /// Pooled for performance.
    /// </summary>
    public class BloodDecal : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Material material;
        private float duration;
        private Color startColor;
        private bool isFading;
        private System.Action<BloodDecal> onComplete;

        private void Awake()
        {
            // Create sprite renderer if it doesn't exist
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

                // Create a simple circular sprite programmatically
                spriteRenderer.sprite = CreateCircleSprite();
                spriteRenderer.sortingOrder = -100; // Below everything else

                // Set material
                material = new Material(Shader.Find("Sprites/Default"));
                spriteRenderer.material = material;
            }
            else
            {
                material = spriteRenderer.material;
            }

            // Rotate to lie flat on ground
            transform.rotation = Quaternion.Euler(90, 0, 0);
        }

        /// <summary>
        /// Initialize and start fading the blood decal.
        /// </summary>
        public void Initialize(
            Vector3 position,
            Color bloodColor,
            float duration,
            float size,
            System.Action<BloodDecal> onComplete)
        {
            // Position slightly above ground to avoid z-fighting
            transform.position = position + Vector3.up * 0.01f;

            // Random rotation for variety
            transform.rotation = Quaternion.Euler(90, Random.Range(0f, 360f), 0);

            // Random size variation
            float randomSize = size * Random.Range(0.7f, 1.3f);
            transform.localScale = Vector3.one * randomSize;

            // Set color
            startColor = bloodColor;
            spriteRenderer.color = startColor;

            this.duration = duration;
            this.onComplete = onComplete;
            isFading = true;

            gameObject.SetActive(true);
            StartCoroutine(FadeOutCoroutine());
        }

        private IEnumerator FadeOutCoroutine()
        {
            // Stay visible for 70% of duration
            float visibleTime = duration * 0.7f;
            yield return new WaitForSeconds(visibleTime);

            // Fade out for remaining 30%
            float fadeTime = duration * 0.3f;
            float fadeElapsed = 0f;

            while (fadeElapsed < fadeTime)
            {
                fadeElapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, fadeElapsed / fadeTime);
                Color newColor = startColor;
                newColor.a = alpha;
                spriteRenderer.color = newColor;
                yield return null;
            }

            isFading = false;
            gameObject.SetActive(false);
            onComplete?.Invoke(this);
        }

        /// <summary>
        /// Force stop fading and return to pool.
        /// </summary>
        public void ForceStop()
        {
            if (isFading)
            {
                StopAllCoroutines();
                isFading = false;
                gameObject.SetActive(false);
                onComplete?.Invoke(this);
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            isFading = false;
        }

        /// <summary>
        /// Create a simple circular sprite for the blood decal.
        /// </summary>
        private Sprite CreateCircleSprite()
        {
            int resolution = 64;
            Texture2D texture = new Texture2D(resolution, resolution);

            Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
            float radius = resolution / 2f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);

                    // Create soft edge
                    float alpha = 1f - Mathf.Clamp01((distance - radius * 0.7f) / (radius * 0.3f));

                    // Add some random splattering
                    if (distance < radius)
                    {
                        float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                        alpha *= noise > 0.3f ? 1f : 0.5f;
                    }

                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f),
                resolution / 2f
            );
        }

        /// <summary>
        /// Check if this decal is currently active.
        /// </summary>
        public bool IsActive()
        {
            return isFading;
        }
    }
}
