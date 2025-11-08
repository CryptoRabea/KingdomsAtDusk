using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

public class UpgradeAllMaterialsToURP : Editor
{
    [MenuItem("Tools/URP/Upgrade All Materials to URP Lit")]
    static void UpgradeAllToURP()
    {
        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
        int count = 0;

        foreach (string guid in materialGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // Skip materials that already use a URP shader
            if (mat.shader != null && mat.shader.name.Contains("Universal Render Pipeline"))
                continue;

            // Assign URP/Lit shader
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null)
            {
                mat.shader = urpLit;
                EditorUtility.SetDirty(mat);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Upgraded {count} materials to URP/Lit shader.");
    }
}
