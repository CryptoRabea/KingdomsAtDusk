using UnityEngine;

namespace RTS.UI.Minimap
{
    /// <summary>
    /// Interface for entities that can appear on the minimap.
    /// Implement this on units and buildings for flexible ownership detection.
    /// </summary>
    public interface IMinimapEntity
    {
        /// <summary>
        /// Get the owner/faction of this entity.
        /// </summary>
        MinimapEntityOwnership GetOwnership();

        /// <summary>
        /// Get the world position of this entity.
        /// </summary>
        Vector3 GetPosition();

        /// <summary>
        /// Get the GameObject representing this entity.
        /// </summary>
        GameObject GetGameObject();
    }

    /// <summary>
    /// Ownership/faction information for minimap entities.
    /// </summary>
    public enum MinimapEntityOwnership
    {
        Friendly,
        Enemy,
        Neutral,
        Ally,
        Player1,
        Player2,
        Player3,
        Player4
    }
}
