using UnityEngine;
using UnityEditor;
using KingdomsAtDusk.Debug;

namespace KingdomsAtDusk.Editor
{
    /// <summary>
    /// Editor utilities for the Performance Monitor system
    /// </summary>
    public static class PerformanceMonitorEditor
    {
        [MenuItem("GameObject/Kingdoms at Dusk/Performance Monitor (Basic)", false, 10)]
        public static void CreateBasicPerformanceMonitor()
        {
            GameObject go = new GameObject("PerformanceMonitor");
            go.AddComponent<PerformanceMonitor>();
            Selection.activeGameObject = go;

            EditorUtility.DisplayDialog(
                "Performance Monitor Created",
                "Basic Performance Monitor has been added to the scene.\n\n" +
                "• Press F3 to toggle display\n" +
                "• Works in builds if 'Enable In Builds' is checked\n" +
                "• Configure settings in the Inspector\n\n" +
                "The monitor will persist across scenes by default.",
                "OK"
            );
        }

        [MenuItem("GameObject/Kingdoms at Dusk/Performance Monitor (Advanced)", false, 11)]
        public static void CreateAdvancedPerformanceMonitor()
        {
            GameObject go = new GameObject("AdvancedPerformanceMonitor");
            go.AddComponent<AdvancedPerformanceMonitor>();
            Selection.activeGameObject = go;

            EditorUtility.DisplayDialog(
                "Advanced Performance Monitor Created",
                "Advanced Performance Monitor has been added to the scene.\n\n" +
                "• Press F3 to toggle display\n" +
                "• Press C to toggle compact mode\n" +
                "• Shows detailed GPU/CPU/Memory stats\n" +
                "• Tracks rendering pipeline events\n" +
                "• Works in builds if 'Enable In Builds' is checked\n\n" +
                "The monitor will persist across scenes by default.",
                "OK"
            );
        }

        [MenuItem("Tools/Kingdoms at Dusk/Performance/Add Basic Monitor to Scene")]
        public static void AddBasicMonitor()
        {
            CreateBasicPerformanceMonitor();
        }

        [MenuItem("Tools/Kingdoms at Dusk/Performance/Add Advanced Monitor to Scene")]
        public static void AddAdvancedMonitor()
        {
            CreateAdvancedPerformanceMonitor();
        }

        [MenuItem("Tools/Kingdoms at Dusk/Performance/Remove All Monitors")]
        public static void RemoveAllMonitors()
        {
            var basicMonitors = Object.FindObjectsOfType<PerformanceMonitor>();
            var advancedMonitors = Object.FindObjectsOfType<AdvancedPerformanceMonitor>();

            int count = basicMonitors.Length + advancedMonitors.Length;

            if (count == 0)
            {
                EditorUtility.DisplayDialog("No Monitors Found", "No performance monitors found in the current scene.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog(
                "Remove Performance Monitors",
                $"Found {count} performance monitor(s). Remove them all?",
                "Yes", "Cancel"))
            {
                foreach (var monitor in basicMonitors)
                {
                    Object.DestroyImmediate(monitor.gameObject);
                }

                foreach (var monitor in advancedMonitors)
                {
                    Object.DestroyImmediate(monitor.gameObject);
                }

                UnityEngine.Debug.Log($"Removed {count} performance monitor(s).");
            }
        }

        [MenuItem("Tools/Kingdoms at Dusk/Performance/Documentation")]
        public static void OpenDocumentation()
        {
            string message = @"PERFORMANCE MONITOR DOCUMENTATION

=== BASIC MONITOR ===
• Lightweight FPS and memory tracking
• Suitable for most use cases
• Lower overhead

Key Features:
- FPS (current, avg, min, max)
- Frame time in milliseconds
- Memory usage (Unity + Mono + GC)
- System information
- Graphics and shadow settings

=== ADVANCED MONITOR ===
• Comprehensive performance tracking
• URP render pipeline integration
• Detailed rendering statistics

Additional Features:
- Per-frame render timing
- Resource counts (textures, materials, meshes)
- GameObject and component counts
- GC collection tracking
- Compact mode toggle

=== CONTROLS ===
• F3: Toggle display on/off
• C: Toggle compact mode (Advanced only)

=== SETUP ===
1. Use menu: GameObject > Kingdoms at Dusk > Performance Monitor
2. Or: Tools > Kingdoms at Dusk > Performance > Add Monitor
3. Configure in Inspector
4. Monitor persists across scenes by default

=== BUILD DEPLOYMENT ===
• Set 'Enable In Builds' to true
• Works in Development and Release builds
• No additional setup required

=== CUSTOMIZATION ===
All settings are in the Inspector:
- Toggle key
- Update interval
- Font size and colors
- Compact mode (Advanced)

For more info, see the script files in:
Assets/Scripts/Debug/
";

            EditorUtility.DisplayDialog("Performance Monitor Documentation", message, "OK");
        }
    }
}
