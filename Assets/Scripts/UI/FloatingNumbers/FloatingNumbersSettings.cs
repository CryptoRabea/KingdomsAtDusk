using UnityEngine;

namespace KAD.UI.FloatingNumbers
{
    /// <summary>
    /// Configuration settings for the floating numbers system.
    /// Controls what numbers are displayed and how they appear.
    /// </summary>
    [CreateAssetMenu(fileName = "FloatingNumbersSettings", menuName = "KAD/UI/Floating Numbers Settings")]
    public class FloatingNumbersSettings : ScriptableObject
    {
        [Header("Feature Toggles")]
        [Tooltip("Show health bars above units and buildings")]
        [SerializeField] private bool showHPBars = true;

        [Tooltip("Show damage numbers when units/buildings take damage")]
        [SerializeField] private bool showDamageNumbers = true;

        [Tooltip("Show healing numbers when units are healed")]
        [SerializeField] private bool showHealNumbers = true;

        [Tooltip("Show resource numbers when gatherers collect resources")]
        [SerializeField] private bool showResourceGatheringNumbers = true;

        [Tooltip("Show resource numbers when buildings generate resources")]
        [SerializeField] private bool showBuildingResourceNumbers = true;

        [Tooltip("Show repair numbers when buildings are repaired")]
        [SerializeField] private bool showRepairNumbers = true;

        [Header("Animation Settings")]
        [Tooltip("How long floating numbers stay visible (seconds)")]
        [SerializeField] private float numberDuration = 1.5f;

        [Tooltip("How high numbers float upward")]
        [SerializeField] private float floatHeight = 2f;

        [Tooltip("Scale animation curve for numbers")]
        [SerializeField] private AnimationCurve scaleAnimationCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 1f);

        [Tooltip("Fade out animation curve")]
        [SerializeField] private AnimationCurve fadeAnimationCurve = AnimationCurve.Linear(0, 1, 1, 0);

        [Header("Visual Settings")]
        [Tooltip("Font size for floating numbers")]
        [SerializeField] private int fontSize = 24;

        [Tooltip("Color for damage numbers")]
        [SerializeField] private Color damageColor = new Color(1f, 0.2f, 0.2f, 1f); // Red

        [Tooltip("Color for healing numbers")]
        [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.2f, 1f); // Green

        [Tooltip("Color for resource gain numbers")]
        [SerializeField] private Color resourceGainColor = new Color(1f, 0.84f, 0f, 1f); // Gold

        [Tooltip("Color for repair numbers")]
        [SerializeField] private Color repairColor = new Color(0.2f, 0.7f, 1f, 1f); // Blue

        [Tooltip("Color for critical hits (future feature)")]
        [SerializeField] private Color criticalColor = new Color(1f, 0.5f, 0f, 1f); // Orange

        [Header("HP Bar Settings")]
        [Tooltip("Show HP bars only for selected units")]
        [SerializeField] private bool hpBarsOnlyWhenSelected = false;

        [Tooltip("Show HP bars only when damaged")]
        [SerializeField] private bool hpBarsOnlyWhenDamaged = true;

        [Tooltip("Width of HP bars")]
        [SerializeField] private float hpBarWidth = 1f;

        [Tooltip("Height of HP bars")]
        [SerializeField] private float hpBarHeight = 0.15f;

        [Tooltip("Offset above unit/building")]
        [SerializeField] private float hpBarOffset = 2f;

        [Tooltip("HP bar background color")]
        [SerializeField] private Color hpBarBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        [Tooltip("HP bar fill color (full health)")]
        [SerializeField] private Color hpBarHealthyColor = new Color(0.2f, 1f, 0.2f, 1f); // Green

        [Tooltip("HP bar fill color (low health)")]
        [SerializeField] private Color hpBarLowHealthColor = new Color(1f, 0.2f, 0.2f, 1f); // Red

        [Tooltip("Health percentage considered 'low' for color change")]
        [SerializeField] private float lowHealthThreshold = 0.3f;

        [Header("Performance Settings")]
        [Tooltip("Maximum number of floating numbers visible at once")]
        [SerializeField] private int maxActiveNumbers = 50;

        [Tooltip("Pool size for floating number objects")]
        [SerializeField] private int poolSize = 100;

        [Tooltip("Update HP bars every N frames (1 = every frame)")]
        [SerializeField] private int hpBarUpdateInterval = 3;

        [Header("Blood Effect Settings")]
        [Tooltip("Enable blood effects when units take damage")]
        [SerializeField] private bool enableBloodEffects = true;

