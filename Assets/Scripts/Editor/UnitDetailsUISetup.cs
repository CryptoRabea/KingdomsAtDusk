using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using RTS.UI;

namespace RTS.Editor
{
    /// <summary>
    /// Editor tool to automatically create and setup the Unit Details UI.
    /// Usage: Tools > RTS > Setup Unit Details UI
    /// </summary>
    public class UnitDetailsUISetup : EditorWindow
    {
        private Canvas targetCanvas;

        [MenuItem("Tools/RTS/Setup Unit Details UI")]
        public static void ShowWindow()
        {
            GetWindow<UnitDetailsUISetup>("Unit Details UI Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Unit Details UI Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will create:\n" +
                "- UnitDetailsUI panel with all stat displays\n" +
                "- Unit portrait, name, and all stats\n" +
                "- Health bar with color coding\n" +
                "- All references will be automatically connected",
                MessageType.Info);

            GUILayout.Space(10);

            targetCanvas = (Canvas)EditorGUILayout.ObjectField(
                "Target Canvas",
                targetCanvas,
                typeof(Canvas),
                true);

            if (targetCanvas == null)
            {
                EditorGUILayout.HelpBox("Please assign a Canvas to create the UI in.", MessageType.Warning);
            }

            GUILayout.Space(20);

            GUI.enabled = targetCanvas != null;
            if (GUILayout.Button("Create Unit Details UI", GUILayout.Height(40)))
            {
                CreateUnitDetailsUI();
            }

            GUI.enabled = true;
        }

