using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoxelRPG.Core;
using VoxelRPG.Core.Crafting;
using VoxelRPG.Core.Items;
using VoxelRPG.Player;

namespace VoxelRPG.UI
{
    /// <summary>
    /// UI controller for the crafting screen.
    /// Displays available recipes and handles crafting actions.
    /// </summary>
    public class CraftingUI : MonoBehaviour
    {
        [Header("Recipe List")]
        [SerializeField] private Transform _recipeListContainer;
        [SerializeField] private RecipeSlotUI _recipeSlotPrefab;
        [SerializeField] private ScrollRect _recipeScrollRect;

        [Header("Recipe Details")]
        [SerializeField] private GameObject _detailsPanel;
        [SerializeField] private TextMeshProUGUI _recipeName;
        [SerializeField] private Image _resultIcon;
        [SerializeField] private TextMeshProUGUI _resultQuantity;
        [SerializeField] private Transform _ingredientsContainer;
        [SerializeField] private CraftingIngredientUI _ingredientPrefab;

        [Header("Crafting")]
        [SerializeField] private Button _craftButton;
        [SerializeField] private TextMeshProUGUI _craftButtonText;
        [SerializeField] private Slider _craftProgressSlider;

        [Header("Filtering")]
        [SerializeField] private TMP_InputField _searchInput;
        [SerializeField] private TMP_Dropdown _categoryDropdown;

        private IRecipeRegistry _recipeRegistry;
        private CraftingManager _craftingManager;
        private PlayerInventory _playerInventory;
        private List<RecipeSlotUI> _recipeSlots = new List<RecipeSlotUI>();
        private Recipe _selectedRecipe;
        private List<CraftingIngredientUI> _ingredientSlots = new List<CraftingIngredientUI>();

        private void OnEnable()
        {
            // Get references
            ServiceLocator.TryGet(out _recipeRegistry);
            ServiceLocator.TryGet(out _craftingManager);
            ServiceLocator.TryGet(out _playerInventory);

            if (_playerInventory != null)
            {
                _playerInventory.OnSlotChanged += OnInventoryChanged;
            }

            PopulateRecipeList();
            ClearSelection();

            // Setup search
            if (_searchInput != null)
            {
                _searchInput.onValueChanged.AddListener(OnSearchChanged);
            }
        }

        private void OnDisable()
        {
            if (_playerInventory != null)
            {
                _playerInventory.OnSlotChanged -= OnInventoryChanged;
            }

            if (_searchInput != null)
            {
                _searchInput.onValueChanged.RemoveListener(OnSearchChanged);
            }
        }

