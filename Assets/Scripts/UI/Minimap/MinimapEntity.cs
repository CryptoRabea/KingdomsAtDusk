using UnityEngine;

namespace RTS.UI.Minimap
{
    /// <summary>
    /// Marks a GameObject as visible on the minimap.
    /// Attach to units, buildings, or any entity that should appear on the minimap.
    /// Optimized for performance and safety.
    /// </summary>
    public class MinimapEntity : MonoBehaviour, IMinimapEntity
    {
        [Header("Ownership")]
        [Tooltip("Ownership/faction of this entity")]
        [SerializeField] private MinimapEntityOwnership ownership = MinimapEntityOwnership.Friendly;

        [Tooltip("Automatically detect ownership from layer/tag on start")]
        [SerializeField] private bool autoDetectOwnership = false;

        [Header("Optional Player ID")]
        [Tooltip("Player ID for multiplayer games (0 = local player)")]
        [SerializeField] private int playerId = 0;

        // Cached layer numbers
        private static readonly int EnemyLayer = LayerMask.NameToLayer("Enemy");

        // Ownership mapping for player IDs
        private static readonly MinimapEntityOwnership[] PlayerOwnershipMap =
        {
            MinimapEntityOwnership.Friendly, // 0
            MinimapEntityOwnership.Player1,  // 1
            MinimapEntityOwnership.Player2,  // 2
            MinimapEntityOwnership.Player3,  // 3
            MinimapEntityOwnership.Player4   // 4
        };

        private void Start()
        {
            if (autoDetectOwnership)
                DetectOwnership();
        }

        // ----- Public API -----
        public MinimapEntityOwnership GetOwnership() => ownership;
        public Vector3 GetPosition() => transform.position;
        public GameObject GetGameObject() => gameObject;

        public void SetOwnership(MinimapEntityOwnership newOwnership)
        {
            if (ownership != newOwnership)
                ownership = newOwnership;
        }

        public int GetPlayerId() => playerId;

        public void SetPlayerId(int newPlayerId)
        {
            if (playerId == newPlayerId) return;

            playerId = newPlayerId;

            // Fast mapping: use array if within bounds
            ownership = (newPlayerId >= 0 && newPlayerId < PlayerOwnershipMap.Length)
                ? PlayerOwnershipMap[newPlayerId]
                : MinimapEntityOwnership.Enemy;
        }

        // ----- Ownership Detection -----
        private void DetectOwnership()
        {
            // Use CompareTag for fastest performance
            if (CompareTag("Friendly")) { ownership = MinimapEntityOwnership.Friendly; return; }
            if (CompareTag("Enemy")) { ownership = MinimapEntityOwnership.Enemy; return; }
            if (CompareTag("Neutral")) { ownership = MinimapEntityOwnership.Neutral; return; }
            if (CompareTag("Ally")) { ownership = MinimapEntityOwnership.Ally; return; }

            // Layer detection fallback
            if (gameObject.layer == EnemyLayer)
            {
                ownership = MinimapEntityOwnership.Enemy;
                return;
            }

            // Default
            ownership = MinimapEntityOwnership.Friendly;
        }

        #region Editor Helper
#if UNITY_EDITOR
        [ContextMenu("Detect Ownership")]
        private void DetectOwnershipEditor()
        {
            DetectOwnership();
            Debug.Log($"{gameObject.name}: Detected ownership = {ownership}");
        }

        [ContextMenu("Set as Friendly")]
        private void SetFriendly() => ownership = MinimapEntityOwnership.Friendly;

        [ContextMenu("Set as Enemy")]
        private void SetEnemy() => ownership = MinimapEntityOwnership.Enemy;

        [ContextMenu("Set as Neutral")]
        private void SetNeutral() => ownership = MinimapEntityOwnership.Neutral;
#endif
        #endregion
    }
}
