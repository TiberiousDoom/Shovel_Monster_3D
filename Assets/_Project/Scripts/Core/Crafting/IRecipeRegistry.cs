using System.Collections.Generic;
using VoxelRPG.Core.Items;

namespace VoxelRPG.Core.Crafting
{
    /// <summary>
    /// Interface for querying available recipes.
    /// </summary>
    public interface IRecipeRegistry
    {
        /// <summary>
        /// Gets all registered recipes.
        /// </summary>
        IEnumerable<Recipe> GetAll();

        /// <summary>
        /// Gets a recipe by its unique ID.
        /// </summary>
        /// <param name="id">The recipe ID.</param>
        /// <returns>The recipe, or null if not found.</returns>
        Recipe Get(string id);

        /// <summary>
        /// Tries to get a recipe by ID.
        /// </summary>
        /// <param name="id">The recipe ID.</param>
        /// <param name="recipe">The recipe if found.</param>
        /// <returns>True if found.</returns>
        bool TryGet(string id, out Recipe recipe);

        /// <summary>
        /// Gets all recipes that produce the specified item.
        /// </summary>
        /// <param name="item">The output item to search for.</param>
        /// <returns>Recipes that produce this item.</returns>
        IEnumerable<Recipe> GetRecipesForOutput(ItemDefinition item);

        /// <summary>
        /// Gets all recipes that use the specified item as an ingredient.
        /// </summary>
        /// <param name="item">The ingredient item to search for.</param>
        /// <returns>Recipes that use this item.</returns>
        IEnumerable<Recipe> GetRecipesUsingIngredient(ItemDefinition item);

        /// <summary>
        /// Gets all recipes in a category.
        /// </summary>
        /// <param name="category">The recipe category.</param>
        /// <returns>Recipes in this category.</returns>
        IEnumerable<Recipe> GetRecipesByCategory(RecipeCategory category);

        /// <summary>
        /// Gets all recipes craftable at a specific station type.
        /// </summary>
        /// <param name="stationType">The station type (empty for hand-craftable).</param>
        /// <returns>Recipes for this station.</returns>
        IEnumerable<Recipe> GetRecipesForStation(string stationType);

        /// <summary>
        /// Gets all hand-craftable recipes.
        /// </summary>
        /// <returns>Recipes that don't require a station.</returns>
        IEnumerable<Recipe> GetHandCraftableRecipes();

        /// <summary>
        /// Gets all recipes that can be crafted with the given inventory.
        /// </summary>
        /// <param name="inventory">The inventory to check.</param>
        /// <param name="stationType">Optional station type filter.</param>
        /// <returns>Recipes that can be crafted.</returns>
        IEnumerable<Recipe> GetCraftableRecipes(IInventory inventory, string stationType = null);
    }
}
