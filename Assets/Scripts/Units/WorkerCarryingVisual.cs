using UnityEngine;
using KingdomsAtDusk.Core;
using RTS.Core.Services;

namespace KingdomsAtDusk.Units
{
    /// <summary>
    /// Visual feedback system for workers carrying resources.
    /// Shows sprite, particle effect, or animation state when worker is carrying resources.
    /// </summary>
    public class WorkerCarryingVisual : MonoBehaviour
    {
        [Header("Visual Configuration")]
        [Tooltip("Method to use for showing carrying state")]
        public CarryingVisualMethod visualMethod = CarryingVisualMethod.SpriteOverlay;

        [Header("Sprite Overlay Method")]
        [Tooltip("Transform where the carrying sprite will be parented (usually above unit's head)")]
        public Transform carryingSpriteAnchor;

        [Tooltip("Offset from anchor position")]
        public Vector3 spriteOffset = new Vector3(0, 1.5f, 0);

        [Tooltip("Resource sprites for different resource types")]
        public ResourceSprite[] resourceSprites;

        [Header("Particle Method")]
        [Tooltip("Particle effect prefab for carrying indication")]
        public GameObject carryingParticlePrefab;

        [Tooltip("Where to spawn the particle effect")]
        public Transform particleAnchor;

        [Header("Animation Method")]
        [Tooltip("Animator parameter name for carrying state")]
        public string carryingAnimatorBool = "IsCarrying";

        [Tooltip("Animator parameter for resource type (0=Wood, 1=Food, 2=Gold, 3=Stone)")]
        public string resourceTypeAnimatorInt = "CarryingResourceType";

        // Runtime references
        private GameObject currentVisual;
        private GameObject currentParticle;
        private Animator animator;
        private bool isCarrying = false;

        private void Awake()
        {
            animator = GetComponent<Animator>();

            // Create sprite anchor if not set
            if (carryingSpriteAnchor == null)
            {
                GameObject anchorObj = new GameObject("CarryingSpriteAnchor");
                anchorObj.transform.SetParent(transform);
                anchorObj.transform.localPosition = spriteOffset;
                carryingSpriteAnchor = anchorObj.transform;
            }

            if (particleAnchor == null)
            {
                particleAnchor = transform;
            }
        }

        /// <summary>
        /// Show visual feedback for carrying resources.
        /// </summary>
        public void ShowCarrying(ResourceType resourceType, int amount)
        {
            if (isCarrying) HideCarrying(); // Clean up previous

            isCarrying = true;

            switch (visualMethod)
            {
                case CarryingVisualMethod.SpriteOverlay:
                    ShowSpriteOverlay(resourceType, amount);
                    break;

                case CarryingVisualMethod.ParticleEffect:
                    ShowParticleEffect(resourceType);
                    break;

                case CarryingVisualMethod.AnimationState:
                    ShowAnimationState(resourceType);
                    break;

                case CarryingVisualMethod.All:
                    ShowSpriteOverlay(resourceType, amount);
                    ShowParticleEffect(resourceType);
                    ShowAnimationState(resourceType);
                    break;
            }
        }

        /// <summary>
        /// Hide carrying visual feedback.
        /// </summary>
        public void HideCarrying()
        {
            if (!isCarrying) return;

            isCarrying = false;

            // Clean up sprite
            if (currentVisual != null)
            {
                Destroy(currentVisual);
                currentVisual = null;
            }

            // Clean up particle
            if (currentParticle != null)
            {
                Destroy(currentParticle);
                currentParticle = null;
            }

            // Reset animation
            if (animator != null)
            {
                animator.SetBool(carryingAnimatorBool, false);
            }
        }

        #region Visual Methods

        private void ShowSpriteOverlay(ResourceType resourceType, int amount)
        {
            // Find the sprite for this resource type
            Sprite sprite = GetSpriteForResource(resourceType);
            if (sprite == null) return;

            // Create sprite GameObject
            currentVisual = new GameObject($"Carrying_{resourceType}");
            currentVisual.transform.SetParent(carryingSpriteAnchor);
            currentVisual.transform.localPosition = Vector3.zero;
            currentVisual.transform.localRotation = Quaternion.identity;

            // Add SpriteRenderer
            SpriteRenderer sr = currentVisual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 100; // Render on top

            // Optional: Scale based on amount
            float scale = 1f + (amount * 0.05f); // Slight scale increase with more resources
            currentVisual.transform.localScale = Vector3.one * Mathf.Min(scale, 2f);

            // Billboard effect (face camera)
            var billboard = currentVisual.AddComponent<Billboard>();
        }

        private void ShowParticleEffect(ResourceType resourceType)
        {
            if (carryingParticlePrefab == null) return;

            currentParticle = Instantiate(carryingParticlePrefab, particleAnchor.position, Quaternion.identity, particleAnchor);

            // Set particle color based on resource
            if (currentParticle.TryGetComponent<ParticleSystem>(out var particleSystem))
            {
                var main = particleSystem.main;
                main.startColor = GetResourceColor(resourceType);
            }
        }

        private void ShowAnimationState(ResourceType resourceType)
        {
            if (animator == null) return;

            animator.SetBool(carryingAnimatorBool, true);
            animator.SetInteger(resourceTypeAnimatorInt, (int)resourceType);
        }

        #endregion

        #region Helper Methods

        private Sprite GetSpriteForResource(ResourceType resourceType)
        {
            if (resourceSprites == null) return null;

            foreach (var rs in resourceSprites)
            {
                if (rs.resourceType == resourceType)
                {
                    return rs.sprite;
                }
            }

            return null;
        }

        private Color GetResourceColor(ResourceType resourceType)
        {
            return resourceType switch
            {
                ResourceType.Wood => new Color(0.6f, 0.4f, 0.2f),   // Brown
                ResourceType.Food => new Color(1f, 1f, 0f),         // Yellow
                ResourceType.Gold => new Color(1f, 0.84f, 0f),      // Gold
                ResourceType.Stone => new Color(0.5f, 0.5f, 0.5f),  // Gray
                _ => Color.white
            };
        }

        #endregion

        /// <summary>
        /// Check if currently showing carrying visual.
        /// </summary>
        public bool IsCarrying => isCarrying;
    }

    /// <summary>
    /// Mapping of resource types to sprites.
    /// </summary>
    [System.Serializable]
    public class ResourceSprite
    {
        public ResourceType resourceType;
        public Sprite sprite;
    }

    /// <summary>
    /// Method to use for showing carrying visual.
    /// </summary>
    public enum CarryingVisualMethod
    {
        SpriteOverlay,      // Show a sprite above the worker's head
        ParticleEffect,     // Show particle effect
        AnimationState,     // Use animator state
        All                 // Use all methods combined
    }

    /// <summary>
    /// Simple billboard component to make sprites face the camera.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (mainCamera != null)
            {
                transform.rotation = mainCamera.transform.rotation;
            }
        }
    }
}
