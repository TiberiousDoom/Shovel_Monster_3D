using System;
using UnityEngine;
using VoxelRPG.Core.Items;

namespace VoxelRPG.Core.Crafting
{
    /// <summary>
    /// Defines a crafting recipe with ingredients and output.
    /// ScriptableObject for data-driven recipe configuration.
    /// </summary>
    [CreateAssetMenu(menuName = "VoxelRPG/Crafting/Recipe", fileName = "NewRecipe")]
    public class Recipe : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Unique identifier for this recipe")]
        [SerializeField] private string _id;

        [Tooltip("Display name shown to players")]
        [SerializeField] private string _displayName;

        [Header("Ingredients")]
        [SerializeField] private RecipeIngredient[] _ingredients;

        [Header("Output")]
        [SerializeField] private ItemDefinition _outputItem;
        [SerializeField] private int _outputAmount = 1;

        [Header("Requirements")]
        [Tooltip("Crafting station type required (empty = hand crafting)")]
        [SerializeField] private string _requiredStation;

        [Tooltip("Time in seconds to craft (0 = instant)")]
        [SerializeField] private float _craftTime;

        [Header("Unlocking")]
        [Tooltip("Whether this recipe is available from the start")]
        [SerializeField] private bool _unlockedByDefault = true;

        [Header("Category")]
        [SerializeField] private RecipeCategory _category = RecipeCategory.Miscellaneous;

        #region Properties

        /// <summary>
        /// Unique identifier for this recipe.
        /// </summary>
        public string Id => _id;

        /// <summary>
        /// Display name shown to players.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// The ingredients required for this recipe.
        /// </summary>
        public RecipeIngredient[] Ingredients => _ingredients;

        /// <summary>
        /// The item produced by this recipe.
        /// </summary>
        public ItemDefinition OutputItem => _outputItem;

        /// <summary>
        /// How many of the output item are produced.
        /// </summary>
        public int OutputAmount => _outputAmount;

        /// <summary>
        /// The output as an ItemStack.
        /// </summary>
        public ItemStack Output => new ItemStack(_outputItem, _outputAmount);

        /// <summary>
        /// The crafting station type required (empty = hand crafting).
        /// </summary>
        public string RequiredStation => _requiredStation;

        /// <summary>
        /// Whether this recipe can be crafted by hand.
        /// </summary>
        public bool IsHandCraftable => string.IsNullOrEmpty(_requiredStation);

        /// <summary>
        /// Time in seconds to craft.
        /// </summary>
        public float CraftTime => _craftTime;

        /// <summary>
        /// Whether crafting is instant.
        /// </summary>
        public bool IsInstant => _craftTime <= 0;

        /// <summary>
        /// Whether this recipe is available from the start.
        /// </summary>
        public bool UnlockedByDefault => _unlockedByDefault;

        /// <summary>
        /// Recipe category for organization.
        /// </summary>
        public RecipeCategory Category => _category;

        #endregion

        /// <summary>
        /// Checks if an inventory has all required ingredients.
        /// </summary>
        /// <param name="inventory">The inventory to check.</param>
        /// <returns>True if all ingredients are present.</returns>
        public bool CanCraft(IInventory inventory)
        {
            if (inventory == null || _ingredients == null) return false;

            foreach (var ingredient in _ingredients)
            {
                if (!inventory.HasItem(ingredient.Item, ingredient.Amount))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if an inventory can hold the output.
        /// </summary>
        /// <param name="inventory">The inventory to check.</param>
        /// <returns>True if there's space for the output.</returns>
        public bool CanHoldOutput(IInventory inventory)
        {
            if (inventory == null || _outputItem == null) return false;
            return inventory.HasSpaceFor(_outputItem, _outputAmount);
        }

        /// <summary>
        /// Gets the missing ingredients for crafting.
        /// </summary>
        /// <param name="inventory">The inventory to check against.</param>
        /// <returns>Array of missing ingredient info.</returns>
        public MissingIngredient[] GetMissingIngredients(IInventory inventory)
        {
            if (_ingredients == null) return Array.Empty<MissingIngredient>();

            var missing = new System.Collections.Generic.List<MissingIngredient>();

            foreach (var ingredient in _ingredients)
            {
                int have = inventory?.GetItemCount(ingredient.Item) ?? 0;
                if (have < ingredient.Amount)
                {
                    missing.Add(new MissingIngredient
                    {
                        Item = ingredient.Item,
                        Required = ingredient.Amount,
                        Have = have
                    });
                }
            }

            return missing.ToArray();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-generate ID from asset name if not set
            if (string.IsNullOrEmpty(_id))
            {
                _id = name.ToLowerInvariant().Replace(" ", "_");
            }

            // Auto-generate display name if not set
            if (string.IsNullOrEmpty(_displayName))
            {
                _displayName = name;
            }

            // Clamp values
            _outputAmount = Mathf.Max(1, _outputAmount);
            _craftTime = Mathf.Max(0, _craftTime);
        }
#endif
    }

    /// <summary>
    /// A single ingredient in a recipe.
    /// </summary>
    [Serializable]
    public struct RecipeIngredient
    {
        [Tooltip("The item required")]
        public ItemDefinition Item;

        [Tooltip("Amount required")]
        public int Amount;

        public RecipeIngredient(ItemDefinition item, int amount)
        {
            Item = item;
            Amount = amount;
        }

        /// <summary>
        /// Whether this ingredient is valid.
        /// </summary>
        public bool IsValid => Item != null && Amount > 0;
    }

    /// <summary>
    /// Information about a missing ingredient.
    /// </summary>
    public struct MissingIngredient
    {
        public ItemDefinition Item;
        public int Required;
        public int Have;

        public int Missing => Required - Have;
    }

    /// <summary>
    /// Categories for organizing recipes.
    /// </summary>
    public enum RecipeCategory
    {
        Miscellaneous,
        Tools,
        Weapons,
        Armor,
        Building,
        Furniture,
        Food,
        Materials,
        Magic
    }
}
