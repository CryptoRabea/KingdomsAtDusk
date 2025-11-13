using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using RTS.UI;
using RTS.Managers;

namespace RTS.Editor
{
    /// <summary>
    /// Advanced UI system generator for creating complete UI systems.
    /// Generates Resource UI, Happiness UI, Notification UI, and more.
    /// Access via: Tools > RTS > UI System Generator
    /// </summary>
    public class UISystemGenerator : EditorWindow
    {
        private enum UIType
        {
            ResourceUI,
            HappinessUI,
            NotificationUI,
            CompleteGameHUD
        }

        private UIType uiType = UIType.CompleteGameHUD;
        private Canvas targetCanvas;

        [Header("Resource UI Settings")]
        private bool includeIcons = true;
        private bool enableAnimations = true;
        private bool createForAllResources = true;

        [Header("Happiness UI Settings")]
        private bool includeSlider = true;
        private bool useColorCoding = true;

        [Header("Notification UI Settings")]
        private int maxNotifications = 5;
        private float notificationDuration = 3f;

        [Header("Complete HUD Settings")]
        private bool includeResourceUI = true;
        private bool includeHappinessUI = true;
        private bool includeNotificationUI = true;

        private Vector2 scrollPos;

        [MenuItem("Tools/RTS/UI System Generator")]
        public static void ShowWindow()
        {
            UISystemGenerator window = GetWindow<UISystemGenerator>("UI System Generator");
            window.minSize = new Vector2(450, 600);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("UI System Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Automatically generates complete UI systems with proper styling and functionality.\n" +
                "Choose from Resource UI, Happiness UI, Notification UI, or create a complete game HUD!",
                MessageType.Info);

            GUILayout.Space(10);

            // Canvas Selection
            targetCanvas = (Canvas)EditorGUILayout.ObjectField(
                "Target Canvas",
                targetCanvas,
                typeof(Canvas),
                true);

            if (targetCanvas == null)
            {
                EditorGUILayout.HelpBox("Please assign a Canvas to create UI elements in.", MessageType.Warning);
            }

            GUILayout.Space(10);

            // UI Type Selection
            uiType = (UIType)EditorGUILayout.EnumPopup("UI Type", uiType);

            GUILayout.Space(10);

            // Draw settings based on type
            switch (uiType)
            {
                case UIType.ResourceUI:
                    DrawResourceUISettings();
                    break;
                case UIType.HappinessUI:
                    DrawHappinessUISettings();
                    break;
                case UIType.NotificationUI:
                    DrawNotificationUISettings();
                    break;
                case UIType.CompleteGameHUD:
                    DrawCompleteHUDSettings();
                    break;
            }

            GUILayout.Space(20);

            // Generate Button
            GUI.enabled = targetCanvas != null;
            if (GUILayout.Button($"Generate {uiType}", GUILayout.Height(40)))
            {
                GenerateUI();
            }
            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
        }

        #region Settings UI

        private void DrawResourceUISettings()
        {
            GUILayout.Label("Resource UI Settings", EditorStyles.boldLabel);
            createForAllResources = EditorGUILayout.Toggle("Create For All Resources", createForAllResources);
            includeIcons = EditorGUILayout.Toggle("Include Icons", includeIcons);
            enableAnimations = EditorGUILayout.Toggle("Enable Animations", enableAnimations);

            EditorGUILayout.HelpBox(
                "Creates a Resource UI panel with displays for all resource types.\n" +
                "Automatically connects to ResourceManager via ServiceLocator.",
                MessageType.Info);
        }

        private void DrawHappinessUISettings()
        {
            GUILayout.Label("Happiness UI Settings", EditorStyles.boldLabel);
            includeSlider = EditorGUILayout.Toggle("Include Slider", includeSlider);
            useColorCoding = EditorGUILayout.Toggle("Use Color Coding", useColorCoding);

            EditorGUILayout.HelpBox(
                "Creates a Happiness UI panel with text and optional slider.\n" +
                "Color-coded to show happiness levels (green/yellow/red).",
                MessageType.Info);
        }

        private void DrawNotificationUISettings()
        {
            GUILayout.Label("Notification UI Settings", EditorStyles.boldLabel);
            maxNotifications = EditorGUILayout.IntSlider("Max Notifications", maxNotifications, 3, 10);
            notificationDuration = EditorGUILayout.Slider("Duration (seconds)", notificationDuration, 1f, 10f);

            EditorGUILayout.HelpBox(
                "Creates a notification system for displaying messages.\n" +
                "Messages auto-fade after the specified duration.",
                MessageType.Info);
        }

