using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using VoxelRPG.Core;
using VoxelRPG.Core.Items;

namespace VoxelRPG.UI
{
    /// <summary>
    /// UI component for a single inventory slot.
    /// Supports drag and drop, hover tooltips, and click interactions.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [Header("References")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _quantityText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private GameObject _hoverHighlight;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color _hoverColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        [SerializeField] private Color _emptyColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        private int _slotIndex;
        private ItemStack _currentStack;
        private InventoryUI _parentInventory;
        private bool _isHovered;

        /// <summary>
        /// The slot index in the inventory.
        /// </summary>
        public int SlotIndex => _slotIndex;

        /// <summary>
        /// Current item stack in this slot.
        /// </summary>
        public ItemStack CurrentStack => _currentStack;

        /// <summary>
        /// Initializes the slot with parent reference and index.
        /// </summary>
        public void Initialize(InventoryUI parent, int index)
        {
            _parentInventory = parent;
            _slotIndex = index;
            UpdateVisuals();
        }

        /// <summary>
        /// Updates the slot with new item stack data.
        /// </summary>
        public void UpdateSlot(ItemStack stack)
        {
            _currentStack = stack;

            if (stack.IsEmpty)
            {
                if (_iconImage != null)
                {
                    _iconImage.enabled = false;
                }

                if (_quantityText != null)
                {
                    _quantityText.text = "";
                }
            }
            else
            {
                if (_iconImage != null)
                {
                    _iconImage.enabled = true;
                    _iconImage.sprite = stack.Item?.Icon;
                }

                if (_quantityText != null)
                {
                    _quantityText.text = stack.Quantity > 1 ? stack.Quantity.ToString() : "";
                }
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_backgroundImage != null)
            {
                if (_currentStack.IsEmpty)
                {
                    _backgroundImage.color = _emptyColor;
                }
                else
                {
                    _backgroundImage.color = _isHovered ? _hoverColor : _normalColor;
                }
            }

            if (_hoverHighlight != null)
            {
                _hoverHighlight.SetActive(_isHovered);
            }
        }

        #region Pointer Events

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_parentInventory == null) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _parentInventory.OnSlotLeftClicked(_slotIndex);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                _parentInventory.OnSlotRightClicked(_slotIndex);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            UpdateVisuals();

            if (!_currentStack.IsEmpty && _parentInventory != null)
            {
                _parentInventory.ShowTooltip(_currentStack, transform.position);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            UpdateVisuals();

            if (_parentInventory != null)
            {
                _parentInventory.HideTooltip();
            }
        }

        #endregion

        #region Drag Events

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_currentStack.IsEmpty) return;
            if (_parentInventory == null) return;

            _parentInventory.BeginDrag(_slotIndex, _currentStack);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_parentInventory == null) return;
            _parentInventory.UpdateDrag(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_parentInventory == null) return;
            _parentInventory.EndDrag();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_parentInventory == null) return;
            _parentInventory.OnSlotDrop(_slotIndex);
        }

        #endregion
    }
}
