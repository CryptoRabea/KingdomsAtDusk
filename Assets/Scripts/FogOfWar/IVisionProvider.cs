using UnityEngine;

namespace KingdomsAtDusk.FogOfWar
{
    /// <summary>
    /// Interface for entities that provide vision (units, buildings, etc.)
    /// </summary>
    public interface IVisionProvider
    {
        /// <summary>
        /// World position of the vision provider
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// Vision radius in world units
        /// </summary>
        float VisionRadius { get; }

        /// <summary>
        /// Whether this vision provider is currently active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Owner of this vision provider (for team-based fog of war)
        /// </summary>
        int OwnerId { get; }

        /// <summary>
        /// GameObject reference for the vision provider
        /// </summary>
        GameObject GameObject { get; }
    }
}
