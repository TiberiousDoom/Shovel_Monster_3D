using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoxelRPG.Core;
using VoxelRPG.Core.Items;

namespace VoxelRPG.UI
{
    /// <summary>
    /// UI component for a single hotbar slot.
    /// Displays item icon, quantity, and selection state.
    /// </summary>
    public class HotbarSlotUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _quantityText;
        [SerializeField] private GameObject _selectionHighlight;
        [SerializeField] private Image _backgroundImage;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color _selectedColor = new Color(0.4f, 0.4f, 0.2f, 0.9f);
        [SerializeField] private Color _emptyColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        [Header("Slot Number")]
        [SerializeField] private TextMeshProUGUI _slotNumberText;
        [SerializeField] private int _slotNumber;

        private bool _isSelected;
        private bool _isEmpty = true;

        private void Awake()
        {
            if (_slotNumberText != null)
            {
                // Display 1-9, 0 for slot 10
                _slotNumberText.text = ((_slotNumber + 1) % 10).ToString();
            }

            UpdateVisuals();
        }

        /// <summary>
        /// Updates the slot with new item stack data.
        /// </summary>
        public void UpdateSlot(ItemStack stack)
        {
            _isEmpty = stack.IsEmpty;

            if (_isEmpty)
            {
                // Empty slot
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
                // Has item
                if (_iconImage != null)
                {
                    _iconImage.enabled = true;
                    _iconImage.sprite = stack.Item?.Icon;
                }

                if (_quantityText != null)
                {
                    // Only show quantity if > 1
                    _quantityText.text = stack.Quantity > 1 ? stack.Quantity.ToString() : "";
                }
            }

            UpdateVisuals();
        }

        /// <summary>
        /// Sets whether this slot is selected.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            if (_selectionHighlight != null)
            {
                _selectionHighlight.SetActive(selected);
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_backgroundImage != null)
            {
                if (_isEmpty)
                {
                    _backgroundImage.color = _emptyColor;
                }
                else
                {
                    _backgroundImage.color = _isSelected ? _selectedColor : _normalColor;
                }
            }
        }

        /// <summary>
        /// Sets the slot number for display.
        /// </summary>
        public void SetSlotNumber(int number)
        {
            _slotNumber = number;
            if (_slotNumberText != null)
            {
                _slotNumberText.text = ((number + 1) % 10).ToString();
            }
        }
    }
}
