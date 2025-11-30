using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoxelRPG.Core;
using VoxelRPG.Core.Items;
using VoxelRPG.Player;

namespace VoxelRPG.UI
{
    /// <summary>
    /// UI controller for the inventory screen.
    /// Manages slot display, drag-and-drop, and item tooltips.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("Grid References")]
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private InventorySlotUI _slotPrefab;
        [SerializeField] private InventorySlotUI[] _preCreatedSlots;

        [Header("Tooltip")]
        [SerializeField] private GameObject _tooltipPanel;
        [SerializeField] private TextMeshProUGUI _tooltipTitle;
        [SerializeField] private TextMeshProUGUI _tooltipDescription;
        [SerializeField] private Vector3 _tooltipOffset = new Vector3(20f, -20f, 0f);

        [Header("Drag Visual")]
        [SerializeField] private Image _dragIcon;
        [SerializeField] private TextMeshProUGUI _dragQuantity;
        [SerializeField] private Canvas _dragCanvas;

        [Header("Layout")]
        [SerializeField] private int _slotsPerRow = 9;

        private PlayerInventory _playerInventory;
        private InventorySlotUI[] _slots;
        private bool _isDragging;
        private int _dragSourceSlot = -1;
        private ItemStack _dragStack;

        private void Awake()
        {
            if (_tooltipPanel != null)
            {
                _tooltipPanel.SetActive(false);
            }

            if (_dragIcon != null)
            {
                _dragIcon.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // Get inventory reference
            if (_playerInventory == null)
            {
                ServiceLocator.TryGet(out _playerInventory);
            }

            if (_playerInventory != null)
            {
                InitializeSlots();
                RefreshAllSlots();
                _playerInventory.OnSlotChanged += OnInventorySlotChanged;
            }
        }

        private void OnDisable()
        {
            if (_playerInventory != null)
            {
                _playerInventory.OnSlotChanged -= OnInventorySlotChanged;
            }

            HideTooltip();
            EndDrag();
        }

        private void Update()
        {
            // Update drag position
            if (_isDragging && _dragIcon != null)
            {
                _dragIcon.transform.position = Input.mousePosition;
            }
        }

        private void InitializeSlots()
        {
            // Use pre-created slots if available (runtime UI)
            if (_preCreatedSlots != null && _preCreatedSlots.Length > 0)
            {
                _slots = _preCreatedSlots;
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i] != null)
                    {
                        _slots[i].Initialize(this, i);
                    }
                }
                return;
            }

            // Otherwise create slots dynamically from prefab
            if (_playerInventory == null || _slotContainer == null || _slotPrefab == null)
            {
                return;
            }

            // Clear existing slots
            foreach (Transform child in _slotContainer)
            {
                Destroy(child.gameObject);
            }

            int slotCount = _playerInventory.SlotCount;
            _slots = new InventorySlotUI[slotCount];

            for (int i = 0; i < slotCount; i++)
            {
                var slotObj = Instantiate(_slotPrefab, _slotContainer);
                var slot = slotObj.GetComponent<InventorySlotUI>();
                slot.Initialize(this, i);
                _slots[i] = slot;
            }
        }

        private void RefreshAllSlots()
        {
            if (_playerInventory == null || _slots == null) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                var stack = _playerInventory.GetSlot(i);
                _slots[i]?.UpdateSlot(stack);
            }
        }

        private void OnInventorySlotChanged(int slot, ItemStack stack)
        {
            if (_slots != null && slot >= 0 && slot < _slots.Length)
            {
                _slots[slot]?.UpdateSlot(stack);
            }
        }

        #region Slot Interactions

        /// <summary>
        /// Called when a slot is left-clicked.
        /// </summary>
        public void OnSlotLeftClicked(int slotIndex)
        {
            if (_playerInventory == null) return;

            // If dragging, try to place/swap
            if (_isDragging)
            {
                OnSlotDrop(slotIndex);
                return;
            }

            // Otherwise, pick up item to drag
            var stack = _playerInventory.GetSlot(slotIndex);
            if (!stack.IsEmpty)
            {
                BeginDrag(slotIndex, stack);
            }
        }

        /// <summary>
        /// Called when a slot is right-clicked.
        /// Split stack or place one item.
        /// </summary>
        public void OnSlotRightClicked(int slotIndex)
        {
            if (_playerInventory == null) return;

            if (_isDragging)
            {
                // Place one item from drag stack
                PlaceOneItem(slotIndex);
            }
            else
            {
                // Pick up half the stack
                var stack = _playerInventory.GetSlot(slotIndex);
                if (!stack.IsEmpty && stack.Quantity > 1)
                {
                    int halfQty = stack.Quantity / 2;
                    BeginDrag(slotIndex, new ItemStack(stack.Item, halfQty));

                    // Remove half from source
                    _playerInventory.TrySetSlot(slotIndex, new ItemStack(stack.Item, stack.Quantity - halfQty));
                }
            }
        }

        /// <summary>
        /// Called when an item is dropped on a slot.
        /// </summary>
        public void OnSlotDrop(int targetSlot)
        {
            if (!_isDragging || _playerInventory == null) return;

            var targetStack = _playerInventory.GetSlot(targetSlot);

            if (targetStack.IsEmpty)
            {
                // Place in empty slot
                _playerInventory.TrySetSlot(targetSlot, _dragStack);
            }
            else if (targetStack.Item == _dragStack.Item && targetStack.CanStack(_dragStack.Quantity))
            {
                // Stack same items
                _playerInventory.TrySetSlot(targetSlot, targetStack.AddQuantity(_dragStack.Quantity));
            }
            else
            {
                // Swap items
                _playerInventory.TrySetSlot(_dragSourceSlot, targetStack);
                _playerInventory.TrySetSlot(targetSlot, _dragStack);
            }

            EndDrag();
        }

        private void PlaceOneItem(int targetSlot)
        {
            if (!_isDragging || _playerInventory == null) return;

            var targetStack = _playerInventory.GetSlot(targetSlot);

            if (targetStack.IsEmpty)
            {
                // Place one item
                _playerInventory.TrySetSlot(targetSlot, new ItemStack(_dragStack.Item, 1));
                _dragStack = _dragStack.RemoveQuantity(1);
            }
            else if (targetStack.Item == _dragStack.Item && targetStack.CanStack(1))
            {
                // Add one to stack
                _playerInventory.TrySetSlot(targetSlot, targetStack.AddQuantity(1));
                _dragStack = _dragStack.RemoveQuantity(1);
            }

            // If drag stack is empty, end drag
            if (_dragStack.IsEmpty)
            {
                EndDrag();
            }
            else
            {
                UpdateDragVisual();
            }
        }

        #endregion

        #region Drag and Drop

        /// <summary>
        /// Begins dragging an item.
        /// </summary>
        public void BeginDrag(int sourceSlot, ItemStack stack)
        {
            if (stack.IsEmpty) return;

            _isDragging = true;
            _dragSourceSlot = sourceSlot;
            _dragStack = stack;

            // Clear source slot
            _playerInventory.TrySetSlot(sourceSlot, ItemStack.Empty);

            // Show drag visual
            if (_dragIcon != null)
            {
                _dragIcon.gameObject.SetActive(true);
                _dragIcon.sprite = stack.Item?.Icon;
            }

            if (_dragQuantity != null)
            {
                _dragQuantity.text = stack.Quantity > 1 ? stack.Quantity.ToString() : "";
            }

            HideTooltip();
        }

        /// <summary>
        /// Updates drag position.
        /// </summary>
        public void UpdateDrag(Vector2 position)
        {
            if (_dragIcon != null)
            {
                _dragIcon.transform.position = position;
            }
        }

        /// <summary>
        /// Ends dragging. Returns item to source if not placed.
        /// </summary>
        public void EndDrag()
        {
            if (!_isDragging) return;

            // If still holding items, return to source
            if (!_dragStack.IsEmpty && _playerInventory != null && _dragSourceSlot >= 0)
            {
                var currentSource = _playerInventory.GetSlot(_dragSourceSlot);
                if (currentSource.IsEmpty)
                {
                    _playerInventory.TrySetSlot(_dragSourceSlot, _dragStack);
                }
                else
                {
                    // Source is occupied, find new slot
                    _playerInventory.TryAddItem(_dragStack.Item, _dragStack.Quantity);
                }
            }

            _isDragging = false;
            _dragSourceSlot = -1;
            _dragStack = ItemStack.Empty;

            if (_dragIcon != null)
            {
                _dragIcon.gameObject.SetActive(false);
            }
        }

        private void UpdateDragVisual()
        {
            if (_dragQuantity != null)
            {
                _dragQuantity.text = _dragStack.Quantity > 1 ? _dragStack.Quantity.ToString() : "";
            }
        }

        #endregion

        #region Tooltip

        /// <summary>
        /// Shows tooltip for an item.
        /// </summary>
        public void ShowTooltip(ItemStack stack, Vector3 position)
        {
            if (_tooltipPanel == null || stack.IsEmpty) return;

            _tooltipPanel.SetActive(true);
            _tooltipPanel.transform.position = position + _tooltipOffset;

            if (_tooltipTitle != null)
            {
                _tooltipTitle.text = stack.Item?.DisplayName ?? "Unknown Item";
            }

            if (_tooltipDescription != null)
            {
                _tooltipDescription.text = stack.Item?.Description ?? "";
            }
        }

        /// <summary>
        /// Hides the tooltip.
        /// </summary>
        public void HideTooltip()
        {
            if (_tooltipPanel != null)
            {
                _tooltipPanel.SetActive(false);
            }
        }

        #endregion
    }
}
