using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using RTSGame.UI.Settings;

namespace RTSGame.Editor
{
    /// <summary>
    /// Editor tool to automatically generate the complete Settings Panel UI hierarchy
    /// and assign all references. Saves hours of manual UI setup work!
    ///
    /// Usage: Window > RTS > Generate Settings Panel UI
    /// </summary>
    public class SettingsPanelGenerator : EditorWindow
    {
        private Canvas targetCanvas;
        private Font defaultFont;

        // Colors
        private Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        private Color tabColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        private Color buttonNormalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        private Color buttonHighlightColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        private Color buttonPressedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        private Color accentColor = new Color(0.2f, 0.6f, 1f, 1f);

        [MenuItem("Window/RTS/Generate Settings Panel UI")]
        public static void ShowWindow()
        {
            var window = GetWindow<SettingsPanelGenerator>("Settings Panel Generator");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("RTS Settings Panel UI Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will automatically generate the complete Settings Panel UI hierarchy " +
                "with all tabs, controls, and references assigned. It will create:\n\n" +
                "• Main Settings Panel\n" +
                "• 9 Tab Panels (General, Graphics, Audio, etc.)\n" +
                "• All UI Controls (Sliders, Toggles, Dropdowns)\n" +
                "• Proper Layout Groups\n" +
                "• Automatic Reference Assignment\n\n" +
                "This saves hours of manual work!",
                MessageType.Info
            );

            GUILayout.Space(10);

            targetCanvas = (Canvas)EditorGUILayout.ObjectField(
                "Target Canvas",
                targetCanvas,
                typeof(Canvas),
                true
            );

            GUILayout.Space(10);

            GUI.enabled = targetCanvas != null;

            if (GUILayout.Button("Generate Settings Panel UI", GUILayout.Height(40)))
            {
                GenerateSettingsPanel();
            }

            GUI.enabled = true;

            GUILayout.Space(10);

            if (GUILayout.Button("Delete Existing Settings Panel", GUILayout.Height(30)))
            {
                DeleteExistingSettingsPanel();
            }

            GUILayout.Space(10);

            // Color customization
            GUILayout.Label("Color Customization", EditorStyles.boldLabel);
            panelColor = EditorGUILayout.ColorField("Panel Background", panelColor);
            tabColor = EditorGUILayout.ColorField("Tab Background", tabColor);
            buttonNormalColor = EditorGUILayout.ColorField("Button Normal", buttonNormalColor);
            accentColor = EditorGUILayout.ColorField("Accent Color", accentColor);
        }

        private void DeleteExistingSettingsPanel()
        {
            if (targetCanvas == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a target Canvas first!", "OK");
                return;
            }

            var existing = targetCanvas.transform.Find("SettingsPanel");
            if (existing != null)
            {
                if (EditorUtility.DisplayDialog(
                    "Confirm Delete",
                    "Are you sure you want to delete the existing Settings Panel?",
                    "Yes, Delete",
                    "Cancel"))
                {
                    DestroyImmediate(existing.gameObject);
                    Debug.Log("[SettingsPanelGenerator] Existing Settings Panel deleted.");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Info", "No existing Settings Panel found.", "OK");
            }
        }

        private void GenerateSettingsPanel()
        {
            if (targetCanvas == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a target Canvas first!", "OK");
                return;
            }

            // Check if already exists
            var existing = targetCanvas.transform.Find("SettingsPanel");
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog(
                    "Settings Panel Exists",
                    "A Settings Panel already exists. Do you want to replace it?",
                    "Yes, Replace",
                    "Cancel"))
                {
                    return;
                }
                DestroyImmediate(existing.gameObject);
            }

            EditorUtility.DisplayProgressBar("Generating Settings Panel", "Creating main panel...", 0f);

