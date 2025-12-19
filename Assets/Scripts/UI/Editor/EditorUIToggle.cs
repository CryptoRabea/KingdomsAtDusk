#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[InitializeOnLoad]
public static class EditorUIToggle
{
    private static readonly List<GameObject> uiRoots = new();
    private const string HiddenKey = "EditorUIToggle_Hidden";

    static EditorUIToggle()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    // Shift + U
    [MenuItem("Tools/UI/Toggle UI Roots %#u")]
    private static void ToggleUI()
    {
        if (Application.isPlaying)
            return;

        CacheUIRoots();

        bool hidden = EditorPrefs.GetBool(HiddenKey, false);
        hidden = !hidden;
        EditorPrefs.SetBool(HiddenKey, hidden);

        foreach (var go in uiRoots)
        {
            if (go)
                go.SetActive(!hidden);
        }

        SceneView.RepaintAll();
    }

    private static void CacheUIRoots()
    {
        uiRoots.Clear();

#if UNITY_2023_1_OR_NEWER
        var canvases = Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
#else
        var canvases = Object.FindObjectsOfType<Canvas>(true);
#endif

        foreach (var canvas in canvases)
        {
            // ONLY disable ROOT UI objects
            if (canvas.transform.parent == null)
                uiRoots.Add(canvas.gameObject);
        }
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            CacheUIRoots();
            foreach (var go in uiRoots)
            {
                if (go)
                    go.SetActive(true);
            }

            EditorPrefs.DeleteKey(HiddenKey);
        }
    }
}
#endif
