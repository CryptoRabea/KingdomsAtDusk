using UnityEngine;
using UnityEditor;
using RTS.Core;

namespace RTS.Editor
{
    /// <summary>
    /// Editor menu items to help set up build optimization scripts.
    /// </summary>
    public static class BuildSetupMenu
    {
        [MenuItem("Tools/RTS/Build Setup/Add ShaderPreloader to Scene")]
        private static void AddShaderPreloader()
        {
            // Check if already exists
            ShaderPreloader existing = Object.FindAnyObjectByType<ShaderPreloader>();
            if (existing != null)
            {
                EditorGUIUtility.PingObject(existing.gameObject);
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // Create new GameObject with ShaderPreloader
            GameObject preloaderObj = new GameObject("ShaderPreloader");
            ShaderPreloader preloader = preloaderObj.AddComponent<ShaderPreloader>();

            // Try to find and add fog of war material
            string[] materialGuids = AssetDatabase.FindAssets("t:Material fog", new[] { "Assets/AOSFogWar" });
            if (materialGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(materialGuids[0]);
                Material fogMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (fogMaterial != null)
                {
                    SerializedObject so = new SerializedObject(preloader);
                    SerializedProperty materialsArray = so.FindProperty("criticalMaterials");
                    materialsArray.arraySize = 1;
                    materialsArray.GetArrayElementAtIndex(0).objectReferenceValue = fogMaterial;
                    so.ApplyModifiedProperties();
                }
            }

            // Select the new object
            Selection.activeGameObject = preloaderObj;
            EditorGUIUtility.PingObject(preloaderObj);

            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        }

        [MenuItem("Tools/RTS/Build Setup/Add Build Diagnostics to Scene")]
        private static void AddBuildDiagnostics()
        {
            // Check if already exists
            BuildDiagnostics existing = Object.FindAnyObjectByType<BuildDiagnostics>();
            if (existing != null)
            {
                EditorGUIUtility.PingObject(existing.gameObject);
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // Create new GameObject with BuildDiagnostics
            GameObject diagnosticsObj = new GameObject("BuildDiagnostics");
            diagnosticsObj.AddComponent<BuildDiagnostics>();

            // Select the new object
            Selection.activeGameObject = diagnosticsObj;
            EditorGUIUtility.PingObject(diagnosticsObj);

            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        }

        [MenuItem("Tools/RTS/Build Setup/Setup All Build Optimizations")]
        private static void SetupAllBuildOptimizations()
        {

            // Add ShaderPreloader
            if (Object.FindAnyObjectByType<ShaderPreloader>() == null)
            {
                AddShaderPreloader();
            }
            else
            {
            }

            // Add BuildDiagnostics
            if (Object.FindAnyObjectByType<BuildDiagnostics>() == null)
            {
                AddBuildDiagnostics();
            }
            else
            {
            }


            EditorUtility.DisplayDialog(
                "Build Optimization Setup",
                "Build optimization scripts have been added to your scene!\n\n" +
                "✓ BuildInitializer - Runs automatically in all builds\n" +
                "✓ ShaderPreloader - Added to scene (configure materials in Inspector)\n" +
                "✓ BuildDiagnostics - Press 'D' in build to show diagnostics\n\n" +
                "See BUILD_ISSUES_FIX_GUIDE.md for detailed instructions.",
                "OK");
        }

        [MenuItem("Tools/RTS/Build Setup/Show Build Guide")]
        private static void ShowBuildGuide()
        {
            string guidePath = "Assets/../BUILD_ISSUES_FIX_GUIDE.md";
            Object guideAsset = AssetDatabase.LoadAssetAtPath<Object>(guidePath);

            if (guideAsset != null)
            {
                EditorGUIUtility.PingObject(guideAsset);
                Selection.activeObject = guideAsset;
            }
            else
            {
                // Try to find it
                string[] guids = AssetDatabase.FindAssets("BUILD_ISSUES_FIX_GUIDE");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    guideAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
                    EditorGUIUtility.PingObject(guideAsset);
                    Selection.activeObject = guideAsset;
                }
                else
                {
                }
            }
        }

        [MenuItem("Tools/RTS/Build Setup/Configure Build Settings")]
        private static void ConfigureBuildSettings()
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Configure Build Settings",
                "This will configure recommended build settings for optimal performance:\n\n" +
                "• Set Color Space to Linear\n" +
                "• Configure Graphics APIs (DirectX11)\n" +
                "• Set Fullscreen mode to Fullscreen Window\n\n" +
                "Continue?",
                "Yes", "Cancel");

            if (!proceed) return;

            // Set color space to Linear
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // Set graphics APIs for Windows
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
                new UnityEngine.Rendering.GraphicsDeviceType[]
                {
                    UnityEngine.Rendering.GraphicsDeviceType.Direct3D11,
                    UnityEngine.Rendering.GraphicsDeviceType.Direct3D12
                });

            // Set fullscreen mode
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;

            // Set default resolution
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;


            EditorUtility.DisplayDialog(
                "Build Settings Configured",
                "Build settings have been configured for optimal performance!\n\n" +
                "Next steps:\n" +
                "1. Build your game (File → Build Settings → Build)\n" +
                "2. Test the build\n" +
                "3. Press 'D' in build to show diagnostics\n" +
                "4. Press 'P' to show FPS counter",
                "OK");
        }
    }
}
