using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Core.Events;
using RTS.Units;
using RTS.Units.Formation;
using System.Collections.Generic;
using System;

namespace RTS.UI
{
    /// <summary>
    /// Displays detailed information about a selected unit.
    /// Shows unit stats from UnitConfigSO when a unit is selected.
    ///
    /// MULTI-UNIT SELECTION:
    /// - When 1 unit selected: Shows detailed stats
    /// - When 2+ units selected: Shows grid of unit icons with HP bars
    /// - Formation buttons remain visible in both modes
    /// </summary>
    public class UnitDetailsUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject unitDetailsPanel;
        [SerializeField] private Image unitPortrait;
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI attackDamageText;
        [SerializeField] private TextMeshProUGUI attackSpeedText;
        [SerializeField] private TextMeshProUGUI attackRangeText;

        [Header("Multi-Unit Selection")]
        [SerializeField] private GameObject singleUnitStatsContainer;
        [SerializeField] private GameObject multiUnitSelectionContainer;
        [SerializeField] private MultiUnitSelectionUI multiUnitSelectionUI;

        [Header("Formation")]
        [SerializeField] private TMP_Dropdown formationDropdown;
        [SerializeField] private FormationGroupManager formationGroupManager;
        [SerializeField] private Button expandFormationsButton;
        [SerializeField] private GameObject formationsPanel; // Expandable panel for all formations
        [SerializeField] private Transform formationsPanelContent; // Content container for formation list items
        [SerializeField] private GameObject formationListItemPrefab; // Prefab for formation list items
        [SerializeField] private FormationSelectorUI formationSelector;
        [SerializeField] private FormationBuilderUI formationBuilder;

        // Dropdown index tracking
        private const int CUSTOMIZE_FORMATION_INDEX = 7; // After standard formations (0-6)
        private Dictionary<int, string> customFormationIndexMap = new Dictionary<int, string>();