        [Tooltip("Use blue blood instead of red (less gore)")]
        [SerializeField] private bool useBlueBlood = false;

        [Tooltip("Show blood gush particle effect on hit")]
        [SerializeField] private bool showBloodGush = true;

        [Tooltip("Show blood dripping when heavily wounded")]
        [SerializeField] private bool showBloodDripping = true;

        [Tooltip("Health percentage below which blood starts dripping")]
        [SerializeField] private float bloodDrippingThreshold = 0.4f;

        [Tooltip("How long blood decals stay on the ground (seconds)")]
        [SerializeField] private float bloodDecalDuration = 10f;

        [Tooltip("Blood drip rate (drips per second when wounded)")]
        [SerializeField] private float bloodDripRate = 0.5f;

        [Tooltip("Maximum number of blood decals on ground")]
        [SerializeField] private int maxBloodDecals = 50;

        [Tooltip("Blood particle count for gush effect")]
        [SerializeField] private int bloodGushParticleCount = 20;

        [Header("Future Extensions")]
        [Tooltip("Show experience gain numbers (for future XP system)")]
        [SerializeField] private bool showExperienceNumbers = false;

        [Tooltip("Show resource pickup numbers (for future pickup system)")]
        [SerializeField] private bool showResourcePickupNumbers = false;

        [Tooltip("Show level up notifications")]
        [SerializeField] private bool showLevelUpNotifications = false;

        // Public properties for read access
        public bool ShowHPBars => showHPBars;
        public bool ShowDamageNumbers => showDamageNumbers;
        public bool ShowHealNumbers => showHealNumbers;
        public bool ShowResourceGatheringNumbers => showResourceGatheringNumbers;
        public bool ShowBuildingResourceNumbers => showBuildingResourceNumbers;
        public bool ShowRepairNumbers => showRepairNumbers;
        public bool ShowExperienceNumbers => showExperienceNumbers;
        public bool ShowResourcePickupNumbers => showResourcePickupNumbers;
        public bool ShowLevelUpNotifications => showLevelUpNotifications;

        public float NumberDuration => numberDuration;
        public float FloatHeight => floatHeight;
        public AnimationCurve ScaleAnimationCurve => scaleAnimationCurve;
        public AnimationCurve FadeAnimationCurve => fadeAnimationCurve;

        public int FontSize => fontSize;
        public Color DamageColor => damageColor;
        public Color HealColor => healColor;
        public Color ResourceGainColor => resourceGainColor;
        public Color RepairColor => repairColor;
        public Color CriticalColor => criticalColor;

        public bool HPBarsOnlyWhenSelected => hpBarsOnlyWhenSelected;
        public bool HPBarsOnlyWhenDamaged => hpBarsOnlyWhenDamaged;
        public float HPBarWidth => hpBarWidth;
        public float HPBarHeight => hpBarHeight;
        public float HPBarOffset => hpBarOffset;
        public Color HPBarBackgroundColor => hpBarBackgroundColor;
        public Color HPBarHealthyColor => hpBarHealthyColor;
        public Color HPBarLowHealthColor => hpBarLowHealthColor;
        public float LowHealthThreshold => lowHealthThreshold;

        public int MaxActiveNumbers => maxActiveNumbers;
        public int PoolSize => poolSize;
        public int HPBarUpdateInterval => hpBarUpdateInterval;

        public bool EnableBloodEffects => enableBloodEffects;
        public bool UseBlueBlood => useBlueBlood;
        public bool ShowBloodGush => showBloodGush;
        public bool ShowBloodDripping => showBloodDripping;
        public float BloodDrippingThreshold => bloodDrippingThreshold;
        public float BloodDecalDuration => bloodDecalDuration;
        public float BloodDripRate => bloodDripRate;
        public int MaxBloodDecals => maxBloodDecals;
        public int BloodGushParticleCount => bloodGushParticleCount;

        // Public methods for runtime modification (used by settings UI)
        public void SetShowHPBars(bool value) => showHPBars = value;
        public void SetShowDamageNumbers(bool value) => showDamageNumbers = value;
        public void SetShowHealNumbers(bool value) => showHealNumbers = value;
        public void SetShowResourceGatheringNumbers(bool value) => showResourceGatheringNumbers = value;
        public void SetShowBuildingResourceNumbers(bool value) => showBuildingResourceNumbers = value;
        public void SetShowRepairNumbers(bool value) => showRepairNumbers = value;
        public void SetShowExperienceNumbers(bool value) => showExperienceNumbers = value;
        public void SetShowResourcePickupNumbers(bool value) => showResourcePickupNumbers = value;
        public void SetShowLevelUpNotifications(bool value) => showLevelUpNotifications = value;

