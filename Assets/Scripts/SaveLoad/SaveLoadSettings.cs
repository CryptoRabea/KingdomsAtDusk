using UnityEngine;

namespace RTS.SaveLoad
{
    /// <summary>
    /// ScriptableObject configuration for the save/load system.
    /// Create via: Right-click in Project > Create > RTS > Save Load Settings
    /// </summary>
    [CreateAssetMenu(fileName = "SaveLoadSettings", menuName = "RTS/Save Load Settings")]
    public class SaveLoadSettings : ScriptableObject
    {
        [Header("Save File Settings")]
        [Tooltip("Directory name for save files (relative to persistentDataPath)")]
        public string saveDirectory = "Saves";

        [Tooltip("File extension for save files")]
        public string saveFileExtension = ".sav";

        [Tooltip("Use compression for save files")]
        public bool useCompression = true;

        [Tooltip("Use encryption for save files (basic obfuscation)")]
        public bool useEncryption = false;

        [Header("Auto-Save Settings")]
        [Tooltip("Enable auto-save feature")]
        public bool enableAutoSave = true;

        [Tooltip("Auto-save interval in seconds")]
        [Range(60f, 600f)]
        public float autoSaveInterval = 300f; // 5 minutes

        [Tooltip("Maximum number of auto-save files to keep")]
        [Range(1, 10)]
        public int maxAutoSaves = 3;

        [Tooltip("Auto-save on quit")]
        public bool autoSaveOnQuit = true;

        [Header("Quick Save Settings")]
        [Tooltip("Quick save slot name")]
        public string quickSaveSlotName = "QuickSave";

        [Header("Manual Save Settings")]
        [Tooltip("Maximum number of manual save files (0 = unlimited)")]
        public int maxManualSaves = 0; // Unlimited

        [Header("Debug Settings")]
        [Tooltip("Log save/load operations to console")]
        public bool enableDebugLogging = true;

        [Tooltip("Create backup before loading a save")]
        public bool createBackupBeforeLoad = false;

        [Header("UI Settings")]
        [Tooltip("Show confirmation dialog before overwriting saves")]
        public bool confirmOverwrite = true;

        [Tooltip("Show save notification on screen")]
        public bool showSaveNotifications = true;

        [Tooltip("Save notification duration (seconds)")]
        public float notificationDuration = 2f;

        /// <summary>
        /// Get the full path to the save directory.
        /// </summary>
        public string GetSaveDirectoryPath()
        {
            return System.IO.Path.Combine(Application.persistentDataPath, saveDirectory);
        }

        /// <summary>
        /// Get the full file path for a save file.
        /// </summary>
        public string GetSaveFilePath(string saveName)
        {
            string fileName = saveName;
            if (!fileName.EndsWith(saveFileExtension))
            {
                fileName += saveFileExtension;
            }
            return System.IO.Path.Combine(GetSaveDirectoryPath(), fileName);
        }

        /// <summary>
        /// Get auto-save file name with index.
        /// </summary>
        public string GetAutoSaveFileName(int index)
        {
            return $"AutoSave_{index:00}";
        }

        private void OnValidate()
        {
            // Ensure valid ranges
            autoSaveInterval = Mathf.Max(60f, autoSaveInterval);
            maxAutoSaves = Mathf.Max(1, maxAutoSaves);
            maxManualSaves = Mathf.Max(0, maxManualSaves);
            notificationDuration = Mathf.Max(0.5f, notificationDuration);
        }
    }
}