        private void PopulateRecipeList()
        {
            // Clear existing
            foreach (var slot in _recipeSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            _recipeSlots.Clear();

            if (_recipeRegistry == null || _recipeListContainer == null || _recipeSlotPrefab == null)
            {
                return;
            }

            // Get all recipes
            var recipes = _recipeRegistry.GetAll();

            foreach (var recipe in recipes)
            {
                if (recipe == null) continue;

                var slotObj = Instantiate(_recipeSlotPrefab, _recipeListContainer);
                var slot = slotObj.GetComponent<RecipeSlotUI>();
                if (slot != null)
                {
                    bool canCraft = recipe.CanCraft(_playerInventory);
                    slot.Initialize(this, recipe, canCraft);
                    _recipeSlots.Add(slot);
                }
            }
        }

        private void RefreshRecipeAvailability()
        {
            foreach (var slot in _recipeSlots)
            {
                if (slot != null && slot.Recipe != null)
                {
                    bool canCraft = slot.Recipe.CanCraft(_playerInventory);
                    slot.SetCanCraft(canCraft);
                }
            }

            RefreshDetails();
        }

        private void OnInventoryChanged(int slot, ItemStack stack)
        {
            RefreshRecipeAvailability();
        }

        private void OnSearchChanged(string searchText)
        {
            FilterRecipes(searchText);
        }

        private void FilterRecipes(string searchText)
        {
            searchText = searchText?.ToLower() ?? "";

            foreach (var slot in _recipeSlots)
            {
                if (slot == null) continue;

                bool visible = string.IsNullOrEmpty(searchText) ||
                               slot.Recipe.DisplayName.ToLower().Contains(searchText);

                slot.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Called when a recipe slot is selected.
        /// </summary>
        public void SelectRecipe(Recipe recipe)
        {
            _selectedRecipe = recipe;
            RefreshDetails();
        }

        private void RefreshDetails()
        {
            if (_selectedRecipe == null)
            {
                ClearSelection();
                return;
            }

            if (_detailsPanel != null)
            {
                _detailsPanel.SetActive(true);
            }

            // Recipe name
            if (_recipeName != null)
            {
                _recipeName.text = _selectedRecipe.DisplayName;
            }

            // Result icon and quantity
            if (_resultIcon != null && _selectedRecipe.OutputItem != null)
            {
                _resultIcon.sprite = _selectedRecipe.OutputItem.Icon;
                _resultIcon.enabled = true;
            }

            if (_resultQuantity != null)
            {
                _resultQuantity.text = _selectedRecipe.OutputAmount > 1
                    ? $"x{_selectedRecipe.OutputAmount}"
                    : "";
            }

            // Ingredients
            RefreshIngredients();

            // Craft button
            bool canCraft = _selectedRecipe.CanCraft(_playerInventory);
            if (_craftButton != null)
            {
                _craftButton.interactable = canCraft;
            }

            if (_craftButtonText != null)
            {
                _craftButtonText.text = canCraft ? "Craft" : "Missing Materials";
            }
        }

        private void RefreshIngredients()
        {
            // Clear existing
            foreach (var slot in _ingredientSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            _ingredientSlots.Clear();

            if (_selectedRecipe == null || _ingredientsContainer == null || _ingredientPrefab == null)
            {
                return;
            }

            // Create ingredient slots
            foreach (var ingredient in _selectedRecipe.Ingredients)
            {
                var slotObj = Instantiate(_ingredientPrefab, _ingredientsContainer);
                var slot = slotObj.GetComponent<CraftingIngredientUI>();
                if (slot != null)
                {
                    int playerHas = _playerInventory?.GetItemCount(ingredient.Item) ?? 0;
                    int required = ingredient.Amount;
                    slot.Initialize(ingredient.Item, required, playerHas);
                    _ingredientSlots.Add(slot);
                }
            }
        }

        private void ClearSelection()
        {
            _selectedRecipe = null;

            if (_detailsPanel != null)
            {
                _detailsPanel.SetActive(false);
            }

            foreach (var slot in _ingredientSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            _ingredientSlots.Clear();
        }

        /// <summary>
        /// Called when craft button is clicked.
        /// </summary>
        public void OnCraftButtonClicked()
        {
            if (_selectedRecipe == null || _craftingManager == null || _playerInventory == null)
            {
                return;
            }

            var job = _craftingManager.TryCraft(_selectedRecipe, _playerInventory);

            if (job != null)
            {
                Debug.Log($"[CraftingUI] Crafted {_selectedRecipe.DisplayName}");

                // Refresh everything
                RefreshRecipeAvailability();
            }
            else
            {
                Debug.Log($"[CraftingUI] Failed to craft {_selectedRecipe.DisplayName}");
            }
        }

        /// <summary>
        /// Called when craft all button is clicked.
        /// </summary>
        public void OnCraftAllButtonClicked()
        {
            if (_selectedRecipe == null || _craftingManager == null || _playerInventory == null)
            {
                return;
            }

            int crafted = 0;
            while (_craftingManager.TryCraft(_selectedRecipe, _playerInventory) != null)
            {
                crafted++;
                if (crafted >= 100) break; // Safety limit
            }

            if (crafted > 0)
            {
                Debug.Log($"[CraftingUI] Crafted {crafted}x {_selectedRecipe.DisplayName}");
            }

            RefreshRecipeAvailability();
        }
    }

    /// <summary>
    /// UI component for a recipe slot in the recipe list.
    /// </summary>
    public class RecipeSlotUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Color _canCraftColor = new Color(0.3f, 0.5f, 0.3f, 0.8f);
        [SerializeField] private Color _cannotCraftColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color _selectedColor = new Color(0.4f, 0.6f, 0.4f, 0.9f);

        private CraftingUI _parent;
        private Recipe _recipe;
        private bool _canCraft;
        private bool _isSelected;

        public Recipe Recipe => _recipe;

        public void Initialize(CraftingUI parent, Recipe recipe, bool canCraft)
        {
            _parent = parent;
            _recipe = recipe;
            _canCraft = canCraft;

            if (_icon != null && recipe.OutputItem != null)
            {
                _icon.sprite = recipe.OutputItem.Icon;
            }

            if (_nameText != null)
            {
                _nameText.text = recipe.DisplayName;
            }

            UpdateVisuals();
        }

        public void SetCanCraft(bool canCraft)
        {
            _canCraft = canCraft;
            UpdateVisuals();
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_backgroundImage != null)
            {
                if (_isSelected)
                {
                    _backgroundImage.color = _selectedColor;
                }
                else
                {
                    _backgroundImage.color = _canCraft ? _canCraftColor : _cannotCraftColor;
                }
            }
        }

        public void OnClick()
        {
            _parent?.SelectRecipe(_recipe);
        }
    }

    /// <summary>
    /// UI component for displaying a crafting ingredient.
    /// </summary>
    public class CraftingIngredientUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _quantityText;
        [SerializeField] private Color _hasEnoughColor = Color.white;
        [SerializeField] private Color _notEnoughColor = Color.red;

        public void Initialize(ItemDefinition item, int required, int playerHas)
        {
            if (_icon != null && item != null)
            {
                _icon.sprite = item.Icon;
            }

            if (_quantityText != null)
            {
                _quantityText.text = $"{playerHas}/{required}";
                _quantityText.color = playerHas >= required ? _hasEnoughColor : _notEnoughColor;
            }
        }
    }
}
