using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelRPG.Core.Items;
using VoxelRPG.Core.Events;

namespace VoxelRPG.Core.Crafting
{
    /// <summary>
    /// Manages crafting operations and in-progress crafting jobs.
    /// </summary>
    public class CraftingManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RecipeRegistry _recipeRegistry;

        [Header("Event Channels")]
        [SerializeField] private VoidEventChannel _onCraftingStarted;
        [SerializeField] private VoidEventChannel _onCraftingCompleted;
        [SerializeField] private VoidEventChannel _onCraftingCancelled;

        private List<CraftingJob> _activeJobs = new List<CraftingJob>();

        /// <summary>
        /// The recipe registry used by this manager.
        /// </summary>
        public IRecipeRegistry RecipeRegistry => _recipeRegistry;

        /// <summary>
        /// Currently active crafting jobs.
        /// </summary>
        public IReadOnlyList<CraftingJob> ActiveJobs => _activeJobs;

        #region Events

        /// <summary>
        /// Raised when crafting starts.
        /// </summary>
        public event Action<CraftingJob> OnCraftingStarted;

        /// <summary>
        /// Raised when crafting completes successfully.
        /// </summary>
        public event Action<CraftingJob> OnCraftingCompleted;

        /// <summary>
        /// Raised when crafting is cancelled.
        /// </summary>
        public event Action<CraftingJob> OnCraftingCancelled;

        /// <summary>
        /// Raised when crafting fails (missing ingredients, no space, etc.).
        /// </summary>
        public event Action<Recipe, CraftingFailReason> OnCraftingFailed;

        #endregion

        private void Awake()
        {
            ServiceLocator.Register<CraftingManager>(this);

            if (_recipeRegistry != null)
            {
                _recipeRegistry.Initialize();
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<CraftingManager>();
        }

        private void Update()
        {
            UpdateActiveJobs();
        }

        /// <summary>
        /// Attempts to craft a recipe using the specified inventory.
        /// </summary>
        /// <param name="recipe">The recipe to craft.</param>
        /// <param name="inventory">The inventory to use for ingredients and output.</param>
        /// <param name="stationType">The station type being used (null for hand crafting).</param>
        /// <returns>The crafting job if started, null if failed.</returns>
        public CraftingJob TryCraft(Recipe recipe, IInventory inventory, string stationType = null)
        {
            if (recipe == null || inventory == null)
            {
                OnCraftingFailed?.Invoke(recipe, CraftingFailReason.InvalidInput);
                return null;
            }

            // Check station requirement
            if (!recipe.IsHandCraftable && recipe.RequiredStation != stationType)
            {
                OnCraftingFailed?.Invoke(recipe, CraftingFailReason.WrongStation);
                Debug.Log($"[CraftingManager] Recipe '{recipe.DisplayName}' requires station '{recipe.RequiredStation}'");
                return null;
            }

            // Check ingredients
            if (!recipe.CanCraft(inventory))
            {
                OnCraftingFailed?.Invoke(recipe, CraftingFailReason.MissingIngredients);
                Debug.Log($"[CraftingManager] Missing ingredients for '{recipe.DisplayName}'");
                return null;
            }

            // Check output space
            if (!recipe.CanHoldOutput(inventory))
            {
                OnCraftingFailed?.Invoke(recipe, CraftingFailReason.InventoryFull);
                Debug.Log($"[CraftingManager] No space for output of '{recipe.DisplayName}'");
                return null;
            }

            // Consume ingredients
            foreach (var ingredient in recipe.Ingredients)
            {
                inventory.TryRemoveItem(ingredient.Item, ingredient.Amount);
            }

            // Create job
            var job = new CraftingJob(recipe, inventory, stationType);

            if (recipe.IsInstant)
            {
                // Complete immediately
                CompleteJob(job);
            }
            else
            {
                // Add to active jobs
                _activeJobs.Add(job);
                OnCraftingStarted?.Invoke(job);
                _onCraftingStarted?.RaiseEvent();

                Debug.Log($"[CraftingManager] Started crafting '{recipe.DisplayName}' ({recipe.CraftTime}s)");
            }

            return job;
        }

        /// <summary>
        /// Attempts instant crafting (for recipes with no craft time).
        /// </summary>
        /// <param name="recipe">The recipe to craft.</param>
        /// <param name="inventory">The inventory to use.</param>
        /// <param name="stationType">The station type.</param>
        /// <returns>True if crafting succeeded.</returns>
        public bool TryCraftInstant(Recipe recipe, IInventory inventory, string stationType = null)
        {
            var job = TryCraft(recipe, inventory, stationType);
            return job != null && job.IsComplete;
        }

        /// <summary>
        /// Cancels a crafting job, refunding ingredients.
        /// </summary>
        /// <param name="job">The job to cancel.</param>
        /// <param name="refundIngredients">Whether to refund ingredients.</param>
        /// <returns>True if cancelled.</returns>
        public bool CancelJob(CraftingJob job, bool refundIngredients = true)
        {
            if (job == null || job.IsComplete) return false;

            if (!_activeJobs.Remove(job)) return false;

            if (refundIngredients)
            {
                // Refund ingredients
                foreach (var ingredient in job.Recipe.Ingredients)
                {
                    job.Inventory.TryAddItem(ingredient.Item, ingredient.Amount);
                }
            }

            job.Cancel();

            OnCraftingCancelled?.Invoke(job);
            _onCraftingCancelled?.RaiseEvent();

            Debug.Log($"[CraftingManager] Cancelled crafting '{job.Recipe.DisplayName}'");

            return true;
        }

        /// <summary>
        /// Gets all jobs for a specific inventory.
        /// </summary>
        public IEnumerable<CraftingJob> GetJobsForInventory(IInventory inventory)
        {
            foreach (var job in _activeJobs)
            {
                if (job.Inventory == inventory)
                {
                    yield return job;
                }
            }
        }

        private void UpdateActiveJobs()
        {
            for (int i = _activeJobs.Count - 1; i >= 0; i--)
            {
                var job = _activeJobs[i];
                job.Update(Time.deltaTime);

                if (job.IsComplete)
                {
                    _activeJobs.RemoveAt(i);
                    CompleteJob(job);
                }
            }
        }

        private void CompleteJob(CraftingJob job)
        {
            // Add output to inventory
            if (job.Inventory.TryAddItem(job.Recipe.Output))
            {
                job.Complete();

                OnCraftingCompleted?.Invoke(job);
                _onCraftingCompleted?.RaiseEvent();

                Debug.Log($"[CraftingManager] Completed crafting '{job.Recipe.DisplayName}'");
            }
            else
            {
                // This shouldn't happen since we checked space, but handle it
                Debug.LogError($"[CraftingManager] Failed to add output for '{job.Recipe.DisplayName}'");
            }
        }
    }