        private void DrawCompleteHUDSettings()
        {
            GUILayout.Label("Complete HUD Settings", EditorStyles.boldLabel);
            includeResourceUI = EditorGUILayout.Toggle("Include Resource UI", includeResourceUI);
            includeHappinessUI = EditorGUILayout.Toggle("Include Happiness UI", includeHappinessUI);
            includeNotificationUI = EditorGUILayout.Toggle("Include Notification UI", includeNotificationUI);

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Creates a complete game HUD with all selected systems.\n" +
                "Perfect for getting started quickly!",
                MessageType.Info);

            // Preview
            GUILayout.Label("Will Create:", EditorStyles.boldLabel);
            if (includeResourceUI) EditorGUILayout.LabelField("  ✓ Resource Display Panel (Top-Left)");
            if (includeHappinessUI) EditorGUILayout.LabelField("  ✓ Happiness Display (Top-Right)");
            if (includeNotificationUI) EditorGUILayout.LabelField("  ✓ Notification System (Bottom-Right)");
        }

        #endregion

        #region Generation Methods

        private void GenerateUI()
        {
            switch (uiType)
            {
                case UIType.ResourceUI:
                    CreateResourceUI();
                    break;
                case UIType.HappinessUI:
                    CreateHappinessUI();
                    break;
                case UIType.NotificationUI:
                    CreateNotificationUI();
                    break;
                case UIType.CompleteGameHUD:
                    CreateCompleteHUD();
                    break;
            }

            EditorUtility.DisplayDialog("Success!",
                $"{uiType} generated successfully!",
                "OK");
        }

        private void CreateResourceUI()
        {
            // Create main panel
            GameObject panelObj = CreateUIElement("ResourceUI_Panel", targetCanvas.transform);
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();

            // Position at top-left
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(250, 150);

            // Add background
            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Add ResourceUI component
            ResourceUI resourceUI = panelObj.AddComponent<ResourceUI>();

            // Create container for displays
            GameObject containerObj = CreateUIElement("DisplayContainer", panelObj.transform);
            RectTransform containerRect = containerObj.GetComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = new Vector2(10, 10);
            containerRect.offsetMax = new Vector2(-10, -10);

            // Add vertical layout
            VerticalLayoutGroup layout = containerObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;

            // Create resource displays
            if (createForAllResources)
            {
                // Get all resource types
                System.Type resourceType = System.Type.GetType("ResourceType");
                if (resourceType != null && resourceType.IsEnum)
                {
                    var resourceTypes = System.Enum.GetValues(resourceType);
                    foreach (var resType in resourceTypes)
                    {
                        CreateResourceDisplay(containerObj.transform, resType.ToString());
                    }
                }
                else
                {
                    // Fallback: create for common resources
                    CreateResourceDisplay(containerObj.transform, "Wood");
                    CreateResourceDisplay(containerObj.transform, "Food");
                    CreateResourceDisplay(containerObj.transform, "Gold");
                    CreateResourceDisplay(containerObj.transform, "Stone");
                }
            }

            Selection.activeGameObject = panelObj;
            EditorGUIUtility.PingObject(panelObj);

            Debug.Log("✅ Resource UI created successfully!");
        }

        private GameObject CreateResourceDisplay(Transform parent, string resourceName)
        {
            GameObject displayObj = CreateUIElement($"{resourceName}Display", parent);
            RectTransform displayRect = displayObj.GetComponent<RectTransform>();
            displayRect.sizeDelta = new Vector2(0, 25);

            // Add horizontal layout
            HorizontalLayoutGroup layout = displayObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;

            // Icon (if enabled)
            if (includeIcons)
            {
                GameObject iconObj = CreateUIElement("Icon", displayObj.transform);
                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.sizeDelta = new Vector2(20, 20);
                Image icon = iconObj.AddComponent<Image>();
                icon.color = GetResourceColor(resourceName);

                LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
                iconLayout.minWidth = 20;
                iconLayout.minHeight = 20;
            }

            // Text
            GameObject textObj = CreateTextElement("Text", displayObj.transform, $"{resourceName}: 0", 14, TextAlignmentOptions.Left);
            LayoutElement textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1;

            return displayObj;
        }

