using UnityEngine;
using System.Collections.Generic;

namespace CircularLensVision
{
    /// <summary>
    /// Component that should be attached to units and obstacles that participate in lens vision.
    /// Handles material switching and shader property animation.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LensVisionTarget : MonoBehaviour
    {
        [Header("Target Configuration")]
        [Tooltip("Type of target (Unit or Obstacle)")]
        [SerializeField] private TargetType targetType = TargetType.Unit;

        [Tooltip("Should this target automatically register with CircularLensVision on start?")]
        [SerializeField] private bool autoRegister = true;

        [Header("Materials")]
        [Tooltip("Original materials (will be cached automatically if empty)")]
        [SerializeField] private Material[] originalMaterials;

        [Tooltip("Lens materials to use when in lens range (optional, will create at runtime if empty)")]
        [SerializeField] private Material[] lensMaterials;

        [Header("Transition Settings")]
        [Tooltip("Fade speed for lens effect")]
        [SerializeField] private float fadeSpeed = 5f;

        [Tooltip("X-Ray color for units")]
        [SerializeField] private Color xrayColor = new Color(0.3f, 0.7f, 1f, 0.8f);

        [Tooltip("Transparency amount for obstacles")]
        [SerializeField] private float transparencyAmount = 0.3f;

        // Runtime state
        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;
        private CircularLensVision lensController;
        private bool isLensActive = false;
        private float currentLensFade = 0f;

        // Shader property IDs (cached for performance)
        private static readonly int XRayColorID = Shader.PropertyToID("_XRayColor");
        private static readonly int XRayIntensityID = Shader.PropertyToID("_XRayIntensity");
        private static readonly int TransparencyAmountID = Shader.PropertyToID("_TransparencyAmount");
        private static readonly int TransparentColorID = Shader.PropertyToID("_TransparentColor");

        public enum TargetType
        {
            Unit,
            Obstacle
        }

        // Public properties
        public TargetType Type => targetType;
        public bool IsLensActive => isLensActive;

        private void Awake()
        {
            // Get all renderers
            renderers = GetComponentsInChildren<Renderer>();

            // Cache original materials if not set
            if (originalMaterials == null || originalMaterials.Length == 0)
            {
                CacheOriginalMaterials();
            }

            // Initialize property block
            propertyBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            // Auto-register with lens controller
            if (autoRegister)
            {
                RegisterWithController();
            }

            // Initialize shader properties
            UpdateShaderProperties();
        }

        private void OnEnable()
        {
            if (lensController != null && autoRegister)
            {
                lensController.RegisterTarget(this);
            }
        }

        private void OnDisable()
        {
            // Ensure lens is deactivated
            SetLensActive(false);

            if (lensController != null)
            {
                lensController.UnregisterTarget(this);
            }
        }

        private void Update()
        {
            // Smoothly fade lens effect in/out
            if (isLensActive)
            {
                currentLensFade = Mathf.MoveTowards(currentLensFade, 1f, Time.deltaTime * fadeSpeed);
            }
            else
            {
                currentLensFade = Mathf.MoveTowards(currentLensFade, 0f, Time.deltaTime * fadeSpeed);
            }

            // Update shader properties based on fade
            if (currentLensFade > 0.01f)
            {
                UpdateShaderProperties();
            }
        }

        /// <summary>
        /// Called by CircularLensVision to activate/deactivate lens effect
        /// </summary>
        public void SetLensActive(bool active)
        {
            if (isLensActive == active) return;

            isLensActive = active;

            // If we have custom lens materials, switch to them
            if (lensMaterials != null && lensMaterials.Length > 0)
            {
                SwapMaterials(active);
            }
        }

        private void RegisterWithController()
        {
            // Find the lens controller in the scene
            lensController = FindFirstObjectByType<CircularLensVision>();

            if (lensController != null)
            {
                lensController.RegisterTarget(this);
            }
            else
            {
                Debug.LogWarning($"LensVisionTarget on {gameObject.name} could not find CircularLensVision controller in scene.", this);
            }
        }

        private void CacheOriginalMaterials()
        {
            List<Material> materials = new List<Material>();

            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    materials.AddRange(renderer.sharedMaterials);
                }
            }

