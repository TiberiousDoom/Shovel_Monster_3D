using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelRPG.Core;
using VoxelRPG.Core.Crafting;
using VoxelRPG.Core.Items;

namespace VoxelRPG.Building
{
    /// <summary>
    /// Base class for crafting stations (workbench, furnace, etc.).
    /// Provides an interface between players and the crafting system.
    /// </summary>
    public class CraftingStation : MonoBehaviour
    {
        [Header("Station Settings")]
        [Tooltip("Unique identifier for this station type (e.g., 'workbench', 'furnace')")]
        [SerializeField] private string _stationType = "workbench";

        [Tooltip("Display name shown to players")]
        [SerializeField] private string _displayName = "Workbench";

        [Header("Interaction")]
        [SerializeField] private float _interactionRange = 3f;

        [Header("Queue Settings")]
        [Tooltip("Maximum number of items that can be queued")]
        [SerializeField] private int _maxQueueSize = 5;

        private CraftingManager _craftingManager;
        private List<CraftingJob> _queue = new List<CraftingJob>();
        private bool _isPlayerNearby;

        #region Properties

        /// <summary>
        /// The station type identifier.
        /// </summary>
        public string StationType => _stationType;

        /// <summary>
        /// Display name shown to players.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Whether the station is currently crafting.
        /// </summary>
        public bool IsCrafting => _queue.Count > 0;

        /// <summary>
        /// The current crafting job (first in queue).
        /// </summary>
        public CraftingJob CurrentJob => _queue.Count > 0 ? _queue[0] : null;

        /// <summary>
        /// Number of jobs in the queue.
        /// </summary>
        public int QueueCount => _queue.Count;

        /// <summary>
        /// Whether the queue is full.
        /// </summary>
        public bool IsQueueFull => _queue.Count >= _maxQueueSize;

        /// <summary>
        /// Whether a player is currently interacting with this station.
        /// </summary>
        public bool IsPlayerNearby => _isPlayerNearby;

        /// <summary>
        /// Available recipes for this station.
        /// </summary>
        public IEnumerable<Recipe> AvailableRecipes
        {
            get
            {
                if (_craftingManager?.RecipeRegistry == null)
                    return System.Linq.Enumerable.Empty<Recipe>();

                return _craftingManager.RecipeRegistry.GetRecipesForStation(_stationType);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when a player starts interacting with this station.
        /// </summary>
        public event Action OnInteractionStarted;

        /// <summary>
        /// Raised when a player stops interacting.
        /// </summary>
        public event Action OnInteractionEnded;

        /// <summary>
        /// Raised when crafting starts at this station.
        /// </summary>
        public event Action<CraftingJob> OnCraftingStarted;

        /// <summary>
        /// Raised when crafting completes at this station.
        /// </summary>
        public event Action<CraftingJob> OnCraftingCompleted;

        /// <summary>
        /// Raised when the queue changes.
        /// </summary>
        public event Action OnQueueChanged;

        #endregion

        protected virtual void Awake()
        {
            // Will be set in Start after CraftingManager registers
        }

        protected virtual void Start()
        {
            if (ServiceLocator.TryGet(out CraftingManager manager))
            {
                _craftingManager = manager;
            }
            else
            {
                Debug.LogWarning($"[CraftingStation] No CraftingManager found for {_displayName}");
            }
        }

        protected virtual void Update()
        {
            UpdateQueue();
        }

        /// <summary>
        /// Called when a player interacts with this station.
        /// </summary>
        public virtual void Interact()
        {
            _isPlayerNearby = true;
            OnInteractionStarted?.Invoke();
            Debug.Log($"[CraftingStation] Player opened {_displayName}");
        }

        /// <summary>
        /// Called when a player stops interacting.
        /// </summary>
        public virtual void EndInteraction()
        {
            _isPlayerNearby = false;
            OnInteractionEnded?.Invoke();
        }

        /// <summary>
        /// Attempts to queue a recipe for crafting.
        /// </summary>
        /// <param name="recipe">The recipe to craft.</param>
        /// <param name="inventory">The inventory to use for ingredients/output.</param>
        /// <returns>True if queued successfully.</returns>
        public bool TryQueueRecipe(Recipe recipe, IInventory inventory)
        {
            if (recipe == null || inventory == null) return false;

            // Check station type
            if (!recipe.IsHandCraftable && recipe.RequiredStation != _stationType)
            {
                Debug.Log($"[CraftingStation] Recipe '{recipe.DisplayName}' cannot be crafted at {_displayName}");
                return false;
            }

            // Check queue capacity
            if (IsQueueFull)
            {
                Debug.Log($"[CraftingStation] Queue is full at {_displayName}");
                return false;
            }

            // Try to start crafting
            var job = _craftingManager?.TryCraft(recipe, inventory, _stationType);

            if (job != null)
            {
                _queue.Add(job);
                OnCraftingStarted?.Invoke(job);
                OnQueueChanged?.Invoke();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Cancels the current crafting job.
        /// </summary>
        /// <param name="refundIngredients">Whether to refund ingredients.</param>
        /// <returns>True if cancelled.</returns>
        public bool CancelCurrentJob(bool refundIngredients = true)
        {
            if (_queue.Count == 0) return false;

            var job = _queue[0];
            if (_craftingManager?.CancelJob(job, refundIngredients) == true)
            {
                _queue.RemoveAt(0);
                OnQueueChanged?.Invoke();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Cancels all queued jobs.
        /// </summary>
        /// <param name="refundIngredients">Whether to refund ingredients.</param>
        public void CancelAllJobs(bool refundIngredients = true)
        {
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                _craftingManager?.CancelJob(_queue[i], refundIngredients);
            }

            _queue.Clear();
            OnQueueChanged?.Invoke();
        }

        /// <summary>
        /// Gets recipes that can be crafted with the given inventory.
        /// </summary>
        public IEnumerable<Recipe> GetCraftableRecipes(IInventory inventory)
        {
            if (_craftingManager?.RecipeRegistry == null)
                yield break;

            foreach (var recipe in _craftingManager.RecipeRegistry.GetRecipesForStation(_stationType))
            {
                if (recipe.CanCraft(inventory))
                {
                    yield return recipe;
                }
            }
        }

        private void UpdateQueue()
        {
            // Remove completed jobs from queue
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                var job = _queue[i];
                if (job.IsComplete || job.IsCancelled)
                {
                    if (job.IsComplete)
                    {
                        OnCraftingCompleted?.Invoke(job);
                    }
                    _queue.RemoveAt(i);
                    OnQueueChanged?.Invoke();
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw interaction range
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }

        private void OnValidate()
        {
            _interactionRange = Mathf.Max(0.5f, _interactionRange);
            _maxQueueSize = Mathf.Max(1, _maxQueueSize);

            if (string.IsNullOrEmpty(_stationType))
            {
                _stationType = name.ToLowerInvariant().Replace(" ", "_");
            }

            if (string.IsNullOrEmpty(_displayName))
            {
                _displayName = name;
            }
        }
#endif
    }
}
