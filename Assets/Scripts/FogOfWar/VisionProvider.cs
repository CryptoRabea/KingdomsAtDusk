using UnityEngine;

namespace KingdomsAtDusk.FogOfWar
{
    /// <summary>
    /// Component that provides vision for the fog of war system.
    /// Attach to units, buildings, or any entity that should reveal fog.
    /// </summary>
    public class VisionProvider : MonoBehaviour, IVisionProvider
    {
        [Header("Vision Settings")]
        [SerializeField] private float visionRadius = 15f;
        [SerializeField] private bool isActive = true;
        [SerializeField] private int ownerId = 0;

        [Header("Auto-detect Settings")]
        [SerializeField] private bool autoDetectRadius = true;
        [Tooltip("Try to get vision radius from UnitConfigSO (for units)")]
        [SerializeField] private bool useUnitDetectionRange = true;

        private Transform cachedTransform;

        public Vector3 Position => cachedTransform.position;
        public float VisionRadius => visionRadius;
        public bool IsActive => isActive && gameObject.activeInHierarchy;
        public int OwnerId => ownerId;
        public GameObject GameObject => gameObject;

        private void Awake()
        {
            cachedTransform = transform;

            // Auto-detect vision radius from unit configuration
            if (autoDetectRadius && useUnitDetectionRange)
            {
                TryGetUnitDetectionRange();
            }
        }

        private void OnEnable()
        {
            RegisterWithManager();
        }

        private void OnDisable()
        {
            UnregisterWithManager();
        }

        private void RegisterWithManager()
        {
            if (FogOfWarManager.Instance != null)
            {
                FogOfWarManager.Instance.RegisterVisionProvider(this);
            }
        }

        private void UnregisterWithManager()
        {
            if (FogOfWarManager.Instance != null)
            {
                FogOfWarManager.Instance.UnregisterVisionProvider(this);
            }
        }

        private void TryGetUnitDetectionRange()
        {
            // Try to get detection range from UnitAIController
            var aiController = GetComponent<RTS.Units.AI.UnitAIController>();
            if (aiController != null && aiController.Config != null)
            {
                visionRadius = aiController.Config.detectionRange;
                Debug.Log($"[VisionProvider] Auto-detected vision radius: {visionRadius} from {gameObject.name}");
                return;
            }

            // Try to get from Building (they might have a different system)
            if (TryGetComponent<RTS.Buildings.Building>(out var building))
            {
                // Buildings get a larger vision radius by default
                visionRadius = 20f;
                Debug.Log($"[VisionProvider] Set building vision radius: {visionRadius} for {gameObject.name}");
            }
        }

        /// <summary>
        /// Manually set the vision radius
        /// </summary>
        public void SetVisionRadius(float radius)
        {
            visionRadius = Mathf.Max(0f, radius);
        }

        /// <summary>
        /// Set whether this provider is active
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
        }

        /// <summary>
        /// Set the owner ID
        /// </summary>
        public void SetOwnerId(int id)
        {
            ownerId = id;

            // Re-register if manager exists
            if (FogOfWarManager.Instance != null)
            {
                UnregisterWithManager();
                RegisterWithManager();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw vision radius
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, visionRadius);
        }
    }
}
