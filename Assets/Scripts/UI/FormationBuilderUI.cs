using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTS.Units.Formation;
using RTS.Core.Events;

namespace RTS.UI
{
    /// <summary>
    /// Main UI controller for the custom formation builder.
    /// Uses a grid of toggleable cells instead of drag-and-drop pieces.
    /// </summary>
    public class FormationBuilderUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject builderPanel;
        [SerializeField] private RectTransform gridContainer;
        [SerializeField] private TMP_InputField formationNameInput;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI cellCountText;

        [Header("Grid Settings")]
        [SerializeField] private GameObject gridCellPrefab;
        [SerializeField] private int gridWidth = 20; // Number of cells horizontally
        [SerializeField] private int gridHeight = 20; // Number of cells vertically
        [SerializeField] private float cellSize = 15f; // Size of each cell in pixels

        // State
        private FormationGridCell[,] gridCells;
        private CustomFormationData currentFormation = null;
        private bool isEditMode = false;

        private void Awake()
        {
            SetupEventListeners();
            builderPanel.SetActive(false);
        }

        private void SetupEventListeners()
        {
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveButtonClicked);

            if (clearButton != null)
                clearButton.onClick.AddListener(OnClearButtonClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseButtonClicked);

            if (formationNameInput != null)
                formationNameInput.onValueChanged.AddListener(OnFormationNameChanged);
        }

        /// <summary>
        /// Create the grid of cells
        /// </summary>
        private void CreateGrid()
        {
            // Clear existing grid
            ClearGrid();

            // Initialize grid array
            gridCells = new FormationGridCell[gridWidth, gridHeight];

            // Setup grid container
            if (gridContainer != null)
            {
                // Add GridLayoutGroup if not present
                GridLayoutGroup gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
                if (gridLayout == null)
                {
                    gridLayout = gridContainer.gameObject.AddComponent<GridLayoutGroup>();
                }

                gridLayout.cellSize = new Vector2(cellSize, cellSize);
                gridLayout.spacing = new Vector2(1f, 1f);
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = gridWidth;
                gridLayout.childAlignment = TextAnchor.MiddleCenter;

                // Set container size
                float totalWidth = gridWidth * cellSize + (gridWidth - 1) * 1f;
                float totalHeight = gridHeight * cellSize + (gridHeight - 1) * 1f;
                gridContainer.sizeDelta = new Vector2(totalWidth, totalHeight);

                // Create cells
                for (int y = 0; y < gridHeight; y++)
                {
                    for (int x = 0; x < gridWidth; x++)
                    {
                        GameObject cellObj = CreateGridCell(x, y);
                        if (cellObj != null)
                        {
                            FormationGridCell cell = cellObj.GetComponent<FormationGridCell>();
                            if (cell == null)
                            {
                                cell = cellObj.AddComponent<FormationGridCell>();
                            }
                            cell.Initialize(this, new Vector2Int(x, y));
                            gridCells[x, y] = cell;
                        }
                    }
                }
            }

            UpdateCellCountDisplay();
        }

        /// <summary>
        /// Create a single grid cell
        /// </summary>
        private GameObject CreateGridCell(int x, int y)
        {
            GameObject cellObj;

            if (gridCellPrefab != null)
            {
                cellObj = Instantiate(gridCellPrefab, gridContainer);
            }
            else
            {
                // Create basic cell if no prefab
                cellObj = new GameObject($"Cell_{x}_{y}");
                cellObj.transform.SetParent(gridContainer, false);

                // Add Image component for the square
                Image img = cellObj.AddComponent<Image>();
                img.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

                // Set size
                RectTransform rect = cellObj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(cellSize, cellSize);
            }

            return cellObj;
        }

        /// <summary>
        /// Clear the grid
        /// </summary>
        private void ClearGrid()
        {
            if (gridContainer != null)
            {
                foreach (Transform child in gridContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            gridCells = null;
        }

        /// <summary>
        /// Open the builder to create a new formation
        /// </summary>
        public void OpenBuilder()
        {
            OpenBuilder(null);
        }

        /// <summary>
        /// Open the builder to edit an existing formation
        /// </summary>
        public void OpenBuilder(CustomFormationData formation)
        {
            builderPanel.SetActive(true);

            // Create the grid
            CreateGrid();

            if (formation != null)
            {
                // Edit mode
                isEditMode = true;
                currentFormation = formation;
                formationNameInput.text = formation.name;

                // Load existing formation
                LoadFormationData(formation);
            }
            else
            {
                // Create mode
                isEditMode = false;
                currentFormation = new CustomFormationData();
                formationNameInput.text = "New Formation";
                formationNameInput.text = currentFormation.name;
            }

            UpdateCellCountDisplay();
        }

        /// <summary>
        /// Close the builder
        /// </summary>
        public void CloseBuilder()
        {
            builderPanel.SetActive(false);
            ClearGrid();
            currentFormation = null;
        }

        /// <summary>
        /// Called when a cell is toggled
        /// </summary>
        public void OnCellToggled(FormationGridCell cell)
        {
            UpdateCellCountDisplay();
        }

        /// <summary>
        /// Load formation data into the grid
        /// </summary>
        private void LoadFormationData(CustomFormationData formation)
        {
            if (gridCells == null || formation == null)
                return;

            // Clear all cells first
            ClearAllCells();

            // Load positions
            foreach (var pos in formation.positions)
            {
                // Convert normalized position (-1 to 1) to grid coordinates
                Vector2Int gridPos = NormalizedToGridPosition(pos.position);

                // Check if position is valid
                if (gridPos.x >= 0 && gridPos.x < gridWidth && gridPos.y >= 0 && gridPos.y < gridHeight)
                {
                    gridCells[gridPos.x, gridPos.y].SetFilled(true);
                }
            }

            UpdateCellCountDisplay();
        }

        /// <summary>
        /// Convert normalized position (-1 to 1) to grid coordinates
        /// </summary>
        private Vector2Int NormalizedToGridPosition(Vector2 normalizedPosition)
        {
            // Map from (-1, 1) to (0, gridWidth/Height)
            int x = Mathf.RoundToInt((normalizedPosition.x + 1f) * 0.5f * (gridWidth - 1));
            int y = Mathf.RoundToInt((normalizedPosition.y + 1f) * 0.5f * (gridHeight - 1));

            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Convert grid coordinates to normalized position (-1 to 1)
        /// </summary>
        private Vector2 GridPositionToNormalized(Vector2Int gridPosition)
        {
            // Map from (0, gridWidth/Height) to (-1, 1)
            float x = (gridPosition.x / (float)(gridWidth - 1)) * 2f - 1f;
            float y = (gridPosition.y / (float)(gridHeight - 1)) * 2f - 1f;

            return new Vector2(x, y);
        }

        /// <summary>
        /// Clear all filled cells
        /// </summary>
        private void ClearAllCells()
        {
            if (gridCells == null)
                return;

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    if (gridCells[x, y] != null)
                    {
                        gridCells[x, y].SetFilled(false);
                    }
                }
            }

            UpdateCellCountDisplay();
        }

        /// <summary>
        /// Get count of filled cells
        /// </summary>
        private int GetFilledCellCount()
        {
            if (gridCells == null)
                return 0;

            int count = 0;
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    if (gridCells[x, y] != null && gridCells[x, y].IsFilled)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Update cell count display
        /// </summary>
        private void UpdateCellCountDisplay()
        {
            if (cellCountText != null)
            {
                int filledCount = GetFilledCellCount();
                cellCountText.text = $"Units: {filledCount}";
            }
        }

        /// <summary>
        /// Save button clicked
        /// </summary>
        private void OnSaveButtonClicked()
        {
            SaveFormation();
        }

        /// <summary>
        /// Save the current formation
        /// </summary>
        private void SaveFormation()
        {
            int filledCount = GetFilledCellCount();
            if (filledCount == 0)
            {
                Debug.LogWarning("Cannot save empty formation!");
                return;
            }

            // Clear existing positions
            currentFormation.positions.Clear();

            // Add all filled cell positions
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    if (gridCells[x, y] != null && gridCells[x, y].IsFilled)
                    {
                        Vector2 normalizedPos = GridPositionToNormalized(new Vector2Int(x, y));
                        currentFormation.AddPosition(normalizedPos);
                    }
                }
            }

            // Update name
            currentFormation.name = formationNameInput.text;

            // Save or update in manager
            if (isEditMode)
            {
                CustomFormationManager.Instance.UpdateFormation(currentFormation);
                Debug.Log($"Formation '{currentFormation.name}' updated!");
            }
            else
            {
                // Check if name already exists
                if (CustomFormationManager.Instance.FormationNameExists(currentFormation.name))
                {
                    string uniqueName = CustomFormationManager.Instance.GetUniqueFormationName(currentFormation.name);
                    currentFormation.name = uniqueName;
                    formationNameInput.text = uniqueName;
                }

                CustomFormationManager.Instance.CreateFormation(currentFormation.name);
                CustomFormationManager.Instance.UpdateFormation(currentFormation);
                Debug.Log($"Formation '{currentFormation.name}' created!");

                // Switch to edit mode
                isEditMode = true;
            }

            // Trigger event to update UI
            EventBus.Publish(new FormationChangedEvent(FormationGroupManager.Instance?.CurrentFormation ?? FormationType.None));

            CloseBuilder();
        }

        /// <summary>
        /// Clear button clicked
        /// </summary>
        private void OnClearButtonClicked()
        {
            ClearAllCells();
        }

        /// <summary>
        /// Close button clicked
        /// </summary>
        private void OnCloseButtonClicked()
        {
            CloseBuilder();
        }

        /// <summary>
        /// Formation name changed
        /// </summary>
        private void OnFormationNameChanged(string newName)
        {
            if (currentFormation != null)
            {
                currentFormation.name = newName;
            }
        }
    }
}
