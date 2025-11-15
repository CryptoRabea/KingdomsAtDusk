using UnityEngine;

namespace RTS.UI.Minimap
{
    /// <summary>
    /// Component that marks a GameObject as visible on the minimap.
    /// Attach this to units, buildings, or any entity that should appear on the minimap.
    /// This is the most flexible method for ownership detection.
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

        private void Start()
        {
            if (autoDetectOwnership)
            {
                DetectOwnership();
            }
        }

        /// <summary>
        /// Get the ownership of this entity.
        /// </summary>
        public MinimapEntityOwnership GetOwnership()
        {
            return ownership;
        }

        /// <summary>
        /// Get the world position of this entity.
        /// </summary>
        public Vector3 GetPosition()
        {
            return transform.position;
        }

        /// <summary>
        /// Get the GameObject representing this entity.
        /// </summary>
        public GameObject GetGameObject()
        {
            return gameObject;
        }

        /// <summary>
        /// Set the ownership at runtime.
        /// </summary>
        public void SetOwnership(MinimapEntityOwnership newOwnership)
        {
            ownership = newOwnership;
        }

        /// <summary>
        /// Get the player ID.
        /// </summary>
        public int GetPlayerId()
        {
            return playerId;
        }

        /// <summary>
        /// Set the player ID at runtime.
        /// </summary>
        public void SetPlayerId(int newPlayerId)
        {
            playerId = newPlayerId;

            // Auto-update ownership based on player ID
            if (newPlayerId == 0)
            {
                ownership = MinimapEntityOwnership.Friendly;
            }
            else
            {
                // Map player IDs to ownership
                switch (newPlayerId)
                {
                    case 1:
                        ownership = MinimapEntityOwnership.Player1;
                        break;
                    case 2:
                        ownership = MinimapEntityOwnership.Player2;
                        break;
                    case 3:
                        ownership = MinimapEntityOwnership.Player3;
                        break;
                    case 4:
                        ownership = MinimapEntityOwnership.Player4;
                        break;
                    default:
                        ownership = MinimapEntityOwnership.Enemy;
                        break;
                }
            }
        }

        /// <summary>
        /// Automatically detect ownership from GameObject layer or tag.
        /// </summary>
        private void DetectOwnership()
        {
            // Try tag first
            if (CompareTag("Friendly"))
            {
                ownership = MinimapEntityOwnership.Friendly;
                return;
            }
            if (CompareTag("Enemy"))
            {
                ownership = MinimapEntityOwnership.Enemy;
                return;
            }
            if (CompareTag("Neutral"))
            {
                ownership = MinimapEntityOwnership.Neutral;
                return;
            }
            if (CompareTag("Ally"))
            {
                ownership = MinimapEntityOwnership.Ally;
                return;
            }

            // Try layer
            if (gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                ownership = MinimapEntityOwnership.Enemy;
                return;
            }

            // Default to friendly
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
        private void SetFriendly()
        {
            ownership = MinimapEntityOwnership.Friendly;
        }

        [ContextMenu("Set as Enemy")]
        private void SetEnemy()
        {
            ownership = MinimapEntityOwnership.Enemy;
        }

        [ContextMenu("Set as Neutral")]
        private void SetNeutral()
        {
            ownership = MinimapEntityOwnership.Neutral;
        }
#endif

        #endregion
    }
}
