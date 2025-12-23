using UnityEngine;
using RTS.Core.Events;

namespace RTS.DebugTools.EnemySpawner
{
    /// <summary>
    /// UI panel for controlling the Enemy Spawner Building.
    /// Shows controls when spawner is selected, hides when deselected.
    ///
    /// Uses Unity's IMGUI for simplicity and to avoid UI dependencies.
    /// Can be easily replaced with a proper UI toolkit implementation.
    ///
    /// TO REMOVE: Delete the entire Assets/Scripts/Debug folder
    /// </summary>
    public class EnemySpawnerUI : MonoBehaviour
    {
        [Header("UI Settings")]
        [SerializeField] private bool showUI = true;
        [SerializeField] private KeyCode toggleUIKey = KeyCode.F9;

        [Header("Panel Position")]
        [SerializeField] private float panelX = 10f;
        [SerializeField] private float panelY = 10f;
        [SerializeField] private float panelWidth = 320f;
        [SerializeField] private float panelHeight = 500f;

        [Header("Style")]
        [SerializeField] private Color panelColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        [SerializeField] private Color headerColor = new Color(0.6f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color buttonColor = new Color(0.3f, 0.3f, 0.4f, 1f);
        [SerializeField] private Color activeButtonColor = new Color(0.2f, 0.6f, 0.2f, 1f);
        [SerializeField] private Color dangerButtonColor = new Color(0.7f, 0.2f, 0.2f, 1f);

        private EnemySpawnerBuilding currentSpawner;
        private bool isPanelVisible = false;
        private Vector2 scrollPosition;
        private GUIStyle panelStyle;
        private GUIStyle headerStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle activeButtonStyle;
        private GUIStyle dangerButtonStyle;
        private GUIStyle boxStyle;
        private Texture2D panelTexture;
        private Texture2D buttonTexture;
        private Texture2D activeButtonTexture;
        private Texture2D dangerButtonTexture;
        private Texture2D headerTexture;
        private Texture2D boxTexture;
        private bool stylesInitialized = false;

        private void OnEnable()
        {
            EventBus.Subscribe<EnemySpawnerSelectedEvent>(OnSpawnerSelected);
            EventBus.Subscribe<EnemySpawnerDeselectedEvent>(OnSpawnerDeselected);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemySpawnerSelectedEvent>(OnSpawnerSelected);
            EventBus.Unsubscribe<EnemySpawnerDeselectedEvent>(OnSpawnerDeselected);

            CleanupTextures();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleUIKey))
            {
                showUI = !showUI;
            }
        }

        private void OnSpawnerSelected(EnemySpawnerSelectedEvent evt)
        {
            currentSpawner = evt.Spawner;
            isPanelVisible = true;
        }

