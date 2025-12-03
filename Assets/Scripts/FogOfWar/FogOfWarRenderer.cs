using UnityEngine;
using System.Collections.Generic;
using KingdomsAtDusk.FogOfWar;


/// <summary>
/// Renders fog of war overlay on the game view using a mesh-based approach
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class FogOfWarRenderer : MonoBehaviour, IFogRenderer
    {
        [Header("Settings")]
        [SerializeField] private Material fogMaterial;
        [SerializeField] private float fogHeight = 50f;
        [SerializeField] private int chunksPerUpdate = 10; // Number of chunks to update per frame

        private FogOfWarManager manager;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh fogMesh;
        private Texture2D fogTexture;
        private Color[] texturePixels;
        private bool isDirty;
        private Queue<Vector2Int> updateQueue = new Queue<Vector2Int>();
        private bool isInitialized;

        // IFogRenderer implementation
        public bool IsInitialized => isInitialized;
        public bool IsEnabled
        {
            get => meshRenderer != null && meshRenderer.enabled;
            set => SetEnabled(value);
        }

        public void Initialize(FogOfWarManager manager)
        {
            this.manager = manager;

            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            // Check if material is assigned
            if (fogMaterial == null)
            {
                Debug.LogWarning("[FogOfWarRenderer] No fog material assigned! Disabling mesh renderer to prevent pink screen.");
                meshRenderer.enabled = false;
                return;
            }

            CreateFogMesh();
            CreateFogTexture();

            if (fogMaterial != null)
            {
                meshRenderer.material = fogMaterial;
                meshRenderer.material.SetTexture("_FogTex", fogTexture);
            }

            isInitialized = true;
            Debug.Log("[FogOfWarRenderer] Initialized");
        }

        private void CreateFogMesh()
        {
            Bounds bounds = manager.Boundary.Bounds;

            // Create a simple quad mesh covering the world bounds
            fogMesh = new Mesh();
            fogMesh.name = "FogOfWarMesh";

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(bounds.min.x, fogHeight, bounds.min.z), // Bottom-left
                new Vector3(bounds.max.x, fogHeight, bounds.min.z), // Bottom-right
                new Vector3(bounds.min.x, fogHeight, bounds.max.z), // Top-left
                new Vector3(bounds.max.x, fogHeight, bounds.max.z)  // Top-right
            };

            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            int[] triangles = new int[6]
            {
                0, 2, 1,
                2, 3, 1
            };

            fogMesh.vertices = vertices;
            fogMesh.uv = uv;
            fogMesh.triangles = triangles;
            fogMesh.RecalculateNormals();

            meshFilter.mesh = fogMesh;
        }

        private void CreateFogTexture()
        {
            int width = manager.Grid.Width;
            int height = manager.Grid.Height;

            fogTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            texturePixels = new Color[width * height];

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

        public void UpdateRenderer()
        {
            if (!isDirty || !meshRenderer.enabled) return;

            UpdateFogTexture();
        }

        private void Update()
        {
            UpdateRenderer();
        }

        private void UpdateFogTexture()
        {
            if (manager == null || manager.Grid == null) return;

            bool needsApply = false;
            int updatesThisFrame = 0;

            // Update texture based on grid state
            for (int x = 0; x < manager.Grid.Width && updatesThisFrame < chunksPerUpdate * 100; x++)
            {
                for (int y = 0; y < manager.Grid.Height && updatesThisFrame < chunksPerUpdate * 100; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    VisionState state = manager.Grid.GetState(cell);
                    float alpha = manager.Grid.GetVisibilityAlpha(cell);

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
                            targetColor = new Color(0, 0, 0, 0); // Fully transparent
                            break;
                        default:
                            targetColor = Color.black;
                            break;
                    }

                    // Apply fade based on visibility alpha
                    if (state == VisionState.Explored)
                    {
                        targetColor.a = Mathf.Lerp(manager.Config.exploredColor.a, 0f, alpha);
                    }

                    int pixelIndex = y * manager.Grid.Width + x;

                    if (texturePixels[pixelIndex] != targetColor)
                    {
                        texturePixels[pixelIndex] = Color.Lerp(texturePixels[pixelIndex], targetColor, Time.deltaTime * manager.Config.fadeSpeed);
                        needsApply = true;
                    }

                    updatesThisFrame++;
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
        /// Enable or disable fog of war rendering
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            if (meshRenderer != null && fogMaterial != null)
            {
                meshRenderer.enabled = enabled;
            }
        }

        private void OnDestroy()
        {
            if (fogTexture != null)
            {
                Destroy(fogTexture);
            }

            if (fogMesh != null)
            {
                Destroy(fogMesh);
            }
        }
    }