        private void CreateUnitDetailsUI()
        {
            // Create wrapper GameObject for the component (stays active to receive events)
            GameObject componentWrapper = CreateUIElement("UnitDetailsUI", targetCanvas.transform);
            UnitDetailsUI detailsUI = componentWrapper.AddComponent<UnitDetailsUI>();

            // Create visual panel as child (this gets hidden/shown)
            GameObject panelRoot = CreateUIElement("UnitDetailsPanel", componentWrapper.transform);
            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();

            // Position panel on the left side of screen
            panelRect.anchorMin = new Vector2(0.02f, 0.2f);
            panelRect.anchorMax = new Vector2(0.25f, 0.6f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Add background image (blocks raycasts to prevent clicking through)
            Image panelBg = panelRoot.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            panelBg.raycastTarget = true;

            // Add vertical layout to panel
            AddVerticalLayout(panelRoot, 10, TextAnchor.UpperCenter);

            // Create header section
            GameObject headerSection = CreateUIElement("HeaderSection", panelRoot.transform);
            AddVerticalLayout(headerSection, 5, TextAnchor.UpperCenter);

            // Unit portrait
            GameObject portraitObj = CreateUIElement("UnitPortrait", headerSection.transform);
            RectTransform portraitRect = portraitObj.GetComponent<RectTransform>();
            portraitRect.sizeDelta = new Vector2(100, 100);
            Image unitPortrait = portraitObj.AddComponent<Image>();
            unitPortrait.color = Color.white;
            unitPortrait.preserveAspect = true;

            // Unit name
            GameObject nameObj = CreateTextElement("UnitName", headerSection.transform, "Unit Name", 20, TextAlignmentOptions.Center);
            TextMeshProUGUI unitNameText = nameObj.GetComponent<TextMeshProUGUI>();
            unitNameText.fontStyle = FontStyles.Bold;

            // Create stats section
            GameObject statsSection = CreateUIElement("StatsSection", panelRoot.transform);
            AddVerticalLayout(statsSection, 5, TextAnchor.UpperLeft);
            Image statsBg = statsSection.AddComponent<Image>();
            statsBg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            statsBg.raycastTarget = true;

            // Health bar container
            GameObject healthBarContainer = CreateUIElement("HealthBarContainer", statsSection.transform);
            RectTransform healthBarRect = healthBarContainer.GetComponent<RectTransform>();
            healthBarRect.sizeDelta = new Vector2(0, 25);
            SetupLayoutElement(healthBarContainer, 0, 25);

            // Health bar background
            GameObject healthBarBg = CreateUIElement("HealthBarBg", healthBarContainer.transform);
            RectTransform healthBarBgRect = healthBarBg.GetComponent<RectTransform>();
            healthBarBgRect.anchorMin = Vector2.zero;
            healthBarBgRect.anchorMax = Vector2.one;
            healthBarBgRect.offsetMin = Vector2.zero;
            healthBarBgRect.offsetMax = Vector2.zero;
            Image healthBarBgImage = healthBarBg.AddComponent<Image>();
            healthBarBgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            // Health bar fill
            GameObject healthBarFillObj = CreateUIElement("HealthBarFill", healthBarBg.transform);
            RectTransform healthBarFillRect = healthBarFillObj.GetComponent<RectTransform>();
            healthBarFillRect.anchorMin = Vector2.zero;
            healthBarFillRect.anchorMax = new Vector2(1, 1);
            healthBarFillRect.pivot = new Vector2(0, 0.5f);
            healthBarFillRect.offsetMin = Vector2.zero;
            healthBarFillRect.offsetMax = Vector2.zero;
            Image healthBarFill = healthBarFillObj.AddComponent<Image>();
            healthBarFill.color = Color.green;
            healthBarFill.type = Image.Type.Filled;
            healthBarFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarFill.fillAmount = 1f;

            // Health text
            GameObject healthTextObj = CreateTextElement("HealthText", statsSection.transform, "Health: 100/100", 14, TextAlignmentOptions.Left);
            TextMeshProUGUI healthText = healthTextObj.GetComponent<TextMeshProUGUI>();

            // Speed text
            GameObject speedTextObj = CreateTextElement("SpeedText", statsSection.transform, "Speed: 5.0", 14, TextAlignmentOptions.Left);
            TextMeshProUGUI speedText = speedTextObj.GetComponent<TextMeshProUGUI>();

            // Attack Damage text
            GameObject attackDamageTextObj = CreateTextElement("AttackDamageText", statsSection.transform, "Attack Damage: 10", 14, TextAlignmentOptions.Left);
            TextMeshProUGUI attackDamageText = attackDamageTextObj.GetComponent<TextMeshProUGUI>();

            // Attack Speed text
            GameObject attackSpeedTextObj = CreateTextElement("AttackSpeedText", statsSection.transform, "Attack Speed: 1.0s", 14, TextAlignmentOptions.Left);
            TextMeshProUGUI attackSpeedText = attackSpeedTextObj.GetComponent<TextMeshProUGUI>();

            // Attack Range text
            GameObject attackRangeTextObj = CreateTextElement("AttackRangeText", statsSection.transform, "Attack Range: 2.0", 14, TextAlignmentOptions.Left);
            TextMeshProUGUI attackRangeText = attackRangeTextObj.GetComponent<TextMeshProUGUI>();

            // Assign all references to UnitDetailsUI
            SerializedObject serializedUI = new SerializedObject(detailsUI);

            serializedUI.FindProperty("unitDetailsPanel").objectReferenceValue = panelRoot;
            serializedUI.FindProperty("unitPortrait").objectReferenceValue = unitPortrait;
            serializedUI.FindProperty("unitNameText").objectReferenceValue = unitNameText;
            serializedUI.FindProperty("healthText").objectReferenceValue = healthText;
            serializedUI.FindProperty("speedText").objectReferenceValue = speedText;
            serializedUI.FindProperty("attackDamageText").objectReferenceValue = attackDamageText;
            serializedUI.FindProperty("attackSpeedText").objectReferenceValue = attackSpeedText;
            serializedUI.FindProperty("attackRangeText").objectReferenceValue = attackRangeText;
            serializedUI.FindProperty("healthBarFill").objectReferenceValue = healthBarFill;

            serializedUI.ApplyModifiedProperties();

            // Keep component wrapper ACTIVE so it can receive events
            componentWrapper.SetActive(true);

            // Initially hide the VISUAL panel (not the component!)
            panelRoot.SetActive(false);

            EditorUtility.SetDirty(componentWrapper);
            Selection.activeGameObject = componentWrapper;

            Debug.Log("[OK] UnitDetailsUI created successfully!");
            Debug.Log("   - UnitDetailsUI component: ACTIVE (receives events)");
            Debug.Log("   - UnitDetailsPanel visual: INACTIVE (will show when unit selected)");
        }

        // Helper methods
        private GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            RectTransform rect = obj.AddComponent<RectTransform>();

            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
            }

            return obj;
        }

        private GameObject CreateTextElement(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject obj = CreateUIElement(name, parent);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return obj;
        }

        private void AddVerticalLayout(GameObject obj, int spacing, TextAnchor alignment)
        {
            VerticalLayoutGroup layout = obj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(10, 10, 10, 10);
        }

        private void SetupLayoutElement(GameObject obj, int preferredWidth, int preferredHeight)
        {
            LayoutElement element = obj.AddComponent<LayoutElement>();
            if (preferredWidth > 0)
                element.preferredWidth = preferredWidth;
            if (preferredHeight > 0)
                element.preferredHeight = preferredHeight;
        }
    }
}