            try
            {
                // Create main settings panel
                GameObject settingsPanelObj = CreateSettingsPanel();

                EditorUtility.DisplayProgressBar("Generating Settings Panel", "Creating tabs...", 0.2f);

                // Create content area with tabs
                GameObject contentArea = CreateContentArea(settingsPanelObj);

                EditorUtility.DisplayProgressBar("Generating Settings Panel", "Creating tab panels...", 0.4f);

                // Create all tab panels
                CreateAllTabPanels(contentArea);

                EditorUtility.DisplayProgressBar("Generating Settings Panel", "Creating action buttons...", 0.8f);

                // Create action buttons
                CreateActionButtons(settingsPanelObj);

                EditorUtility.DisplayProgressBar("Generating Settings Panel", "Assigning references...", 0.9f);

                // Add and configure SettingsPanel component
                var settingsPanelComponent = settingsPanelObj.AddComponent<SettingsPanel>();
                AssignReferences(settingsPanelComponent, settingsPanelObj);

                EditorUtility.DisplayProgressBar("Generating Settings Panel", "Complete!", 1f);

                // Select the created object
                Selection.activeGameObject = settingsPanelObj;

                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog(
                    "Success!",
                    "Settings Panel UI generated successfully!\n\n" +
                    "The panel is now selected in the hierarchy. All references have been automatically assigned.\n\n" +
                    "Note: You may want to adjust sizes and positions to fit your specific design.",
                    "OK"
                );

                Debug.Log("[SettingsPanelGenerator] Settings Panel UI generated successfully!");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", "Failed to generate Settings Panel:\n" + e.Message, "OK");
                Debug.LogError($"[SettingsPanelGenerator] Error: {e.Message}\n{e.StackTrace}");
            }
        }

        private GameObject CreateSettingsPanel()
        {
            GameObject panel = new GameObject("SettingsPanel");
            panel.transform.SetParent(targetCanvas.transform, false);

            // Add RectTransform
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // Add Image (background)
            Image image = panel.AddComponent<Image>();
            image.color = panelColor;

            // Add Canvas Group for fade control
            CanvasGroup canvasGroup = panel.AddComponent<CanvasGroup>();

            // Start inactive
            panel.SetActive(false);

            return panel;
        }

        private GameObject CreateContentArea(GameObject parent)
        {
            GameObject contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(parent.transform, false);

            RectTransform rect = contentArea.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.1f);
            rect.anchorMax = new Vector2(1, 0.9f);
            rect.sizeDelta = Vector2.zero;