        private void OnSpawnerDeselected(EnemySpawnerDeselectedEvent evt)
        {
            if (currentSpawner == evt.Spawner)
            {
                isPanelVisible = false;
                currentSpawner = null;
            }
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            // Create textures
            panelTexture = MakeTexture(2, 2, panelColor);
            buttonTexture = MakeTexture(2, 2, buttonColor);
            activeButtonTexture = MakeTexture(2, 2, activeButtonColor);
            dangerButtonTexture = MakeTexture(2, 2, dangerButtonColor);
            headerTexture = MakeTexture(2, 2, headerColor);
            boxTexture = MakeTexture(2, 2, new Color(0.2f, 0.2f, 0.25f, 0.8f));

            // Panel style
            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = panelTexture;
            panelStyle.padding = new RectOffset(10, 10, 10, 10);

            // Header style
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.normal.background = headerTexture;
            headerStyle.normal.textColor = Color.white;
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.padding = new RectOffset(5, 5, 8, 8);

            // Label style
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 12;

            // Button style
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = buttonTexture;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.background = buttonTexture;
            buttonStyle.hover.textColor = Color.yellow;
            buttonStyle.active.background = buttonTexture;
            buttonStyle.fontSize = 12;
            buttonStyle.padding = new RectOffset(10, 10, 6, 6);

            // Active button style
            activeButtonStyle = new GUIStyle(buttonStyle);
            activeButtonStyle.normal.background = activeButtonTexture;
            activeButtonStyle.hover.background = activeButtonTexture;
            activeButtonStyle.active.background = activeButtonTexture;

            // Danger button style
            dangerButtonStyle = new GUIStyle(buttonStyle);
            dangerButtonStyle.normal.background = dangerButtonTexture;
            dangerButtonStyle.hover.background = dangerButtonTexture;
            dangerButtonStyle.active.background = dangerButtonTexture;

            // Box style
            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = boxTexture;
            boxStyle.padding = new RectOffset(8, 8, 8, 8);

            stylesInitialized = true;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void CleanupTextures()
        {
            if (panelTexture != null) Destroy(panelTexture);
            if (buttonTexture != null) Destroy(buttonTexture);
            if (activeButtonTexture != null) Destroy(activeButtonTexture);
            if (dangerButtonTexture != null) Destroy(dangerButtonTexture);
            if (headerTexture != null) Destroy(headerTexture);
            if (boxTexture != null) Destroy(boxTexture);
            stylesInitialized = false;
        }

        private void OnGUI()
        {
            if (!showUI || !isPanelVisible || currentSpawner == null) return;

            InitStyles();

            // Main panel
            Rect panelRect = new Rect(panelX, panelY, panelWidth, panelHeight);
            GUI.Box(panelRect, "", panelStyle);

            GUILayout.BeginArea(new Rect(panelRect.x + 10, panelRect.y + 10, panelRect.width - 20, panelRect.height - 20));

            // Header
            GUILayout.Label("ENEMY SPAWNER", headerStyle);
            GUILayout.Space(10);

            // Scroll view for controls
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(panelHeight - 80));

            DrawEnemySelection();
            GUILayout.Space(10);

            DrawSpawnSettings();
            GUILayout.Space(10);

            DrawIncrementalSettings();
            GUILayout.Space(10);

            DrawSpawnControls();
            GUILayout.Space(10);

            DrawStatistics();
            GUILayout.Space(10);

            DrawDangerZone();

            GUILayout.EndScrollView();

            // Close button
            GUILayout.Space(5);
            if (GUILayout.Button("Close Panel", buttonStyle))
            {
                currentSpawner.Deselect();
            }

            GUILayout.EndArea();
        }

        private void DrawEnemySelection()
        {
            GUILayout.Label("Enemy Type", labelStyle);

            GUILayout.BeginVertical(boxStyle);

            if (currentSpawner.Config != null && currentSpawner.Config.spawnableEnemies.Count > 0)
            {
                string[] enemyNames = currentSpawner.Config.GetEnemyNames();
                int newIndex = GUILayout.SelectionGrid(
                    currentSpawner.SelectedEnemyIndex,
                    enemyNames,
                    2,
                    buttonStyle
                );
                currentSpawner.SelectedEnemyIndex = newIndex;
            }
            else
            {
                GUILayout.Label("No enemies configured!", labelStyle);
            }

            GUILayout.EndVertical();
        }

