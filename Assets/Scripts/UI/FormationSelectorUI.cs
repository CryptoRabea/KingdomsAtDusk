using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Units.Formation;

namespace RTS.UI
{
    /// <summary>
    /// UI for browsing and selecting custom formations
    /// </summary>
    public class FormationSelectorUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject selectorPanel;
        [SerializeField] private Transform formationListContainer;
        [SerializeField] private GameObject formationListItemPrefab;
        [SerializeField] private Button createNewButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Formation Builder")]
        [SerializeField] private FormationBuilderUI formationBuilder;

        [Header("Formation Group Manager")]
        [SerializeField] private FormationGroupManager formationGroupManager;

        private List<GameObject> listItems = new List<GameObject>();

        private void Awake()
        {
            SetupEventListeners();
            selectorPanel.SetActive(false);
        }

        private void OnEnable()
        {
            // Subscribe to formation changes
            if (CustomFormationManager.Instance != null)
            {
                CustomFormationManager.Instance.OnFormationsChanged += OnFormationsChanged;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from formation changes
            if (CustomFormationManager.Instance != null)
            {
                CustomFormationManager.Instance.OnFormationsChanged -= OnFormationsChanged;
            }
        }

        private void SetupEventListeners()
        {
            if (createNewButton != null)
                createNewButton.onClick.AddListener(OnCreateNewClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
        }

        /// <summary>
        /// Open the formation selector
        /// </summary>
        public void OpenSelector()
        {
            selectorPanel.SetActive(true);
            RefreshFormationList();
        }

        /// <summary>
        /// Close the formation selector
        /// </summary>
        public void CloseSelector()
        {
            selectorPanel.SetActive(false);
        }

        /// <summary>
        /// Refresh the list of formations
        /// </summary>
        private void RefreshFormationList()
        {
            // Clear existing list items
            foreach (var item in listItems)
            {
                if (item != null)
                    Destroy(item);
            }
            listItems.Clear();

            // Get all formations
            List<CustomFormationData> formations = CustomFormationManager.Instance.GetAllFormations();

            if (formations.Count == 0)
            {
                // Show empty message
                CreateEmptyListItem();
                return;
            }

            // Create list items for each formation
            foreach (var formation in formations)
            {
                CreateFormationListItem(formation);
            }
        }

        /// <summary>
        /// Create an empty list item message
        /// </summary>
        private void CreateEmptyListItem()
        {
            GameObject itemObj = new GameObject("EmptyMessage");
            itemObj.transform.SetParent(formationListContainer, false);

            TextMeshProUGUI text = itemObj.AddComponent<TextMeshProUGUI>();
            text.text = "No custom formations yet.\nClick 'Create New' to get started!";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 16;
            text.color = new Color(0.7f, 0.7f, 0.7f);

            RectTransform rect = itemObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 60);

            listItems.Add(itemObj);
        }

        /// <summary>
        /// Create a list item for a formation
        /// </summary>
        private void CreateFormationListItem(CustomFormationData formation)
        {
            GameObject itemObj;

            if (formationListItemPrefab != null)
            {
                itemObj = Instantiate(formationListItemPrefab, formationListContainer);
            }
            else
            {
                // Create a basic list item if no prefab is provided
                itemObj = CreateBasicListItem(formation);
            }

            // Setup item
            if (!itemObj.TryGetComponent<FormationListItem>(out var listItem))
            {
                listItem = itemObj.AddComponent<FormationListItem>();
            }

            listItem.Setup(formation, this);
            listItems.Add(itemObj);
        }

        /// <summary>
        /// Create a basic list item (fallback if no prefab)
        /// </summary>
        private GameObject CreateBasicListItem(CustomFormationData formation)
        {
            GameObject itemObj = new GameObject($"Formation_{formation.name}");
            itemObj.transform.SetParent(formationListContainer, false);

            // Add background image
            Image bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Setup layout
            HorizontalLayoutGroup layout = itemObj.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Add layout element
            LayoutElement layoutElement = itemObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 40;
            layoutElement.minHeight = 40;

            // Add name text
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = formation.name;
            nameText.fontSize = 16;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(200, 30);

            // Add piece count text
            GameObject countObj = new GameObject("CountText");
            countObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI countText = countObj.AddComponent<TextMeshProUGUI>();
            countText.text = $"({formation.positions.Count} units)";
            countText.fontSize = 14;
            countText.color = new Color(0.7f, 0.7f, 0.7f);
            countText.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform countRect = countObj.GetComponent<RectTransform>();
            countRect.sizeDelta = new Vector2(100, 30);

            return itemObj;
        }

        /// <summary>
        /// Called when a formation is selected from the list
        /// </summary>
        public void OnFormationSelected(CustomFormationData formation)
        {
            if (formationGroupManager != null)
            {
                formationGroupManager.SetCustomFormation(formation.id);
                Debug.Log($"Selected custom formation: {formation.name}");
            }
            else
            {
                Debug.LogWarning("FormationGroupManager not assigned!");
            }
        }

        /// <summary>
        /// Called when edit is clicked for a formation
        /// </summary>
        public void OnFormationEdit(CustomFormationData formation)
        {
            if (formationBuilder != null)
            {
                CloseSelector();
                formationBuilder.OpenBuilder(formation);
            }
            else
            {
                Debug.LogWarning("FormationBuilder not assigned!");
            }
        }

        /// <summary>
        /// Called when delete is clicked for a formation
        /// </summary>
        public void OnFormationDelete(CustomFormationData formation)
        {
            // In a production game, you'd show a confirmation dialog here
            CustomFormationManager.Instance.DeleteFormation(formation.id);
            RefreshFormationList();
        }

        /// <summary>
        /// Called when duplicate is clicked for a formation
        /// </summary>
        public void OnFormationDuplicate(CustomFormationData formation)
        {
            CustomFormationManager.Instance.DuplicateFormation(formation.id);
            RefreshFormationList();
        }

        /// <summary>
        /// Called when create new button is clicked
        /// </summary>
        private void OnCreateNewClicked()
        {
            if (formationBuilder != null)
            {
                CloseSelector();
                formationBuilder.OpenBuilder();
            }
            else
            {
                Debug.LogWarning("FormationBuilder not assigned!");
            }
        }

        /// <summary>
        /// Called when close button is clicked
        /// </summary>
        private void OnCloseClicked()
        {
            CloseSelector();
        }

        /// <summary>
        /// Called when formations change
        /// </summary>
        private void OnFormationsChanged(List<CustomFormationData> formations)
        {
            if (selectorPanel.activeSelf)
            {
                RefreshFormationList();
            }
        }
    }