        public void SetHPBarsOnlyWhenSelected(bool value) => hpBarsOnlyWhenSelected = value;
        public void SetHPBarsOnlyWhenDamaged(bool value) => hpBarsOnlyWhenDamaged = value;

        public void SetEnableBloodEffects(bool value) => enableBloodEffects = value;
        public void SetUseBlueBlood(bool value) => useBlueBlood = value;
        public void SetShowBloodGush(bool value) => showBloodGush = value;
        public void SetShowBloodDripping(bool value) => showBloodDripping = value;

        /// <summary>
        /// Get the blood color based on settings (red or blue).
        /// </summary>
        public Color GetBloodColor()
        {
            return useBlueBlood ? new Color(0.2f, 0.4f, 1f, 1f) : new Color(0.8f, 0f, 0f, 1f);
        }

        /// <summary>
        /// Get interpolated HP bar color based on health percentage.
        /// </summary>
        public Color GetHPBarColor(float healthPercentage)
        {
            if (healthPercentage <= lowHealthThreshold)
            {
                return Color.Lerp(hpBarLowHealthColor, hpBarHealthyColor, healthPercentage / lowHealthThreshold);
            }
            return hpBarHealthyColor;
        }

        /// <summary>
        /// Validate settings on load/change.
        /// </summary>
        private void OnValidate()
        {
            numberDuration = Mathf.Max(0.1f, numberDuration);
            floatHeight = Mathf.Max(0.1f, floatHeight);
            fontSize = Mathf.Max(8, fontSize);
            maxActiveNumbers = Mathf.Max(1, maxActiveNumbers);
            poolSize = Mathf.Max(10, poolSize);
            hpBarUpdateInterval = Mathf.Max(1, hpBarUpdateInterval);
            hpBarWidth = Mathf.Max(0.1f, hpBarWidth);
            hpBarHeight = Mathf.Max(0.05f, hpBarHeight);
            lowHealthThreshold = Mathf.Clamp01(lowHealthThreshold);
            bloodDrippingThreshold = Mathf.Clamp01(bloodDrippingThreshold);
            bloodDecalDuration = Mathf.Max(1f, bloodDecalDuration);
            bloodDripRate = Mathf.Max(0.1f, bloodDripRate);
            maxBloodDecals = Mathf.Max(1, maxBloodDecals);
            bloodGushParticleCount = Mathf.Max(5, bloodGushParticleCount);
        }

        /// <summary>
        /// Reset to default values.
        /// </summary>
        public void ResetToDefaults()
        {
            showHPBars = true;
            showDamageNumbers = true;
            showHealNumbers = true;
            showResourceGatheringNumbers = true;
            showBuildingResourceNumbers = true;
            showRepairNumbers = true;
            showExperienceNumbers = false;
            showResourcePickupNumbers = false;
            showLevelUpNotifications = false;

            numberDuration = 1.5f;
            floatHeight = 2f;
            fontSize = 24;

            // Create default animation curves
            scaleAnimationCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 1f);
            fadeAnimationCurve = AnimationCurve.Linear(0, 1, 1, 0);

            damageColor = new Color(1f, 0.2f, 0.2f, 1f);
            healColor = new Color(0.2f, 1f, 0.2f, 1f);
            resourceGainColor = new Color(1f, 0.84f, 0f, 1f);
            repairColor = new Color(0.2f, 0.7f, 1f, 1f);
            criticalColor = new Color(1f, 0.5f, 0f, 1f);

            hpBarsOnlyWhenSelected = false;
            hpBarsOnlyWhenDamaged = true;
            hpBarWidth = 1f;
            hpBarHeight = 0.15f;
            hpBarOffset = 2f;
            hpBarBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            hpBarHealthyColor = new Color(0.2f, 1f, 0.2f, 1f);
            hpBarLowHealthColor = new Color(1f, 0.2f, 0.2f, 1f);
            lowHealthThreshold = 0.3f;

            maxActiveNumbers = 50;
            poolSize = 100;
            hpBarUpdateInterval = 3;

            enableBloodEffects = true;
            useBlueBlood = false;
            showBloodGush = true;
            showBloodDripping = true;
            bloodDrippingThreshold = 0.4f;
            bloodDecalDuration = 10f;
            bloodDripRate = 0.5f;
            maxBloodDecals = 50;
            bloodGushParticleCount = 20;
        }
    }
}
