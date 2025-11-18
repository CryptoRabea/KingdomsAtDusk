using UnityEngine;
using RTS.Core.Events;

namespace KingdomsAtDusk.FogOfWar
{
    /// <summary>
    /// Automatically integrates fog of war components with newly spawned units and buildings.
    /// Subscribes to game events to add VisionProvider and FogOfWarEntityVisibility components.
    /// </summary>
    public class FogOfWarAutoIntegrator : MonoBehaviour
    {
        [Header("Auto-Integration Settings")]
        [SerializeField] private bool autoAddVisionToUnits = true;
        [SerializeField] private bool autoAddVisionToBuildings = true;
        [SerializeField] private bool autoAddVisibilityControl = true;

        [Header("Vision Settings")]
        [SerializeField] private float defaultUnitVision = 15f;
        [SerializeField] private float defaultBuildingVision = 20f;

        private void OnEnable()
        {
            // Subscribe to unit spawned events
            EventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);

            // Subscribe to building events
            EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            EventBus.Unsubscribe<UnitSpawnedEvent>(OnUnitSpawned);
            EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Unsubscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        }

        private void OnUnitSpawned(UnitSpawnedEvent evt)
        {
            if (!autoAddVisionToUnits || evt.Unit == null) return;

            Debug.Log($"[FogOfWarAutoIntegrator] Unit spawned: {evt.Unit.name}");

            // Add VisionProvider if not already present
            var visionProvider = evt.Unit.GetComponent<VisionProvider>();
            if (visionProvider == null)
            {
                visionProvider = evt.Unit.AddComponent<VisionProvider>();

                // Try to detect ownership from MinimapEntity
                var minimapEntity = evt.Unit.GetComponent<RTS.UI.Minimap.MinimapEntity>();
                if (minimapEntity != null)
                {
                    int ownerId = minimapEntity.GetOwnership() == RTS.UI.Minimap.MinimapEntityOwnership.Friendly ? 0 : 1;
                    visionProvider.SetOwnerId(ownerId);

                    Debug.Log($"[FogOfWarAutoIntegrator] ✓ Added VisionProvider to unit: {evt.Unit.name} (Owner: {ownerId}, Ownership: {minimapEntity.GetOwnership()})");
                }
                else
                {
                    // Try to detect from layer
                    int ownerId = evt.Unit.layer == LayerMask.NameToLayer("Enemy") ? 1 : 0;
                    visionProvider.SetOwnerId(ownerId);
                    Debug.Log($"[FogOfWarAutoIntegrator] ✓ Added VisionProvider to unit: {evt.Unit.name} (Owner from layer: {ownerId}, Layer: {evt.Unit.layer})");
                }
            }
            else
            {
                Debug.Log($"[FogOfWarAutoIntegrator] Unit {evt.Unit.name} already has VisionProvider (Owner: {visionProvider.OwnerId})");
            }

            // Add visibility control if enabled
            if (autoAddVisibilityControl)
            {
                AddVisibilityControl(evt.Unit);
            }
        }

        private void OnBuildingPlaced(BuildingPlacedEvent evt)
        {
            if (!autoAddVisionToBuildings || evt.Building == null) return;

            AddBuildingVision(evt.Building);
        }

        private void OnBuildingCompleted(BuildingCompletedEvent evt)
        {
            if (!autoAddVisionToBuildings || evt.Building == null) return;

            // Ensure vision is added (in case it wasn't added during placement)
            AddBuildingVision(evt.Building);
        }

        private void AddBuildingVision(GameObject building)
        {
            var visionProvider = building.GetComponent<VisionProvider>();
            if (visionProvider == null)
            {
                visionProvider = building.AddComponent<VisionProvider>();
                visionProvider.SetOwnerId(0); // Buildings are typically player-owned
                visionProvider.SetVisionRadius(defaultBuildingVision);

                Debug.Log($"[FogOfWarAutoIntegrator] Added VisionProvider to building: {building.name}");
            }

            // Buildings are player-owned so they should always be visible
            if (autoAddVisibilityControl)
            {
                if (!building.TryGetComponent<FogOfWarEntityVisibility>(out var visibility))
                {
                    visibility = building.AddComponent<FogOfWarEntityVisibility>();
                    visibility.SetPlayerOwned(true); // Buildings are always visible
                }
            }
        }

        private void AddVisibilityControl(GameObject unit)
        {
            if (unit.TryGetComponent<FogOfWarEntityVisibility>(out var visibility))
            {
                Debug.Log($"[FogOfWarAutoIntegrator] Unit {unit.name} already has FogOfWarEntityVisibility");
                return;
            }

            visibility = unit.AddComponent<FogOfWarEntityVisibility>();

            // Determine if player-owned
            var minimapEntity = unit.GetComponent<RTS.UI.Minimap.MinimapEntity>();
            bool isPlayerOwned = false;

            if (minimapEntity != null)
            {
                isPlayerOwned = minimapEntity.GetOwnership() == RTS.UI.Minimap.MinimapEntityOwnership.Friendly;
            }
            else
            {
                // Fallback to layer check
                isPlayerOwned = unit.layer != LayerMask.NameToLayer("Enemy");
            }

            visibility.SetPlayerOwned(isPlayerOwned);

            Debug.Log($"[FogOfWarAutoIntegrator] ✓ Added FogOfWarEntityVisibility to unit: {unit.name} (Player Owned: {isPlayerOwned})");
        }
    }
}
