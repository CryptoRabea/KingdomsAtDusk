using UnityEngine;

namespace RTS.Buildings
{
    /// <summary>
    /// Construction visual that shows a wireframe/scaffolding effect during construction.
    /// Gradually transitions from wireframe to solid as construction progresses.
    /// </summary>
    public class ScaffoldingConstructionVisual : BaseConstructionVisual
    {
        [Header("Scaffolding Settings")]
        [SerializeField] private bool showWireframe = true;
        [SerializeField] private Color wireframeColor = new Color(1f, 0.5f, 0f, 1f); // Orange wireframe
        [SerializeField] private float wireframeThickness = 0.02f;
        [SerializeField] private float transitionPoint = 0.7f; // When to start transitioning to solid (0-1)

        [Header("Grid Overlay")]
        [SerializeField] private bool showGridOverlay = true;
        [SerializeField] private Material gridMaterial; // Optional grid material
        [SerializeField] private float gridSize = 0.5f;

        [Header("Particle Effects")]
        [SerializeField] private bool spawnConstructionParticles = true;
        [SerializeField] private GameObject sparkParticlePrefab; // Sparks/welding particles
        [SerializeField] private int particlesPerUpdate = 2;

        [Header("Audio")]
        [SerializeField] private AudioClip[] constructionSounds;
        [SerializeField] private float soundInterval = 1f;

        private GameObject wireframeObject;
        private Material[] originalMaterials;
        private MaterialPropertyBlock propertyBlock;
        private float lastSoundTime = 0f;
        private AudioSource audioSource;

        // Shader property IDs
        private static readonly int ColorPropertyID = Shader.PropertyToID("_Color");
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

        protected override void Initialize()
        {
            propertyBlock = new MaterialPropertyBlock();

            // Create wireframe visualization
            if (showWireframe)
            {
                CreateWireframe();
            }

            // Set up audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && constructionSounds != null && constructionSounds.Length > 0)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.maxDistance = 50f;
                audioSource.volume = 0.5f;
            }

            // Initialize materials
            StoreOriginalMaterials();

            // Start with building semi-transparent
            SetBuildingAlpha(0.3f);

            UpdateVisual(0f);
        }

        private void CreateWireframe()
        {
            // Create a wireframe representation using line renderers
            wireframeObject = new GameObject("Wireframe");
            wireframeObject.transform.SetParent(transform);
            wireframeObject.transform.localPosition = Vector3.zero;
            wireframeObject.transform.localRotation = Quaternion.identity;
            wireframeObject.transform.localScale = Vector3.one;

            // Draw wireframe boxes around the building bounds
            CreateWireframeCube(combinedBounds);
        }

        private void CreateWireframeCube(Bounds bounds)
        {
            // Create 12 edges of a cube using line renderers
            Vector3 center = transform.InverseTransformPoint(bounds.center);
            Vector3 size = bounds.size;

            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-size.x, -size.y, -size.z) * 0.5f;
            corners[1] = center + new Vector3(size.x, -size.y, -size.z) * 0.5f;
            corners[2] = center + new Vector3(size.x, -size.y, size.z) * 0.5f;
            corners[3] = center + new Vector3(-size.x, -size.y, size.z) * 0.5f;
            corners[4] = center + new Vector3(-size.x, size.y, -size.z) * 0.5f;
            corners[5] = center + new Vector3(size.x, size.y, -size.z) * 0.5f;
            corners[6] = center + new Vector3(size.x, size.y, size.z) * 0.5f;
            corners[7] = center + new Vector3(-size.x, size.y, size.z) * 0.5f;

            // Define 12 edges
            int[,] edges = new int[,]
            {
                {0, 1}, {1, 2}, {2, 3}, {3, 0}, // Bottom
                {4, 5}, {5, 6}, {6, 7}, {7, 4}, // Top
                {0, 4}, {1, 5}, {2, 6}, {3, 7}  // Vertical
            };

            for (int i = 0; i < edges.GetLength(0); i++)
            {
                CreateLine(corners[edges[i, 0]], corners[edges[i, 1]], $"Edge_{i}");
            }