        private void CreateHappinessUI()
        {
            // Create main panel
            GameObject panelObj = CreateUIElement("HappinessUI_Panel", targetCanvas.transform);
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();

            // Position at top-right
            panelRect.anchorMin = new Vector2(1, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 1);
            panelRect.anchoredPosition = new Vector2(-10, -10);
            panelRect.sizeDelta = new Vector2(200, includeSlider ? 80 : 40);

            // Add background
            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Add HappinessUI component
            HappinessUI happinessUI = panelObj.AddComponent<HappinessUI>();

            // Create container
            GameObject containerObj = CreateUIElement("Container", panelObj.transform);
            RectTransform containerRect = containerObj.GetComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = new Vector2(10, 10);
            containerRect.offsetMax = new Vector2(-10, -10);

            // Add vertical layout
            VerticalLayoutGroup layout = containerObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.MiddleCenter;

            // Happiness text
            GameObject textObj = CreateTextElement("HappinessText", containerObj.transform, "Happiness: 100%", 16, TextAlignmentOptions.Center);
            TextMeshProUGUI happinessText = textObj.GetComponent<TextMeshProUGUI>();
            happinessText.fontStyle = FontStyles.Bold;

            if (useColorCoding)
            {
                happinessText.color = Color.green;
            }

            // Slider (if enabled)
            Slider slider = null;
            if (includeSlider)
            {
                GameObject sliderObj = CreateSlider("HappinessSlider", containerObj.transform);
                slider = sliderObj.GetComponent<Slider>();
                slider.minValue = 0;
                slider.maxValue = 1;
                slider.value = 1;
                slider.interactable = false;

                // Color the fill
                Image fillImage = slider.fillRect.GetComponent<Image>();
                if (fillImage != null && useColorCoding)
                {
                    fillImage.color = Color.green;
                }
            }

            // Assign to component via SerializedObject
            SerializedObject so = new SerializedObject(happinessUI);
            so.FindProperty("happinessText").objectReferenceValue = happinessText;
            if (slider != null)
            {
                so.FindProperty("happinessSlider").objectReferenceValue = slider;
            }
            so.ApplyModifiedProperties();

            Selection.activeGameObject = panelObj;
            EditorGUIUtility.PingObject(panelObj);

            Debug.Log("✅ Happiness UI created successfully!");
        }

        private void CreateNotificationUI()
        {
            // Create main panel
            GameObject panelObj = CreateUIElement("NotificationUI_Panel", targetCanvas.transform);
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();

            // Position at bottom-right
            panelRect.anchorMin = new Vector2(1, 0);
            panelRect.anchorMax = new Vector2(1, 0);
            panelRect.pivot = new Vector2(1, 0);
            panelRect.anchoredPosition = new Vector2(-10, 10);
            panelRect.sizeDelta = new Vector2(300, 200);

            // Add vertical layout
            VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.LowerRight;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;

            // Add NotificationUI component (if it exists)
            System.Type notificationUIType = System.Type.GetType("RTS.UI.NotificationUI, Assembly-CSharp");
            if (notificationUIType != null)
            {
                panelObj.AddComponent(notificationUIType);
                Debug.Log("✅ Added NotificationUI component");
            }

            Selection.activeGameObject = panelObj;
            EditorGUIUtility.PingObject(panelObj);

            Debug.Log("✅ Notification UI panel created successfully!");
        }

        private void CreateCompleteHUD()
        {
            if (includeResourceUI)
            {
                CreateResourceUI();
            }

            if (includeHappinessUI)
            {
                CreateHappinessUI();
            }

            if (includeNotificationUI)
            {
                CreateNotificationUI();
            }

            Debug.Log("✅✅✅ Complete Game HUD created successfully!");
        }

        #endregion

        #region Helper Methods

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

        private GameObject CreateSlider(string name, Transform parent)
        {
            GameObject sliderObj = CreateUIElement(name, parent);
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(0, 20);

            // Add slider component
            Slider slider = sliderObj.AddComponent<Slider>();

            // Background
            GameObject bgObj = CreateUIElement("Background", sliderObj.transform);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Fill Area
            GameObject fillAreaObj = CreateUIElement("Fill Area", sliderObj.transform);
            RectTransform fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(5, 5);
            fillAreaRect.offsetMax = new Vector2(-5, -5);

            // Fill
            GameObject fillObj = CreateUIElement("Fill", fillAreaObj.transform);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = Color.green;

            // Assign to slider
            slider.fillRect = fillRect;

            return sliderObj;
        }

        private Color GetResourceColor(string resourceName)
        {
            return resourceName.ToLower() switch
            {
                "wood" => new Color(0.6f, 0.4f, 0.2f),
                "food" => new Color(0.8f, 0.6f, 0.2f),
                "gold" => new Color(1f, 0.84f, 0f),
                "stone" => new Color(0.5f, 0.5f, 0.5f),
                _ => Color.white
            };
        }

        #endregion
    }
}
