using UnityEngine;
using System.Linq;

namespace KingdomsAtDusk.FogOfWar
{
    /// <summary>
    /// Diagnostic utility for troubleshooting fog of war setup issues
    /// Attach to any GameObject and run diagnostics via context menu
    /// </summary>
    public class FogOfWarDiagnostics : MonoBehaviour
    {
        [ContextMenu("Run Full Diagnostics")]
        public void RunFullDiagnostics()
        {
            Debug.Log("========================================");
            Debug.Log("FOG OF WAR FULL DIAGNOSTICS");
            Debug.Log("========================================\n");

            CheckFogOfWarManager();
            CheckVisionProviders();
            CheckMinimapSetup();
            CheckEnemySetup();
            CheckLayerConfiguration();

            Debug.Log("\n========================================");
            Debug.Log("DIAGNOSTICS COMPLETE");
            Debug.Log("========================================");
        }

        private void CheckFogOfWarManager()
        {
            Debug.Log("--- CHECKING FOG OF WAR MANAGER ---");

            if (FogOfWarManager.Instance == null)
            {
                Debug.LogError("✗ FogOfWarManager.Instance is NULL!");
                Debug.LogError("  FIX: Add FogOfWarManager component to a GameObject in the scene");
                Debug.LogError("  Make sure it's active and enabled");
                return;
            }

            Debug.Log("✓ FogOfWarManager instance found");

            if (FogOfWarManager.Instance.Grid == null)
            {
                Debug.LogWarning("✗ FogOfWarManager.Grid is NULL!");
                Debug.LogWarning("  This means the manager hasn't initialized yet");
                Debug.LogWarning("  Check that the GameObject is active and Start() has been called");
            }
            else
            {
                Debug.Log($"✓ Fog grid initialized: {FogOfWarManager.Instance.Grid.Width}x{FogOfWarManager.Instance.Grid.Height} cells");
            }

            Debug.Log($"  Local Player ID: {FogOfWarManager.Instance.LocalPlayerId}");
            Debug.Log($"  Config: {(FogOfWarManager.Instance.Config != null ? "Present" : "NULL")}");
            Debug.Log("");
        }

        private void CheckVisionProviders()
        {
            Debug.Log("--- CHECKING VISION PROVIDERS ---");

            var allProviders = FindObjectsByType<VisionProvider>(FindObjectsSortMode.None);
            Debug.Log($"Total VisionProvider components in scene: {allProviders.Length}");

            if (allProviders.Length == 0)
            {
                Debug.LogWarning("✗ No VisionProvider components found!");
                Debug.LogWarning("  FIX: Make sure FogOfWarAutoIntegrator is in the scene and enabled");
                Debug.LogWarning("  Or manually add VisionProvider to your units/buildings");
            }

            var friendlyProviders = allProviders.Where(p => p.OwnerId == 0).ToArray();
            var enemyProviders = allProviders.Where(p => p.OwnerId != 0).ToArray();

            Debug.Log($"  Friendly (Player 0): {friendlyProviders.Length}");
            Debug.Log($"  Enemy (Other): {enemyProviders.Length}");

            if (friendlyProviders.Length == 0)
            {
                Debug.LogError("✗ No friendly vision providers found!");
                Debug.LogError("  Without friendly units providing vision, the entire map will be dark");
                Debug.LogError("  FIX: Ensure your player units have VisionProvider with OwnerId = 0");
            }

            // Show first few providers
            for (int i = 0; i < Mathf.Min(5, allProviders.Length); i++)
            {
                var p = allProviders[i];
                Debug.Log($"  [{i}] {p.gameObject.name} - Owner: {p.OwnerId}, Radius: {p.VisionRadius}, Active: {p.IsActive}");
            }

            Debug.Log("");
        }

