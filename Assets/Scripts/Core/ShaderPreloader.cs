using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

namespace RTS.Core
{
    /// <summary>
    /// Preloads and warms up shaders and materials to prevent:
    /// - Black screens on first render
    /// - Missing textures
    /// - Shader compilation stutters
    /// - Material initialization issues
    /// </summary>
    public class ShaderPreloader : MonoBehaviour
    {
        [Header("Preload Settings")]
        [SerializeField] private bool preloadOnStart = true;
        [SerializeField] private bool createDummyObjects = true;
        [SerializeField] private float preloadDuration = 2f;

        [Header("Materials to Preload")]
        [SerializeField] private Material[] criticalMaterials;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private List<GameObject> dummyObjects = new List<GameObject>();

        private void Start()
        {
            if (preloadOnStart)
            {
                StartCoroutine(PreloadShaders());
            }
        }

        private IEnumerator PreloadShaders()
        {
            LogDebug("=== Shader Preloader Starting ===");

            // Step 1: Warmup all shaders in build
            yield return StartCoroutine(WarmupAllShaders());

            // Step 2: Preload critical materials
            yield return StartCoroutine(PreloadCriticalMaterials());

            // Step 3: Create dummy objects to force material initialization
            if (createDummyObjects)
            {
                yield return StartCoroutine(CreateDummyRenderObjects());
            }

            // Step 4: Wait for rendering
            yield return new WaitForSeconds(preloadDuration);

            // Step 5: Clean up dummy objects
            CleanupDummyObjects();

            LogDebug("=== Shader Preloader Complete ===");
        }

        private IEnumerator WarmupAllShaders()
        {
            LogDebug("Warming up all shaders...");

            // Warmup built-in shaders
            Shader.WarmupAllShaders();

            yield return null;

            // Find and warmup URP shaders
            string[] urpShaderNames = new string[]
            {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Unlit",
                "Universal Render Pipeline/Terrain/Lit",
                "Shader Graphs/FogOfWar",
                "Hidden/Universal Render Pipeline/FallbackError",
            };

            foreach (string shaderName in urpShaderNames)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    // Create a temporary material to ensure shader is loaded
                    Material tempMat = new Material(shader);
                    Destroy(tempMat);
                    LogDebug($"Warmed up shader: {shaderName}");
                }
            }

            yield return null;
        }

        private IEnumerator PreloadCriticalMaterials()
        {
            if (criticalMaterials == null || criticalMaterials.Length == 0)
            {
                LogDebug("No critical materials assigned for preloading");
                yield break;
            }

            LogDebug($"Preloading {criticalMaterials.Length} critical materials...");

            foreach (Material mat in criticalMaterials)
            {
                if (mat != null)
                {
                    // Access material properties to force initialization
                    _ = mat.shader;
                    _ = mat.mainTexture;
                    _ = mat.color;

                    // Set and get a property to ensure material is fully loaded
                    if (mat.HasProperty("_MainTex"))
                    {
                        Texture tex = mat.GetTexture("_MainTex");
                        if (tex != null)
                        {
                            // Force texture load
                            tex.filterMode = tex.filterMode;
                        }
                    }

                    LogDebug($"Preloaded material: {mat.name}");
                }
                yield return null;
            }
        }

        private IEnumerator CreateDummyRenderObjects()
        {
            LogDebug("Creating dummy render objects for material initialization...");

            // Create a dummy camera if needed
            Camera dummyCamera = null;
            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("DummyCamera");
                dummyCamera = camObj.AddComponent<Camera>();
                dummyCamera.enabled = false;
                dummyObjects.Add(camObj);
            }

            // Create dummy quads with critical materials
            if (criticalMaterials != null)
            {
                for (int i = 0; i < criticalMaterials.Length; i++)
                {
                    Material mat = criticalMaterials[i];
                    if (mat == null) continue;

                    GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.name = $"DummyQuad_{mat.name}";
                    quad.transform.position = new Vector3(1000, 1000, 1000); // Far away
                    quad.GetComponent<MeshRenderer>().material = mat;

                    // Disable collider
                    Collider col = quad.GetComponent<Collider>();
                    if (col != null) Destroy(col);

                    dummyObjects.Add(quad);

                    yield return null;
                }
            }

            // Force a render
            yield return new WaitForEndOfFrame();

            LogDebug($"Created {dummyObjects.Count} dummy objects");
        }

        private void CleanupDummyObjects()
        {
            LogDebug("Cleaning up dummy objects...");

            foreach (GameObject obj in dummyObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            dummyObjects.Clear();
            LogDebug("Dummy objects cleaned up");
        }

        private void LogDebug(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[ShaderPreloader] {message}");
            }
        }

        // Public API
        public void PreloadMaterial(Material material)
        {
            if (material == null) return;

            _ = material.shader;
            _ = material.mainTexture;
            _ = material.color;

            LogDebug($"Manually preloaded material: {material.name}");
        }

        public void PreloadShader(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null)
            {
                Material tempMat = new Material(shader);
                Destroy(tempMat);
                LogDebug($"Manually preloaded shader: {shaderName}");
            }
            else
            {
                Debug.LogWarning($"[ShaderPreloader] Shader not found: {shaderName}");
            }
        }

        private void OnDestroy()
        {
            CleanupDummyObjects();
        }
    }
}
