using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RTS.Units.Formation;

namespace RTS.UI
{
    /// <summary>
    /// Main UI controller for the custom formation builder
    /// </summary>
    public class FormationBuilderUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject builderPanel;
        [SerializeField] private RectTransform gridPanel;
        [SerializeField] private RectTransform garbageBin;
        [SerializeField] private TMP_InputField formationNameInput;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button deleteFormationButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI pieceCountText;

        [Header("Piece Settings")]
        [SerializeField] private GameObject piecePrefab;
        [SerializeField] private float pieceSize = 30f;
        [SerializeField] private int maxPieces = 50;

        [Header("Grid Settings")]
        [SerializeField] private Vector2 gridSize = new Vector2(400f, 400f);
        [SerializeField] private Color gridLineColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private int gridLines = 8; // Number of grid lines (creates gridLines x gridLines cells)

        // State
        private List<FormationPiece> pieces = new List<FormationPiece>();
        private FormationPiece selectedPiece = null;
        private CustomFormationData currentFormation = null;
        private bool isEditMode = false;

        // Grid rendering
        private Image gridBackground;

        private void Awake()
        {
            SetupUI();
            SetupEventListeners();
            builderPanel.SetActive(false);
        }

        private void SetupUI()
        {
            // Setup grid panel
            if (gridPanel != null)
            {
                gridPanel.sizeDelta = gridSize;

                // Add background if not present
                gridBackground = gridPanel.GetComponent<Image>();
                if (gridBackground == null)
                {
                    gridBackground = gridPanel.gameObject.AddComponent<Image>();
                }
                gridBackground.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

                // Add event trigger for right-click piece placement
                EventTrigger trigger = gridPanel.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = gridPanel.gameObject.AddComponent<EventTrigger>();
                }

                // Right-click to place pieces
                EventTrigger.Entry pointerClick = new EventTrigger.Entry();
                pointerClick.eventID = EventTriggerType.PointerClick;
                pointerClick.callback.AddListener((data) => { OnGridClicked((PointerEventData)data); });
                trigger.triggers.Add(pointerClick);
            }

            // Setup garbage bin visual
            if (garbageBin != null)
            {
                Image binImage = garbageBin.GetComponent<Image>();
                if (binImage == null)
                {
                    binImage = garbageBin.gameObject.AddComponent<Image>();
                }
                binImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);

