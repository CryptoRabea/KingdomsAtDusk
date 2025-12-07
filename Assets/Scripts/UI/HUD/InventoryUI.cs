using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace RTS.UI.HUD
{
    /// <summary>
    /// Inventory UI component for displaying unit items (Warcraft 3 style).
    /// Supports configurable grid sizes and item management.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private Vector2Int gridSize = new Vector2Int(3, 2); // 3 columns, 2 rows (6 slots)
        [SerializeField] private float slotSize = 50f;
        [SerializeField] private float slotSpacing = 5f;

        [Header("References")]
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private GameObject slotPrefab;

        [Header("Visual Settings")]
        [SerializeField] private Color emptySlotColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color filledSlotColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        [SerializeField] private Color highlightColor = Color.yellow;

        private List<InventorySlot> slots = new List<InventorySlot>();
        private InventoryData currentInventory;

        private void Awake()
        {
            // Initialize inventory grid
            InitializeGrid();
        }

        /// <summary>
        /// Initializes the inventory grid based on grid size.
        /// </summary>
        private void InitializeGrid()
        {
            if (slotsContainer == null || slotPrefab == null)
            {
                return;
            }

            // Clear existing slots
            foreach (Transform child in slotsContainer)
            {
                Destroy(child.gameObject);
            }
            slots.Clear();

            // Configure grid layout
            var gridLayout = slotsContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = gridSize.x;
                gridLayout.cellSize = new Vector2(slotSize, slotSize);
                gridLayout.spacing = new Vector2(slotSpacing, slotSpacing);
            }

            // Create slots
            int totalSlots = gridSize.x * gridSize.y;
            for (int i = 0; i < totalSlots; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
                var slot = slotObj.GetComponent<InventorySlot>();

                if (slot == null)
                {
                    slot = slotObj.AddComponent<InventorySlot>();
                }

                slot.Initialize(i, emptySlotColor, filledSlotColor, highlightColor);
                slots.Add(slot);

                // Set size
                RectTransform rt = slotObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(slotSize, slotSize);
                }
            }
        }

        /// <summary>
        /// Sets the inventory data to display.
        /// </summary>
        public void SetInventory(InventoryData inventory)
        {
            currentInventory = inventory;
            RefreshDisplay();
        }

        /// <summary>
        /// Refreshes the inventory display.
        /// </summary>
        private void RefreshDisplay()
        {
            if (currentInventory == null)
            {
                ClearInventory();
                return;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (i < currentInventory.items.Count)
                {
                    slots[i].SetItem(currentInventory.items[i]);
                }
                else
                {
                    slots[i].ClearSlot();
                }
            }
        }

        /// <summary>
        /// Clears all inventory slots.
        /// </summary>
        public void ClearInventory()
        {
            foreach (var slot in slots)
            {
                slot.ClearSlot();
            }
        }

        /// <summary>
        /// Configures the inventory grid size.
        /// </summary>
        public void ConfigureGrid(Vector2Int newGridSize)
        {
            gridSize = newGridSize;
            InitializeGrid();
            RefreshDisplay();
        }

        /// <summary>
        /// Shows or hides the inventory.
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Individual inventory slot component.
    /// </summary>
    public class InventorySlot : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI stackText;
        [SerializeField] private Image highlightBorder;

        private int slotIndex;
        private ItemData currentItem;
        private Color emptyColor;
        private Color filledColor;
        private Color highlightColor;

        private void Awake()
        {
            // Auto-find components if not assigned
            if (background == null)
            {
                background = GetComponent<Image>();
            }

            var images = GetComponentsInChildren<Image>();
            if (images.Length > 1)
            {
                itemIcon = images[1];
            }

            stackText = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Initialize(int index, Color empty, Color filled, Color highlight)
        {
            slotIndex = index;
            emptyColor = empty;
            filledColor = filled;
            highlightColor = highlight;

            if (background != null)
            {
                background.color = emptyColor;
            }

            if (itemIcon != null)
            {
                itemIcon.enabled = false;
            }

            if (stackText != null)
            {
                stackText.text = "";
            }

            if (highlightBorder != null)
            {
                highlightBorder.enabled = false;
            }
        }

        public void SetItem(ItemData item)
        {
            currentItem = item;

            if (background != null)
            {
                background.color = filledColor;
            }

            if (itemIcon != null && item.icon != null)
            {
                itemIcon.sprite = item.icon;
                itemIcon.enabled = true;
            }

            if (stackText != null && item.stackSize > 1)
            {
                stackText.text = item.stackSize.ToString();
            }
        }

        public void ClearSlot()
        {
            currentItem = null;

            if (background != null)
            {
                background.color = emptyColor;
            }

            if (itemIcon != null)
            {
                itemIcon.enabled = false;
            }

            if (stackText != null)
            {
                stackText.text = "";
            }
        }

        public void SetHighlight(bool highlighted)
        {
            if (highlightBorder != null)
            {
                highlightBorder.enabled = highlighted;
                if (highlighted)
                {
                    highlightBorder.color = highlightColor;
                }
            }
        }
    }

    /// <summary>
    /// Data structure for inventory.
    /// </summary>
    [System.Serializable]
    public class InventoryData
    {
        public List<ItemData> items = new List<ItemData>();

        public void AddItem(ItemData item)
        {
            items.Add(item);
        }

        public void RemoveItem(int index)
        {
            if (index >= 0 && index < items.Count)
            {
                items.RemoveAt(index);
            }
        }

        public void Clear()
        {
            items.Clear();
        }
    }

    /// <summary>
    /// Data structure for individual items.
    /// </summary>
    [System.Serializable]
    public class ItemData
    {
        public string itemName;
        public string description;
        public Sprite icon;
        public int stackSize = 1;
        public ItemType type;

        public ItemData(string name, Sprite itemIcon, ItemType itemType = ItemType.Consumable)
        {
            itemName = name;
            icon = itemIcon;
            type = itemType;
        }
    }

    /// <summary>
    /// Types of items.
    /// </summary>
    public enum ItemType
    {
        Consumable,
        Equipment,
        Artifact,
        Resource,
        Quest
    }
}
