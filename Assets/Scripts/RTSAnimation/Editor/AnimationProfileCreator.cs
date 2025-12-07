using UnityEngine;
using UnityEditor;
using System.IO;

namespace RTS.Units.Animation.Editor
{
    /// <summary>
    /// Editor utility for creating pre-configured animation profiles.
    /// Creates example profiles for Archer, Knight, and Mage unit types.
    /// </summary>
    public class AnimationProfileCreator : EditorWindow
    {
        private string savePath = "Assets/Data/AnimationProfiles";

        [MenuItem("RTS/Animation/Create Example Profiles")]
        public static void ShowWindow()
        {
            var window = GetWindow<AnimationProfileCreator>("Animation Profile Creator");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Animation Profile Creator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool creates example animation profiles for common unit types.\n" +
                "Profiles will be created as ScriptableObjects that you can customize.",
                MessageType.Info);

            GUILayout.Space(10);

            // Save path
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Save Path:", GUILayout.Width(100));
            savePath = EditorGUILayout.TextField(savePath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.SaveFolderPanel("Select Save Folder", savePath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    savePath = GetRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);

            // Create buttons
            if (GUILayout.Button("Create Archer Profile", GUILayout.Height(30)))
            {
                CreateArcherProfile();
            }

            if (GUILayout.Button("Create Knight Profile", GUILayout.Height(30)))
            {
                CreateKnightProfile();
            }

            if (GUILayout.Button("Create Mage Profile", GUILayout.Height(30)))
            {
                CreateMageProfile();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Create All Profiles", GUILayout.Height(40)))
            {
                CreateArcherProfile();
                CreateKnightProfile();
                CreateMageProfile();
            }

            GUILayout.Space(20);

            EditorGUILayout.HelpBox(
                "After creating profiles, assign your animation clips in the Inspector.\n" +
                "Then attach UnitAnimatorProfileLoader to your unit prefabs and assign the profile.",
                MessageType.Info);
        }

        private void CreateArcherProfile()
        {
            var profile = CreateProfile("ArcherAnimationProfile", "Archer");

            // Configure archer-specific settings
            profile.minIdleTime = 8f;
            profile.maxIdleTime = 20f;
            profile.idleActionProbability = 0.6f;
            profile.lookWeight = 0.7f;
            profile.lookSpeed = 3f;
            profile.enableLookAt = true;
            profile.animationSpeedMultiplier = 1.1f;
            profile.attackSpeedMultiplier = 1.2f;
            profile.transitionDuration = 0.15f;

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            Selection.activeObject = profile;
        }

        private void CreateKnightProfile()
        {
            var profile = CreateProfile("KnightAnimationProfile", "Knight");

            // Configure knight-specific settings
            profile.minIdleTime = 10f;
            profile.maxIdleTime = 25f;
            profile.idleActionProbability = 0.5f;
            profile.lookWeight = 0.4f;
            profile.lookSpeed = 1.5f;
            profile.enableLookAt = false;
            profile.animationSpeedMultiplier = 0.9f;
            profile.attackSpeedMultiplier = 0.8f;
            profile.transitionDuration = 0.2f;

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            Selection.activeObject = profile;
        }

        private void CreateMageProfile()
        {
            var profile = CreateProfile("MageAnimationProfile", "Mage");

            // Configure mage-specific settings
            profile.minIdleTime = 6f;
            profile.maxIdleTime = 15f;
            profile.idleActionProbability = 0.8f;
            profile.lookWeight = 0.6f;
            profile.lookSpeed = 2.5f;
            profile.enableLookAt = true;
            profile.animationSpeedMultiplier = 1.0f;
            profile.attackSpeedMultiplier = 1.5f;
            profile.transitionDuration = 0.1f;

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            Selection.activeObject = profile;
        }

        private UnitAnimationProfile CreateProfile(string fileName, string unitType)
        {
            // Ensure directory exists
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            // Create asset
            var profile = ScriptableObject.CreateInstance<UnitAnimationProfile>();

            // Set basic info
            var serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("profileName").stringValue = unitType;
            serializedProfile.FindProperty("description").stringValue =
                $"Animation profile for {unitType} units.\nAssign animation clips in the Inspector.";
            serializedProfile.ApplyModifiedProperties();

            // Save asset
            string assetPath = $"{savePath}/{fileName}.asset";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(profile, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return profile;
        }

        private string GetRelativePath(string absolutePath)
        {
            if (absolutePath.StartsWith(Application.dataPath))
            {
                return "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }
            return absolutePath;
        }
    }

    /// <summary>
    /// Quick menu items for creating individual profiles.
    /// </summary>
    public static class ProfileCreatorQuickMenu
    {
        [MenuItem("Assets/Create/RTS/Animation Profile/Archer Profile")]
        public static void CreateArcherProfile()
        {
            CreateProfile("ArcherAnimationProfile", "Archer", 0.7f, 3f, 1.1f, 1.2f);
        }

        [MenuItem("Assets/Create/RTS/Animation Profile/Knight Profile")]
        public static void CreateKnightProfile()
        {
            CreateProfile("KnightAnimationProfile", "Knight", 0.4f, 1.5f, 0.9f, 0.8f);
        }

        [MenuItem("Assets/Create/RTS/Animation Profile/Mage Profile")]
        public static void CreateMageProfile()
        {
            CreateProfile("MageAnimationProfile", "Mage", 0.6f, 2.5f, 1.0f, 1.5f);
        }

        private static void CreateProfile(string fileName, string unitType,
            float lookWeight, float lookSpeed, float animSpeed, float attackSpeed)
        {
            var profile = ScriptableObject.CreateInstance<UnitAnimationProfile>();

            // Set properties
            var serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("profileName").stringValue = unitType;
            serializedProfile.FindProperty("description").stringValue =
                $"Animation profile for {unitType} units.\nAssign animation clips in the Inspector.";
            serializedProfile.FindProperty("lookWeight").floatValue = lookWeight;
            serializedProfile.FindProperty("lookSpeed").floatValue = lookSpeed;
            serializedProfile.FindProperty("animationSpeedMultiplier").floatValue = animSpeed;
            serializedProfile.FindProperty("attackSpeedMultiplier").floatValue = attackSpeed;
            serializedProfile.FindProperty("enableLookAt").boolValue = lookWeight > 0.5f;
            serializedProfile.ApplyModifiedProperties();

            // Get selected folder path
            string path = "Assets";
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                }
                break;
            }

            // Save asset
            string assetPath = $"{path}/{fileName}.asset";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(profile, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);

        }
    }
}
