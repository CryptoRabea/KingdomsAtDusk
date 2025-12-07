using UnityEngine;
using System.Collections.Generic;
using RTS.Core.Services;
using TMPro;
using UnityEngine.UI;
using RTS.Core.Events;
using RTS.Buildings;
using KAD.UI.FloatingNumbers;

namespace Assets.Scripts.UI.FloatingNumbers
{
    /// <summary>
    /// Manages floating numbers and HP bars for the entire game.
    /// Subscribes to game events and displays appropriate visual feedback.
    /// Uses object pooling for performance.
    /// </summary>
    public class FloatingNumbersManager : MonoBehaviour, IFloatingNumberService
    {
        [Header("Configuration")]
        [SerializeField] private FloatingNumbersSettings settings;

        [Header("Prefabs")]
        [SerializeField] private GameObject floatingNumberPrefab;
        [SerializeField] private GameObject hpBarPrefab;

        [Header("Canvas References")]
        [SerializeField] private Canvas floatingNumberCanvas;
        [SerializeField] private Canvas hpBarCanvas;

        // Pools
        private Queue<FloatingNumber> floatingNumberPool = new Queue<FloatingNumber>();
        private Queue<HPBar> hpBarPool = new Queue<HPBar>();
        private Queue<BloodEffect> bloodEffectPool = new Queue<BloodEffect>();
        private Queue<BloodDecal> bloodDecalPool = new Queue<BloodDecal>();

        private List<FloatingNumber> activeNumbers = new List<FloatingNumber>();
        private Dictionary<GameObject, HPBar> activeHPBars = new Dictionary<GameObject, HPBar>();
        private List<BloodEffect> activeBloodEffects = new List<BloodEffect>();
        private List<BloodDecal> activeBloodDecals = new List<BloodDecal>();
        private Dictionary<GameObject, BloodDripper> activeDrippers = new Dictionary<GameObject, BloodDripper>();

        private Camera mainCamera;

        public FloatingNumbersSettings Settings => settings;

