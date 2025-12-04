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

        // Runtime prefabs created if not provided
        private RecipeSlotUI _runtimeRecipeSlotPrefab;
        private CraftingIngredientUI _runtimeIngredientPrefab;

        private IRecipeRegistry _recipeRegistry;
        private CraftingManager _craftingManager;
        private PlayerInventory _playerInventory;
        private List<RecipeSlotUI> _recipeSlots = new List<RecipeSlotUI>();
        private Recipe _selectedRecipe;
        private List<CraftingIngredientUI> _ingredientSlots = new List<CraftingIngredientUI>();
        private string _currentStationType;

        /// <summary>
        /// Sets the crafting station type to filter recipes for.
        /// Pass null or empty string for hand crafting (shows all hand-craftable recipes).
        /// </summary>
        public void SetStationType(string stationType)
        {
            _currentStationType = stationType;
            Debug.Log($"[CraftingUI] Station type set to: {stationType ?? "Hand Crafting"}");
        }

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

            if (_recipeRegistry == null || _recipeListContainer == null)
            {
                return;
            }

            // Create runtime prefab if not provided
            var prefab = _recipeSlotPrefab ?? _runtimeRecipeSlotPrefab ?? CreateRecipeSlotPrefab();
            if (prefab == null)
            {
                return;
            }

            // Get recipes filtered by station type
            IEnumerable<Recipe> recipes;
            if (string.IsNullOrEmpty(_currentStationType))
            {
                // Hand crafting - show hand-craftable recipes
                recipes = _recipeRegistry.GetHandCraftableRecipes();
            }
            else
            {
                // Station crafting - show recipes for this station + hand-craftable
                var stationRecipes = _recipeRegistry.GetRecipesForStation(_currentStationType);
                var handRecipes = _recipeRegistry.GetHandCraftableRecipes();
                recipes = stationRecipes.Concat(handRecipes).Distinct();
            }

            foreach (var recipe in recipes)
            {
                if (recipe == null) continue;

                var slotObj = Instantiate(prefab, _recipeListContainer);
                var slot = slotObj.GetComponent<RecipeSlotUI>();
                if (slot != null)
                {
                    bool canCraft = recipe.CanCraft(_playerInventory);
                    slot.Initialize(this, recipe, canCraft);
                    _recipeSlots.Add(slot);
                }
            }
        }

        private RecipeSlotUI CreateRecipeSlotPrefab()
        {
            var prefabObj = new GameObject("RecipeSlotPrefab");
            prefabObj.SetActive(false);

            var rect = prefabObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 40);

            var image = prefabObj.AddComponent<Image>();
            image.color = new Color(0.3f, 0.5f, 0.3f, 0.8f);

            var button = prefabObj.AddComponent<Button>();
            button.targetGraphic = image;

            // Icon
            var icon = new GameObject("Icon");
            icon.transform.SetParent(prefabObj.transform, false);
            var iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0.15f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(32, 32);
            iconRect.anchoredPosition = new Vector2(20, 0);
            var iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.clear;

            // Name text
            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(prefabObj.transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.15f, 0);
            nameRect.anchorMax = Vector2.one;
            nameRect.offsetMin = new Vector2(5, 0);
            nameRect.offsetMax = new Vector2(-5, 0);
            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "Recipe Name";
            nameText.fontSize = 16;
            nameText.alignment = TextAlignmentOptions.MiddleLeft;
            nameText.color = Color.white;

            var slot = prefabObj.AddComponent<RecipeSlotUI>();

            // Set private fields via reflection
            var iconField = typeof(RecipeSlotUI).GetField("_icon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (iconField != null) iconField.SetValue(slot, iconImage);

            var nameField = typeof(RecipeSlotUI).GetField("_nameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (nameField != null) nameField.SetValue(slot, nameText);

            var bgField = typeof(RecipeSlotUI).GetField("_backgroundImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (bgField != null) bgField.SetValue(slot, image);

            button.onClick.AddListener(() => slot.OnClick());

            _runtimeRecipeSlotPrefab = slot;
            return slot;
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

            if (_selectedRecipe == null || _ingredientsContainer == null)
            {
                return;
            }

            // Create runtime prefab if not provided
            var prefab = _ingredientPrefab ?? _runtimeIngredientPrefab ?? CreateIngredientPrefab();
            if (prefab == null)
            {
                return;
            }

            // Create ingredient slots
            foreach (var ingredient in _selectedRecipe.Ingredients)
            {
                var slotObj = Instantiate(prefab, _ingredientsContainer);
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

        private CraftingIngredientUI CreateIngredientPrefab()
        {
            var prefabObj = new GameObject("IngredientPrefab");
            prefabObj.SetActive(false);

            var layout = prefabObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 2, 2);
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var rect = prefabObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 32);

            // Icon
            var icon = new GameObject("Icon");
            icon.transform.SetParent(prefabObj.transform, false);
            var iconRect = icon.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(32, 32);
            var iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.clear;
            icon.AddComponent<LayoutElement>().preferredWidth = 32;

            // Quantity text
            var quantityObj = new GameObject("Quantity");
            quantityObj.transform.SetParent(prefabObj.transform, false);
            var quantityText = quantityObj.AddComponent<TextMeshProUGUI>();
            quantityText.text = "0/0";
            quantityText.fontSize = 14;
            quantityText.alignment = TextAlignmentOptions.MiddleLeft;
            quantityText.color = Color.white;

            var slot = prefabObj.AddComponent<CraftingIngredientUI>();

            // Set private fields via reflection
            var iconField = typeof(CraftingIngredientUI).GetField("_icon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (iconField != null) iconField.SetValue(slot, iconImage);

            var quantityField = typeof(CraftingIngredientUI).GetField("_quantityText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (quantityField != null) quantityField.SetValue(slot, quantityText);

            _runtimeIngredientPrefab = slot;
            return slot;
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