        private void CheckMinimapSetup()
        {
            Debug.Log("--- CHECKING MINIMAP SETUP ---");

            var minimapController = FindFirstObjectByType<RTS.UI.MiniMapControllerPro>();

            if (minimapController == null)
            {
                Debug.LogWarning("✗ MiniMapController not found");
                Debug.LogWarning("  Minimap fog of war won't work without it");
                return;
            }

            Debug.Log("✓ MiniMapController found");

            var minimapRenderer = FindFirstObjectByType<FogOfWarMinimapRenderer>();
            if (minimapRenderer == null)
            {
                Debug.LogWarning("✗ FogOfWarMinimapRenderer not found!");
                Debug.LogWarning("  FIX: Add FogOfWarMinimapRenderer component to handle minimap fog");
            }
            else
            {
                Debug.Log("✓ FogOfWarMinimapRenderer found");
            }

            Debug.Log("");
        }

        private void CheckEnemySetup()
        {
            Debug.Log("--- CHECKING ENEMY UNIT SETUP ---");

            var allUnits = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            var enemyLayerNumber = LayerMask.NameToLayer("Enemy");

            if (enemyLayerNumber == -1)
            {
                Debug.LogError("✗ 'Enemy' layer not defined!");
                Debug.LogError("  FIX: Add 'Enemy' layer in Project Settings > Tags and Layers");
                return;
            }

            Debug.Log($"✓ Enemy layer exists (Layer {enemyLayerNumber})");

            var enemyUnits = allUnits.Where(u => u.gameObject.layer == enemyLayerNumber).Take(5);
            int enemyCount = enemyUnits.Count();

            if (enemyCount == 0)
            {
                Debug.LogWarning("✗ No units on Enemy layer found");
                Debug.LogWarning("  Enemy units won't be hidden by fog of war without proper layer setup");
            }
            else
            {
                Debug.Log($"✓ Found {enemyCount} objects on Enemy layer (showing first 5):");
                foreach (var enemy in enemyUnits)
                {
                    var visibility = enemy.GetComponent<FogOfWarEntityVisibility>();
                    var minimapEntity = enemy.GetComponent<RTS.UI.Minimap.MinimapEntity>();

                    Debug.Log($"  - {enemy.gameObject.name}");
                    Debug.Log($"    FogOfWarEntityVisibility: {(visibility != null ? "✓" : "✗")}");
                    Debug.Log($"    MinimapEntity: {(minimapEntity != null ? $"✓ ({minimapEntity.GetOwnership()})" : "✗")}");
                }
            }

            Debug.Log("");
        }

        private void CheckLayerConfiguration()
        {
            Debug.Log("--- CHECKING LAYER CONFIGURATION ---");

            string[] requiredLayers = { "Enemy", "Default", "UI" };

            foreach (var layerName in requiredLayers)
            {
                int layerNum = LayerMask.NameToLayer(layerName);
                if (layerNum == -1)
                {
                    Debug.LogWarning($"✗ Layer '{layerName}' not found");
                }
                else
                {
                    Debug.Log($"✓ Layer '{layerName}' exists (Layer {layerNum})");
                }
            }

            Debug.Log("");
        }

        [ContextMenu("List All Vision Providers")]
        public void ListAllVisionProviders()
        {
            var allProviders = FindObjectsByType<VisionProvider>(FindObjectsSortMode.None);
            Debug.Log($"=== ALL VISION PROVIDERS ({allProviders.Length}) ===");

            foreach (var p in allProviders)
            {
                Debug.Log($"{p.gameObject.name} - Owner: {p.OwnerId}, Radius: {p.VisionRadius}, Active: {p.IsActive}, Pos: {p.Position}");
            }
        }

        [ContextMenu("Test Fog at Camera Position")]
        public void TestFogAtCameraPosition()
        {
            if (FogOfWarManager.Instance == null)
            {
                Debug.LogError("FogOfWarManager.Instance is NULL!");
                return;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("Main camera not found!");
                return;
            }

            Vector3 pos = cam.transform.position;
            var state = FogOfWarManager.Instance.GetVisionState(pos);

            Debug.Log($"=== FOG STATE AT CAMERA ===");
            Debug.Log($"Position: {pos}");
            Debug.Log($"Vision State: {state}");
            Debug.Log($"Is Visible: {FogOfWarManager.Instance.IsVisible(pos)}");
            Debug.Log($"Is Explored: {FogOfWarManager.Instance.IsExplored(pos)}");
        }
    }
}