            originalMaterials = materials.ToArray();
        }

        private void SwapMaterials(bool useLensMaterials)
        {
            if (renderers == null || renderers.Length == 0) return;

            Material[] materialsToUse = useLensMaterials ? lensMaterials : originalMaterials;

            if (materialsToUse == null || materialsToUse.Length == 0) return;

            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    // Create material array matching renderer's material count
                    Material[] newMaterials = new Material[renderer.sharedMaterials.Length];

                    for (int i = 0; i < newMaterials.Length; i++)
                    {
                        // Use lens material if available, otherwise use original
                        newMaterials[i] = i < materialsToUse.Length ? materialsToUse[i] : renderer.sharedMaterials[i];
                    }

                    renderer.sharedMaterials = newMaterials;
                }
            }
        }

        private void UpdateShaderProperties()
        {
            if (renderers == null || renderers.Length == 0) return;

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                // Get current property block
                renderer.GetPropertyBlock(propertyBlock);

                if (targetType == TargetType.Unit)
                {
                    // Update unit x-ray properties
                    propertyBlock.SetColor(XRayColorID, xrayColor);
                    propertyBlock.SetFloat(XRayIntensityID, currentLensFade);
                }
                else // Obstacle
                {
                    // Update obstacle transparency properties
                    Color transparentColor = new Color(0.5f, 0.5f, 0.5f, transparencyAmount);
                    propertyBlock.SetFloat(TransparencyAmountID, transparencyAmount * currentLensFade);
                    propertyBlock.SetColor(TransparentColorID, transparentColor);
                }

                // Apply property block
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        /// <summary>
        /// Manually set the lens controller (alternative to auto-registration)
        /// </summary>
        public void SetLensController(CircularLensVision controller)
        {
            if (lensController != null)
            {
                lensController.UnregisterTarget(this);
            }

            lensController = controller;

            if (lensController != null)
            {
                lensController.RegisterTarget(this);
            }
        }

        /// <summary>
        /// Update the x-ray color at runtime
        /// </summary>
        public void SetXRayColor(Color color)
        {
            xrayColor = color;
            UpdateShaderProperties();
        }

        /// <summary>
        /// Update transparency amount at runtime
        /// </summary>
        public void SetTransparencyAmount(float amount)
        {
            transparencyAmount = Mathf.Clamp01(amount);
            UpdateShaderProperties();
        }

        /// <summary>
        /// Force immediate update of lens state (useful after material changes)
        /// </summary>
        public void RefreshLensState()
        {
            CacheOriginalMaterials();
            UpdateShaderProperties();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Clamp values in editor
            fadeSpeed = Mathf.Max(0.1f, fadeSpeed);
            transparencyAmount = Mathf.Clamp01(transparencyAmount);
        }

        private void Reset()
        {
            // Auto-detect target type based on layer
            int layer = gameObject.layer;
            string layerName = LayerMask.LayerToName(layer);

            if (layerName.Contains("Unit") || layerName.Contains("Player") || layerName.Contains("Enemy"))
            {
                targetType = TargetType.Unit;
            }
            else
            {
                targetType = TargetType.Obstacle;
            }
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // Draw bounds of all renderers
            Renderer[] currentRenderers = GetComponentsInChildren<Renderer>();

            Gizmos.color = targetType == TargetType.Unit ? Color.cyan : Color.yellow;

            foreach (var renderer in currentRenderers)
            {
                if (renderer != null)
                {
                    Gizmos.DrawWireCube(renderer.bounds.center, renderer.bounds.size);
                }
            }
        }
    }
}
