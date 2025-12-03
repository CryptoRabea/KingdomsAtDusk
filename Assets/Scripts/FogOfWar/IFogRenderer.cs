using UnityEngine;

namespace KingdomsAtDusk.FogOfWar
{
    /// <summary>
    /// Interface for fog of war rendering implementations.
    /// Allows for different rendering strategies (mesh overlay, camera effect, minimap, etc.)
    /// </summary>
    public interface IFogRenderer
    {
        /// <summary>
        /// Initialize the renderer with the fog of war manager
        /// </summary>
        /// <param name="manager">The FogOfWarManager instance</param>
        void Initialize(FogOfWarManager manager);

        /// <summary>
        /// Called when vision data has been updated
        /// </summary>
        void OnVisionUpdated();

        /// <summary>
        /// Called every frame to update renderer state
        /// </summary>
        void UpdateRenderer();

        /// <summary>
        /// Check if renderer is initialized and ready
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Enable or disable the renderer
        /// </summary>
        bool IsEnabled { get; set; }
    }
}