    /// <summary>
    /// Individual list item for a formation
    /// </summary>
    public class FormationListItem : MonoBehaviour
    {
        private CustomFormationData formation;
        private FormationSelectorUI selectorUI;

        [Header("UI Elements (Optional - will auto-find)")]
        [SerializeField] private Button selectButton;
        [SerializeField] private Button editButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button duplicateButton;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI countText;

        public void Setup(CustomFormationData formationData, FormationSelectorUI selector)
        {
            formation = formationData;
            selectorUI = selector;

            // Auto-find buttons if not assigned
            if (selectButton == null)
                selectButton = GetComponentInChildren<Button>();

            // Update UI elements
            if (nameText != null)
                nameText.text = formation.name;

            if (countText != null)
                countText.text = $"({formation.positions.Count} units)";

            // Setup button listeners
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => selectorUI.OnFormationSelected(formation));
            }

            if (editButton != null)
            {
                editButton.onClick.RemoveAllListeners();
                editButton.onClick.AddListener(() => selectorUI.OnFormationEdit(formation));
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(() => selectorUI.OnFormationDelete(formation));
            }

            if (duplicateButton != null)
            {
                duplicateButton.onClick.RemoveAllListeners();
                duplicateButton.onClick.AddListener(() => selectorUI.OnFormationDuplicate(formation));
            }
        }
    }
}
