using UnityEngine;

namespace RTS.Units.AI
{
    /// <summary>
    /// ScriptableObjects containing global AI settings.
    /// Shared across all AI units for consistent behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "AISettings", menuName = "RTS/AISettings")]
    public class AISettingsSO : ScriptableObject
    {
        [Header("Update Settings")]
        [Tooltip("How often AI recalculates targets (in seconds)")]
        public float updateInterval = 0.5f;
        
        [Header("Layer Masks")]
        [Tooltip("What layers are considered enemies")]
        public LayerMask enemyLayer;
        
        [Tooltip("What layers are considered allies")]
        public LayerMask allyLayer;
        
        [Header("Performance")]
        [Tooltip("Maximum number of AI units that can update per frame")]
        public int maxUpdatesPerFrame = 50;
        
        [Header("Debug")]
        public bool showDebugGizmos = true;
        public bool logStateChanges = false;
    }
}