            // Add grid lines if enabled
            if (showGridOverlay)
            {
                CreateGridLines(bounds);
            }
        }

        private void CreateLine(Vector3 start, Vector3 end, string name)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.SetParent(wireframeObject.transform);
            lineObj.transform.localPosition = Vector3.zero;

            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.startWidth = wireframeThickness;
            line.endWidth = wireframeThickness;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = wireframeColor;
            line.endColor = wireframeColor;
            line.useWorldSpace = false;
        }

        private void CreateGridLines(Bounds bounds)
        {
            // Create horizontal grid lines on each face
            Vector3 center = transform.InverseTransformPoint(bounds.center);
            Vector3 size = bounds.size;

            // Only create a few grid lines to avoid clutter
            int gridLines = Mathf.CeilToInt(size.y / gridSize);
            float yStart = center.y - size.y * 0.5f;

            for (int i = 0; i <= gridLines; i++)
            {
                float y = yStart + i * gridSize;
                if (y > center.y + size.y * 0.5f) break;

                // Front and back grid lines
                CreateLine(
                    new Vector3(center.x - size.x * 0.5f, y, center.z + size.z * 0.5f),
                    new Vector3(center.x + size.x * 0.5f, y, center.z + size.z * 0.5f),
                    $"GridH_Front_{i}"
                );
            }
        }

        private void StoreOriginalMaterials()
        {
            int totalMaterials = 0;
            foreach (var rend in renderers)
            {
                if (rend != null)
                    totalMaterials += rend.sharedMaterials.Length;
            }

            originalMaterials = new Material[totalMaterials];

            int index = 0;
            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                foreach (var mat in rend.sharedMaterials)
                {
                    originalMaterials[index] = mat;
                    index++;
                }
            }
        }

        protected override void UpdateVisual(float progress)
        {
            // Wireframe fades out as construction progresses
            if (wireframeObject != null)
            {
                float wireframeAlpha = 1f - Mathf.Clamp01(progress / transitionPoint);
                UpdateWireframeAlpha(wireframeAlpha);
            }

            // Building fades in as construction progresses
            float buildingAlpha = Mathf.Clamp01((progress - (1f - transitionPoint)) / transitionPoint);
            SetBuildingAlpha(Mathf.Lerp(0.3f, 1f, buildingAlpha));

            // Add construction glow effect
            if (progress < 1f)
            {
                ApplyConstructionGlow(progress);
            }

            // Spawn particles
            if (spawnConstructionParticles && sparkParticlePrefab != null && progress < 1f)
            {
                SpawnConstructionParticles();
            }

            // Play construction sounds
            PlayConstructionSounds();
        }

        private void UpdateWireframeAlpha(float alpha)
        {
            LineRenderer[] lines = wireframeObject.GetComponentsInChildren<LineRenderer>();
            foreach (var line in lines)
            {
                Color color = wireframeColor;
                color.a = alpha;
                line.startColor = color;
                line.endColor = color;
            }
        }

        private void SetBuildingAlpha(float alpha)
        {
            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                rend.GetPropertyBlock(propertyBlock);

                Color color = Color.white;
                color.a = alpha;

                propertyBlock.SetColor(ColorPropertyID, color);
                propertyBlock.SetColor(BaseColorID, color);

                rend.SetPropertyBlock(propertyBlock);
            }
        }

        private void ApplyConstructionGlow(float progress)
        {
            // Pulse orange glow during construction
            float glowIntensity = Mathf.Sin(Time.time * 2f) * 0.5f + 0.5f;
            Color glowColor = wireframeColor * glowIntensity * (1f - progress);

            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(EmissionColorID, glowColor);
                rend.SetPropertyBlock(propertyBlock);
            }
        }

        private void SpawnConstructionParticles()
        {
            // Spawn sparks at random positions on the building
            for (int i = 0; i < particlesPerUpdate; i++)
            {
                Vector3 spawnPos = new Vector3(
                    Random.Range(combinedBounds.min.x, combinedBounds.max.x),
                    Random.Range(combinedBounds.min.y, combinedBounds.max.y),
                    Random.Range(combinedBounds.min.z, combinedBounds.max.z)
                );

                GameObject particle = Instantiate(sparkParticlePrefab, spawnPos, Quaternion.identity);
                particle.transform.SetParent(transform);
                Destroy(particle, 1f);
            }
        }

        private void PlayConstructionSounds()
        {
            if (audioSource == null || constructionSounds == null || constructionSounds.Length == 0)
                return;

            if (Time.time - lastSoundTime >= soundInterval)
            {
                lastSoundTime = Time.time;
                AudioClip clip = constructionSounds[Random.Range(0, constructionSounds.Length)];
                audioSource.PlayOneShot(clip);
            }
        }

        protected override void Cleanup()
        {
            // Remove wireframe
            if (wireframeObject != null)
            {
                Destroy(wireframeObject);
            }

            // Reset building materials
            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorPropertyID, Color.white);
                propertyBlock.SetColor(BaseColorID, Color.white);
                propertyBlock.SetColor(EmissionColorID, Color.black);
                rend.SetPropertyBlock(propertyBlock);
            }
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Draw construction progress
            Gizmos.color = wireframeColor;
            float height = combinedBounds.size.y * currentProgress;
            Gizmos.DrawWireCube(
                new Vector3(combinedBounds.center.x, combinedBounds.min.y + height * 0.5f, combinedBounds.center.z),
                new Vector3(combinedBounds.size.x, height, combinedBounds.size.z)
            );
        }
#endif
    }
}