                // Add text to garbage bin
                TextMeshProUGUI binText = garbageBin.GetComponentInChildren<TextMeshProUGUI>();
                if (binText == null)
                {
                    GameObject textObj = new GameObject("Text");
                    textObj.transform.SetParent(garbageBin, false);
                    binText = textObj.AddComponent<TextMeshProUGUI>();
                    binText.text = "🗑️ DELETE";
                    binText.alignment = TextAlignmentOptions.Center;
                    binText.fontSize = 18;
                    RectTransform textRect = textObj.GetComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                }
            }

            UpdatePieceCountDisplay();
        }

        private void SetupEventListeners()
        {
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveButtonClicked);

            if (deleteFormationButton != null)
                deleteFormationButton.onClick.AddListener(OnDeleteFormationButtonClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseButtonClicked);

            if (formationNameInput != null)
                formationNameInput.onValueChanged.AddListener(OnFormationNameChanged);
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
            ClearAllPieces();

            if (formation != null)
            {
                // Edit mode
                isEditMode = true;
                currentFormation = formation;
                formationNameInput.text = formation.name;
                deleteFormationButton.gameObject.SetActive(true);

                // Load existing pieces
                LoadFormationPieces(formation);
            }
            else
            {
                // Create mode
                isEditMode = false;
                currentFormation = new CustomFormationData();
                formationNameInput.text = currentFormation.name;
                deleteFormationButton.gameObject.SetActive(false);
            }

            UpdatePieceCountDisplay();
        }

        /// <summary>
        /// Close the builder
        /// </summary>
        public void CloseBuilder()
        {
            builderPanel.SetActive(false);
            ClearAllPieces();
            selectedPiece = null;
            currentFormation = null;
        }

        /// <summary>
        /// Handle grid clicks for placing pieces (right-click)
        /// </summary>
        private void OnGridClicked(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // Right-click to place a new piece
                PlacePiece(eventData.position);
            }
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                // Left-click on empty space deselects
                DeselectAllPieces();
            }
        }

        /// <summary>
        /// Place a new piece at the clicked position
        /// </summary>
        private void PlacePiece(Vector2 screenPosition)
        {
            if (pieces.Count >= maxPieces)
            {
                Debug.LogWarning($"Maximum piece count ({maxPieces}) reached!");
                return;
            }

            if (piecePrefab == null)
            {
                Debug.LogError("Piece prefab is not assigned!");
                return;
            }

            // Convert screen position to local position in grid
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gridPanel,
                screenPosition,
                null,
                out Vector2 localPoint);

            // Check if position is within grid bounds
            if (!IsPositionInGrid(localPoint))
                return;

            // Instantiate piece
            GameObject pieceObj = Instantiate(piecePrefab, gridPanel);
            FormationPiece piece = pieceObj.GetComponent<FormationPiece>();

            if (piece == null)
            {
                piece = pieceObj.AddComponent<FormationPiece>();
            }

            // Setup piece
            RectTransform pieceRect = pieceObj.GetComponent<RectTransform>();
            pieceRect.sizeDelta = new Vector2(pieceSize, pieceSize);
            pieceRect.anchoredPosition = localPoint;

            piece.Initialize(this, pieces.Count);
            pieces.Add(piece);

            UpdatePieceCountDisplay();
        }

        /// <summary>
        /// Load pieces from a formation data
        /// </summary>
        private void LoadFormationPieces(CustomFormationData formation)
        {
            foreach (var pos in formation.positions)
            {
                // Convert normalized position (-1 to 1) to grid position
                Vector2 gridPos = NormalizedToGridPosition(pos.position);

                if (piecePrefab == null)
                {
                    Debug.LogError("Piece prefab is not assigned!");
                    continue;
                }

                // Instantiate piece
                GameObject pieceObj = Instantiate(piecePrefab, gridPanel);
                FormationPiece piece = pieceObj.GetComponent<FormationPiece>();

                if (piece == null)
                {
                    piece = pieceObj.AddComponent<FormationPiece>();
                }

                // Setup piece
                RectTransform pieceRect = pieceObj.GetComponent<RectTransform>();
                pieceRect.sizeDelta = new Vector2(pieceSize, pieceSize);
                pieceRect.anchoredPosition = gridPos;

                piece.Initialize(this, pieces.Count);
                pieces.Add(piece);
            }
        }

        /// <summary>
        /// Called when a piece is clicked
        /// </summary>
        public void OnPieceClicked(FormationPiece piece)
        {
            // Deselect all other pieces
            DeselectAllPieces();

            // Select this piece
            piece.SetSelected(true);
            selectedPiece = piece;
        }

        /// <summary>
        /// Called when a piece starts being dragged
        /// </summary>
        public void OnPieceDragStart(FormationPiece piece)
        {
            // Deselect all pieces during drag
            DeselectAllPieces();
        }

        /// <summary>
        /// Called when a piece is dropped
        /// </summary>
        public void OnPieceDragEnd(FormationPiece piece, PointerEventData eventData)
        {
            // Check if dropped on garbage bin
            if (IsOverGarbageBin(eventData.position))
            {
                RemovePiece(piece);
                Destroy(piece.gameObject);
                return;
            }

            // Check if dropped outside grid
            Vector2 localPoint = piece.GetPosition();
            if (!IsPositionInGrid(localPoint))
            {
                // Restore to original position
                piece.RestoreOriginalPosition();
            }
        }

        /// <summary>
        /// Remove a piece from the formation
        /// </summary>
        public void RemovePiece(FormationPiece piece)
        {
            pieces.Remove(piece);
            UpdatePieceIndices();
            UpdatePieceCountDisplay();

            if (selectedPiece == piece)
            {
                selectedPiece = null;
            }
        }

        /// <summary>
        /// Clear all pieces
        /// </summary>
        private void ClearAllPieces()
        {
            foreach (var piece in pieces)
            {
                if (piece != null)
                    Destroy(piece.gameObject);
            }
            pieces.Clear();
            selectedPiece = null;
            UpdatePieceCountDisplay();
        }

        /// <summary>
        /// Deselect all pieces
        /// </summary>
        private void DeselectAllPieces()
        {
            foreach (var piece in pieces)
            {
                piece.SetSelected(false);
            }
            selectedPiece = null;
        }

        /// <summary>
        /// Update piece indices after removal
        /// </summary>
        private void UpdatePieceIndices()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                pieces[i].PieceIndex = i;
            }
        }

        /// <summary>
        /// Check if a position is within the grid bounds
        /// </summary>
        private bool IsPositionInGrid(Vector2 localPosition)
        {
            float halfWidth = gridSize.x / 2f;
            float halfHeight = gridSize.y / 2f;

            return localPosition.x >= -halfWidth && localPosition.x <= halfWidth &&
                   localPosition.y >= -halfHeight && localPosition.y <= halfHeight;
        }

        /// <summary>
        /// Check if a screen position is over the garbage bin
        /// </summary>
        private bool IsOverGarbageBin(Vector2 screenPosition)
        {
            if (garbageBin == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(garbageBin, screenPosition, null);
        }

        /// <summary>
        /// Convert grid position to normalized position (-1 to 1)
        /// </summary>
        private Vector2 GridPositionToNormalized(Vector2 gridPosition)
        {
            float normalizedX = gridPosition.x / (gridSize.x / 2f);
            float normalizedY = gridPosition.y / (gridSize.y / 2f);
            return new Vector2(normalizedX, normalizedY);
        }

        /// <summary>
        /// Convert normalized position to grid position
        /// </summary>
        private Vector2 NormalizedToGridPosition(Vector2 normalizedPosition)
        {
            float gridX = normalizedPosition.x * (gridSize.x / 2f);
            float gridY = normalizedPosition.y * (gridSize.y / 2f);
            return new Vector2(gridX, gridY);
        }

        /// <summary>
        /// Update piece count display
        /// </summary>
        private void UpdatePieceCountDisplay()
        {
            if (pieceCountText != null)
            {
                pieceCountText.text = $"Pieces: {pieces.Count}/{maxPieces}";
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
            if (pieces.Count == 0)
            {
                Debug.LogWarning("Cannot save empty formation!");
                return;
            }

            // Clear existing positions
            currentFormation.positions.Clear();

            // Add all piece positions
            foreach (var piece in pieces)
            {
                Vector2 gridPos = piece.GetPosition();
                Vector2 normalizedPos = GridPositionToNormalized(gridPos);
                currentFormation.AddPosition(normalizedPos);
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
                deleteFormationButton.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Delete formation button clicked
        /// </summary>
        private void OnDeleteFormationButtonClicked()
        {
            if (currentFormation != null && isEditMode)
            {
                CustomFormationManager.Instance.DeleteFormation(currentFormation.id);
                Debug.Log($"Formation '{currentFormation.name}' deleted!");
                CloseBuilder();
            }
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

        private void Update()
        {
            // Handle delete key for selected piece
            if (selectedPiece != null && Input.GetKeyDown(KeyCode.Delete))
            {
                RemovePiece(selectedPiece);
                Destroy(selectedPiece.gameObject);
                selectedPiece = null;
            }
        }

        private void OnDrawGizmos()
        {
            // Draw grid lines in editor for visualization
            if (gridPanel != null && Application.isPlaying)
            {
                DrawGrid();
            }
        }

        /// <summary>
        /// Draw grid lines on the panel
        /// </summary>
        private void DrawGrid()
        {
            // This would typically be done with a shader or UI elements
            // For now, we'll just outline the approach
            // You can add actual grid line UI elements as children of gridPanel if needed
        }
    }
}