        private void Awake()
        {
            mainCamera = Camera.main;

            // Ensure settings exist
            if (settings == null)
            {
                // Create default settings
                settings = ScriptableObject.CreateInstance<FloatingNumbersSettings>();
                // Initialize with default values by calling ResetToDefaults
                settings.ResetToDefaults();
            }
            else
            {
            }


            InitializeCanvases();
            WarmupPools();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeCanvases()
        {
            // Create floating number canvas if not assigned
            if (floatingNumberCanvas == null)
            {
                GameObject canvasObj = new GameObject("FloatingNumberCanvas");
                canvasObj.transform.SetParent(transform);
                floatingNumberCanvas = canvasObj.AddComponent<Canvas>();
                floatingNumberCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                floatingNumberCanvas.sortingOrder = 100; // Above most UI

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            // Create HP bar canvas if not assigned
            if (hpBarCanvas == null)
            {
                GameObject canvasObj = new GameObject("HPBarCanvas");
                canvasObj.transform.SetParent(transform);
                hpBarCanvas = canvasObj.AddComponent<Canvas>();
                hpBarCanvas.renderMode = RenderMode.WorldSpace;
                hpBarCanvas.sortingOrder = 10;
            }

            // Create prefabs if not assigned
            if (floatingNumberPrefab == null)
            {
                floatingNumberPrefab = CreateFloatingNumberPrefab();
            }

            if (hpBarPrefab == null)
            {
                hpBarPrefab = CreateHPBarPrefab();
            }
        }

        private GameObject CreateFloatingNumberPrefab()
        {
            GameObject prefab = new GameObject("FloatingNumberPrefab");
            prefab.transform.SetParent(floatingNumberCanvas.transform, false);

            RectTransform rectTransform = prefab.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50);

            TextMeshProUGUI textMesh = prefab.AddComponent<TextMeshProUGUI>();
            textMesh.fontSize = settings.FontSize;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            textMesh.fontStyle = FontStyles.Bold;

            prefab.AddComponent<FloatingNumber>();
            prefab.SetActive(false);

            return prefab;
        }

        private GameObject CreateHPBarPrefab()
        {
            GameObject prefab = new GameObject("HPBarPrefab");

            Canvas canvas = prefab.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            CanvasScaler scaler = prefab.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            prefab.AddComponent<CanvasGroup>();

            RectTransform rectTransform = prefab.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(settings.HPBarWidth * 100f, settings.HPBarHeight * 100f);

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(prefab.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = settings.HPBarBackgroundColor;

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(prefab.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = settings.HPBarHealthyColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

            HPBar hpBar = prefab.AddComponent<HPBar>();

            // Set serialized fields via reflection for setup
            System.Reflection.FieldInfo bgField = typeof(HPBar).GetField("backgroundImage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo fillField = typeof(HPBar).GetField("fillImage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo canvasField = typeof(HPBar).GetField("canvas",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo canvasGroupField = typeof(HPBar).GetField("canvasGroup",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (bgField != null) bgField.SetValue(hpBar, bgImage);
            if (fillField != null) fillField.SetValue(hpBar, fillImage);
            if (canvasField != null) canvasField.SetValue(hpBar, canvas);
            if (canvasGroupField != null) canvasGroupField.SetValue(hpBar, prefab.GetComponent<CanvasGroup>());

            prefab.SetActive(false);

            return prefab;
        }

        private void WarmupPools()
        {
            // Warmup floating number pool
            for (int i = 0; i < settings.PoolSize; i++)
            {
                FloatingNumber number = CreateFloatingNumber();
                number.gameObject.SetActive(false);
                floatingNumberPool.Enqueue(number);
            }

            // Warmup HP bar pool (smaller pool)
            for (int i = 0; i < 50; i++)
            {
                HPBar hpBar = CreateHPBar();
                hpBar.gameObject.SetActive(false);
                hpBarPool.Enqueue(hpBar);
            }

            // Warmup blood effect pool
            for (int i = 0; i < 30; i++)
            {
                BloodEffect bloodEffect = CreateBloodEffect();
                bloodEffect.gameObject.SetActive(false);
                bloodEffectPool.Enqueue(bloodEffect);
            }

            // Warmup blood decal pool
            for (int i = 0; i < settings.MaxBloodDecals; i++)
            {
                BloodDecal bloodDecal = CreateBloodDecal();
                bloodDecal.gameObject.SetActive(false);
                bloodDecalPool.Enqueue(bloodDecal);
            }
        }

        private FloatingNumber CreateFloatingNumber()
        {
            GameObject obj = Instantiate(floatingNumberPrefab, floatingNumberCanvas.transform);
            return obj.GetComponent<FloatingNumber>();
        }

        private HPBar CreateHPBar()
        {
            GameObject obj = Instantiate(hpBarPrefab, hpBarCanvas.transform);
            return obj.GetComponent<HPBar>();
        }

        private FloatingNumber GetFloatingNumber()
        {
            if (floatingNumberPool.Count > 0)
            {
                return floatingNumberPool.Dequeue();
            }

            return CreateFloatingNumber();
        }

        private void ReturnFloatingNumber(FloatingNumber number)
        {
            if (number == null) return;

            activeNumbers.Remove(number);
            number.gameObject.SetActive(false);
            floatingNumberPool.Enqueue(number);
        }

        private HPBar GetHPBar()
        {
            if (hpBarPool.Count > 0)
            {
                return hpBarPool.Dequeue();
            }

            return CreateHPBar();
        }

        private void ReturnHPBar(HPBar hpBar)
        {
            if (hpBar == null) return;

            hpBar.Cleanup();
            hpBarPool.Enqueue(hpBar);
        }

        private BloodEffect CreateBloodEffect()
        {
            GameObject obj = new GameObject("BloodEffect");
            obj.transform.SetParent(transform);
            return obj.AddComponent<BloodEffect>();
        }

        private BloodEffect GetBloodEffect()
        {
            if (bloodEffectPool.Count > 0)
            {
                return bloodEffectPool.Dequeue();
            }

            return CreateBloodEffect();
        }

        private void ReturnBloodEffect(BloodEffect effect)
        {
            if (effect == null) return;

            activeBloodEffects.Remove(effect);
            effect.gameObject.SetActive(false);
            bloodEffectPool.Enqueue(effect);
        }

        private BloodDecal CreateBloodDecal()
        {
            GameObject obj = new GameObject("BloodDecal");
            obj.transform.SetParent(transform);
            return obj.AddComponent<BloodDecal>();
        }

        private BloodDecal GetBloodDecal()
        {
            if (bloodDecalPool.Count > 0)
            {
                return bloodDecalPool.Dequeue();
            }

            // If pool exhausted and at max, force remove oldest
            if (activeBloodDecals.Count >= settings.MaxBloodDecals)
            {
                BloodDecal oldest = activeBloodDecals[0];
                if (oldest != null)
                {
                    oldest.ForceStop();
                }
            }

            return CreateBloodDecal();
        }

        private void ReturnBloodDecal(BloodDecal decal)
        {
            if (decal == null) return;

            activeBloodDecals.Remove(decal);
            decal.gameObject.SetActive(false);
            bloodDecalPool.Enqueue(decal);
        }

        #region IFloatingNumberService Implementation

        public void ShowDamageNumber(Vector3 worldPosition, float damageAmount, bool isCritical = false)
        {

            if (settings == null)
            {
                return;
            }


            if (!settings.ShowDamageNumbers)
            {
                return;
            }

            Vector2 screenPos = WorldToCanvasPosition(worldPosition);
            Color color = isCritical ? settings.CriticalColor : settings.DamageColor;
            string text = $"-{Mathf.RoundToInt(damageAmount)}";

            ShowNumber(text, screenPos, color);
        }

        public void ShowHealNumber(Vector3 worldPosition, float healAmount)
        {
            if (!settings.ShowHealNumbers) return;

            Vector2 screenPos = WorldToCanvasPosition(worldPosition);
            string text = $"+{Mathf.RoundToInt(healAmount)}";

            ShowNumber(text, screenPos, settings.HealColor);
        }

        public void ShowResourceNumber(Vector3 worldPosition, ResourceType resourceType, int amount)
        {
            if (!settings.ShowBuildingResourceNumbers && !settings.ShowResourceGatheringNumbers) return;

            Vector2 screenPos = WorldToCanvasPosition(worldPosition);
            string resourceIcon = GetResourceIcon(resourceType);
            string text = $"+{amount} {resourceIcon}";

            ShowNumber(text, screenPos, settings.ResourceGainColor);
        }

        public void ShowRepairNumber(Vector3 worldPosition, float repairAmount)
        {
            if (!settings.ShowRepairNumbers) return;

            Vector2 screenPos = WorldToCanvasPosition(worldPosition);
            string text = $"+{Mathf.RoundToInt(repairAmount)}";

            ShowNumber(text, screenPos, settings.RepairColor);
        }

        public void ShowExperienceNumber(Vector3 worldPosition, int xpAmount)
        {
            if (!settings.ShowExperienceNumbers) return;

            Vector2 screenPos = WorldToCanvasPosition(worldPosition);
            string text = $"+{xpAmount} XP";

            ShowNumber(text, screenPos, Color.cyan);
        }

        public void RegisterHPBar(GameObject target, System.Func<float> getCurrentHealth, System.Func<float> getMaxHealth)
        {
            if (!settings.ShowHPBars) return;
            if (target == null) return;

            // Don't create duplicate HP bars
            if (activeHPBars.ContainsKey(target))
            {
                return;
            }

            HPBar hpBar = GetHPBar();
            hpBar.Initialize(target, getCurrentHealth, getMaxHealth, settings);
            activeHPBars[target] = hpBar;
        }

        public void UnregisterHPBar(GameObject target)
        {
            if (target == null) return;

            if (activeHPBars.TryGetValue(target, out HPBar hpBar))
            {
                activeHPBars.Remove(target);
                ReturnHPBar(hpBar);
            }
        }

        public void RefreshSettings()
        {
            // Refresh all active HP bars
            foreach (var hpBar in activeHPBars.Values)
            {
                hpBar.Refresh();
            }
        }

        public void ShowBloodGush(Vector3 worldPosition, Vector3 direction, int particleCount = -1)
        {
            if (!settings.EnableBloodEffects || !settings.ShowBloodGush) return;

            int count = particleCount > 0 ? particleCount : settings.BloodGushParticleCount;
            Color bloodColor = settings.GetBloodColor();

            BloodEffect effect = GetBloodEffect();
            activeBloodEffects.Add(effect);

            effect.Initialize(worldPosition, direction, bloodColor, count, ReturnBloodEffect);
        }

        public void ShowBloodDecal(Vector3 worldPosition)
        {
            if (!settings.EnableBloodEffects) return;

            Color bloodColor = settings.GetBloodColor();

            BloodDecal decal = GetBloodDecal();
            activeBloodDecals.Add(decal);

            decal.Initialize(
                worldPosition,
                bloodColor,
                settings.BloodDecalDuration,
                0.3f, // base size
                ReturnBloodDecal
            );
        }

        public void StartBloodDripping(GameObject target, System.Func<float> getCurrentHealth, System.Func<float> getMaxHealth)
        {
            if (!settings.EnableBloodEffects || !settings.ShowBloodDripping) return;
            if (target == null) return;

            // Check if already dripping
            if (activeDrippers.ContainsKey(target))
                return;

            // Check if wounded enough
            if (getCurrentHealth() / getMaxHealth() > settings.BloodDrippingThreshold)
                return;

            // Add dripper component
            BloodDripper dripper = target.AddComponent<BloodDripper>();
            dripper.Initialize(getCurrentHealth, getMaxHealth, settings, this);
            activeDrippers[target] = dripper;
        }

        public void StopBloodDripping(GameObject target)
        {
            if (target == null) return;

            if (activeDrippers.TryGetValue(target, out BloodDripper dripper))
            {
                activeDrippers.Remove(target);
                if (dripper != null)
                {
                    dripper.StopDripping();
                }
            }
        }

        #endregion

        private void ShowNumber(string text, Vector2 screenPosition, Color color)
        {

            if (settings == null)
            {
                return;
            }

            // Check if we've hit the max active numbers limit
            if (activeNumbers.Count >= settings.MaxActiveNumbers)
            {
                // Force oldest number to complete
                if (activeNumbers.Count > 0)
                {
                    activeNumbers[0].ForceStop();
                }
            }

            FloatingNumber number = GetFloatingNumber();
            if (number == null)
            {
                return;
            }

            activeNumbers.Add(number);

            number.Initialize(
                text,
                screenPosition,
                color,
                settings.FontSize,
                settings.NumberDuration,
                settings.FloatHeight * 100f, // Convert to screen space
                settings.ScaleAnimationCurve,
                settings.FadeAnimationCurve,
                ReturnFloatingNumber
            );

        }

        private Vector2 WorldToCanvasPosition(Vector3 worldPosition)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return Vector2.zero;
            }

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCamera, worldPosition);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                floatingNumberCanvas.GetComponent<RectTransform>(),
                screenPoint,
                floatingNumberCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera,
                out Vector2 canvasPosition
            );

            return canvasPosition;
        }

        private string GetResourceIcon(ResourceType resourceType)
        {
            return resourceType switch
            {
                ResourceType.Wood => "🪵",
                ResourceType.Food => "🌾",
                ResourceType.Gold => "💰",
                ResourceType.Stone => "🪨",
                _ => ""
            };
        }

        #region Event Subscriptions

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<HealingAppliedEvent>(OnHealingApplied);
            EventBus.Subscribe<ResourcesGeneratedEvent>(OnResourcesGenerated);
            EventBus.Subscribe<BuildingDamagedEvent>(OnBuildingDamaged);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<HealingAppliedEvent>(OnHealingApplied);
            EventBus.Unsubscribe<ResourcesGeneratedEvent>(OnResourcesGenerated);
            EventBus.Unsubscribe<BuildingDamagedEvent>(OnBuildingDamaged);
        }

        private void OnDamageDealt(DamageDealtEvent evt)
        {

            if (evt.Target == null || evt.Target.transform == null)
            {
                return;
            }

            if (settings == null)
            {
                return;
            }

            Vector3 position = evt.Target.transform.position + Vector3.up;

            // Show damage number
            ShowDamageNumber(position, evt.Damage);

            // Show blood gush effect
            if (settings.EnableBloodEffects && settings.ShowBloodGush)
            {
                Vector3 direction = Vector3.zero;
                if (evt.Attacker != null && evt.Attacker.transform != null)
                {
                    // Direction from attacker to target
                    direction = (evt.Target.transform.position - evt.Attacker.transform.position).normalized;
                }
                else
                {
                    // Random direction if no attacker
                    direction = new Vector3(Random.Range(-1f, 1f), Random.Range(0f, 1f), Random.Range(-1f, 1f)).normalized;
                }

                ShowBloodGush(position, direction);
            }

            // Show blood decal on ground
            if (settings.EnableBloodEffects)
            {
                Vector3 groundPosition = evt.Target.transform.position;
                groundPosition.y = 0.01f; // Just above ground
                ShowBloodDecal(groundPosition);
            }

            // Start blood dripping if unit is heavily wounded
            var unitHealth = evt.Target.GetComponent<RTS.Units.UnitHealth>();
            if (unitHealth != null)
            {
                float healthPercent = unitHealth.CurrentHealth / unitHealth.MaxHealth;
                if (healthPercent <= settings.BloodDrippingThreshold)
                {
                    StartBloodDripping(
                        evt.Target,
                        () => unitHealth.CurrentHealth,
                        () => unitHealth.MaxHealth
                    );
                }
            }
        }

        private void OnHealingApplied(HealingAppliedEvent evt)
        {
            if (evt.Target == null || evt.Target.transform == null) return;

            Vector3 position = evt.Target.transform.position + Vector3.up;
            ShowHealNumber(position, evt.Amount);
        }

        private void OnResourcesGenerated(ResourcesGeneratedEvent evt)
        {
            if (!settings.ShowBuildingResourceNumbers) return;

            // Try to find the building GameObject by name
            // This is a simplified approach - you might want to improve this
            GameObject buildingObj = GameObject.Find(evt.BuildingName);
            if (buildingObj != null)
            {
                Vector3 position = buildingObj.transform.position + Vector3.up * 2f;
                ShowResourceNumber(position, evt.ResourceType, evt.Amount);
            }
        }

        private void OnBuildingDamaged(BuildingDamagedEvent evt)
        {
            if (evt.Building == null || evt.Building.transform == null) return;

            Vector3 position = evt.Building.transform.position + Vector3.up * 2f;

            // If delta is positive, it's repair. If negative, it's damage.
            if (evt.Delta > 0)
            {
                ShowRepairNumber(position, evt.Delta);
            }
            else if (evt.Delta < 0)
            {
                ShowDamageNumber(position, Mathf.Abs(evt.Delta));
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Cleanup all active objects
            foreach (var number in activeNumbers)
            {
                if (number != null)
                {
                    Destroy(number.gameObject);
                }
            }

            foreach (var hpBar in activeHPBars.Values)
            {
                if (hpBar != null)
                {
                    Destroy(hpBar.gameObject);
                }
            }

            foreach (var effect in activeBloodEffects)
            {
                if (effect != null)
                {
                    Destroy(effect.gameObject);
                }
            }

            foreach (var decal in activeBloodDecals)
            {
                if (decal != null)
                {
                    Destroy(decal.gameObject);
                }
            }

            foreach (var dripper in activeDrippers.Values)
            {
                if (dripper != null)
                {
                    dripper.StopDripping();
                }
            }

            activeNumbers.Clear();
            activeHPBars.Clear();
            activeBloodEffects.Clear();
            activeBloodDecals.Clear();
            activeDrippers.Clear();
        }
    }
}
