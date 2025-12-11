using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using RTS.UI;

namespace RTS.Editor
{
    /// <summary>
    /// Tool to create or fix the Formation Dropdown in UnitDetailsUI
    /// </summary>
    public class FormationDropdownSetup : EditorWindow
    {
        [MenuItem("Tools/RTS/Fix Formation Dropdown")]
        public static void ShowWindow()
        {
            var window = GetWindow<FormationDropdownSetup>("Fix Formation Dropdown");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Formation Dropdown Fix Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will:\n" +
                "1. Find your UnitDetailsUI in the scene\n" +
                "2. Find or create a proper TMP_Dropdown component\n" +
                "3. Connect it to UnitDetailsUI\n" +
                "4. Populate it with formations",
                MessageType.Info);

            GUILayout.Space(20);

            if (GUILayout.Button("Fix Formation Dropdown", GUILayout.Height(40)))
            {
                FixFormationDropdown();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Create New Dropdown From Scratch", GUILayout.Height(40)))
            {
                CreateNewDropdown();
            }
        }

        private static void FixFormationDropdown()
        {
            // Find UnitDetailsUI
            var unitDetailsUI = FindFirstObjectByType<UnitDetailsUI>();
            if (unitDetailsUI == null)
            {
                EditorUtility.DisplayDialog("Error", "UnitDetailsUI not found in scene!", "OK");
                return;
            }

            Debug.Log($"Found UnitDetailsUI on: {unitDetailsUI.gameObject.name}");

            // Try to find existing dropdown in the same hierarchy
            TMP_Dropdown dropdown = unitDetailsUI.GetComponentInChildren<TMP_Dropdown>();

            if (dropdown == null)
            {
                // Look for any TMP_Dropdown in the scene
                dropdown = FindFirstObjectByType<TMP_Dropdown>();
            }

            if (dropdown == null)
            {
                // Create new dropdown
                Debug.Log("No TMP_Dropdown found. Creating new one...");
                dropdown = CreateDropdownUI(unitDetailsUI.transform);
            }
            else
            {
                Debug.Log($"Found existing TMP_Dropdown on: {dropdown.gameObject.name}");
            }

            // Assign to UnitDetailsUI
            SerializedObject so = new SerializedObject(unitDetailsUI);
            so.FindProperty("formationDropdown").objectReferenceValue = dropdown;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(unitDetailsUI);

            Debug.Log("Formation Dropdown fixed successfully!");
            EditorUtility.DisplayDialog("Success",
                $"Formation Dropdown has been set up!\n\n" +
                $"Dropdown: {dropdown.gameObject.name}\n" +
                $"Connected to: {unitDetailsUI.gameObject.name}\n\n" +
                $"Run the game to see formations populate.",
                "OK");

            // Select the dropdown so user can see it
            Selection.activeGameObject = dropdown.gameObject;
        }

        private static void CreateNewDropdown()
        {
            var unitDetailsUI = FindFirstObjectByType<UnitDetailsUI>();
            if (unitDetailsUI == null)
            {
                EditorUtility.DisplayDialog("Error", "UnitDetailsUI not found in scene!", "OK");
                return;
            }

            TMP_Dropdown dropdown = CreateDropdownUI(unitDetailsUI.transform);

            // Assign to UnitDetailsUI
            SerializedObject so = new SerializedObject(unitDetailsUI);
            so.FindProperty("formationDropdown").objectReferenceValue = dropdown;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(unitDetailsUI);

            Debug.Log("New Formation Dropdown created successfully!");
            EditorUtility.DisplayDialog("Success",
                $"New Formation Dropdown created!\n\n" +
                $"Location: {dropdown.transform.GetPath()}\n" +
                $"Connected to: {unitDetailsUI.gameObject.name}",
                "OK");

            Selection.activeGameObject = dropdown.gameObject;
        }

