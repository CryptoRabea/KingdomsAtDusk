using UnityEngine;
using System.Linq;
using RTS.Core.Services;

namespace RTS.SaveLoad
{
    /// <summary>
    /// Handles automatic saving at configured intervals.
    /// Manages auto-save file rotation and save-on-quit functionality.
    /// </summary>
    public class AutoSaveSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] internal SaveLoadSettings settings;

        private ISaveLoadService saveLoadService;
        private float timeSinceLastAutoSave = 0f;
        private int currentAutoSaveIndex = 0;

        private void Start()
        {
            saveLoadService = ServiceLocator.TryGet<ISaveLoadService>();
            if (saveLoadService == null)
            {
                Debug.LogWarning("AutoSaveSystem: ISaveLoadService not found! Auto-save disabled.");
                enabled = false;
                return;
            }

            // Find the next auto-save slot to use
            DetermineNextAutoSaveSlot();
        }

        private void Update()
        {
            if (!settings.enableAutoSave || saveLoadService == null)
                return;

            // Don't auto-save when paused
            var gameState = ServiceLocator.TryGet<IGameStateService>();
            if (gameState != null && gameState.IsPaused)
                return;

            // Track time
            timeSinceLastAutoSave += Time.deltaTime;

            // Auto-save when interval reached
            if (timeSinceLastAutoSave >= settings.autoSaveInterval)
            {
                PerformAutoSave();
                timeSinceLastAutoSave = 0f;
            }
        }

        private void OnApplicationQuit()
        {
            if (settings.autoSaveOnQuit && saveLoadService != null)
            {
                Debug.Log("Performing auto-save on quit...");
                PerformAutoSave();
            }
        }

        private void PerformAutoSave()
        {
            if (saveLoadService == null)
                return;

            string autoSaveName = settings.GetAutoSaveFileName(currentAutoSaveIndex);
            bool success = saveLoadService.SaveGame(autoSaveName, isAutoSave: true, isQuickSave: false);

            if (success)
            {
                Debug.Log($"✅ Auto-save successful: {autoSaveName}");

                // Move to next slot
                currentAutoSaveIndex = (currentAutoSaveIndex + 1) % settings.maxAutoSaves;

                // Delete oldest auto-save if we've exceeded the limit
                CleanupOldAutoSaves();
            }
            else
            {
                Debug.LogError($"❌ Auto-save failed: {autoSaveName}");
            }
        }

        private void DetermineNextAutoSaveSlot()
        {
            // Find all existing auto-saves
            string[] allSaves = saveLoadService.GetAllSaves();
            var autoSaves = allSaves.Where(s => s.StartsWith("AutoSave_")).ToList();

            if (autoSaves.Count == 0)
            {
                currentAutoSaveIndex = 0;
                return;
            }

            // Find the most recent auto-save by checking save dates
            SaveFileInfo newestAutoSave = null;
            System.DateTime newestDate = System.DateTime.MinValue;

            foreach (var autoSave in autoSaves)
            {
                SaveFileInfo info = saveLoadService.GetSaveInfo(autoSave);
                if (info != null)
                {
                    if (System.DateTime.TryParse(info.saveDate, out System.DateTime saveDate))
                    {
                        if (saveDate > newestDate)
                        {
                            newestDate = saveDate;
                            newestAutoSave = info;
                        }
                    }
                }
            }

            // Extract index from newest auto-save and increment
            if (newestAutoSave != null)
            {
                string indexStr = newestAutoSave.saveName.Replace("AutoSave_", "");
                if (int.TryParse(indexStr, out int index))
                {
                    currentAutoSaveIndex = (index + 1) % settings.maxAutoSaves;
                }
            }
        }

        private void CleanupOldAutoSaves()
        {
            string[] allSaves = saveLoadService.GetAllSaves();
            var autoSaves = allSaves.Where(s => s.StartsWith("AutoSave_")).ToList();

            // If we have more auto-saves than allowed, delete the oldest ones
            if (autoSaves.Count > settings.maxAutoSaves)
            {
                // Sort by date
                var sortedAutoSaves = autoSaves
                    .Select(s => new { Name = s, Info = saveLoadService.GetSaveInfo(s) })
                    .Where(x => x.Info != null)
                    .OrderBy(x => System.DateTime.Parse(x.Info.saveDate))
                    .ToList();

                // Delete oldest ones
                int toDelete = sortedAutoSaves.Count - settings.maxAutoSaves;
                for (int i = 0; i < toDelete; i++)
                {
                    saveLoadService.DeleteSave(sortedAutoSaves[i].Name);
                    Debug.Log($"Deleted old auto-save: {sortedAutoSaves[i].Name}");
                }
            }
        }

        /// <summary>
        /// Manually trigger an auto-save (for testing or special events).
        /// </summary>
        public void TriggerAutoSave()
        {
            PerformAutoSave();
            timeSinceLastAutoSave = 0f;
        }

        /// <summary>
        /// Reset auto-save timer (useful after manual saves).
        /// </summary>
        public void ResetAutoSaveTimer()
        {
            timeSinceLastAutoSave = 0f;
        }
    }
}