    /// <summary>
    /// Represents an in-progress or completed crafting operation.
    /// </summary>
    public class CraftingJob
    {
        /// <summary>
        /// The recipe being crafted.
        /// </summary>
        public Recipe Recipe { get; }

        /// <summary>
        /// The inventory used for this job.
        /// </summary>
        public IInventory Inventory { get; }

        /// <summary>
        /// The station type used (null for hand crafting).
        /// </summary>
        public string StationType { get; }

        /// <summary>
        /// Time elapsed on this job.
        /// </summary>
        public float ElapsedTime { get; private set; }

        /// <summary>
        /// Total time required.
        /// </summary>
        public float TotalTime => Recipe.CraftTime;

        /// <summary>
        /// Progress from 0 to 1.
        /// </summary>
        public float Progress => TotalTime > 0 ? Mathf.Clamp01(ElapsedTime / TotalTime) : 1f;

        /// <summary>
        /// Whether the job is complete.
        /// </summary>
        public bool IsComplete { get; private set; }

        /// <summary>
        /// Whether the job was cancelled.
        /// </summary>
        public bool IsCancelled { get; private set; }

        /// <summary>
        /// Raised when progress updates.
        /// </summary>
        public event Action<float> OnProgressChanged;

        public CraftingJob(Recipe recipe, IInventory inventory, string stationType)
        {
            Recipe = recipe;
            Inventory = inventory;
            StationType = stationType;
            ElapsedTime = 0f;
            IsComplete = recipe.IsInstant;
        }

        internal void Update(float deltaTime)
        {
            if (IsComplete || IsCancelled) return;

            ElapsedTime += deltaTime;
            OnProgressChanged?.Invoke(Progress);

            if (ElapsedTime >= TotalTime)
            {
                IsComplete = true;
            }
        }

        internal void Complete()
        {
            IsComplete = true;
        }

        internal void Cancel()
        {
            IsCancelled = true;
        }
    }

    /// <summary>
    /// Reasons crafting can fail.
    /// </summary>
    public enum CraftingFailReason
    {
        InvalidInput,
        MissingIngredients,
        WrongStation,
        InventoryFull,
        AlreadyCrafting
    }
}
