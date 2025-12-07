using UnityEngine;

namespace RTS.Buildings
{
    /// <summary>
    /// Base class for construction visual effects.
    /// Automatically tracks the parent Building's construction progress.
    /// Override UpdateVisual() to implement custom construction animations.
    /// </summary>
    public abstract class BaseConstructionVisual : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] protected bool affectChildren = true;
        [SerializeField] protected float updateInterval = 0.05f; // Update visuals every 50ms for performance

        protected Building parentBuilding;
        protected float currentProgress = 0f;
        protected float lastUpdateTime = 0f;
        protected Renderer[] renderers;
        protected MeshFilter[] meshFilters;
        protected Bounds combinedBounds;

        protected virtual void Awake()
        {
            // Find parent building
            parentBuilding = GetComponentInParent<Building>();

            if (parentBuilding == null)
            {
                enabled = false;
                return;
            }

            // Cache renderers and mesh filters
            if (affectChildren)
            {
                renderers = GetComponentsInChildren<Renderer>();
                meshFilters = GetComponentsInChildren<MeshFilter>();
            }
            else
            {
                renderers = GetComponents<Renderer>();
                meshFilters = GetComponents<MeshFilter>();
            }

            // Calculate combined bounds from all meshes
            CalculateCombinedBounds();

            // Initialize the visual
            Initialize();
        }

        protected virtual void OnEnable()
        {
            currentProgress = 0f;
            lastUpdateTime = 0f;
            Initialize();
        }

        protected virtual void Update()
        {
            if (parentBuilding == null) return;

            // Get current construction progress from parent building
            float newProgress = parentBuilding.ConstructionProgress;

            // Only update if enough time has passed and progress changed
            if (Time.time - lastUpdateTime >= updateInterval || Mathf.Abs(newProgress - currentProgress) > 0.01f)
            {
                currentProgress = newProgress;
                lastUpdateTime = Time.time;

                // Call the visual update method (implemented by child classes)
                UpdateVisual(currentProgress);
            }
        }

        /// <summary>
        /// Calculate the combined bounds of all meshes in the building
        /// </summary>
        protected virtual void CalculateCombinedBounds()
        {
            if (renderers.Length == 0) return;

            combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
        }

        /// <summary>
        /// Initialize the visual effect. Called on Awake and OnEnable.
        /// </summary>
        protected abstract void Initialize();

        /// <summary>
        /// Update the visual effect based on construction progress (0.0 to 1.0).
        /// </summary>
        /// <param name="progress">Construction progress from 0.0 (started) to 1.0 (complete)</param>
        protected abstract void UpdateVisual(float progress);

        /// <summary>
        /// Helper method to get mesh bounds along a specific axis
        /// </summary>
        protected Vector3 GetBoundsSize()
        {
            return combinedBounds.size;
        }

        /// <summary>
        /// Helper method to get mesh center in world space
        /// </summary>
        protected Vector3 GetBoundsCenter()
        {
            return combinedBounds.center;
        }

        protected virtual void OnDisable()
        {
            // Cleanup when disabled (called when construction completes)
            Cleanup();
        }

        /// <summary>
        /// Cleanup resources when the visual is disabled
        /// </summary>
        protected virtual void Cleanup()
        {
            // Override in child classes if needed
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            // Draw bounds for debugging
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(combinedBounds.center, combinedBounds.size);
        }
#endif
    }
}