        [Header("Health Bar")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color damagedColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;

        private GameObject currentSelectedUnit;
        private UnitHealth currentUnitHealth;
        private int currentSelectionCount = 0;

        private void OnEnable()
        {
            EventBus.Subscribe<UnitSelectedEvent>(OnUnitSelected);
            EventBus.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
            EventBus.Subscribe<UnitHealthChangedEvent>(OnUnitHealthChanged);
            EventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
            EventBus.Subscribe<FormationChangedEvent>(OnFormationChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
            EventBus.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
            EventBus.Unsubscribe<UnitHealthChangedEvent>(OnUnitHealthChanged);
            EventBus.Unsubscribe<SelectionChangedEvent>(OnSelectionChanged);
            EventBus.Unsubscribe<FormationChangedEvent>(OnFormationChanged);
        }

        private void Start()
        {
            // Hide panel initially
            if (unitDetailsPanel != null)
            {
                unitDetailsPanel.SetActive(false);
            }

            // Configure health bar fill image to work as a horizontal slider
            // We'll use RectTransform scaling for the slider effect
            if (healthBarFill != null)
            {
                RectTransform fillRect = healthBarFill.GetComponent<RectTransform>();
                if (fillRect != null)
                {
                    // Set anchors to stretch from left
                    fillRect.anchorMin = new Vector2(0, 0);
                    fillRect.anchorMax = new Vector2(0, 1);
                    fillRect.pivot = new Vector2(0, 0.5f);
                    fillRect.anchoredPosition = Vector2.zero;

                    // Get the parent width to set the fill width
                    RectTransform parentRect = healthBarFill.transform.parent.GetComponent<RectTransform>();
                    if (parentRect != null)
                    {
                        fillRect.sizeDelta = new Vector2(parentRect.rect.width, 0);
                    }
                }
            }

            // Initialize formation dropdown
            InitializeFormationDropdown();

            // Setup expand button
            if (expandFormationsButton != null)
            {
                expandFormationsButton.onClick.AddListener(OnExpandFormationsButtonClicked);
            }

            // Hide formations panel initially
            if (formationsPanel != null)
            {
                formationsPanel.SetActive(false);
            }

            // Subscribe to custom formation changes
            if (CustomFormationManager.Instance != null)
            {
                CustomFormationManager.Instance.OnFormationsChanged += OnCustomFormationsChanged;
            }
        }

        private void InitializeFormationDropdown()
        {
            if (formationDropdown == null) return;

            // Clear existing options and mapping
            formationDropdown.ClearOptions();
            customFormationIndexMap.Clear();

            // Add all formation types
            List<string> options = new List<string>();
            foreach (FormationType formationType in Enum.GetValues(typeof(FormationType)))
            {
                options.Add(FormatFormationName(formationType));
            }

            // Add "Customize Formation" option
            options.Add("⚙️ Customize Formation");

            // Add custom formations from quick list
            if (CustomFormationManager.Instance != null)
            {
                var customFormations = CustomFormationManager.Instance.GetAllFormations();
                foreach (var formation in customFormations)
                {
                    if (formation.isInQuickList)
                    {
                        int index = options.Count;
                        options.Add(formation.name);
                        customFormationIndexMap[index] = formation.id;
                    }
                }
            }

            formationDropdown.AddOptions(options);

            // Set current formation
            if (formationGroupManager != null)
            {
                formationDropdown.value = (int)formationGroupManager.CurrentFormation;
                formationDropdown.RefreshShownValue();
            }

            // Add listener
            formationDropdown.onValueChanged.RemoveAllListeners();
            formationDropdown.onValueChanged.AddListener(OnFormationDropdownChanged);
        }

        private string FormatFormationName(FormationType type)
        {
            switch (type)
            {
                case FormationType.None: return "No Formation";
                case FormationType.Line: return "Line";
                case FormationType.Column: return "Column";
                case FormationType.Box: return "Box";
                case FormationType.Wedge: return "Wedge";
                case FormationType.Circle: return "Circle";
                case FormationType.Scatter: return "Scatter";
                default: return type.ToString();
            }
        }

        private void OnFormationDropdownChanged(int index)
        {
            // Check if "Customize Formation" was selected
            if (index == CUSTOMIZE_FORMATION_INDEX)
            {
                // Open formation builder to create new formation
                if (formationBuilder != null)
                {
                    formationBuilder.OpenBuilder();
                }

                // Reset dropdown to current formation
                if (formationGroupManager != null)
                {
                    formationDropdown.value = (int)formationGroupManager.CurrentFormation;
                    formationDropdown.RefreshShownValue();
                }
                return;
            }

            // Check if a custom formation was selected
            if (customFormationIndexMap.ContainsKey(index))
            {
                string formationId = customFormationIndexMap[index];
                if (formationGroupManager != null)
                {
                    formationGroupManager.SetCustomFormation(formationId);
                }
                return;
            }

            // Otherwise, it's a standard formation type
            if (index < CUSTOMIZE_FORMATION_INDEX && formationGroupManager != null)
            {
                FormationType newFormation = (FormationType)index;
                formationGroupManager.CurrentFormation = newFormation;
            }
        }

        private void OnFormationChanged(FormationChangedEvent evt)
        {
            // Update dropdown to match current formation (in case it was changed elsewhere)
            if (formationDropdown != null)
            {
                formationDropdown.value = (int)evt.FormationType;
                formationDropdown.RefreshShownValue();
            }
        }

        private void OnUnitSelected(UnitSelectedEvent evt)
        {
            // For multi-select, only show details for the first selected unit
            if (currentSelectedUnit == null)
            {
                currentSelectedUnit = evt.Unit;
                ShowUnitDetails(evt.Unit);
            }
        }

        private void OnUnitDeselected(UnitDeselectedEvent evt)
        {
            // Only hide if this was the unit we're showing
            if (currentSelectedUnit == evt.Unit)
            {
                HideUnitDetails();
            }
        }

        private void OnSelectionChanged(SelectionChangedEvent evt)
        {
            currentSelectionCount = evt.SelectionCount;

            // Hide details when selection is cleared
            if (evt.SelectionCount == 0)
            {
                HideUnitDetails();
            }
            else if (evt.SelectionCount == 1)
            {
                // Show single unit stats
                ShowSingleUnitMode();
            }
            else
            {
                // Show multi-unit selection grid
                ShowMultiUnitMode();
            }
        }

        private void OnUnitHealthChanged(UnitHealthChangedEvent evt)
        {
            // Update health display if this is the current unit
            if (currentSelectedUnit != null && evt.Unit == currentSelectedUnit)
            {
                UpdateHealthDisplay(evt.CurrentHealth, evt.MaxHealth);
            }
        }

        private void ShowUnitDetails(GameObject unit)
        {
            if (unit == null)
            {
                HideUnitDetails();
                return;
            }

            // Get the UnitAIController component which has the config
            var unitAI = unit.GetComponent<RTS.Units.AI.UnitAIController>();
            if (unitAI == null || unitAI.Config == null)
            {
                HideUnitDetails();
                return;
            }

            UnitConfigSO config = unitAI.Config;

            // Show panel
            if (unitDetailsPanel != null)
            {
                unitDetailsPanel.SetActive(true);
            }

            // Set unit portrait
            if (unitPortrait != null && config.unitIcon != null)
            {
                unitPortrait.sprite = config.unitIcon;
                unitPortrait.color = Color.white;
            }

            // Set unit name
            if (unitNameText != null)
            {
                unitNameText.text = config.unitName;
            }

            // Get current health
            currentUnitHealth = unit.GetComponent<UnitHealth>();
            float currentHealth = config.maxHealth;
            float maxHealth = config.maxHealth;

            if (currentUnitHealth != null)
            {
                currentHealth = currentUnitHealth.CurrentHealth;
                maxHealth = currentUnitHealth.MaxHealth;
            }

            // Set stats
            if (healthText != null)
            {
                healthText.text = $"Health: {currentHealth:F0}/{maxHealth:F0}";
            }

            if (speedText != null)
            {
                speedText.text = $"Speed: {config.speed:F1}";
            }

            if (attackDamageText != null)
            {
                attackDamageText.text = $"Attack Damage: {config.attackDamage:F0}";
            }

            if (attackSpeedText != null)
            {
                // attackRate is attacks per second, so attack speed in seconds is 1/attackRate
                float attackSpeed = config.attackRate > 0 ? 1f / config.attackRate : 0f;
                attackSpeedText.text = $"Attack Speed: {attackSpeed:F2}s";
            }

            if (attackRangeText != null)
            {
                attackRangeText.text = $"Attack Range: {config.attackRange:F1}";
            }

            // Update health bar
            UpdateHealthDisplay(currentHealth, maxHealth);
        }

        private void UpdateHealthDisplay(float currentHealth, float maxHealth)
        {
            // Update health text
            if (healthText != null)
            {
                healthText.text = $"Health: {currentHealth:F0}/{maxHealth:F0}";
            }

            // Update health bar fill using RectTransform scaling
            if (healthBarFill != null && maxHealth > 0)
            {
                float healthPercent = currentHealth / maxHealth;

                // Scale the fill image horizontally based on health percentage
                RectTransform fillRect = healthBarFill.GetComponent<RectTransform>();
                if (fillRect != null)
                {
                    fillRect.localScale = new Vector3(healthPercent, 1, 1);
                }

                // Change color based on health percentage
                if (healthPercent > 0.6f)
                {
                    healthBarFill.color = healthyColor;
                }
                else if (healthPercent > 0.3f)
                {
                    healthBarFill.color = damagedColor;
                }
                else
                {
                    healthBarFill.color = criticalColor;
                }
            }
        }

        private void HideUnitDetails()
        {
            currentSelectedUnit = null;
            currentUnitHealth = null;
            currentSelectionCount = 0;

            if (unitDetailsPanel != null)
            {
                unitDetailsPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Show single unit stats mode (hides multi-unit grid)
        /// </summary>
        private void ShowSingleUnitMode()
        {
            if (singleUnitStatsContainer != null)
            {
                singleUnitStatsContainer.SetActive(true);
            }

            if (multiUnitSelectionContainer != null)
            {
                multiUnitSelectionContainer.SetActive(false);
            }
        }

        /// <summary>
        /// Show multi-unit selection grid mode (hides single unit stats)
        /// </summary>
        private void ShowMultiUnitMode()
        {
            if (singleUnitStatsContainer != null)
            {
                singleUnitStatsContainer.SetActive(false);
            }

            if (multiUnitSelectionContainer != null)
            {
                multiUnitSelectionContainer.SetActive(true);
            }

            // Force refresh the multi-unit UI
            if (multiUnitSelectionUI != null)
            {
                multiUnitSelectionUI.ForceRefresh();
            }
        }

        /// <summary>
        /// Called when the expand formations button is clicked
        /// Toggles the expandable formations panel
        /// </summary>
        private void OnExpandFormationsButtonClicked()
        {
            if (formationsPanel != null)
            {
                bool isActive = formationsPanel.activeSelf;
                formationsPanel.SetActive(!isActive);

                if (!isActive)
                {
                    // Panel is being opened, refresh the list
                    RefreshFormationsPanel();
                }
            }
        }

        /// <summary>
        /// Called when custom formations change
        /// </summary>
        private void OnCustomFormationsChanged(List<CustomFormationData> formations)
        {
            // Refresh dropdown to include updated quick list
            InitializeFormationDropdown();

            // Refresh panel if it's open
            if (formationsPanel != null && formationsPanel.activeSelf)
            {
                RefreshFormationsPanel();
            }
        }

        /// <summary>
        /// Refresh the formations panel with all formations
        /// </summary>
        private void RefreshFormationsPanel()
        {
            if (formationsPanelContent == null)
                return;

            // Clear existing items
            foreach (Transform child in formationsPanelContent)
            {
                Destroy(child.gameObject);
            }

            // Get all formations
            if (CustomFormationManager.Instance == null)
                return;

            var formations = CustomFormationManager.Instance.GetAllFormations();

            if (formations.Count == 0)
            {
                // Show empty message
                GameObject emptyObj = new GameObject("EmptyMessage");
                emptyObj.transform.SetParent(formationsPanelContent, false);

                TextMeshProUGUI emptyText = emptyObj.AddComponent<TextMeshProUGUI>();
                emptyText.text = "No formations yet.\nClick 'Customize Formation' to create one!";
                emptyText.alignment = TextAlignmentOptions.Center;
                emptyText.fontSize = 14;
                emptyText.color = new Color(0.7f, 0.7f, 0.7f);

                return;
            }

            // Create list items for each formation
            foreach (var formation in formations)
            {
                CreateFormationPanelItem(formation);
            }
        }

        /// <summary>
        /// Create a list item for a formation in the expandable panel
        /// </summary>
        private void CreateFormationPanelItem(CustomFormationData formation)
        {
            GameObject itemObj;

            if (formationListItemPrefab != null)
            {
                itemObj = Instantiate(formationListItemPrefab, formationsPanelContent);
            }
            else
            {
                // Create basic item
                itemObj = CreateBasicFormationItem(formation);
            }

            // Setup the item's buttons
            SetupFormationItemButtons(itemObj, formation);
        }

        /// <summary>
        /// Create a basic formation item (fallback if no prefab)
        /// </summary>
        private GameObject CreateBasicFormationItem(CustomFormationData formation)
        {
            GameObject itemObj = new GameObject($"Formation_{formation.name}");
            itemObj.transform.SetParent(formationsPanelContent, false);

            // Add background
            Image bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Add horizontal layout
            HorizontalLayoutGroup layout = itemObj.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;

            // Add layout element
            LayoutElement layoutElement = itemObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 35;

            // Add name text
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = formation.name;
            nameText.fontSize = 14;
            LayoutElement nameLayout = nameObj.AddComponent<LayoutElement>();
            nameLayout.preferredWidth = 150;
            nameLayout.flexibleWidth = 1;

            // Add Select button
            CreateButton(itemObj.transform, "Select", () => OnFormationSelectClicked(formation));

            // Add Edit button
            CreateButton(itemObj.transform, "Edit", () => OnFormationEditClicked(formation));

            // Add Remove button
            CreateButton(itemObj.transform, "Remove", () => OnFormationRemoveClicked(formation));

            // Add Quick List toggle button
            string quickListText = formation.isInQuickList ? "Remove from Quick" : "Add to Quick";
            CreateButton(itemObj.transform, quickListText, () => OnFormationQuickListToggleClicked(formation));

            return itemObj;
        }

        /// <summary>
        /// Create a button for the formation item
        /// </summary>
        private Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject($"{label}Button");
            btnObj.transform.SetParent(parent, false);

            Button btn = btnObj.AddComponent<Button>();
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.3f, 0.5f, 0.7f);

            LayoutElement btnLayout = btnObj.AddComponent<LayoutElement>();
            btnLayout.preferredWidth = 80;
            btnLayout.preferredHeight = 25;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
            btnText.text = label;
            btnText.fontSize = 10;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            btn.onClick.AddListener(onClick);

            return btn;
        }

        /// <summary>
        /// Setup buttons for a formation item
        /// </summary>
        private void SetupFormationItemButtons(GameObject itemObj, CustomFormationData formation)
        {
            // Find buttons in the prefab and set up their callbacks
            Button[] buttons = itemObj.GetComponentsInChildren<Button>();

            foreach (var btn in buttons)
            {
                string btnName = btn.gameObject.name.ToLower();

                if (btnName.Contains("select"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnFormationSelectClicked(formation));
                }
                else if (btnName.Contains("edit"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnFormationEditClicked(formation));
                }
                else if (btnName.Contains("remove") || btnName.Contains("delete"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnFormationRemoveClicked(formation));
                }
                else if (btnName.Contains("quick"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnFormationQuickListToggleClicked(formation));

                    // Update button text
                    TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                    {
                        btnText.text = formation.isInQuickList ? "Remove from Quick" : "Add to Quick";
                    }
                }
            }
        }

        /// <summary>
        /// Called when a formation's Select button is clicked
        /// </summary>
        private void OnFormationSelectClicked(CustomFormationData formation)
        {
            if (formationGroupManager != null)
            {
                formationGroupManager.SetCustomFormation(formation.id);
            }
        }

        /// <summary>
        /// Called when a formation's Edit button is clicked
        /// </summary>
        private void OnFormationEditClicked(CustomFormationData formation)
        {
            if (formationBuilder != null)
            {
                formationBuilder.OpenBuilder(formation);
                formationsPanel.SetActive(false); // Close the panel
            }
        }

        /// <summary>
        /// Called when a formation's Remove button is clicked
        /// </summary>
        private void OnFormationRemoveClicked(CustomFormationData formation)
        {
            if (CustomFormationManager.Instance != null)
            {
                CustomFormationManager.Instance.DeleteFormation(formation.id);
            }
        }

        /// <summary>
        /// Called when a formation's Quick List toggle button is clicked
        /// </summary>
        private void OnFormationQuickListToggleClicked(CustomFormationData formation)
        {
            if (CustomFormationManager.Instance != null)
            {
                if (formation.isInQuickList)
                {
                    formation.RemoveFromQuickList();
                }
                else
                {
                    formation.AddToQuickList();
                }

                CustomFormationManager.Instance.UpdateFormation(formation);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from custom formation changes
            if (CustomFormationManager.Instance != null)
            {
                CustomFormationManager.Instance.OnFormationsChanged -= OnCustomFormationsChanged;
            }
        }
    }
}