            // Add horizontal layout
            HorizontalLayoutGroup layout = contentArea.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);

            // Create tab buttons area (left side)
            CreateTabButtonsArea(contentArea);

            // Create tabs container (right side)
            GameObject tabsContainer = new GameObject("TabsContainer");
            tabsContainer.transform.SetParent(contentArea.transform, false);

            RectTransform tabsRect = tabsContainer.AddComponent<RectTransform>();
            LayoutElement tabsLayout = tabsContainer.AddComponent<LayoutElement>();
            tabsLayout.flexibleWidth = 4f; // Takes more space than buttons

            return tabsContainer;
        }

        private void CreateTabButtonsArea(GameObject parent)
        {
            GameObject tabButtonsArea = new GameObject("TabButtonsArea");
            tabButtonsArea.transform.SetParent(parent.transform, false);

            RectTransform rect = tabButtonsArea.AddComponent<RectTransform>();

            LayoutElement layout = tabButtonsArea.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            layout.preferredWidth = 200f;

            // Add vertical layout for buttons
            VerticalLayoutGroup vertLayout = tabButtonsArea.AddComponent<VerticalLayoutGroup>();
            vertLayout.childControlWidth = true;
            vertLayout.childControlHeight = false;
            vertLayout.childForceExpandWidth = true;
            vertLayout.spacing = 5;
            vertLayout.padding = new RectOffset(5, 5, 5, 5);

            // Add background
            Image bg = tabButtonsArea.AddComponent<Image>();
            bg.color = tabColor;

            // Create tab buttons
            string[] tabNames = new string[]
            {
                "General", "Graphics", "Audio", "Gameplay",
                "Controls", "UI", "Accessibility", "Network", "System"
            };

            foreach (string tabName in tabNames)
            {
                CreateTabButton(tabButtonsArea, tabName);
            }
        }

        private GameObject CreateTabButton(GameObject parent, string tabName)
        {
            GameObject button = new GameObject(tabName + "TabButton");
            button.transform.SetParent(parent.transform, false);

            RectTransform rect = button.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 40);

            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredHeight = 40;

            Image image = button.AddComponent<Image>();
            image.color = buttonNormalColor;

            Button btn = button.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = buttonNormalColor;
            colors.highlightedColor = buttonHighlightColor;
            colors.pressedColor = buttonPressedColor;
            colors.selectedColor = accentColor;
            btn.colors = colors;

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(button.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = tabName;
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            return button;
        }

        private void CreateAllTabPanels(GameObject parent)
        {
            CreateGeneralTab(parent);
            CreateGraphicsTab(parent);
            CreateAudioTab(parent);
            CreateGameplayTab(parent);
            CreateControlsTab(parent);
            CreateUITab(parent);
            CreateAccessibilityTab(parent);
            CreateNetworkTab(parent);
            CreateSystemTab(parent);
        }

        private GameObject CreateTabPanel(GameObject parent, string name)
        {
            GameObject tab = new GameObject(name + "Tab");
            tab.transform.SetParent(parent.transform, false);

            RectTransform rect = tab.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // Add scroll view
            GameObject scrollView = CreateScrollView(tab);

            tab.SetActive(false); // Start inactive

            return scrollView;
        }

        private GameObject CreateScrollView(GameObject parent)
        {
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(parent.transform, false);

            RectTransform rect = scrollView.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image bg = scrollView.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.05f, 0.5f);

            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // Create viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);

            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;

            Image vpImage = viewport.AddComponent<Image>();
            vpImage.color = Color.clear;

            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Create content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 1000);

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 10;
            layout.padding = new RectOffset(20, 20, 20, 20);

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRect;
            scroll.content = contentRect;

            return content;
        }

        // Tab creation methods with common controls
        private void CreateGeneralTab(GameObject parent)
        {
            GameObject content = CreateTabPanel(parent, "General");

            CreateLabel(content, "Language Settings");
            CreateDropdown(content, "Language");

            CreateLabel(content, "Interface");
            CreateSlider(content, "UI Scale", 0.8f, 1.4f, 1.0f);
            CreateDropdown(content, "Theme");

            CreateLabel(content, "Features");
            CreateToggle(content, "Show Tooltips");
            CreateToggle(content, "Show Tutorials");

            CreateLabel(content, "Auto-Save");
            CreateDropdown(content, "Auto-Save Interval");

            CreateLabel(content, "Developer");
            CreateToggle(content, "Enable Developer Console");
        }

        private void CreateGraphicsTab(GameObject parent)
        {
            GameObject content = CreateTabPanel(parent, "Graphics");

            CreateLabel(content, "Display Settings");
            CreateDropdown(content, "Fullscreen Mode");
            CreateDropdown(content, "Resolution");
            CreateDropdown(content, "Refresh Rate");
            CreateDropdown(content, "VSync");
            CreateDropdown(content, "Quality Preset");

            CreateLabel(content, "Rendering");
            CreateDropdown(content, "Anti-Aliasing");
            CreateSlider(content, "Render Scale", 0.5f, 1.4f, 1.0f);
            CreateSlider(content, "Shadow Distance", 0f, 150f, 100f);
            CreateDropdown(content, "Shadow Quality");
            CreateDropdown(content, "Texture Quality");
            CreateDropdown(content, "Anisotropic Filtering");
            CreateDropdown(content, "Terrain Quality");
            CreateSlider(content, "Vegetation Density", 0f, 1f, 1f);
            CreateSlider(content, "Grass Draw Distance", 0f, 200f, 100f);

            CreateLabel(content, "Post-Processing");
            CreateToggle(content, "Bloom");
            CreateToggle(content, "Motion Blur");
            CreateSlider(content, "Motion Blur Intensity", 0f, 1f, 0.5f);
            CreateToggle(content, "Ambient Occlusion");
            CreateDropdown(content, "Color Grading");
            CreateToggle(content, "Depth of Field");

            CreateLabel(content, "RTS Graphics");
            CreateToggle(content, "Unit Outlines");
            CreateToggle(content, "Selection Circles");
            CreateToggle(content, "Health Bars");
            CreateDropdown(content, "Fog of War Quality");
        }

        private void CreateAudioTab(GameObject parent)
        {
            GameObject content = CreateTabPanel(parent, "Audio");

            CreateLabel(content, "Volume Control");
            CreateSlider(content, "Master Volume", 0f, 1f, 1f);
            CreateSlider(content, "Music Volume", 0f, 1f, 0.7f);
            CreateSlider(content, "SFX Volume", 0f, 1f, 0.8f);
            CreateSlider(content, "UI Volume", 0f, 1f, 0.6f);
            CreateSlider(content, "Voice Volume", 0f, 1f, 0.9f);

            CreateLabel(content, "Sound Options");
            CreateSlider(content, "Spatial Blend", 0f, 1f, 1f);
            CreateDropdown(content, "Dynamic Range");

            CreateLabel(content, "RTS Audio");
            CreateToggle(content, "Alert Notifications");
            CreateDropdown(content, "Unit Voices");
            CreateDropdown(content, "Battle SFX Intensity");
        }

        private void CreateGameplayTab(GameObject parent)
        {
            GameObject content = CreateTabPanel(parent, "Gameplay");

            CreateLabel(content, "Camera Settings");
            CreateSlider(content, "Camera Pan Speed", 10f, 100f, 50f);
            CreateSlider(content, "Camera Rotation Speed", 10f, 200f, 100f);
            CreateToggle(content, "Edge Scrolling");
            CreateSlider(content, "Edge Scroll Sensitivity", 0f, 1f, 0.5f);
            CreateSlider(content, "Zoom Speed", 1f, 50f, 10f);
            CreateSlider(content, "Min Zoom", 5f, 50f, 10f);
            CreateSlider(content, "Max Zoom", 50f, 200f, 100f);
            CreateToggle(content, "Invert Panning");
            CreateToggle(content, "Invert Zoom");

            CreateLabel(content, "Difficulty");
            CreateDropdown(content, "AI Difficulty");
            CreateDropdown(content, "Game Speed");

            CreateLabel(content, "RTS Mechanics");
            CreateToggle(content, "Automatic Unit Grouping");
            CreateToggle(content, "Smart Pathfinding");
            CreateToggle(content, "Unit Collision");
            CreateToggle(content, "Minimap Rotation");
            CreateDropdown(content, "Minimap Icon Size");
            CreateToggle(content, "Auto-Rebuild Workers");
            CreateToggle(content, "Auto-Repair Buildings");
        }

        private void CreateControlsTab(GameObject parent)
        {
            GameObject content = CreateTabPanel(parent, "Controls");

            CreateLabel(content, "Mouse Settings");
            CreateSlider(content, "Mouse Sensitivity", 0.1f, 2f, 1f);
            CreateToggle(content, "Mouse Smoothing");
            CreateDropdown(content, "Camera Drag Button");
            CreateDropdown(content, "Command Button");

            CreateLabel(content, "Control Style");
            CreateDropdown(content, "Unit Control Style");

            CreateLabel(content, "Keybinds");
            CreateButton(content, "Rebind Keys");
            CreateLabel(content, "Keybind info text will appear here");
        }

        private void CreateUITab(GameObject parent)
        {
            GameObject content = CreateTabPanel(parent, "UI");

            CreateLabel(content, "UI Settings");
            CreateSlider(content, "UI Scale", 0.8f, 1.4f, 1f);
            CreateDropdown(content, "Nameplates");
            CreateToggle(content, "Damage Numbers");
            CreateDropdown(content, "Cursor Style");
            CreateToggle(content, "Flash Alerts");
            CreateDropdown(content, "Colorblind Mode");
        }

        private void CreateAccessibilityTab(GameObject parent)
        {
            GameObject content = CreateTabPanel(parent, "Accessibility");

            CreateLabel(content, "Visual");
            CreateToggle(content, "High-Contrast Mode");
            CreateDropdown(content, "Colorblind Filter");
            CreateToggle(content, "Subtitles");
            CreateSlider(content, "Subtitle Size", 0.5f, 2f, 1f);
            CreateSlider(content, "Subtitle Opacity", 0f, 1f, 0.7f);

            CreateLabel(content, "Gameplay Aid");
            CreateToggle(content, "Reduced Camera Shake");
            CreateToggle(content, "Simplified Effects");
        }

        private void CreateNetworkTab(GameObject parent)
        {
            GameObject content = CreateTabPanel(parent, "Network");

            CreateLabel(content, "Network Settings (Placeholder)");
            CreateDropdown(content, "Region");
            CreateSlider(content, "Max Ping", 50f, 300f, 150f);
            CreateToggle(content, "Auto-Reconnect");
            CreateDropdown(content, "Packet Rate");

            CreateLabel(content, "Voice Chat");
            CreateToggle(content, "Voice Chat");
            CreateSlider(content, "Voice Chat Volume", 0f, 1f, 1f);
            CreateToggle(content, "Push to Talk");
        }

        private void CreateSystemTab(GameObject parent)
        {
            GameObject content = CreateTabPanel(parent, "System");

            CreateLabel(content, "System Settings");
            CreateToggle(content, "Diagnostics Log");
            CreateDropdown(content, "FPS Counter");
            CreateDropdown(content, "FPS Cap");

            CreateLabel(content, "Utilities");
            CreateButton(content, "Clear Cache");
            CreateButton(content, "Open Save Folder");
        }

        private void CreateActionButtons(GameObject parent)
        {
            GameObject buttonsArea = new GameObject("ActionButtons");
            buttonsArea.transform.SetParent(parent.transform, false);

            RectTransform rect = buttonsArea.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0.08f);
            rect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup layout = buttonsArea.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.spacing = 10;
            layout.padding = new RectOffset(20, 20, 10, 10);

            CreateButton(buttonsArea, "Apply", accentColor);
            CreateButton(buttonsArea, "Reset", new Color(0.8f, 0.3f, 0.2f, 1f));
            CreateButton(buttonsArea, "Close", buttonNormalColor);
        }

        // UI Element creation helpers
        private GameObject CreateLabel(GameObject parent, string text)
        {
            GameObject label = new GameObject(text.Replace(" ", "") + "Label");
            label.transform.SetParent(parent.transform, false);

            RectTransform rect = label.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 30);

            LayoutElement layout = label.AddComponent<LayoutElement>();
            layout.preferredHeight = 30;

            TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = accentColor;

            return label;
        }

        private GameObject CreateToggle(GameObject parent, string labelText)
        {
            GameObject toggleObj = new GameObject(labelText.Replace(" ", "") + "Toggle");
            toggleObj.transform.SetParent(parent.transform, false);

            RectTransform rect = toggleObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 30);

            LayoutElement layout = toggleObj.AddComponent<LayoutElement>();
            layout.preferredHeight = 30;

            HorizontalLayoutGroup hLayout = toggleObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;
            hLayout.spacing = 10;

            // Toggle
            GameObject toggle = new GameObject("Toggle");
            toggle.transform.SetParent(toggleObj.transform, false);

            RectTransform toggleRect = toggle.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(30, 30);

            LayoutElement toggleLayout = toggle.AddComponent<LayoutElement>();
            toggleLayout.preferredWidth = 30;

            Image bg = toggle.AddComponent<Image>();
            bg.color = buttonNormalColor;

            Toggle toggleComp = toggle.AddComponent<Toggle>();

            // Checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(toggle.transform, false);

            RectTransform checkRect = checkmark.AddComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.sizeDelta = new Vector2(-10, -10);

            Image checkImage = checkmark.AddComponent<Image>();
            checkImage.color = accentColor;

            toggleComp.targetGraphic = bg;
            toggleComp.graphic = checkImage;

            // Label
            GameObject label = new GameObject("Label");
            label.transform.SetParent(toggleObj.transform, false);

            LayoutElement labelLayout = label.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1;

            TextMeshProUGUI text = label.AddComponent<TextMeshProUGUI>();
            text.text = labelText;
            text.fontSize = 14;
            text.color = Color.white;

            return toggleObj;
        }

        private GameObject CreateSlider(GameObject parent, string labelText, float min, float max, float defaultValue)
        {
            GameObject sliderObj = new GameObject(labelText.Replace(" ", "") + "Slider");
            sliderObj.transform.SetParent(parent.transform, false);

            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 40);

            LayoutElement layout = sliderObj.AddComponent<LayoutElement>();
            layout.preferredHeight = 40;

            VerticalLayoutGroup vLayout = sliderObj.AddComponent<VerticalLayoutGroup>();
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.spacing = 5;

            // Label row
            GameObject labelRow = new GameObject("LabelRow");
            labelRow.transform.SetParent(sliderObj.transform, false);

            LayoutElement labelRowLayout = labelRow.AddComponent<LayoutElement>();
            labelRowLayout.preferredHeight = 20;

            HorizontalLayoutGroup labelHLayout = labelRow.AddComponent<HorizontalLayoutGroup>();
            labelHLayout.childForceExpandWidth = true;

            // Label
            GameObject label = new GameObject("Label");
            label.transform.SetParent(labelRow.transform, false);

            TextMeshProUGUI labelTextComp = label.AddComponent<TextMeshProUGUI>();
            labelTextComp.text = labelText;
            labelTextComp.fontSize = 14;
            labelTextComp.color = Color.white;

            // Value text
            GameObject valueText = new GameObject("ValueText");
            valueText.transform.SetParent(labelRow.transform, false);

            TextMeshProUGUI valueTextComp = valueText.AddComponent<TextMeshProUGUI>();
            valueTextComp.text = defaultValue.ToString("F2");
            valueTextComp.fontSize = 14;
            valueTextComp.color = accentColor;
            valueTextComp.alignment = TextAlignmentOptions.Right;

            // Slider
            GameObject slider = new GameObject("Slider");
            slider.transform.SetParent(sliderObj.transform, false);

            RectTransform sliderRect = slider.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(0, 20);

            LayoutElement sliderLayout = slider.AddComponent<LayoutElement>();
            sliderLayout.preferredHeight = 20;

            Slider sliderComp = slider.AddComponent<Slider>();
            sliderComp.minValue = min;
            sliderComp.maxValue = max;
            sliderComp.value = defaultValue;

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(slider.transform, false);

            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = buttonNormalColor;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(slider.transform, false);

            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);

            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = accentColor;

            // Handle Slide Area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(slider.transform, false);

            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = Vector2.zero;

            // Handle
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);

            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);

            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;

            sliderComp.fillRect = fillRect;
            sliderComp.handleRect = handleRect;
            sliderComp.targetGraphic = handleImage;

            return sliderObj;
        }

        private GameObject CreateDropdown(GameObject parent, string labelText)
        {
            GameObject dropdownObj = new GameObject(labelText.Replace(" ", "") + "Dropdown");
            dropdownObj.transform.SetParent(parent.transform, false);

            RectTransform rect = dropdownObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 60);

            LayoutElement layout = dropdownObj.AddComponent<LayoutElement>();
            layout.preferredHeight = 60;

            VerticalLayoutGroup vLayout = dropdownObj.AddComponent<VerticalLayoutGroup>();
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.spacing = 5;

            // Label
            GameObject label = new GameObject("Label");
            label.transform.SetParent(dropdownObj.transform, false);

            TextMeshProUGUI labelTextComp = label.AddComponent<TextMeshProUGUI>();
            labelTextComp.text = labelText;
            labelTextComp.fontSize = 14;
            labelTextComp.color = Color.white;

            LayoutElement labelLayout = label.AddComponent<LayoutElement>();
            labelLayout.preferredHeight = 20;

            // Dropdown
            GameObject dropdown = new GameObject("Dropdown");
            dropdown.transform.SetParent(dropdownObj.transform, false);

            RectTransform ddRect = dropdown.AddComponent<RectTransform>();
            ddRect.sizeDelta = new Vector2(0, 30);

            LayoutElement ddLayout = dropdown.AddComponent<LayoutElement>();
            ddLayout.preferredHeight = 30;

            Image ddBg = dropdown.AddComponent<Image>();
            ddBg.color = buttonNormalColor;

            TMP_Dropdown ddComp = dropdown.AddComponent<TMP_Dropdown>();

            // Label for dropdown
            GameObject ddLabel = new GameObject("Label");
            ddLabel.transform.SetParent(dropdown.transform, false);

            RectTransform ddLabelRect = ddLabel.AddComponent<RectTransform>();
            ddLabelRect.anchorMin = Vector2.zero;
            ddLabelRect.anchorMax = Vector2.one;
            ddLabelRect.offsetMin = new Vector2(10, 0);
            ddLabelRect.offsetMax = new Vector2(-25, 0);

            TextMeshProUGUI ddLabelText = ddLabel.AddComponent<TextMeshProUGUI>();
            ddLabelText.text = "Option";
            ddLabelText.fontSize = 14;
            ddLabelText.color = Color.white;

            // Arrow
            GameObject arrow = new GameObject("Arrow");
            arrow.transform.SetParent(dropdown.transform, false);

            RectTransform arrowRect = arrow.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0);
            arrowRect.anchorMax = Vector2.one;
            arrowRect.sizeDelta = new Vector2(20, 0);
            arrowRect.anchoredPosition = new Vector2(-15, 0);

            TextMeshProUGUI arrowText = arrow.AddComponent<TextMeshProUGUI>();
            arrowText.text = "▼";
            arrowText.fontSize = 12;
            arrowText.color = Color.white;
            arrowText.alignment = TextAlignmentOptions.Center;

            // Template (simplified)
            GameObject template = new GameObject("Template");
            template.transform.SetParent(dropdown.transform, false);
            template.SetActive(false);

            RectTransform tempRect = template.AddComponent<RectTransform>();
            tempRect.anchorMin = new Vector2(0, 0);
            tempRect.anchorMax = new Vector2(1, 0);
            tempRect.pivot = new Vector2(0.5f, 1);
            tempRect.anchoredPosition = new Vector2(0, 2);
            tempRect.sizeDelta = new Vector2(0, 150);

            Image tempBg = template.AddComponent<Image>();
            tempBg.color = buttonNormalColor;

            ScrollRect tempScroll = template.AddComponent<ScrollRect>();

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(template.transform, false);

            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;

            Mask vpMask = viewport.AddComponent<Mask>();
            Image vpImage = viewport.AddComponent<Image>();
            vpImage.color = Color.clear;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 28);

            // Item
            GameObject item = new GameObject("Item");
            item.transform.SetParent(content.transform, false);

            RectTransform itemRect = item.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 20);

            Toggle itemToggle = item.AddComponent<Toggle>();
            Image itemBg = item.AddComponent<Image>();
            itemBg.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            itemToggle.targetGraphic = itemBg;

            GameObject itemLabel = new GameObject("Item Label");
            itemLabel.transform.SetParent(item.transform, false);

            RectTransform itemLabelRect = itemLabel.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(10, 0);
            itemLabelRect.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI itemText = itemLabel.AddComponent<TextMeshProUGUI>();
            itemText.text = "Option";
            itemText.fontSize = 14;
            itemText.color = Color.white;

            tempScroll.content = contentRect;
            tempScroll.viewport = vpRect;
            tempScroll.horizontal = false;

            ddComp.targetGraphic = ddBg;
            ddComp.template = tempRect;
            ddComp.captionText = ddLabelText;
            ddComp.itemText = itemText;

            // Add default options
            ddComp.options.Add(new TMP_Dropdown.OptionData("Option 1"));
            ddComp.options.Add(new TMP_Dropdown.OptionData("Option 2"));
            ddComp.options.Add(new TMP_Dropdown.OptionData("Option 3"));

            return dropdownObj;
        }

        private GameObject CreateButton(GameObject parent, string text, Color? color = null)
        {
            GameObject button = new GameObject(text.Replace(" ", "") + "Button");
            button.transform.SetParent(parent.transform, false);

            RectTransform rect = button.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 40);

            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredHeight = 40;
            layout.flexibleWidth = 1;

            Image image = button.AddComponent<Image>();
            image.color = color ?? buttonNormalColor;

            Button btn = button.AddComponent<Button>();
            btn.targetGraphic = image;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(button.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = 16;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.color = Color.white;

            return button;
        }

        private void AssignReferences(SettingsPanel panel, GameObject root)
        {
            var so = new SerializedObject(panel);

            // Find and assign all references using reflection
            Transform tabs = root.transform.Find("ContentArea/TabsContainer");

            // Assign panel references
            AssignField(so, "settingsPanel", root);
            AssignField(so, "generalTab", tabs.Find("GeneralTab"));
            AssignField(so, "graphicsTab", tabs.Find("GraphicsTab"));
            AssignField(so, "audioTab", tabs.Find("AudioTab"));
            AssignField(so, "gameplayTab", tabs.Find("GameplayTab"));
            AssignField(so, "controlsTab", tabs.Find("ControlsTab"));
            AssignField(so, "uiTab", tabs.Find("UITab"));
            AssignField(so, "accessibilityTab", tabs.Find("AccessibilityTab"));
            AssignField(so, "networkTab", tabs.Find("NetworkTab"));
            AssignField(so, "systemTab", tabs.Find("SystemTab"));

            // Assign tab buttons
            Transform tabButtons = root.transform.Find("ContentArea/TabButtonsArea");
            AssignField(so, "generalTabButton", tabButtons.Find("GeneralTabButton").GetComponent<Button>());
            AssignField(so, "graphicsTabButton", tabButtons.Find("GraphicsTabButton").GetComponent<Button>());
            AssignField(so, "audioTabButton", tabButtons.Find("AudioTabButton").GetComponent<Button>());
            AssignField(so, "gameplayTabButton", tabButtons.Find("GameplayTabButton").GetComponent<Button>());
            AssignField(so, "controlsTabButton", tabButtons.Find("ControlsTabButton").GetComponent<Button>());
            AssignField(so, "uiTabButton", tabButtons.Find("UITabButton").GetComponent<Button>());
            AssignField(so, "accessibilityTabButton", tabButtons.Find("AccessibilityTabButton").GetComponent<Button>());
            AssignField(so, "networkTabButton", tabButtons.Find("NetworkTabButton").GetComponent<Button>());
            AssignField(so, "systemTabButton", tabButtons.Find("SystemTabButton").GetComponent<Button>());

            // Assign action buttons
            Transform actionButtons = root.transform.Find("ActionButtons");
            AssignField(so, "applyButton", actionButtons.Find("ApplyButton").GetComponent<Button>());
            AssignField(so, "resetButton", actionButtons.Find("ResetButton").GetComponent<Button>());
            AssignField(so, "closeButton", actionButtons.Find("CloseButton").GetComponent<Button>());

            // Note: Individual control references would be assigned here
            // This is a simplified version - you may need to manually assign specific controls

            so.ApplyModifiedProperties();

            Debug.Log("[SettingsPanelGenerator] References assigned. Note: You may need to manually assign specific control references in the Inspector.");
        }

        private void AssignField(SerializedObject so, string fieldName, UnityEngine.Object value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private void AssignField(SerializedObject so, string fieldName, Transform transform)
        {
            if (transform != null)
            {
                AssignField(so, fieldName, transform.gameObject);
            }
        }
    }
}