        private static TMP_Dropdown CreateDropdownUI(Transform parent)
        {
            // Create dropdown GameObject
            GameObject dropdownObj = new GameObject("FormationDropdown");
            dropdownObj.transform.SetParent(parent, false);

            // Add RectTransform
            RectTransform rect = dropdownObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(200, 30);
            rect.anchoredPosition = new Vector2(10, -10);

            // Add Image (background)
            Image bgImage = dropdownObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Add TMP_Dropdown component
            TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();

            // Create Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.sizeDelta = new Vector2(-30, 0);
            labelRect.anchoredPosition = new Vector2(-5, 0);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = "Formation";
            labelText.fontSize = 14;
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.color = Color.white;

            // Create Arrow
            GameObject arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-15, 0);

            TextMeshProUGUI arrowText = arrowObj.AddComponent<TextMeshProUGUI>();
            arrowText.text = "▼";
            arrowText.fontSize = 14;
            arrowText.alignment = TextAlignmentOptions.Center;
            arrowText.color = Color.white;

            // Create Template
            GameObject templateObj = new GameObject("Template");
            templateObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform templateRect = templateObj.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.sizeDelta = new Vector2(0, 150);
            templateRect.anchoredPosition = new Vector2(0, 2);

            Image templateImage = templateObj.AddComponent<Image>();
            templateImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            // Create Viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(templateObj.transform, false);
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = new Vector2(-18, 0);
            viewportRect.anchoredPosition = new Vector2(-9, 0);

            Mask mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            Image maskImage = viewportObj.AddComponent<Image>();

            // Create Content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 28);
            contentRect.anchoredPosition = Vector2.zero;

            // Create Item
            GameObject itemObj = new GameObject("Item");
            itemObj.transform.SetParent(contentObj.transform, false);
            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 20);

            Toggle itemToggle = itemObj.AddComponent<Toggle>();

            // Item Background
            GameObject itemBgObj = new GameObject("Item Background");
            itemBgObj.transform.SetParent(itemObj.transform, false);
            RectTransform itemBgRect = itemBgObj.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.sizeDelta = Vector2.zero;

            Image itemBg = itemBgObj.AddComponent<Image>();
            itemBg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            // Item Checkmark
            GameObject checkmarkObj = new GameObject("Item Checkmark");
            checkmarkObj.transform.SetParent(itemObj.transform, false);
            RectTransform checkmarkRect = checkmarkObj.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0, 0.5f);
            checkmarkRect.pivot = new Vector2(0.5f, 0.5f);
            checkmarkRect.sizeDelta = new Vector2(20, 20);
            checkmarkRect.anchoredPosition = new Vector2(10, 0);

            TextMeshProUGUI checkmark = checkmarkObj.AddComponent<TextMeshProUGUI>();
            checkmark.text = "✓";
            checkmark.fontSize = 14;
            checkmark.alignment = TextAlignmentOptions.Center;
            checkmark.color = Color.green;

            // Item Label
            GameObject itemLabelObj = new GameObject("Item Label");
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.sizeDelta = new Vector2(-30, 0);
            itemLabelRect.anchoredPosition = new Vector2(5, 0);

            TextMeshProUGUI itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
            itemLabel.text = "Option";
            itemLabel.fontSize = 14;
            itemLabel.alignment = TextAlignmentOptions.Left;
            itemLabel.color = Color.white;

            // Wire up Toggle
            itemToggle.targetGraphic = itemBg;
            itemToggle.graphic = checkmark;
            itemToggle.isOn = true;

            // Wire up ScrollRect
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;

            // Wire up Dropdown
            dropdown.targetGraphic = bgImage;
            dropdown.template = templateRect;
            dropdown.captionText = labelText;
            dropdown.itemText = itemLabel;

            // Hide template
            templateObj.SetActive(false);

            Debug.Log($"Created new TMP_Dropdown at: {dropdownObj.transform.GetPath()}");

            return dropdown;
        }
    }

    public static class TransformExtensions
    {
        public static string GetPath(this Transform transform)
        {
            string path = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
