using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoxelRPG.Core.Items;

namespace VoxelRPG.Core.Crafting
{
    /// <summary>
    /// Registry for crafting recipes.
    /// ScriptableObject-based for easy configuration in the editor.
    /// </summary>
    [CreateAssetMenu(menuName = "VoxelRPG/Crafting/Recipe Registry", fileName = "RecipeRegistry")]
    public class RecipeRegistry : ScriptableObject, IRecipeRegistry
    {
        [Header("Recipes")]
        [SerializeField] private Recipe[] _recipes;

        private Dictionary<string, Recipe> _lookup;
        private Dictionary<ItemDefinition, List<Recipe>> _byOutput;
        private Dictionary<ItemDefinition, List<Recipe>> _byIngredient;
        private Dictionary<RecipeCategory, List<Recipe>> _byCategory;
        private Dictionary<string, List<Recipe>> _byStation;
        private List<Recipe> _handCraftable;
        private bool _isInitialized;

        /// <summary>
        /// Number of recipes in the registry.
        /// </summary>
        public int Count => _recipes?.Length ?? 0;

        /// <summary>
        /// Initializes the registry lookup structures.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _lookup = new Dictionary<string, Recipe>();
            _byOutput = new Dictionary<ItemDefinition, List<Recipe>>();
            _byIngredient = new Dictionary<ItemDefinition, List<Recipe>>();
            _byCategory = new Dictionary<RecipeCategory, List<Recipe>>();
            _byStation = new Dictionary<string, List<Recipe>>();
            _handCraftable = new List<Recipe>();

            if (_recipes == null || _recipes.Length == 0)
            {
                Debug.LogWarning("[RecipeRegistry] No recipes configured");
                _isInitialized = true;
                return;
            }

            foreach (var recipe in _recipes)
            {
                if (recipe == null) continue;

                // By ID
                _lookup[recipe.Id] = recipe;

                // By output
                if (recipe.OutputItem != null)
                {
                    if (!_byOutput.TryGetValue(recipe.OutputItem, out var outputList))
                    {
                        outputList = new List<Recipe>();
                        _byOutput[recipe.OutputItem] = outputList;
                    }
                    outputList.Add(recipe);
                }

                // By ingredient
                if (recipe.Ingredients != null)
                {
                    foreach (var ingredient in recipe.Ingredients)
                    {
                        if (ingredient.Item == null) continue;

                        if (!_byIngredient.TryGetValue(ingredient.Item, out var ingredientList))
                        {
                            ingredientList = new List<Recipe>();
                            _byIngredient[ingredient.Item] = ingredientList;
                        }
                        if (!ingredientList.Contains(recipe))
                        {
                            ingredientList.Add(recipe);
                        }
                    }
                }

                // By category
                if (!_byCategory.TryGetValue(recipe.Category, out var categoryList))
                {
                    categoryList = new List<Recipe>();
                    _byCategory[recipe.Category] = categoryList;
                }
                categoryList.Add(recipe);

                // By station
                string station = recipe.RequiredStation ?? "";
                if (!_byStation.TryGetValue(station, out var stationList))
                {
                    stationList = new List<Recipe>();
                    _byStation[station] = stationList;
                }
                stationList.Add(recipe);

                // Hand craftable
                if (recipe.IsHandCraftable)
                {
                    _handCraftable.Add(recipe);
                }
            }

            Debug.Log($"[RecipeRegistry] Initialized with {_lookup.Count} recipes");
            _isInitialized = true;
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized) Initialize();
        }

        private void OnEnable()
        {
            _isInitialized = false;
        }

        /// <summary>
        /// Gets all registered recipes.
        /// </summary>
        public IEnumerable<Recipe> GetAll()
        {
            EnsureInitialized();
            return _lookup.Values;
        }

        /// <summary>
        /// Gets a recipe by ID.
        /// </summary>
        public Recipe Get(string id)
        {
            EnsureInitialized();
            return _lookup.TryGetValue(id, out var recipe) ? recipe : null;
        }

        /// <summary>
        /// Tries to get a recipe by ID.
        /// </summary>
        public bool TryGet(string id, out Recipe recipe)
        {
            EnsureInitialized();
            return _lookup.TryGetValue(id, out recipe);
        }

        /// <summary>
        /// Gets all recipes that produce the specified item.
        /// </summary>
        public IEnumerable<Recipe> GetRecipesForOutput(ItemDefinition item)
        {
            EnsureInitialized();
            if (item == null) return Enumerable.Empty<Recipe>();
            return _byOutput.TryGetValue(item, out var list) ? list : Enumerable.Empty<Recipe>();
        }

        /// <summary>
        /// Gets all recipes that use the specified item as an ingredient.
        /// </summary>
        public IEnumerable<Recipe> GetRecipesUsingIngredient(ItemDefinition item)
        {
            EnsureInitialized();
            if (item == null) return Enumerable.Empty<Recipe>();
            return _byIngredient.TryGetValue(item, out var list) ? list : Enumerable.Empty<Recipe>();
        }

        /// <summary>
        /// Gets all recipes in a category.
        /// </summary>
        public IEnumerable<Recipe> GetRecipesByCategory(RecipeCategory category)
        {
            EnsureInitialized();
            return _byCategory.TryGetValue(category, out var list) ? list : Enumerable.Empty<Recipe>();
        }

        /// <summary>
        /// Gets all recipes for a station type.
        /// </summary>
        public IEnumerable<Recipe> GetRecipesForStation(string stationType)
        {
            EnsureInitialized();
            string key = stationType ?? "";
            return _byStation.TryGetValue(key, out var list) ? list : Enumerable.Empty<Recipe>();
        }

        /// <summary>
        /// Gets all hand-craftable recipes.
        /// </summary>
        public IEnumerable<Recipe> GetHandCraftableRecipes()
        {
            EnsureInitialized();
            return _handCraftable;
        }

        /// <summary>
        /// Gets all recipes craftable with the given inventory.
        /// </summary>
        public IEnumerable<Recipe> GetCraftableRecipes(IInventory inventory, string stationType = null)
        {
            EnsureInitialized();

            IEnumerable<Recipe> candidates = stationType != null
                ? GetRecipesForStation(stationType)
                : GetAll();

            return candidates.Where(r => r.CanCraft(inventory));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_recipes == null) return;

            var ids = new HashSet<string>();
            foreach (var recipe in _recipes)
            {
                if (recipe == null) continue;

                if (string.IsNullOrEmpty(recipe.Id))
                {
                    Debug.LogWarning($"[RecipeRegistry] Recipe '{recipe.name}' has no ID", recipe);
                    continue;
                }

                if (!ids.Add(recipe.Id))
                {
                    Debug.LogError($"[RecipeRegistry] Duplicate recipe ID: '{recipe.Id}'", this);
                }
            }
        }
#endif
    }
}