        private void DrawSpawnSettings()
        {
            GUILayout.Label("Spawn Settings", labelStyle);

            GUILayout.BeginVertical(boxStyle);

            // Quantity
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Quantity: {currentSpawner.SpawnQuantity}", labelStyle, GUILayout.Width(100));
            currentSpawner.SpawnQuantity = Mathf.RoundToInt(
                GUILayout.HorizontalSlider(currentSpawner.SpawnQuantity, 1, 50)
            );
            GUILayout.EndHorizontal();

            // Quick quantity buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1", buttonStyle, GUILayout.Width(40))) currentSpawner.SpawnQuantity = 1;
            if (GUILayout.Button("5", buttonStyle, GUILayout.Width(40))) currentSpawner.SpawnQuantity = 5;
            if (GUILayout.Button("10", buttonStyle, GUILayout.Width(40))) currentSpawner.SpawnQuantity = 10;
            if (GUILayout.Button("25", buttonStyle, GUILayout.Width(40))) currentSpawner.SpawnQuantity = 25;
            if (GUILayout.Button("50", buttonStyle, GUILayout.Width(40))) currentSpawner.SpawnQuantity = 50;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Interval
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Interval: {currentSpawner.SpawnInterval:F1}s", labelStyle, GUILayout.Width(100));
            currentSpawner.SpawnInterval = GUILayout.HorizontalSlider(currentSpawner.SpawnInterval, 0.1f, 10f);
            GUILayout.EndHorizontal();

            // Initial Delay
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Initial Delay: {currentSpawner.InitialDelay:F1}s", labelStyle, GUILayout.Width(120));
            currentSpawner.InitialDelay = GUILayout.HorizontalSlider(currentSpawner.InitialDelay, 0f, 30f);
            GUILayout.EndHorizontal();

            // Loop toggle
            GUILayout.BeginHorizontal();
            currentSpawner.LoopSpawning = GUILayout.Toggle(currentSpawner.LoopSpawning, " Continuous Spawning", GUILayout.Width(160));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawIncrementalSettings()
        {
            GUILayout.Label("Incremental Spawning", labelStyle);

            GUILayout.BeginVertical(boxStyle);

            currentSpawner.IncrementalEnabled = GUILayout.Toggle(
                currentSpawner.IncrementalEnabled,
                " Enable Incremental Waves"
            );

            if (currentSpawner.IncrementalEnabled)
            {
                GUILayout.Space(5);

                // Increment amount
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Increase Per Wave: +{currentSpawner.IncrementalAmount}", labelStyle, GUILayout.Width(140));
                currentSpawner.IncrementalAmount = Mathf.RoundToInt(
                    GUILayout.HorizontalSlider(currentSpawner.IncrementalAmount, 0, 10)
                );
                GUILayout.EndHorizontal();

                // Wave interval
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Wave Interval: {currentSpawner.IncrementalInterval:F0}s", labelStyle, GUILayout.Width(140));
                currentSpawner.IncrementalInterval = GUILayout.HorizontalSlider(
                    currentSpawner.IncrementalInterval, 5f, 120f
                );
                GUILayout.EndHorizontal();

                // Preview
                int previewWave3 = currentSpawner.SpawnQuantity + 2 * currentSpawner.IncrementalAmount;
                int previewWave5 = currentSpawner.SpawnQuantity + 4 * currentSpawner.IncrementalAmount;
                GUILayout.Label($"Preview: Wave 1={currentSpawner.SpawnQuantity}, Wave 3={previewWave3}, Wave 5={previewWave5}", labelStyle);
            }

            GUILayout.EndVertical();
        }

        private void DrawSpawnControls()
        {
            GUILayout.Label("Spawn Controls", labelStyle);

            GUILayout.BeginVertical(boxStyle);

            // Main toggle button
            GUIStyle toggleStyle = currentSpawner.IsSpawningActive ? activeButtonStyle : buttonStyle;
            string toggleText = currentSpawner.IsSpawningActive ? "STOP SPAWNING" : "START SPAWNING";

            if (GUILayout.Button(toggleText, toggleStyle, GUILayout.Height(35)))
            {
                currentSpawner.ToggleSpawning();
            }

            GUILayout.Space(5);

            // Quick spawn buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn 1", buttonStyle))
            {
                currentSpawner.SpawnSingleEnemy();
            }
            if (GUILayout.Button($"Spawn {currentSpawner.SpawnQuantity}", buttonStyle))
            {
                currentSpawner.SpawnBatch(currentSpawner.SpawnQuantity);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawStatistics()
        {
            GUILayout.Label("Statistics", labelStyle);

            GUILayout.BeginVertical(boxStyle);

            GUILayout.Label($"Status: {(currentSpawner.IsSpawningActive ? "ACTIVE" : "Idle")}", labelStyle);
            GUILayout.Label($"Current Wave: {currentSpawner.CurrentWaveNumber}", labelStyle);
            GUILayout.Label($"Total Spawned: {currentSpawner.TotalSpawnedThisSession}", labelStyle);
            GUILayout.Label($"Active Enemies: {currentSpawner.ActiveEnemyCount}", labelStyle);

            if (GUILayout.Button("Reset Stats", buttonStyle))
            {
                currentSpawner.ResetStats();
            }

            GUILayout.EndVertical();
        }

        private void DrawDangerZone()
        {
            GUILayout.Label("Danger Zone", labelStyle);

            GUILayout.BeginVertical(boxStyle);

            if (GUILayout.Button("Kill All Spawned Enemies", dangerButtonStyle))
            {
                currentSpawner.KillAllSpawnedEnemies();
            }

            if (GUILayout.Button("Destroy All (No Effects)", dangerButtonStyle))
            {
                currentSpawner.DestroyAllSpawnedEnemies();
            }

            GUILayout.EndVertical();
        }
    }
}
