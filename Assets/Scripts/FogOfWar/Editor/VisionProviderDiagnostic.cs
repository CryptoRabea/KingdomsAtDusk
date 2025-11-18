using UnityEngine;
using UnityEditor;
using System.Linq;

namespace KingdomsAtDusk.FogOfWar.Editor
{
    /// <summary>
    /// Diagnostic tool to find buildings with VisionProvider at or near world center
    /// </summary>
    public class VisionProviderDiagnostic
    {
        [MenuItem("Tools/Fog of War/Find Buildings Near World Center")]
        public static void FindBuildingsNearWorldCenter()
        {
            // Find all VisionProviders in the scene
            var providers = Object.FindObjectsByType<VisionProvider>(FindObjectsSortMode.None);

            Debug.Log($"=== Vision Provider Diagnostic ===");
            Debug.Log($"Found {providers.Length} VisionProviders in scene");

            int nearCenterCount = 0;
            float centerThreshold = 50f; // Within 50 units of (0,0,0)

            foreach (var provider in providers)
            {
                Vector3 pos = provider.transform.position;
                float distanceFromCenter = new Vector3(pos.x, 0, pos.z).magnitude;

                if (distanceFromCenter < centerThreshold)
                {
                    nearCenterCount++;
                    Debug.LogWarning($"[NEAR CENTER] {provider.gameObject.name} at position {pos} (distance from center: {distanceFromCenter:F2})", provider.gameObject);
                }
                else
                {
                    Debug.Log($"{provider.gameObject.name} at position {pos} (distance from center: {distanceFromCenter:F2})");
                }
            }

            if (nearCenterCount > 0)
            {
                Debug.LogError($"Found {nearCenterCount} VisionProviders near world center (0,0,0)! These may be revealing fog of war at the minimap center.");
                Debug.LogError("Select the highlighted objects in the Hierarchy and move them to their correct positions.");
            }
            else
            {
                Debug.Log("✓ No VisionProviders found near world center.");
            }

            Debug.Log($"=================================");
        }

        [MenuItem("Tools/Fog of War/List All Vision Providers")]
        public static void ListAllVisionProviders()
        {
            var providers = Object.FindObjectsByType<VisionProvider>(FindObjectsSortMode.None)
                .OrderBy(p => p.transform.position.magnitude);

            Debug.Log($"=== All Vision Providers (sorted by distance from origin) ===");

            foreach (var provider in providers)
            {
                Vector3 pos = provider.transform.position;
                float distanceFromOrigin = pos.magnitude;

                // Get component info
                string typeInfo = "";
                if (provider.GetComponent<RTS.Buildings.Building>() != null)
                    typeInfo = "[BUILDING]";
                else if (provider.GetComponent<RTS.Units.UnitAIController>() != null)
                    typeInfo = "[UNIT]";
                else
                    typeInfo = "[OTHER]";

                Debug.Log($"{typeInfo} {provider.gameObject.name} at {pos} (distance: {distanceFromOrigin:F2}, radius: {provider.VisionRadius}, owner: {provider.OwnerId})", provider.gameObject);
            }

            Debug.Log($"=================================");
        }

        [MenuItem("Tools/Fog of War/Check Building Prefabs for VisionProvider")]
        public static void CheckBuildingPrefabs()
        {
            Debug.Log($"=== Checking Building Prefabs for VisionProvider ===");

            // Find all BuildingDataSO assets
            string[] guids = AssetDatabase.FindAssets("t:BuildingDataSO");
            int prefabsWithVision = 0;
            int totalPrefabs = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var buildingData = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                // Use reflection to get buildingPrefab field (since we don't have direct access to BuildingDataSO here)
                var field = buildingData.GetType().GetField("buildingPrefab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    GameObject prefab = field.GetValue(buildingData) as GameObject;
                    if (prefab != null)
                    {
                        totalPrefabs++;
                        var visionProvider = prefab.GetComponent<VisionProvider>();
                        if (visionProvider != null)
                        {
                            prefabsWithVision++;
                            Debug.Log($"✓ {prefab.name} has VisionProvider (radius: {visionProvider.VisionRadius})", prefab);
                        }
                    }
                }
            }

            Debug.Log($"Found {prefabsWithVision}/{totalPrefabs} building prefabs with VisionProvider attached");
            Debug.Log($"=================================");
        }
    }
}
