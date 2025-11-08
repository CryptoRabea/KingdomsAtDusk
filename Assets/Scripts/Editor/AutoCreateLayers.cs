using UnityEngine;
using UnityEditor;
using System.Reflection;

[InitializeOnLoad]
public static class AutoCreateLayers
{
    static AutoCreateLayers()
    {
        CreateLayer("Enemy");
        CreateLayer("SelectableUnit");
        CreateLayer("Player");
        CreateLayer("Ground");

        CreateLayer("Ally");
    }

    static void CreateLayer(string layerName)
    {
        // Load TagManager asset
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (asset == null || asset.Length == 0)
        {
            Debug.LogError("TagManager asset not found!");
            return;
        }

        SerializedObject tagManager = new SerializedObject(asset[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        bool layerExists = false;

        // Check if layer already exists
        for (int i = 8; i <= 31; i++)
        {
            SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
            if (sp != null && sp.stringValue == layerName)
            {
                layerExists = true;
                break;
            }
        }

        if (!layerExists)
        {
            // Find the first empty layer slot
            for (int j = 8; j <= 31; j++)
            {
                SerializedProperty sp = layersProp.GetArrayElementAtIndex(j);
                if (sp != null && string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    Debug.Log($"✅ Layer '{layerName}' added to slot {j}");
                    tagManager.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    return;
                }
            }

            Debug.LogWarning($"⚠️ No available layer slots to add '{layerName}'. (Max 32 layers)");
        }
        else
        {
            // Layer already exists
            // (Keep this log minimal to avoid spam when reloading scripts)
        }
    }
}
