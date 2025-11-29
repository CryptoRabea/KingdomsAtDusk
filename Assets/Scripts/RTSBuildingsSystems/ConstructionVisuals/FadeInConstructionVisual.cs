using UnityEngine;

namespace RTS.Buildings
{
    /// <summary>
    /// Construction visual that makes the building materialize by fading in.
    /// Gradually increases opacity from transparent to solid.
    /// </summary>
    public class FadeInConstructionVisual : BaseConstructionVisual
    {
        [Header("Fade Settings")]
        [SerializeField] private float startAlpha = 0f; // Starting transparency (0 = invisible)
        [SerializeField] private float endAlpha = 1f; // Ending transparency (1 = solid)
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Material Settings")]
        [SerializeField] private bool createMaterialInstances = true; // Create material instances or use property blocks
        [SerializeField] private Color constructionTint = new Color(0.4f, 0.8f, 1f, 1f); // Blue construction tint
        [SerializeField] private bool useTint = true;

        [Header("Particle Effects")]
        [SerializeField] private bool spawnParticles = true;
        [SerializeField] private GameObject materializationParticlePrefab; // Optional particle effect
        [SerializeField] private float particleSpawnRate = 0.1f; // Spawn particles every 10% progress

        private Material[] originalMaterials;
        private Material[] instancedMaterials;
        private MaterialPropertyBlock propertyBlock;
        private float lastParticleProgress = 0f;

        // Shader property IDs
        private static readonly int AlphaID = Shader.PropertyToID("_Alpha");
        private static readonly int ColorPropertyID = Shader.PropertyToID("_Color");
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

        protected override void Initialize()
        {
            propertyBlock = new MaterialPropertyBlock();

            if (createMaterialInstances)
            {
                CreateMaterialInstances();
            }

            // Reset particle spawn tracking
            lastParticleProgress = 0f;

            UpdateVisual(0f);
        }

        private void CreateMaterialInstances()
        {
            // Store original materials
            int totalMaterials = 0;
            foreach (var rend in renderers)
            {
                if (rend != null)
                    totalMaterials += rend.sharedMaterials.Length;
            }

            originalMaterials = new Material[totalMaterials];
            instancedMaterials = new Material[totalMaterials];

            int index = 0;
            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                Material[] materials = rend.sharedMaterials;
                Material[] instances = new Material[materials.Length];

                for (int i = 0; i < materials.Length; i++)
                {
                    originalMaterials[index] = materials[i];

                    // Create instance
                    instances[i] = new Material(materials[i]);

                    // Enable transparency if not already enabled
                    EnableTransparency(instances[i]);

                    instancedMaterials[index] = instances[i];
                    index++;
                }

                rend.materials = instances;
            }
        }

        private void EnableTransparency(Material mat)
        {
            // Set rendering mode to transparent (Unity Standard Shader)
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000; // Transparent queue
        }

        protected override void UpdateVisual(float progress)
        {
            // Apply fade curve
            float curvedProgress = fadeCurve.Evaluate(progress);

            // Calculate current alpha
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, curvedProgress);

            // Update materials or property blocks
            if (createMaterialInstances)
            {
                UpdateMaterialAlpha(currentAlpha, curvedProgress);
            }
            else
            {
                UpdatePropertyBlockAlpha(currentAlpha, curvedProgress);
            }

            // Spawn particles at intervals
            if (spawnParticles && materializationParticlePrefab != null)
            {
                SpawnParticlesIfNeeded(progress);
            }
        }

        private void UpdateMaterialAlpha(float alpha, float progress)
        {
            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                Material[] materials = rend.materials;
                foreach (var mat in materials)
                {
                    if (mat == null) continue;

                    Color color = mat.color;

                    // Apply construction tint
                    if (useTint)
                    {
                        Color tintedColor = Color.Lerp(constructionTint, Color.white, progress);
                        color.r = tintedColor.r;
                        color.g = tintedColor.g;
                        color.b = tintedColor.b;
                    }

                    color.a = alpha;
                    mat.color = color;

                    // Also try setting base color for URP/HDRP
                    if (mat.HasProperty(BaseColorID))
                    {
                        mat.SetColor(BaseColorID, color);
                    }
                }
            }
        }

        private void UpdatePropertyBlockAlpha(float alpha, float progress)
        {
            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                rend.GetPropertyBlock(propertyBlock);

                Color color = Color.white;
                if (useTint)
                {
                    color = Color.Lerp(constructionTint, Color.white, progress);
                }

                color.a = alpha;

                propertyBlock.SetColor(ColorPropertyID, color);
                propertyBlock.SetColor(BaseColorID, color);
                propertyBlock.SetFloat(AlphaID, alpha);

                rend.SetPropertyBlock(propertyBlock);
            }
        }

        private void SpawnParticlesIfNeeded(float progress)
        {
            // Spawn particles at regular intervals
            if (progress - lastParticleProgress >= particleSpawnRate)
            {
                lastParticleProgress = progress;

                // Spawn particle at random position on building bounds
                Vector3 spawnPos = new Vector3(
                    Random.Range(combinedBounds.min.x, combinedBounds.max.x),
                    Random.Range(combinedBounds.min.y, combinedBounds.max.y),
                    Random.Range(combinedBounds.min.z, combinedBounds.max.z)
                );

                GameObject particle = Instantiate(materializationParticlePrefab, spawnPos, Quaternion.identity);
                Destroy(particle, 2f); // Clean up after 2 seconds
            }
        }

        protected override void Cleanup()
        {
            // Restore original materials
            if (createMaterialInstances && originalMaterials != null)
            {
                int index = 0;
                foreach (var rend in renderers)
                {
                    if (rend == null) continue;

                    Material[] materials = rend.materials;
                    Material[] originals = new Material[materials.Length];

                    for (int i = 0; i < materials.Length && index < originalMaterials.Length; i++)
                    {
                        originals[i] = originalMaterials[index];

                        // Destroy instanced material
                        if (instancedMaterials[index] != null)
                        {
                            Destroy(instancedMaterials[index]);
                        }

                        index++;
                    }

                    rend.materials = originals;
                }
            }
            else
            {
                // Reset property blocks
                foreach (var rend in renderers)
                {
                    if (rend == null) continue;

                    rend.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetColor(ColorPropertyID, Color.white);
                    propertyBlock.SetColor(BaseColorID, Color.white);
                    propertyBlock.SetFloat(AlphaID, 1f);
                    rend.SetPropertyBlock(propertyBlock);
                }
            }
        }

        private void OnDestroy()
        {
            // Cleanup material instances
            if (instancedMaterials != null)
            {
                foreach (var mat in instancedMaterials)
                {
                    if (mat != null)
                    {
                        Destroy(mat);
                    }
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Test Fade In")]
        private void TestFadeIn()
        {
            StartCoroutine(TestFadeCoroutine());
        }

        private System.Collections.IEnumerator TestFadeCoroutine()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 0.2f; // 5 second test
                UpdateVisual(t);
                yield return null;
            }
        }
#endif
    }
}
