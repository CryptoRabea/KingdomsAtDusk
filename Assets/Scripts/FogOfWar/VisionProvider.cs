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

            Debug.Log($"[VisionProvider] {gameObject.name} - Awake (Owner: {ownerId}, Radius: {visionRadius})");
        }

        private void OnEnable()
        {
            // Delay registration by one frame to allow preview cleanup code to destroy this component
            // This prevents previews from revealing fog of war at instantiation position
            StartCoroutine(DelayedRegistration());
        }

        private System.Collections.IEnumerator DelayedRegistration()
        {
            // Wait one frame - this gives preview creation code time to destroy this component
            yield return null;

            // If we've been destroyed (e.g., on a preview), don't register
            if (this == null || !gameObject.activeInHierarchy)
                yield break;

            RegisterWithManager();

            // If manager isn't ready yet, try again in a moment
            if (FogOfWarManager.Instance == null)
            {
                StartCoroutine(RetryRegistration());
            }
        }

        private System.Collections.IEnumerator RetryRegistration()
        {
            int attempts = 0;
            while (FogOfWarManager.Instance == null && attempts < 50)
            {
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }

            if (FogOfWarManager.Instance != null)
            {
                RegisterWithManager();
                Debug.Log($"[VisionProvider] {gameObject.name} - Successfully registered after retry");
            }
            else
            {
                Debug.LogError($"[VisionProvider] {gameObject.name} - Failed to register: FogOfWarManager not found after 5 seconds!");
            }
        }

        private void OnDisable()
        {
            UnregisterWithManager();
        }

        private void RegisterWithManager()
        {
            if (FogOfWarManager.Instance != null)
            {
                Debug.Log($"[VisionProvider] {gameObject.name} - Registering at position {Position} (Owner: {ownerId}, Radius: {visionRadius})");
                FogOfWarManager.Instance.RegisterVisionProvider(this);
            }
            else
            {
                Debug.LogWarning($"[VisionProvider] {gameObject.name} - FogOfWarManager.Instance is null! Cannot register.");
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
