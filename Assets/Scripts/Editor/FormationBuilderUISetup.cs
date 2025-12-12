using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using RTS.UI;

namespace RTS.Editor
{
    /// <summary>
    /// Auto-setup tool for creating the Formation Builder UI panel.
    /// Creates all necessary UI elements programmatically.
    /// </summary>
    public class FormationBuilderUISetup : EditorWindow
    {
        [MenuItem("Tools/RTS/Create Formation Builder UI")]
        public static void ShowWindow()
        {
            var window = GetWindow<FormationBuilderUISetup>("Formation Builder Setup");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Formation Builder UI Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will create a complete Formation Builder UI panel.\n\n" +
                "It creates:\n" +
                "- Builder panel with background\n" +
                "- Formation name input field\n" +
                "- Grid container for placing units\n" +
                "- Save, Clear, and Close buttons\n" +
                "- Unit count display\n" +
                "- All properly connected to FormationBuilderUI component",
                MessageType.Info);

            GUILayout.Space(20);

            if (GUILayout.Button("Create Formation Builder UI", GUILayout.Height(40)))
            {
                CreateFormationBuilderUI();
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "After creation:\n" +
                "1. The panel will be hidden by default\n" +
                "2. Call FormationBuilderUI.OpenBuilder() to show it\n" +
                "3. Player clicks grid cells to place units\n" +
                "4. Player clicks Save to create the custom formation",
                MessageType.Info);
        }

        private static void CreateFormationBuilderUI()
        {
            // Find or create Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Debug.Log("Created new Canvas");
            }

            // Create main builder panel
            GameObject builderPanel = new GameObject("FormationBuilderPanel");
            builderPanel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = builderPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            // Add dark background
            Image panelBg = builderPanel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.8f);

            // Create content container
            GameObject contentPanel = new GameObject("ContentPanel");
            contentPanel.transform.SetParent(builderPanel.transform, false);

            RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(600, 700);
            contentRect.anchoredPosition = Vector2.zero;

            Image contentBg = contentPanel.AddComponent<Image>();
            contentBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Create title bar (drag handle) FIRST
            GameObject titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(contentPanel.transform, false);

            RectTransform titleBarRect = titleBar.AddComponent<RectTransform>();
            titleBarRect.anchorMin = new Vector2(0, 1);
            titleBarRect.anchorMax = Vector2.one;
            titleBarRect.pivot = new Vector2(0.5f, 1f);
            titleBarRect.sizeDelta = new Vector2(0, 40);
            titleBarRect.anchoredPosition = Vector2.zero;

            Image titleBarBg = titleBar.AddComponent<Image>();
            titleBarBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Add draggable component to title bar and wire it up
            DraggablePanel draggable = titleBar.AddComponent<DraggablePanel>();
            SerializedObject draggableSO = new SerializedObject(draggable);
            draggableSO.FindProperty("panelRectTransform").objectReferenceValue = contentRect;
            draggableSO.FindProperty("dragHandleRect").objectReferenceValue = titleBarRect;
            draggableSO.FindProperty("canvas").objectReferenceValue = canvas;
            draggableSO.ApplyModifiedProperties();

            // Add resizable component to content panel and wire it up
            ResizablePanel resizable = contentPanel.AddComponent<ResizablePanel>();
            SerializedObject resizableSO = new SerializedObject(resizable);
            resizableSO.FindProperty("panelRectTransform").objectReferenceValue = contentRect;
            resizableSO.FindProperty("minSize").vector2Value = new Vector2(600, 600);
            resizableSO.FindProperty("maxSize").vector2Value = new Vector2(1400, 1000);
            resizableSO.FindProperty("resizeHandleSize").floatValue = 20f;
            resizableSO.ApplyModifiedProperties();

            // Create title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(contentPanel.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(500, 40);
            titleRect.anchoredPosition = new Vector2(0, -10);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Formation Builder";
            titleText.fontSize = 24;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            // Create name input field
            GameObject nameInputObj = CreateInputField("FormationNameInput", "Formation Name");
            nameInputObj.transform.SetParent(contentPanel.transform, false);

            RectTransform nameInputRect = nameInputObj.GetComponent<RectTransform>();
            nameInputRect.anchorMin = new Vector2(0.5f, 1f);
            nameInputRect.anchorMax = new Vector2(0.5f, 1f);
            nameInputRect.pivot = new Vector2(0.5f, 1f);
            nameInputRect.sizeDelta = new Vector2(500, 40);
            nameInputRect.anchoredPosition = new Vector2(0, -60);

            TMP_InputField nameInput = nameInputObj.GetComponent<TMP_InputField>();

            // Create grid scroll view
            GameObject scrollView = CreateScrollView("GridScrollView");
            scrollView.transform.SetParent(contentPanel.transform, false);

            RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.sizeDelta = new Vector2(500, 450);
            scrollRect.anchoredPosition = new Vector2(0, -10);

            // Get the content container from scroll view
            RectTransform gridContainer = scrollView.transform.Find("Viewport/Content").GetComponent<RectTransform>();

            // Add zoom functionality
            ZoomableScrollRect zoomable = scrollView.AddComponent<ZoomableScrollRect>();

            // Create zoom level display
            GameObject zoomLevelObj = new GameObject("ZoomLevelText");
            zoomLevelObj.transform.SetParent(contentPanel.transform, false);

            RectTransform zoomLevelRect = zoomLevelObj.AddComponent<RectTransform>();
            zoomLevelRect.anchorMin = new Vector2(0, 0.5f);
            zoomLevelRect.anchorMax = new Vector2(0, 0.5f);
            zoomLevelRect.pivot = new Vector2(0, 0.5f);
            zoomLevelRect.sizeDelta = new Vector2(100, 30);
            zoomLevelRect.anchoredPosition = new Vector2(10, -10);

            TextMeshProUGUI zoomLevelText = zoomLevelObj.AddComponent<TextMeshProUGUI>();
            zoomLevelText.text = "Zoom: 100%";
            zoomLevelText.fontSize = 14;
            zoomLevelText.alignment = TextAlignmentOptions.MidlineLeft;
            zoomLevelText.color = Color.white;

            // Wire up zoom level text to zoomable component
            SerializedObject zoomSO = new SerializedObject(zoomable);
            zoomSO.FindProperty("zoomLevelText").objectReferenceValue = zoomLevelText;
            zoomSO.ApplyModifiedProperties();

            // Create zoom buttons
            GameObject zoomInButton = CreateButton("ZoomInButton", "+", new Color(0.4f, 0.6f, 0.4f));
            zoomInButton.transform.SetParent(contentPanel.transform, false);
            RectTransform zoomInRect = zoomInButton.GetComponent<RectTransform>();
            zoomInRect.anchorMin = new Vector2(1, 0.5f);
            zoomInRect.anchorMax = new Vector2(1, 0.5f);
            zoomInRect.pivot = new Vector2(1, 0.5f);
            zoomInRect.sizeDelta = new Vector2(40, 40);
            zoomInRect.anchoredPosition = new Vector2(-10, 20);

            GameObject zoomOutButton = CreateButton("ZoomOutButton", "-", new Color(0.6f, 0.4f, 0.4f));
            zoomOutButton.transform.SetParent(contentPanel.transform, false);
            RectTransform zoomOutRect = zoomOutButton.GetComponent<RectTransform>();
            zoomOutRect.anchorMin = new Vector2(1, 0.5f);
            zoomOutRect.anchorMax = new Vector2(1, 0.5f);
            zoomOutRect.pivot = new Vector2(1, 0.5f);
            zoomOutRect.sizeDelta = new Vector2(40, 40);
            zoomOutRect.anchoredPosition = new Vector2(-10, -25);

            // Wire up zoom buttons
            zoomInButton.GetComponent<Button>().onClick.AddListener(zoomable.ZoomIn);
            zoomOutButton.GetComponent<Button>().onClick.AddListener(zoomable.ZoomOut);

            // Create unit count text
            GameObject countTextObj = new GameObject("UnitCountText");
            countTextObj.transform.SetParent(contentPanel.transform, false);

            RectTransform countRect = countTextObj.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.5f, 0f);
            countRect.anchorMax = new Vector2(0.5f, 0f);
            countRect.pivot = new Vector2(0.5f, 0f);
            countRect.sizeDelta = new Vector2(500, 30);
            countRect.anchoredPosition = new Vector2(0, 130);

            TextMeshProUGUI countText = countTextObj.AddComponent<TextMeshProUGUI>();
            countText.text = "Units: 0";
            countText.fontSize = 18;
            countText.alignment = TextAlignmentOptions.Center;
            countText.color = Color.white;

            // Create buttons
            GameObject saveButton = CreateButton("SaveButton", "Save Formation", new Color(0.2f, 0.7f, 0.2f));
            saveButton.transform.SetParent(contentPanel.transform, false);
            RectTransform saveRect = saveButton.GetComponent<RectTransform>();
            saveRect.anchorMin = new Vector2(0.5f, 0f);
            saveRect.anchorMax = new Vector2(0.5f, 0f);
            saveRect.pivot = new Vector2(0.5f, 0f);
            saveRect.sizeDelta = new Vector2(150, 40);
            saveRect.anchoredPosition = new Vector2(-160, 80);

            GameObject clearButton = CreateButton("ClearButton", "Clear All", new Color(0.7f, 0.5f, 0.2f));
            clearButton.transform.SetParent(contentPanel.transform, false);
            RectTransform clearRect = clearButton.GetComponent<RectTransform>();
            clearRect.anchorMin = new Vector2(0.5f, 0f);
            clearRect.anchorMax = new Vector2(0.5f, 0f);
            clearRect.pivot = new Vector2(0.5f, 0f);
            clearRect.sizeDelta = new Vector2(150, 40);
            clearRect.anchoredPosition = new Vector2(0, 80);

            GameObject closeButton = CreateButton("CloseButton", "Close", new Color(0.7f, 0.2f, 0.2f));
            closeButton.transform.SetParent(contentPanel.transform, false);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(150, 40);
            closeRect.anchoredPosition = new Vector2(160, 80);

            // Create grid cell prefab
            GameObject cellPrefab = CreateGridCellPrefab();

            // Add FormationBuilderUI component
            FormationBuilderUI builderUI = builderPanel.AddComponent<FormationBuilderUI>();

            // Wire up references using SerializedObject
            SerializedObject so = new SerializedObject(builderUI);
            so.FindProperty("builderPanel").objectReferenceValue = builderPanel;
            so.FindProperty("gridContainer").objectReferenceValue = gridContainer;
            so.FindProperty("formationNameInput").objectReferenceValue = nameInput;
            so.FindProperty("saveButton").objectReferenceValue = saveButton.GetComponent<Button>();
            so.FindProperty("clearButton").objectReferenceValue = clearButton.GetComponent<Button>();
            so.FindProperty("closeButton").objectReferenceValue = closeButton.GetComponent<Button>();
            so.FindProperty("cellCountText").objectReferenceValue = countText;
            so.FindProperty("gridCellPrefab").objectReferenceValue = cellPrefab;
            so.FindProperty("gridWidth").intValue = 20;
            so.FindProperty("gridHeight").intValue = 20;
            so.FindProperty("cellSize").floatValue = 15f;
            so.ApplyModifiedProperties();

            // Add CustomCursorController to canvas if not already there
            CustomCursorController cursorController = canvas.GetComponent<CustomCursorController>();
            if (cursorController == null)
            {
                cursorController = canvas.gameObject.AddComponent<CustomCursorController>();
                Debug.Log("Added CustomCursorController to Canvas");
                Debug.Log("NOTE: Assign custom cursor textures in CustomCursorController component for hover/select/deselect cursors");
            }

            // Hide panel by default
            builderPanel.SetActive(false);

            // Select the created object
            Selection.activeGameObject = builderPanel;

            Debug.Log("Formation Builder UI created successfully!");
            Debug.Log("Grid Cell Prefab created at: Assets/Prefabs/FormationGridCell.prefab");
            Debug.Log("Features: Draggable title bar, resizable edges, zoom with mouse wheel, custom cursors");
            Debug.Log("To open the builder, call: FormationBuilderUI.OpenBuilder()");

            EditorUtility.SetDirty(builderPanel);
            EditorUtility.SetDirty(canvas.gameObject);
        }

        private static GameObject CreateInputField(string name, string placeholder)
        {
            GameObject inputObj = new GameObject(name);

            RectTransform rect = inputObj.AddComponent<RectTransform>();
            Image bg = inputObj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();

            // Create text area
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.sizeDelta = new Vector2(-20, -10);
            textAreaRect.anchoredPosition = Vector2.zero;

            // Create placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            placeholderRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = 18;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;

            // Create text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "";
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;

            inputField.textViewport = textAreaRect;
            inputField.textComponent = text;
            inputField.placeholder = placeholderText;

            return inputObj;
        }

        private static GameObject CreateScrollView(string name)
        {
            GameObject scrollViewObj = new GameObject(name);
            RectTransform scrollRect = scrollViewObj.AddComponent<RectTransform>();

            Image scrollBg = scrollViewObj.AddComponent<Image>();
            scrollBg.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            ScrollRect scroll = scrollViewObj.AddComponent<ScrollRect>();

            // Create Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollViewObj.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;

            Image viewportMask = viewport.AddComponent<Image>();
            viewportMask.color = Color.white;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Create Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(0, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.sizeDelta = new Vector2(400, 400);
            contentRect.anchoredPosition = Vector2.zero;

            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;

            return scrollViewObj;
        }

        private static GameObject CreateButton(string name, string text, Color color)
        {
            GameObject buttonObj = new GameObject(name);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            Image bg = buttonObj.AddComponent<Image>();
            bg.color = color;

            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = bg;

            // Create text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return buttonObj;
        }

        private static GameObject CreateGridCellPrefab()
        {
            // Create prefab folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // Create cell object
            GameObject cellObj = new GameObject("FormationGridCell");

            RectTransform rect = cellObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(15, 15);

            Image bg = cellObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            FormationGridCell cell = cellObj.AddComponent<FormationGridCell>();

            // Save as prefab
            string prefabPath = "Assets/Prefabs/FormationGridCell.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cellObj, prefabPath);

            // Clean up temporary object
            DestroyImmediate(cellObj);

            return prefab;
        }
    }
}
