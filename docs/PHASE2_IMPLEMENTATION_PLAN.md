# Phase 2 Implementation Plan

**Created:** November 2025
**Status:** Active
**Purpose:** Detailed implementation guide for completing Phase 2 (Colony Alpha)

---

## Executive Summary

**Goal:** NPC settlement building with autonomous workers
**Current State:** Phase 1 complete with survival systems, inventory, crafting, monsters
**Effort Required:** ~1800 lines of code + asset creation
**Organized Into:** 8 implementation tasks, ordered by dependency

### Configuration Parameters

| Parameter | Value |
|-----------|-------|
| Art Style | Placeholder capsules/cubes (NPCs: blue, stockpiles: brown) |
| Max NPCs | 20 (Phase 2 limit) |
| NPC Carry Capacity | 4 slots (16 items max per type) |
| Task Priority Order | Defense > Construction > Hauling > Mining |
| NPC Arrival Rate | 1 per settlement milestone |
| Morale Range | 0-100 |

---

## Task 1: NPC Core System

**Goal:** Create the foundational NPC controller, state machine, and needs system

### 1.1 Create Assembly Definition

**File:** `Assets/_Project/Scripts/NPC/VoxelRPG.NPC.asmdef`

```json
{
    "name": "VoxelRPG.NPC",
    "rootNamespace": "VoxelRPG.NPC",
    "references": [
        "VoxelRPG.Core",
        "VoxelRPG.Combat",
        "VoxelRPG.Voxel",
        "Unity.AI.Navigation"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### 1.2 NPCDefinition ScriptableObject

**File:** `Assets/_Project/Scripts/NPC/NPCDefinition.cs`

```csharp
using UnityEngine;

namespace VoxelRPG.NPC
{
    /// <summary>
    /// Base NPC stats and configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNPC", menuName = "VoxelRPG/NPC/NPC Definition")]
    public class NPCDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 3.5f;
        [SerializeField] private float _runSpeed = 6f;

        [Header("Stats")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _maxHunger = 100f;
        [SerializeField] private float _maxEnergy = 100f;

        [Header("Work")]
        [SerializeField] private float _workSpeed = 1f;
        [SerializeField] private int _carryCapacity = 4;

        [Header("Needs Drain Rates (per minute)")]
        [SerializeField] private float _hungerDrainRate = 0.5f;
        [SerializeField] private float _energyDrainRate = 0.2f;

        [Header("Visuals")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Color _skinColor = Color.white;

        public string Id => _id;
        public string DisplayName => _displayName;
        public float MoveSpeed => _moveSpeed;
        public float RunSpeed => _runSpeed;
        public float MaxHealth => _maxHealth;
        public float MaxHunger => _maxHunger;
        public float MaxEnergy => _maxEnergy;
        public float WorkSpeed => _workSpeed;
        public int CarryCapacity => _carryCapacity;
        public float HungerDrainRate => _hungerDrainRate;
        public float EnergyDrainRate => _energyDrainRate;
        public GameObject Prefab => _prefab;
        public Color SkinColor => _skinColor;
    }
}
```

### 1.3 INPCController Interface

**File:** `Assets/_Project/Scripts/NPC/INPCController.cs`

```csharp
using UnityEngine;

namespace VoxelRPG.NPC
{
    /// <summary>
    /// Interface for NPC control. Multiplayer-ready with request pattern.
    /// </summary>
    public interface INPCController
    {
        string Id { get; }
        NPCDefinition Definition { get; }
        Vector3 Position { get; }
        NPCStateMachine StateMachine { get; }
        NPCNeeds Needs { get; }
        NPCInventory Inventory { get; }

        void Initialize(NPCDefinition definition, string id);
        void RequestMoveTo(Vector3 destination);
        void RequestStopMovement();
        bool IsAtPosition(Vector3 position, float threshold = 0.5f);
    }
}
```

### 1.4 NPCController

**File:** `Assets/_Project/Scripts/NPC/NPCController.cs`

```csharp
using System;
using UnityEngine;
using UnityEngine.AI;
using VoxelRPG.Combat;

namespace VoxelRPG.NPC
{
    /// <summary>
    /// Core NPC controller handling movement and coordination.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCController : MonoBehaviour, INPCController, IDamageable
    {
        [Header("References")]
        [SerializeField] private NPCDefinition _definition;
        [SerializeField] private NPCStateMachine _stateMachine;
        [SerializeField] private NPCNeeds _needs;
        [SerializeField] private NPCInventory _inventory;

        private NavMeshAgent _agent;
        private string _id;
        private bool _isAlive = true;

        #region Properties

        public string Id => _id;
        public NPCDefinition Definition => _definition;
        public Vector3 Position => transform.position;
        public NPCStateMachine StateMachine => _stateMachine;
        public NPCNeeds Needs => _needs;
        public NPCInventory Inventory => _inventory;
        public NavMeshAgent Agent => _agent;

        #endregion

        #region IDamageable

        public float CurrentHealth => _needs.CurrentHealth;
        public float MaxHealth => _definition.MaxHealth;
        public bool IsAlive => _isAlive;

        public float TakeDamage(float damage, GameObject source = null)
        {
            if (!_isAlive) return 0f;

            float actual = _needs.TakeDamage(damage);

            if (_needs.CurrentHealth <= 0)
            {
                Die();
            }
            else if (source != null)
            {
                // Notify state machine of attack for flee response
                _stateMachine.OnAttacked(source);
            }

            return actual;
        }

        public float Heal(float amount)
        {
            return _needs.Heal(amount);
        }

        #endregion

        #region Events

        public event Action<NPCController> OnNPCDeath;
        public event Action OnReachedDestination;

        #endregion

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();

            if (_stateMachine == null)
                _stateMachine = GetComponent<NPCStateMachine>();
            if (_needs == null)
                _needs = GetComponent<NPCNeeds>();
            if (_inventory == null)
                _inventory = GetComponent<NPCInventory>();
        }

        public void Initialize(NPCDefinition definition, string id)
        {
            _definition = definition;
            _id = id;

            _agent.speed = definition.MoveSpeed;

            _needs.Initialize(definition);
            _inventory.Initialize(definition.CarryCapacity);
            _stateMachine.Initialize(this);
        }

        private void Update()
        {
            if (!_isAlive) return;

            // Check if reached destination
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                if (!_agent.hasPath || _agent.velocity.sqrMagnitude < 0.01f)
                {
                    OnReachedDestination?.Invoke();
                }
            }
        }

        public void RequestMoveTo(Vector3 destination)
        {
            if (!_isAlive) return;
            _agent.SetDestination(destination);
        }

        public void RequestStopMovement()
        {
            _agent.ResetPath();
        }

        public bool IsAtPosition(Vector3 position, float threshold = 0.5f)
        {
            return Vector3.Distance(transform.position, position) <= threshold;
        }

        private void Die()
        {
            _isAlive = false;
            _stateMachine.ChangeState(new DeadState());
            OnNPCDeath?.Invoke(this);
        }

        #region Save/Load

        public NPCSaveData GetSaveData()
        {
            return new NPCSaveData
            {
                Id = _id,
                DefinitionId = _definition.Id,
                Position = transform.position,
                Rotation = transform.rotation.eulerAngles,
                NeedsData = _needs.GetSaveData(),
                InventoryData = _inventory.GetSaveData(),
                CurrentStateName = _stateMachine.CurrentState?.GetType().Name ?? "IdleState",
                IsAlive = _isAlive
            };
        }

        #endregion
    }

    [Serializable]
    public class NPCSaveData
    {
        public string Id;
        public string DefinitionId;
        public Vector3 Position;
        public Vector3 Rotation;
        public NPCNeedsSaveData NeedsData;
        public NPCInventorySaveData InventoryData;
        public string CurrentStateName;
        public bool IsAlive;
    }
}
```

### 1.5 NPCStateMachine

**File:** `Assets/_Project/Scripts/NPC/NPCStateMachine.cs`

```csharp
using System;
using UnityEngine;

namespace VoxelRPG.NPC
{
    /// <summary>
    /// Manages NPC state transitions and current behavior.
    /// </summary>
    public class NPCStateMachine : MonoBehaviour
    {
        private NPCController _npc;
        private INPCState _currentState;
        private INPCState _previousState;

        public INPCState CurrentState => _currentState;
        public INPCState PreviousState => _previousState;

        public event Action<INPCState, INPCState> OnStateChanged;

        public void Initialize(NPCController npc)
        {
            _npc = npc;
            ChangeState(new IdleState());
        }

        private void Update()
        {
            _currentState?.Update(_npc);
        }

        public void ChangeState(INPCState newState)
        {
            if (newState == null) return;

            _previousState = _currentState;
            _currentState?.Exit(_npc);

            _currentState = newState;
            _currentState.Enter(_npc);

            OnStateChanged?.Invoke(_previousState, _currentState);
        }

        public void RevertToPreviousState()
        {
            if (_previousState != null)
            {
                ChangeState(_previousState);
            }
        }

        public void OnAttacked(GameObject attacker)
        {
            // Interrupt current state and flee
            if (_currentState is not FleeState && _currentState is not DeadState)
            {
                var fleeState = new FleeState(attacker.transform);
                ChangeState(fleeState);
            }
        }
    }

    /// <summary>
    /// Base interface for NPC states.
    /// </summary>
    public interface INPCState
    {
        string Name { get; }
        void Enter(NPCController npc);
        void Update(NPCController npc);
        void Exit(NPCController npc);
    }
}
```

### 1.6 NPCNeeds

**File:** `Assets/_Project/Scripts/NPC/NPCNeeds.cs`

```csharp
using System;
using UnityEngine;

namespace VoxelRPG.NPC
{
    /// <summary>
    /// Manages NPC hunger, energy, morale, and health.
    /// </summary>
    public class NPCNeeds : MonoBehaviour
    {
        private NPCDefinition _definition;

        private float _currentHealth;
        private float _currentHunger;
        private float _currentEnergy;
        private float _currentMorale = 50f;

        #region Properties

        public float CurrentHealth => _currentHealth;
        public float CurrentHunger => _currentHunger;
        public float CurrentEnergy => _currentEnergy;
        public float CurrentMorale => _currentMorale;

        public float HealthNormalized => _definition != null ? _currentHealth / _definition.MaxHealth : 0f;
        public float HungerNormalized => _definition != null ? _currentHunger / _definition.MaxHunger : 0f;
        public float EnergyNormalized => _definition != null ? _currentEnergy / _definition.MaxEnergy : 0f;
        public float MoraleNormalized => _currentMorale / 100f;

        public bool IsHungry => _currentHunger < 30f;
        public bool IsTired => _currentEnergy < 20f;
        public bool IsUnhappy => _currentMorale < 30f;

        #endregion

        #region Events

        public event Action<float> OnHealthChanged;
        public event Action<float> OnHungerChanged;
        public event Action<float> OnEnergyChanged;
        public event Action<float> OnMoraleChanged;

        #endregion

        public void Initialize(NPCDefinition definition)
        {
            _definition = definition;
            _currentHealth = definition.MaxHealth;
            _currentHunger = definition.MaxHunger;
            _currentEnergy = definition.MaxEnergy;
            _currentMorale = 50f;
        }

        private void Update()
        {
            if (_definition == null) return;

            float deltaMinutes = Time.deltaTime / 60f;

            // Drain hunger
            float hungerDrain = _definition.HungerDrainRate * deltaMinutes;
            SetHunger(_currentHunger - hungerDrain);

            // Drain energy (faster if hungry)
            float energyMultiplier = IsHungry ? 1.5f : 1f;
            float energyDrain = _definition.EnergyDrainRate * energyMultiplier * deltaMinutes;
            SetEnergy(_currentEnergy - energyDrain);

            // Starvation damage
            if (_currentHunger <= 0)
            {
                TakeDamage(1f * Time.deltaTime);
            }
        }

        public float TakeDamage(float damage)
        {
            float actual = Mathf.Min(damage, _currentHealth);
            _currentHealth -= actual;
            _currentHealth = Mathf.Max(0, _currentHealth);
            OnHealthChanged?.Invoke(_currentHealth);
            return actual;
        }

        public float Heal(float amount)
        {
            if (_definition == null) return 0f;
            float actual = Mathf.Min(amount, _definition.MaxHealth - _currentHealth);
            _currentHealth += actual;
            OnHealthChanged?.Invoke(_currentHealth);
            return actual;
        }

        public void SetHunger(float value)
        {
            _currentHunger = Mathf.Clamp(value, 0f, _definition?.MaxHunger ?? 100f);
            OnHungerChanged?.Invoke(_currentHunger);
        }

        public void SetEnergy(float value)
        {
            _currentEnergy = Mathf.Clamp(value, 0f, _definition?.MaxEnergy ?? 100f);
            OnEnergyChanged?.Invoke(_currentEnergy);
        }

        public void SetMorale(float value)
        {
            _currentMorale = Mathf.Clamp(value, 0f, 100f);
            OnMoraleChanged?.Invoke(_currentMorale);
        }

        public void ModifyMorale(float delta)
        {
            SetMorale(_currentMorale + delta);
        }

        public void Eat(float nutritionValue)
        {
            SetHunger(_currentHunger + nutritionValue);
            ModifyMorale(2f); // Eating improves morale slightly
        }

        public void Rest(float restValue)
        {
            SetEnergy(_currentEnergy + restValue);
        }

        #region Save/Load

        public NPCNeedsSaveData GetSaveData()
        {
            return new NPCNeedsSaveData
            {
                CurrentHealth = _currentHealth,
                CurrentHunger = _currentHunger,
                CurrentEnergy = _currentEnergy,
                CurrentMorale = _currentMorale
            };
        }

        public void LoadSaveData(NPCNeedsSaveData data)
        {
            _currentHealth = data.CurrentHealth;
            _currentHunger = data.CurrentHunger;
            _currentEnergy = data.CurrentEnergy;
            _currentMorale = data.CurrentMorale;
        }

        #endregion
    }

    [Serializable]
    public class NPCNeedsSaveData
    {
        public float CurrentHealth;
        public float CurrentHunger;
        public float CurrentEnergy;
        public float CurrentMorale;
    }
}
```

### 1.7 NPCInventory

**File:** `Assets/_Project/Scripts/NPC/NPCInventory.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelRPG.Core.Items;

namespace VoxelRPG.NPC
{
    /// <summary>
    /// NPC carrying capacity and item management.
    /// </summary>
    public class NPCInventory : MonoBehaviour
    {
        private Inventory _inventory;

        public int SlotCount => _inventory?.SlotCount ?? 0;
        public bool HasItems => _inventory != null && _inventory.GetOccupiedSlotCount() > 0;
        public bool HasSpace => _inventory != null && _inventory.GetEmptySlotCount() > 0;

        public event Action OnInventoryChanged;

        public void Initialize(int capacity)
        {
            _inventory = new Inventory(capacity);
            _inventory.OnInventoryChanged += () => OnInventoryChanged?.Invoke();
        }

        public bool TryAddItem(ItemDefinition item, int amount)
        {
            return _inventory?.TryAddItem(item, amount) ?? false;
        }

        public bool TryRemoveItem(ItemDefinition item, int amount)
        {
            return _inventory?.TryRemoveItem(item, amount) ?? false;
        }

        public bool HasItem(ItemDefinition item, int amount = 1)
        {
            return _inventory?.HasItem(item, amount) ?? false;
        }

        public int GetItemCount(ItemDefinition item)
        {
            return _inventory?.GetItemCount(item) ?? 0;
        }

        public IEnumerable<ItemStack> GetAllItems()
        {
            if (_inventory == null) yield break;
            foreach (var stack in _inventory.GetAllItems())
            {
                yield return stack;
            }
        }

        public void DropAllItems(Vector3 position)
        {
            // TODO: Spawn ItemDrop entities at position
            _inventory?.Clear();
        }

        #region Save/Load

        public NPCInventorySaveData GetSaveData()
        {
            return new NPCInventorySaveData
            {
                InventoryData = _inventory?.GetSaveData()
            };
        }

        public void LoadSaveData(NPCInventorySaveData data, Func<string, ItemDefinition> itemResolver)
        {
            _inventory?.LoadSaveData(data.InventoryData, itemResolver);
        }

        #endregion
    }

    [Serializable]
    public class NPCInventorySaveData
    {
        public InventorySaveData InventoryData;
    }
}
```

### Assets to Create

```
Assets/_Project/ScriptableObjects/NPCs/
├── Settler_Base.asset (default settler type)
└── Settler_Miner.asset (mining-focused settler)

Assets/_Project/Prefabs/NPCs/
└── NPC_Settler.prefab
    ├── Capsule (MeshFilter + MeshRenderer) - Blue material
    ├── NPCController component
    ├── NPCStateMachine component
    ├── NPCNeeds component
    ├── NPCInventory component
    ├── NavMeshAgent component
    └── CapsuleCollider
```

### Estimated Lines: ~450 lines

### Dependencies: None (first task)

---

## Task 2: NPC State Implementations

**Goal:** Create all required NPC states for worker behavior

### 2.1 IdleState

**File:** `Assets/_Project/Scripts/NPC/States/IdleState.cs`

```csharp
using UnityEngine;

namespace VoxelRPG.NPC.States
{
    /// <summary>
    /// Default state when NPC has no task. Looks for work.
    /// </summary>
    public class IdleState : INPCState
    {
        public string Name => "Idle";

        private float _taskSearchTimer;
        private const float TASK_SEARCH_INTERVAL = 1f;

        public void Enter(NPCController npc)
        {
            npc.RequestStopMovement();
            _taskSearchTimer = 0f;
        }

        public void Update(NPCController npc)
        {
            // Check needs first
            if (npc.Needs.IsHungry)
            {
                npc.StateMachine.ChangeState(new SeekFoodState());
                return;
            }

            if (npc.Needs.IsTired)
            {
                npc.StateMachine.ChangeState(new RestingState());
                return;
            }

            // Periodically search for tasks
            _taskSearchTimer += Time.deltaTime;
            if (_taskSearchTimer >= TASK_SEARCH_INTERVAL)
            {
                _taskSearchTimer = 0f;
                npc.StateMachine.ChangeState(new SeekingTaskState());
            }
        }

        public void Exit(NPCController npc) { }
    }
}
```

### 2.2 SeekingTaskState

**File:** `Assets/_Project/Scripts/NPC/States/SeekingTaskState.cs`

```csharp
using VoxelRPG.NPC.Tasks;
using VoxelRPG.Core;

namespace VoxelRPG.NPC.States
{
    /// <summary>
    /// State where NPC looks for available work.
    /// </summary>
    public class SeekingTaskState : INPCState
    {
        public string Name => "SeekingTask";

        public void Enter(NPCController npc)
        {
            // Try to claim a task
            var taskManager = ServiceLocator.TryGet<ITaskManager>(out var tm) ? tm : null;
            if (taskManager != null)
            {
                var task = taskManager.FindBestTaskFor(npc);
                if (task != null)
                {
                    taskManager.RequestTaskClaim(npc.Id, task.Id);
                    npc.StateMachine.ChangeState(new TravelingState(task));
                    return;
                }
            }

            // No task found, return to idle
            npc.StateMachine.ChangeState(new IdleState());
        }

        public void Update(NPCController npc) { }

        public void Exit(NPCController npc) { }
    }
}
```

### 2.3 TravelingState

**File:** `Assets/_Project/Scripts/NPC/States/TravelingState.cs`

```csharp
using UnityEngine;
using VoxelRPG.NPC.Tasks;

namespace VoxelRPG.NPC.States
{
    /// <summary>
    /// State where NPC moves to task location.
    /// </summary>
    public class TravelingState : INPCState
    {
        public string Name => "Traveling";

        private readonly ITask _task;
        private bool _hasArrived;

        public TravelingState(ITask task)
        {
            _task = task;
        }

        public void Enter(NPCController npc)
        {
            _hasArrived = false;
            npc.OnReachedDestination += HandleArrival;
            npc.RequestMoveTo(_task.TargetPosition);
        }

        public void Update(NPCController npc)
        {
            if (_hasArrived)
            {
                // Transition to appropriate work state
                var workState = _task.GetWorkState();
                npc.StateMachine.ChangeState(workState);
            }
        }

        public void Exit(NPCController npc)
        {
            npc.OnReachedDestination -= HandleArrival;
        }

        private void HandleArrival()
        {
            _hasArrived = true;
        }
    }
}
```

### 2.4 MiningState

**File:** `Assets/_Project/Scripts/NPC/States/MiningState.cs`

```csharp
using UnityEngine;
using VoxelRPG.NPC.Tasks;
using VoxelRPG.Core;
using VoxelRPG.Voxel;

namespace VoxelRPG.NPC.States
{
    /// <summary>
    /// State where NPC mines blocks.
    /// </summary>
    public class MiningState : INPCState
    {
        public string Name => "Mining";

        private readonly MiningTask _task;
        private float _miningProgress;

        public MiningState(MiningTask task)
        {
            _task = task;
        }

        public void Enter(NPCController npc)
        {
            _miningProgress = 0f;
        }

        public void Update(NPCController npc)
        {
            // Validate task still valid
            if (!_task.IsStillValid())
            {
                _task.Cancel(TaskCancelReason.TargetRemoved);
                npc.StateMachine.ChangeState(new IdleState());
                return;
            }

            // Progress mining
            float workRate = npc.Definition.WorkSpeed * Time.deltaTime;
            _miningProgress += workRate;

            if (_miningProgress >= _task.MiningDuration)
            {
                // Complete mining
                var voxelWorld = ServiceLocator.TryGet<IVoxelWorld>(out var vw) ? vw : null;
                if (voxelWorld != null)
                {
                    var blockType = voxelWorld.GetBlock(_task.TargetBlock);

                    // Add dropped item to NPC inventory
                    if (blockType.DroppedItem != null && npc.Inventory.HasSpace)
                    {
                        npc.Inventory.TryAddItem(blockType.DroppedItem, blockType.DropAmount);
                    }

                    // Remove block
                    voxelWorld.RequestBlockChange(_task.TargetBlock, null);
                }

                _task.Complete();

                // Check if we need to haul items
                if (npc.Inventory.HasItems)
                {
                    npc.StateMachine.ChangeState(new SeekingTaskState()); // Will find haul task
                }
                else
                {
                    npc.StateMachine.ChangeState(new IdleState());
                }
            }
        }

        public void Exit(NPCController npc) { }
    }
}
```

### 2.5 HaulingState

**File:** `Assets/_Project/Scripts/NPC/States/HaulingState.cs`

```csharp
using VoxelRPG.NPC.Tasks;
using VoxelRPG.Core;

namespace VoxelRPG.NPC.States
{
    /// <summary>
    /// State where NPC picks up or delivers items.
    /// </summary>
    public class HaulingState : INPCState
    {
        public string Name => "Hauling";

        private readonly HaulTask _task;
        private HaulPhase _phase;

        private enum HaulPhase { PickingUp, Delivering }

        public HaulingState(HaulTask task)
        {
            _task = task;
            _phase = HaulPhase.PickingUp;
        }

        public void Enter(NPCController npc)
        {
            // Already at pickup location (TravelingState got us here)
            PickUpItems(npc);
        }

        public void Update(NPCController npc)
        {
            if (_phase == HaulPhase.Delivering && npc.IsAtPosition(_task.DestinationPosition))
            {
                DeliverItems(npc);
            }
        }

        public void Exit(NPCController npc) { }

        private void PickUpItems(NPCController npc)
        {
            // Get items from source (ItemDrop or Stockpile)
            if (_task.Source is IStockpile stockpile)
            {
                var items = stockpile.TryWithdraw(_task.ResourceType, _task.Amount);
                if (items.Amount > 0)
                {
                    npc.Inventory.TryAddItem(items.Item, items.Amount);
                }
            }

            // Move to destination
            _phase = HaulPhase.Delivering;
            npc.RequestMoveTo(_task.DestinationPosition);
        }

        private void DeliverItems(NPCController npc)
        {
            // Deliver to stockpile
            if (_task.Destination is IStockpile stockpile)
            {
                var count = npc.Inventory.GetItemCount(_task.ResourceType);
                if (stockpile.TryDeposit(_task.ResourceType, count))
                {
                    npc.Inventory.TryRemoveItem(_task.ResourceType, count);
                }
            }

            _task.Complete();
            npc.StateMachine.ChangeState(new IdleState());
        }
    }
}
```

### 2.6 BuildingState

**File:** `Assets/_Project/Scripts/NPC/States/BuildingState.cs`

```csharp
using UnityEngine;
using VoxelRPG.NPC.Tasks;
using VoxelRPG.Core;
using VoxelRPG.Voxel;

namespace VoxelRPG.NPC.States
{
    /// <summary>
    /// State where NPC places blocks for construction.
    /// </summary>
    public class BuildingState : INPCState
    {
        public string Name => "Building";

        private readonly BuildTask _task;
        private float _buildProgress;

        public BuildingState(BuildTask task)
        {
            _task = task;
        }

        public void Enter(NPCController npc)
        {
            _buildProgress = 0f;

            // Consume required materials from inventory
            if (!npc.Inventory.HasItem(_task.RequiredItem, 1))
            {
                _task.Fail(TaskFailureReason.MissingMaterials);
                npc.StateMachine.ChangeState(new IdleState());
            }
        }

        public void Update(NPCController npc)
        {
            float workRate = npc.Definition.WorkSpeed * Time.deltaTime;
            _buildProgress += workRate;

            if (_buildProgress >= _task.BuildDuration)
            {
                // Consume material
                npc.Inventory.TryRemoveItem(_task.RequiredItem, 1);

                // Place block
                var voxelWorld = ServiceLocator.TryGet<IVoxelWorld>(out var vw) ? vw : null;
                voxelWorld?.RequestBlockChange(_task.TargetPosition, _task.BlockToPlace);

                _task.Complete();
                npc.StateMachine.ChangeState(new IdleState());
            }
        }

        public void Exit(NPCController npc) { }
    }
}
```

### 2.7 RestingState

**File:** `Assets/_Project/Scripts/NPC/States/RestingState.cs`

```csharp
using UnityEngine;

namespace VoxelRPG.NPC.States
{
    /// <summary>
    /// State where NPC recovers energy.
    /// </summary>
    public class RestingState : INPCState
    {
        public string Name => "Resting";

        private const float REST_RATE = 10f; // Energy per minute
        private const float ENERGY_THRESHOLD = 80f;

        public void Enter(NPCController npc)
        {
            npc.RequestStopMovement();
        }

        public void Update(NPCController npc)
        {
            float restAmount = REST_RATE * (Time.deltaTime / 60f);
            npc.Needs.Rest(restAmount);

            if (npc.Needs.CurrentEnergy >= ENERGY_THRESHOLD)
            {
                npc.StateMachine.ChangeState(new IdleState());
            }
        }

        public void Exit(NPCController npc) { }
    }
}
```

### 2.8 FleeState

**File:** `Assets/_Project/Scripts/NPC/States/FleeState.cs`

```csharp
using UnityEngine;

namespace VoxelRPG.NPC.States
{
    /// <summary>
    /// State where NPC runs from danger.
    /// </summary>
    public class FleeState : INPCState
    {
        public string Name => "Fleeing";

        private readonly Transform _threat;
        private const float FLEE_DISTANCE = 20f;
        private const float SAFE_DISTANCE = 25f;

        public FleeState(Transform threat = null)
        {
            _threat = threat;
        }

        public void Enter(NPCController npc)
        {
            // Calculate flee direction
            Vector3 fleeDirection = _threat != null
                ? (npc.Position - _threat.position).normalized
                : Random.insideUnitSphere;

            fleeDirection.y = 0;
            Vector3 fleePosition = npc.Position + fleeDirection * FLEE_DISTANCE;

            // Set faster speed
            npc.Agent.speed = npc.Definition.RunSpeed;
            npc.RequestMoveTo(fleePosition);
        }

        public void Update(NPCController npc)
        {
            // Check if safe
            if (_threat == null || Vector3.Distance(npc.Position, _threat.position) >= SAFE_DISTANCE)
            {
                npc.StateMachine.ChangeState(new IdleState());
            }
        }

        public void Exit(NPCController npc)
        {
            // Restore normal speed
            npc.Agent.speed = npc.Definition.MoveSpeed;
        }
    }
}
```

### 2.9 DeadState

**File:** `Assets/_Project/Scripts/NPC/States/DeadState.cs`

```csharp
namespace VoxelRPG.NPC.States
{
    /// <summary>
    /// Terminal state when NPC dies.
    /// </summary>
    public class DeadState : INPCState
    {
        public string Name => "Dead";

        public void Enter(NPCController npc)
        {
            npc.RequestStopMovement();
            npc.Inventory.DropAllItems(npc.Position);
            // TODO: Play death animation, schedule cleanup
        }

        public void Update(NPCController npc) { }

        public void Exit(NPCController npc) { }
    }
}
```

### 2.10 SeekFoodState

**File:** `Assets/_Project/Scripts/NPC/States/SeekFoodState.cs`

```csharp
using VoxelRPG.Core;

namespace VoxelRPG.NPC.States
{
    /// <summary>
    /// State where NPC looks for food.
    /// </summary>
    public class SeekFoodState : INPCState
    {
        public string Name => "SeekingFood";

        public void Enter(NPCController npc)
        {
            // Check if NPC has food in inventory
            // TODO: Check for food items

            // Otherwise, find stockpile with food
            var stockpileManager = ServiceLocator.TryGet<IStockpileManager>(out var sm) ? sm : null;
            if (stockpileManager != null)
            {
                // TODO: Find food stockpile and travel there
            }

            // Can't find food, return to idle (will take starvation damage)
            npc.StateMachine.ChangeState(new IdleState());
        }

        public void Update(NPCController npc) { }

        public void Exit(NPCController npc) { }
    }
}
```

### Estimated Lines: ~400 lines

### Dependencies: Task 1

---

## Task 3: Task System

**Goal:** Create the task management system for NPC work assignment

### 3.1 ITask Interface

**File:** `Assets/_Project/Scripts/NPC/Tasks/ITask.cs`

```csharp
using UnityEngine;

namespace VoxelRPG.NPC.Tasks
{
    /// <summary>
    /// Base task contract for all NPC work items.
    /// </summary>
    public interface ITask
    {
        string Id { get; }
        TaskType Type { get; }
        TaskPriority Priority { get; }
        TaskStatus Status { get; }
        Vector3 TargetPosition { get; }

        bool CanBeClaimedBy(NPCController npc);
        bool IsStillValid();
        INPCState GetWorkState();

        void Claim(string npcId);
        void Release();
        void Suspend();
        void Resume();
        void Complete();
        void Cancel(TaskCancelReason reason);
        void Fail(TaskFailureReason reason);
    }

    public enum TaskType
    {
        Mining,
        Hauling,
        Building,
        Gathering,
        Guarding
    }

    public enum TaskPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    public enum TaskStatus
    {
        Pending,
        Claimed,
        InProgress,
        Suspended,
        Completed,
        Cancelled,
        Failed
    }

    public enum TaskCancelReason
    {
        UserCancelled,
        TargetRemoved,
        PathBlocked,
        NPCDied,
        InvalidatedWhileFleeing
    }

    public enum TaskFailureReason
    {
        PathfindingFailed,
        NoStorageSpace,
        MissingMaterials,
        Timeout
    }
}
```

### 3.2 BaseTask

**File:** `Assets/_Project/Scripts/NPC/Tasks/BaseTask.cs`

```csharp
using System;
using UnityEngine;

namespace VoxelRPG.NPC.Tasks
{
    /// <summary>
    /// Base implementation for all tasks.
    /// </summary>
    public abstract class BaseTask : ITask
    {
        public string Id { get; }
        public abstract TaskType Type { get; }
        public TaskPriority Priority { get; protected set; }
        public TaskStatus Status { get; protected set; }
        public abstract Vector3 TargetPosition { get; }

        public string ClaimedByNpcId { get; protected set; }
        public bool CanBeReassigned { get; protected set; } = true;

        public event Action<ITask> OnStatusChanged;

        protected BaseTask(TaskPriority priority = TaskPriority.Normal)
        {
            Id = Guid.NewGuid().ToString();
            Priority = priority;
            Status = TaskStatus.Pending;
        }

        public virtual bool CanBeClaimedBy(NPCController npc)
        {
            return Status == TaskStatus.Pending;
        }

        public abstract bool IsStillValid();
        public abstract INPCState GetWorkState();

        public void Claim(string npcId)
        {
            if (Status != TaskStatus.Pending) return;
            ClaimedByNpcId = npcId;
            Status = TaskStatus.Claimed;
            OnStatusChanged?.Invoke(this);
        }

        public void Release()
        {
            ClaimedByNpcId = null;
            Status = TaskStatus.Pending;
            OnStatusChanged?.Invoke(this);
        }

        public void Suspend()
        {
            if (Status == TaskStatus.InProgress || Status == TaskStatus.Claimed)
            {
                Status = TaskStatus.Suspended;
                OnStatusChanged?.Invoke(this);
            }
        }

        public void Resume()
        {
            if (Status == TaskStatus.Suspended)
            {
                Status = ClaimedByNpcId != null ? TaskStatus.Claimed : TaskStatus.Pending;
                OnStatusChanged?.Invoke(this);
            }
        }

        public void Complete()
        {
            Status = TaskStatus.Completed;
            OnStatusChanged?.Invoke(this);
        }

        public void Cancel(TaskCancelReason reason)
        {
            Status = TaskStatus.Cancelled;
            OnStatusChanged?.Invoke(this);
        }

        public void Fail(TaskFailureReason reason)
        {
            Status = TaskStatus.Failed;
            OnStatusChanged?.Invoke(this);
        }
    }
}
```

### 3.3 MiningTask

**File:** `Assets/_Project/Scripts/NPC/Tasks/MiningTask.cs`

```csharp
using UnityEngine;
using VoxelRPG.Core;
using VoxelRPG.Voxel;
using VoxelRPG.NPC.States;

namespace VoxelRPG.NPC.Tasks
{
    /// <summary>
    /// Task to mine a specific block.
    /// </summary>
    public class MiningTask : BaseTask
    {
        public override TaskType Type => TaskType.Mining;
        public override Vector3 TargetPosition => (Vector3)TargetBlock + Vector3.one * 0.5f;

        public Vector3Int TargetBlock { get; }
        public float MiningDuration { get; }

        public MiningTask(Vector3Int targetBlock, float miningDuration = 2f, TaskPriority priority = TaskPriority.Normal)
            : base(priority)
        {
            TargetBlock = targetBlock;
            MiningDuration = miningDuration;
        }

        public override bool IsStillValid()
        {
            var voxelWorld = ServiceLocator.TryGet<IVoxelWorld>(out var vw) ? vw : null;
            if (voxelWorld == null) return false;

            var block = voxelWorld.GetBlock(TargetBlock);
            return block != null && block.IsSolid;
        }

        public override INPCState GetWorkState()
        {
            return new MiningState(this);
        }
    }
}
```

### 3.4 HaulTask

**File:** `Assets/_Project/Scripts/NPC/Tasks/HaulTask.cs`

```csharp
using UnityEngine;
using VoxelRPG.Core.Items;
using VoxelRPG.NPC.States;

namespace VoxelRPG.NPC.Tasks
{
    /// <summary>
    /// Task to transport items between locations.
    /// </summary>
    public class HaulTask : BaseTask
    {
        public override TaskType Type => TaskType.Hauling;
        public override Vector3 TargetPosition => SourcePosition;

        public object Source { get; }
        public object Destination { get; }
        public Vector3 SourcePosition { get; }
        public Vector3 DestinationPosition { get; }
        public ItemDefinition ResourceType { get; }
        public int Amount { get; }

        public HaulTask(
            object source, Vector3 sourcePos,
            object destination, Vector3 destPos,
            ItemDefinition resourceType, int amount,
            TaskPriority priority = TaskPriority.Normal)
            : base(priority)
        {
            Source = source;
            SourcePosition = sourcePos;
            Destination = destination;
            DestinationPosition = destPos;
            ResourceType = resourceType;
            Amount = amount;
        }

        public override bool IsStillValid()
        {
            // Validate source and destination still exist
            if (Source is IStockpile sourceStockpile)
            {
                return sourceStockpile.HasItem(ResourceType, Amount);
            }
            return true;
        }

        public override INPCState GetWorkState()
        {
            return new HaulingState(this);
        }

        public void RedirectTo(IStockpile newDestination)
        {
            // Update destination for stockpile full handling
        }
    }
}
```

### 3.5 BuildTask

**File:** `Assets/_Project/Scripts/NPC/Tasks/BuildTask.cs`

```csharp
using UnityEngine;
using VoxelRPG.Core.Items;
using VoxelRPG.Voxel;
using VoxelRPG.NPC.States;

namespace VoxelRPG.NPC.Tasks
{
    /// <summary>
    /// Task to place a block for construction.
    /// </summary>
    public class BuildTask : BaseTask
    {
        public override TaskType Type => TaskType.Building;
        public override Vector3 TargetPosition => (Vector3)_targetPosition + Vector3.one * 0.5f;

        private readonly Vector3Int _targetPosition;
        public BlockType BlockToPlace { get; }
        public ItemDefinition RequiredItem { get; }
        public float BuildDuration { get; }

        public BuildTask(
            Vector3Int targetPosition,
            BlockType blockToPlace,
            ItemDefinition requiredItem,
            float buildDuration = 1f,
            TaskPriority priority = TaskPriority.Normal)
            : base(priority)
        {
            _targetPosition = targetPosition;
            BlockToPlace = blockToPlace;
            RequiredItem = requiredItem;
            BuildDuration = buildDuration;
        }

        public override bool IsStillValid()
        {
            // Position should be empty (air)
            var voxelWorld = ServiceLocator.TryGet<IVoxelWorld>(out var vw) ? vw : null;
            if (voxelWorld == null) return false;

            var block = voxelWorld.GetBlock(_targetPosition);
            return block == null || !block.IsSolid;
        }

        public override INPCState GetWorkState()
        {
            return new BuildingState(this);
        }
    }
}
```

### 3.6 ITaskManager Interface

**File:** `Assets/_Project/Scripts/NPC/Tasks/ITaskManager.cs`

```csharp
using System.Collections.Generic;

namespace VoxelRPG.NPC.Tasks
{
    /// <summary>
    /// Task coordination interface. Multiplayer-ready with request pattern.
    /// </summary>
    public interface ITaskManager
    {
        void AddTask(ITask task);
        void RemoveTask(string taskId);
        void RequestTaskClaim(string npcId, string taskId);
        void ReportTaskProgress(string taskId, float progress);
        void ReportTaskComplete(string taskId);
        void ReportTaskFailed(string taskId, TaskFailureReason reason);
        ITask FindBestTaskFor(NPCController npc);
        ITask GetTaskForNPC(string npcId);
        IEnumerable<ITask> GetTasksAtPosition<T>(Vector3Int position) where T : ITask;
        void RequeueTask(ITask task);
    }
}
```

### 3.7 TaskManager

**File:** `Assets/_Project/Scripts/NPC/Tasks/TaskManager.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoxelRPG.NPC.Tasks
{
    /// <summary>
    /// Central task coordination for NPC work assignment.
    /// </summary>
    public class TaskManager : MonoBehaviour, ITaskManager
    {
        private readonly List<ITask> _tasks = new();
        private readonly Dictionary<string, ITask> _npcTasks = new();

        public event Action<ITask> OnTaskAdded;
        public event Action<ITask> OnTaskCompleted;
        public event Action<ITask> OnTaskFailed;

        public void AddTask(ITask task)
        {
            _tasks.Add(task);
            OnTaskAdded?.Invoke(task);
        }

        public void RemoveTask(string taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                _tasks.Remove(task);
            }
        }

        public void RequestTaskClaim(string npcId, string taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null && task.Status == TaskStatus.Pending)
            {
                task.Claim(npcId);
                _npcTasks[npcId] = task;
            }
        }

        public void ReportTaskProgress(string taskId, float progress)
        {
            // Track progress for UI/debugging
        }

        public void ReportTaskComplete(string taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.Complete();
                CleanupTask(task);
                OnTaskCompleted?.Invoke(task);
            }
        }

        public void ReportTaskFailed(string taskId, TaskFailureReason reason)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.Fail(reason);
                CleanupTask(task);
                OnTaskFailed?.Invoke(task);
            }
        }

        public ITask FindBestTaskFor(NPCController npc)
        {
            return _tasks
                .Where(t => t.Status == TaskStatus.Pending && t.CanBeClaimedBy(npc))
                .OrderByDescending(t => (int)t.Priority)
                .ThenBy(t => Vector3.Distance(npc.Position, t.TargetPosition))
                .FirstOrDefault();
        }

        public ITask GetTaskForNPC(string npcId)
        {
            return _npcTasks.TryGetValue(npcId, out var task) ? task : null;
        }

        public IEnumerable<ITask> GetTasksAtPosition<T>(Vector3Int position) where T : ITask
        {
            return _tasks
                .OfType<T>()
                .Where(t => Vector3Int.FloorToInt(t.TargetPosition) == position);
        }

        public void RequeueTask(ITask task)
        {
            task.Release();
            // Task remains in list, now pending again
        }

        private void CleanupTask(ITask task)
        {
            // Remove from NPC mapping
            var npcId = _npcTasks.FirstOrDefault(kvp => kvp.Value == task).Key;
            if (npcId != null)
            {
                _npcTasks.Remove(npcId);
            }

            // Remove completed/failed tasks
            if (task.Status == TaskStatus.Completed || task.Status == TaskStatus.Failed)
            {
                _tasks.Remove(task);
            }
        }

        #region Save/Load

        public TaskManagerSaveData GetSaveData()
        {
            return new TaskManagerSaveData
            {
                // Serialize pending tasks
            };
        }

        #endregion
    }

    [Serializable]
    public class TaskManagerSaveData
    {
        public List<TaskSaveData> PendingTasks;
    }

    [Serializable]
    public class TaskSaveData
    {
        public string Id;
        public string Type;
        public int Priority;
        public string Status;
        public Vector3 TargetPosition;
        public string ClaimedByNpcId;
    }
}
```

### 3.8 TaskInterruptHandler

**File:** `Assets/_Project/Scripts/NPC/Tasks/TaskInterruptHandler.cs`

```csharp
using UnityEngine;
using VoxelRPG.Core;

namespace VoxelRPG.NPC.Tasks
{
    /// <summary>
    /// Handles task interruption scenarios gracefully.
    /// </summary>
    public class TaskInterruptHandler : MonoBehaviour
    {
        [SerializeField] private TaskManager _taskManager;

        private void Awake()
        {
            if (_taskManager == null)
            {
                _taskManager = ServiceLocator.TryGet<ITaskManager>(out var tm)
                    ? tm as TaskManager
                    : null;
            }
        }

        public void HandleNPCDeath(NPCController npc)
        {
            var task = _taskManager.GetTaskForNPC(npc.Id);
            if (task == null) return;

            // Release any reserved resources
            if (task is HaulTask haulTask)
            {
                // Release stockpile reservations
                // Drop carried items
                if (npc.Inventory.HasItems)
                {
                    npc.Inventory.DropAllItems(npc.Position);
                }
            }

            // Return task to queue or cancel
            if (task is BaseTask baseTask && baseTask.CanBeReassigned)
            {
                _taskManager.RequeueTask(task);
            }
            else
            {
                task.Cancel(TaskCancelReason.NPCDied);
            }
        }

        public void HandleBlockRemoved(Vector3Int position)
        {
            // Cancel mining tasks at this position
            var miningTasks = _taskManager.GetTasksAtPosition<MiningTask>(position);
            foreach (var task in miningTasks)
            {
                task.Cancel(TaskCancelReason.TargetRemoved);
            }

            // Complete build tasks at this position (someone else placed the block)
            var buildTasks = _taskManager.GetTasksAtPosition<BuildTask>(position);
            foreach (var task in buildTasks)
            {
                task.Complete();
            }
        }

        public void HandleNPCAttacked(NPCController npc, float damage)
        {
            var task = _taskManager.GetTaskForNPC(npc.Id);
            if (task == null) return;

            // Suspend task while fleeing
            task.Suspend();
        }

        public void HandleNPCReachedSafety(NPCController npc)
        {
            var task = _taskManager.GetTaskForNPC(npc.Id);
            if (task == null) return;

            if (task.IsStillValid())
            {
                task.Resume();
            }
            else
            {
                task.Cancel(TaskCancelReason.InvalidatedWhileFleeing);
            }
        }
    }
}
```

### Estimated Lines: ~500 lines

### Dependencies: Task 1, Task 2

---

## Task 4: Stockpile System

**Goal:** Create storage zones for resources

### 4.1 IStockpile Interface

**File:** `Assets/_Project/Scripts/Building/Stockpiles/IStockpile.cs`

```csharp
using VoxelRPG.Core.Items;

namespace VoxelRPG.Building
{
    /// <summary>
    /// Storage zone interface for resources.
    /// </summary>
    public interface IStockpile
    {
        string Id { get; }
        UnityEngine.Vector3 Position { get; }
        int TotalSlots { get; }
        int UsedSlots { get; }
        int AvailableSlots { get; }

        bool HasItem(ItemDefinition item, int amount = 1);
        int GetItemCount(ItemDefinition item);
        bool TryDeposit(ItemDefinition item, int amount);
        ItemStack TryWithdraw(ItemDefinition item, int amount);
        bool HasSpaceFor(ItemDefinition item, int amount);

        bool AcceptsItem(ItemDefinition item);
        void SetFilter(ResourceFilter filter);
    }
}
```

### 4.2 Stockpile

**File:** `Assets/_Project/Scripts/Building/Stockpiles/Stockpile.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelRPG.Core.Items;

namespace VoxelRPG.Building
{
    /// <summary>
    /// Storage zone for resources.
    /// </summary>
    public class Stockpile : MonoBehaviour, IStockpile
    {
        [Header("Configuration")]
        [SerializeField] private int _slotCount = 16;
        [SerializeField] private ResourceFilter _filter;

        private string _id;
        private readonly List<StockpileSlot> _slots = new();
        private readonly Dictionary<string, int> _reservations = new();

        #region Properties

        public string Id => _id;
        public Vector3 Position => transform.position;
        public int TotalSlots => _slotCount;
        public int UsedSlots => _slots.FindAll(s => !s.IsEmpty).Count;
        public int AvailableSlots => _slotCount - UsedSlots;

        #endregion

        #region Events

        public event Action<IStockpile> OnContentsChanged;

        #endregion

        private void Awake()
        {
            _id = Guid.NewGuid().ToString();
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            _slots.Clear();
            for (int i = 0; i < _slotCount; i++)
            {
                _slots.Add(new StockpileSlot());
            }
        }

        public bool HasItem(ItemDefinition item, int amount = 1)
        {
            return GetItemCount(item) >= amount;
        }

        public int GetItemCount(ItemDefinition item)
        {
            int count = 0;
            foreach (var slot in _slots)
            {
                if (slot.Item == item)
                {
                    count += slot.Amount;
                }
            }
            return count;
        }

        public bool TryDeposit(ItemDefinition item, int amount)
        {
            if (!AcceptsItem(item)) return false;
            if (!HasSpaceFor(item, amount)) return false;

            int remaining = amount;

            // Stack with existing
            foreach (var slot in _slots)
            {
                if (slot.Item == item && !slot.IsFull)
                {
                    int toAdd = Mathf.Min(remaining, slot.SpaceRemaining);
                    slot.AddAmount(toAdd);
                    remaining -= toAdd;
                    if (remaining <= 0) break;
                }
            }

            // Use empty slots
            foreach (var slot in _slots)
            {
                if (slot.IsEmpty && remaining > 0)
                {
                    int toAdd = Mathf.Min(remaining, item.MaxStackSize);
                    slot.SetContents(item, toAdd);
                    remaining -= toAdd;
                    if (remaining <= 0) break;
                }
            }

            OnContentsChanged?.Invoke(this);
            return remaining == 0;
        }

        public ItemStack TryWithdraw(ItemDefinition item, int amount)
        {
            int available = GetItemCount(item);
            int toWithdraw = Mathf.Min(amount, available);

            if (toWithdraw <= 0) return ItemStack.Empty;

            int remaining = toWithdraw;

            foreach (var slot in _slots)
            {
                if (slot.Item == item && remaining > 0)
                {
                    int toRemove = Mathf.Min(remaining, slot.Amount);
                    slot.RemoveAmount(toRemove);
                    remaining -= toRemove;
                }
            }

            OnContentsChanged?.Invoke(this);
            return new ItemStack(item, toWithdraw);
        }

        public bool HasSpaceFor(ItemDefinition item, int amount)
        {
            int remaining = amount;

            foreach (var slot in _slots)
            {
                if (slot.Item == item)
                {
                    remaining -= slot.SpaceRemaining;
                }
                else if (slot.IsEmpty)
                {
                    remaining -= item.MaxStackSize;
                }

                if (remaining <= 0) return true;
            }

            return remaining <= 0;
        }

        public bool AcceptsItem(ItemDefinition item)
        {
            return _filter == null || _filter.Accepts(item);
        }

        public void SetFilter(ResourceFilter filter)
        {
            _filter = filter;
        }

        #region Save/Load

        public StockpileSaveData GetSaveData()
        {
            var data = new StockpileSaveData
            {
                Id = _id,
                Position = transform.position,
                SlotCount = _slotCount,
                Slots = new List<StockpileSlotSaveData>()
            };

            for (int i = 0; i < _slots.Count; i++)
            {
                if (!_slots[i].IsEmpty)
                {
                    data.Slots.Add(new StockpileSlotSaveData
                    {
                        SlotIndex = i,
                        ItemId = _slots[i].Item.Id,
                        Amount = _slots[i].Amount
                    });
                }
            }

            return data;
        }

        #endregion
    }

    [Serializable]
    public class StockpileSaveData
    {
        public string Id;
        public Vector3 Position;
        public int SlotCount;
        public List<StockpileSlotSaveData> Slots;
    }

    [Serializable]
    public class StockpileSlotSaveData
    {
        public int SlotIndex;
        public string ItemId;
        public int Amount;
    }
}
```

### 4.3 StockpileSlot

**File:** `Assets/_Project/Scripts/Building/Stockpiles/StockpileSlot.cs`

```csharp
using VoxelRPG.Core.Items;

namespace VoxelRPG.Building
{
    /// <summary>
    /// Individual storage slot within a stockpile.
    /// </summary>
    public class StockpileSlot
    {
        public ItemDefinition Item { get; private set; }
        public int Amount { get; private set; }
        public bool IsEmpty => Item == null || Amount <= 0;
        public bool IsFull => Item != null && Amount >= Item.MaxStackSize;
        public int SpaceRemaining => Item != null ? Item.MaxStackSize - Amount : 0;

        public void SetContents(ItemDefinition item, int amount)
        {
            Item = item;
            Amount = amount;
        }

        public void AddAmount(int amount)
        {
            Amount += amount;
            if (Item != null && Amount > Item.MaxStackSize)
            {
                Amount = Item.MaxStackSize;
            }
        }

        public void RemoveAmount(int amount)
        {
            Amount -= amount;
            if (Amount <= 0)
            {
                Clear();
            }
        }

        public void Clear()
        {
            Item = null;
            Amount = 0;
        }
    }
}
```

### 4.4 IStockpileManager Interface

**File:** `Assets/_Project/Scripts/Building/Stockpiles/IStockpileManager.cs`

```csharp
using UnityEngine;
using VoxelRPG.Core.Items;

namespace VoxelRPG.Building
{
    /// <summary>
    /// Coordinates stockpile operations across the settlement.
    /// </summary>
    public interface IStockpileManager
    {
        void RegisterStockpile(IStockpile stockpile);
        void UnregisterStockpile(IStockpile stockpile);

        IStockpile FindNearestWithItem(Vector3 position, ItemDefinition item, int amount = 1);
        IStockpile FindNearestWithSpace(Vector3 position, ItemDefinition item, int amount = 1);
        IStockpile FindAlternateDeposit(Vector3 position, ItemDefinition item, IStockpile excluding);

        int GetTotalItemCount(ItemDefinition item);
        void ReleaseReservation(string reservationId);
    }
}
```

### 4.5 StockpileManager

**File:** `Assets/_Project/Scripts/Building/Stockpiles/StockpileManager.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoxelRPG.Core.Items;

namespace VoxelRPG.Building
{
    /// <summary>
    /// Central coordination for all stockpiles.
    /// </summary>
    public class StockpileManager : MonoBehaviour, IStockpileManager
    {
        private readonly List<IStockpile> _stockpiles = new();
        private readonly Dictionary<string, StockpileReservation> _reservations = new();

        public void RegisterStockpile(IStockpile stockpile)
        {
            if (!_stockpiles.Contains(stockpile))
            {
                _stockpiles.Add(stockpile);
            }
        }

        public void UnregisterStockpile(IStockpile stockpile)
        {
            _stockpiles.Remove(stockpile);
        }

        public IStockpile FindNearestWithItem(Vector3 position, ItemDefinition item, int amount = 1)
        {
            return _stockpiles
                .Where(s => s.HasItem(item, amount))
                .OrderBy(s => Vector3.Distance(position, s.Position))
                .FirstOrDefault();
        }

        public IStockpile FindNearestWithSpace(Vector3 position, ItemDefinition item, int amount = 1)
        {
            return _stockpiles
                .Where(s => s.AcceptsItem(item) && s.HasSpaceFor(item, amount))
                .OrderBy(s => Vector3.Distance(position, s.Position))
                .FirstOrDefault();
        }

        public IStockpile FindAlternateDeposit(Vector3 position, ItemDefinition item, IStockpile excluding)
        {
            return _stockpiles
                .Where(s => s != excluding && s.AcceptsItem(item) && s.HasSpaceFor(item, 1))
                .OrderBy(s => Vector3.Distance(position, s.Position))
                .FirstOrDefault();
        }

        public int GetTotalItemCount(ItemDefinition item)
        {
            return _stockpiles.Sum(s => s.GetItemCount(item));
        }

        public void ReleaseReservation(string reservationId)
        {
            _reservations.Remove(reservationId);
        }
    }

    public class StockpileReservation
    {
        public string Id;
        public IStockpile Stockpile;
        public ItemDefinition Item;
        public int Amount;
    }
}
```

### 4.6 ResourceFilter

**File:** `Assets/_Project/Scripts/Building/Stockpiles/ResourceFilter.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using VoxelRPG.Core.Items;

namespace VoxelRPG.Building
{
    /// <summary>
    /// Defines which items a stockpile accepts.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFilter", menuName = "VoxelRPG/Building/Resource Filter")]
    public class ResourceFilter : ScriptableObject
    {
        [Header("Filter Mode")]
        [SerializeField] private FilterMode _mode = FilterMode.Whitelist;

        [Header("Items")]
        [SerializeField] private List<ItemDefinition> _items = new();

        [Header("Categories")]
        [SerializeField] private List<ItemCategory> _categories = new();

        public bool Accepts(ItemDefinition item)
        {
            if (item == null) return false;

            bool inList = _items.Contains(item) || _categories.Contains(item.Category);

            return _mode == FilterMode.Whitelist ? inList : !inList;
        }

        public enum FilterMode
        {
            Whitelist,
            Blacklist
        }
    }
}
```

### Assets to Create

```
Assets/_Project/Prefabs/Building/
└── Stockpile.prefab
    ├── Cube (MeshFilter + MeshRenderer) - Brown material, scaled to zone size
    └── Stockpile component

Assets/_Project/ScriptableObjects/Filters/
├── Filter_All.asset (accepts everything)
├── Filter_Resources.asset (wood, stone, ore)
├── Filter_Food.asset (food items only)
└── Filter_Tools.asset (tools only)
```

### Estimated Lines: ~350 lines

### Dependencies: Task 1 (uses ItemDefinition)

---

## Task 5: Building Orchestrator

**Goal:** Coordinate mining, hauling, and construction operations

### 5.1 IBuildingOrchestrator Interface

**File:** `Assets/_Project/Scripts/Building/IBuildingOrchestrator.cs`

```csharp
using UnityEngine;

namespace VoxelRPG.Building
{
    /// <summary>
    /// Central coordinator for all building operations.
    /// </summary>
    public interface IBuildingOrchestrator
    {
        void DesignateMining(Vector3Int position);
        void DesignateMiningArea(Vector3Int start, Vector3Int end);
        void CancelMiningDesignation(Vector3Int position);

        void PlaceConstructionSite(Vector3Int position, Blueprint blueprint);
        void CancelConstruction(string siteId);

        void CreateStockpile(Vector3 position, Vector3 size);
    }
}
```

### 5.2 BuildingOrchestrator

**File:** `Assets/_Project/Scripts/Building/BuildingOrchestrator.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using VoxelRPG.Core;
using VoxelRPG.Core.Events;
using VoxelRPG.NPC.Tasks;
using VoxelRPG.Voxel;

namespace VoxelRPG.Building
{
    /// <summary>
    /// Central coordinator for mining, hauling, and construction.
    /// </summary>
    public class BuildingOrchestrator : MonoBehaviour, IBuildingOrchestrator
    {
        [Header("References")]
        [SerializeField] private TaskManager _taskManager;
        [SerializeField] private StockpileManager _stockpileManager;

        [Header("Event Channels")]
        [SerializeField] private Vector3IntEventChannel _onBlockChanged;

        private readonly HashSet<Vector3Int> _miningDesignations = new();
        private readonly List<ConstructionSite> _constructionSites = new();

        private void Awake()
        {
            if (_onBlockChanged != null)
            {
                _onBlockChanged.OnEventRaised += OnBlockChanged;
            }
        }

        private void OnDestroy()
        {
            if (_onBlockChanged != null)
            {
                _onBlockChanged.OnEventRaised -= OnBlockChanged;
            }
        }

        #region Mining

        public void DesignateMining(Vector3Int position)
        {
            if (_miningDesignations.Contains(position)) return;

            var voxelWorld = ServiceLocator.TryGet<IVoxelWorld>(out var vw) ? vw : null;
            if (voxelWorld == null) return;

            var block = voxelWorld.GetBlock(position);
            if (block == null || !block.IsSolid) return;

            _miningDesignations.Add(position);

            // Create mining task
            float miningDuration = block.Hardness;
            var task = new MiningTask(position, miningDuration, TaskPriority.Normal);
            _taskManager.AddTask(task);
        }

        public void DesignateMiningArea(Vector3Int start, Vector3Int end)
        {
            Vector3Int min = Vector3Int.Min(start, end);
            Vector3Int max = Vector3Int.Max(start, end);

            for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
            for (int z = min.z; z <= max.z; z++)
            {
                DesignateMining(new Vector3Int(x, y, z));
            }
        }

        public void CancelMiningDesignation(Vector3Int position)
        {
            if (!_miningDesignations.Contains(position)) return;

            _miningDesignations.Remove(position);

            // Cancel associated tasks
            var tasks = _taskManager.GetTasksAtPosition<MiningTask>(position);
            foreach (var task in tasks)
            {
                task.Cancel(TaskCancelReason.UserCancelled);
            }
        }

        #endregion

        #region Construction

        public void PlaceConstructionSite(Vector3Int position, Blueprint blueprint)
        {
            var site = new ConstructionSite(position, blueprint);
            _constructionSites.Add(site);

            // Create build tasks for each block in blueprint
            foreach (var block in blueprint.GetBlocks())
            {
                Vector3Int worldPos = position + block.LocalPosition;
                var buildTask = new BuildTask(
                    worldPos,
                    block.BlockType,
                    block.RequiredItem,
                    1f,
                    TaskPriority.Normal
                );
                _taskManager.AddTask(buildTask);
                site.AddTask(buildTask);
            }
        }

        public void CancelConstruction(string siteId)
        {
            var site = _constructionSites.Find(s => s.Id == siteId);
            if (site == null) return;

            site.Cancel();
            _constructionSites.Remove(site);
        }

        #endregion

        #region Stockpiles

        public void CreateStockpile(Vector3 position, Vector3 size)
        {
            var stockpileObj = new GameObject("Stockpile");
            stockpileObj.transform.position = position;

            var stockpile = stockpileObj.AddComponent<Stockpile>();
            _stockpileManager.RegisterStockpile(stockpile);
        }

        #endregion

        private void OnBlockChanged(Vector3Int position)
        {
            // Remove from mining designations if block was removed
            _miningDesignations.Remove(position);
        }

        #region Haul Task Generation

        private void Update()
        {
            // Periodically check for items on ground that need hauling
            CheckForHaulTasks();
        }

        private float _haulCheckTimer;
        private const float HAUL_CHECK_INTERVAL = 2f;

        private void CheckForHaulTasks()
        {
            _haulCheckTimer += Time.deltaTime;
            if (_haulCheckTimer < HAUL_CHECK_INTERVAL) return;
            _haulCheckTimer = 0f;

            // Find all ItemDrop entities and create haul tasks
            var itemDrops = FindObjectsByType<ItemDrop>(FindObjectsSortMode.None);
            foreach (var drop in itemDrops)
            {
                if (drop.HasPendingHaulTask) continue;

                var stockpile = _stockpileManager.FindNearestWithSpace(
                    drop.transform.position,
                    drop.Item,
                    drop.Amount
                );

                if (stockpile != null)
                {
                    var haulTask = new HaulTask(
                        drop, drop.transform.position,
                        stockpile, stockpile.Position,
                        drop.Item, drop.Amount,
                        TaskPriority.Low
                    );
                    _taskManager.AddTask(haulTask);
                    drop.HasPendingHaulTask = true;
                }
            }
        }

        #endregion
    }
}
```

### 5.3 Blueprint ScriptableObject

**File:** `Assets/_Project/Scripts/Building/Blueprint.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using VoxelRPG.Core.Items;
using VoxelRPG.Voxel;

namespace VoxelRPG.Building
{
    /// <summary>
    /// Building template defining structure layout.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBlueprint", menuName = "VoxelRPG/Building/Blueprint")]
    public class Blueprint : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;

        [Header("Layout")]
        [SerializeField] private List<BlueprintBlock> _blocks = new();

        [Header("Requirements")]
        [SerializeField] private List<ItemRequirement> _totalMaterials = new();

        public string Id => _id;
        public string DisplayName => _displayName;
        public int BlockCount => _blocks.Count;

        public IEnumerable<BlueprintBlock> GetBlocks()
        {
            return _blocks;
        }

        public IEnumerable<ItemRequirement> GetRequirements()
        {
            return _totalMaterials;
        }
    }

    [System.Serializable]
    public class BlueprintBlock
    {
        public Vector3Int LocalPosition;
        public BlockType BlockType;
        public ItemDefinition RequiredItem;
    }

    [System.Serializable]
    public class ItemRequirement
    {
        public ItemDefinition Item;
        public int Amount;
    }
}
```

### 5.4 ConstructionSite

**File:** `Assets/_Project/Scripts/Building/ConstructionSite.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelRPG.NPC.Tasks;

namespace VoxelRPG.Building
{
    /// <summary>
    /// Active construction site tracking progress.
    /// </summary>
    public class ConstructionSite
    {
        public string Id { get; }
        public Vector3Int Position { get; }
        public Blueprint Blueprint { get; }
        public bool IsComplete { get; private set; }
        public float Progress => _completedTasks / (float)_totalTasks;

        private readonly List<BuildTask> _tasks = new();
        private int _completedTasks;
        private int _totalTasks;

        public event Action<ConstructionSite> OnProgressChanged;
        public event Action<ConstructionSite> OnComplete;
        public event Action<ConstructionSite> OnCancelled;

        public ConstructionSite(Vector3Int position, Blueprint blueprint)
        {
            Id = Guid.NewGuid().ToString();
            Position = position;
            Blueprint = blueprint;
            _totalTasks = blueprint.BlockCount;
        }

        public void AddTask(BuildTask task)
        {
            _tasks.Add(task);
            task.OnStatusChanged += OnTaskStatusChanged;
        }

        private void OnTaskStatusChanged(ITask task)
        {
            if (task.Status == TaskStatus.Completed)
            {
                _completedTasks++;
                OnProgressChanged?.Invoke(this);

                if (_completedTasks >= _totalTasks)
                {
                    IsComplete = true;
                    OnComplete?.Invoke(this);
                }
            }
        }

        public void Cancel()
        {
            foreach (var task in _tasks)
            {
                if (task.Status == TaskStatus.Pending || task.Status == TaskStatus.Claimed)
                {
                    task.Cancel(TaskCancelReason.UserCancelled);
                }
            }
            OnCancelled?.Invoke(this);
        }
    }
}
```

### Estimated Lines: ~300 lines

### Dependencies: Task 3, Task 4

---

## Task 6: NPC Arrival & Settlement Stats

**Goal:** Track settlement metrics and trigger NPC arrivals

### 6.1 ISettlementStats Interface

**File:** `Assets/_Project/Scripts/Core/ISettlementStats.cs`

```csharp
using System;

namespace VoxelRPG.Core
{
    /// <summary>
    /// Settlement metrics interface. Used by companion recovery in Phase 4.
    /// </summary>
    public interface ISettlementStats
    {
        int Population { get; }
        int ClosedPortals { get; }
        int BuildingsConstructed { get; }
        float TerritoryControlled { get; }
        float AverageNPCMorale { get; }
        float DefenseRating { get; }
        float OverallProgress { get; }

        event Action<string, float> OnStatChanged;
    }
}
```

### 6.2 SettlementStats

**File:** `Assets/_Project/Scripts/Core/SettlementStats.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoxelRPG.NPC;

namespace VoxelRPG.Core
{
    /// <summary>
    /// Tracks settlement metrics for NPC attraction and companion recovery.
    /// </summary>
    public class SettlementStats : MonoBehaviour, ISettlementStats
    {
        [Header("Weights for Overall Progress")]
        [SerializeField] private float _populationWeight = 0.25f;
        [SerializeField] private float _portalsWeight = 0.25f;
        [SerializeField] private float _buildingsWeight = 0.2f;
        [SerializeField] private float _territoryWeight = 0.15f;
        [SerializeField] private float _moraleWeight = 0.15f;

        [Header("Scaling")]
        [SerializeField] private int _maxPopulationForProgress = 20;
        [SerializeField] private int _maxPortalsForProgress = 5;
        [SerializeField] private int _maxBuildingsForProgress = 50;

        private readonly List<NPCController> _npcs = new();
        private int _closedPortals;
        private int _buildingsConstructed;
        private float _territoryControlled;
        private float _defenseRating;

        #region Properties

        public int Population => _npcs.Count(n => n.IsAlive);

        public int ClosedPortals
        {
            get => _closedPortals;
            set
            {
                _closedPortals = value;
                RaiseStatChanged("ClosedPortals", value);
            }
        }

        public int BuildingsConstructed
        {
            get => _buildingsConstructed;
            set
            {
                _buildingsConstructed = value;
                RaiseStatChanged("BuildingsConstructed", value);
            }
        }

        public float TerritoryControlled
        {
            get => _territoryControlled;
            set
            {
                _territoryControlled = Mathf.Clamp01(value);
                RaiseStatChanged("TerritoryControlled", value);
            }
        }

        public float DefenseRating
        {
            get => _defenseRating;
            set
            {
                _defenseRating = value;
                RaiseStatChanged("DefenseRating", value);
            }
        }

        public float AverageNPCMorale
        {
            get
            {
                if (_npcs.Count == 0) return 50f;
                return _npcs.Where(n => n.IsAlive).Average(n => n.Needs.CurrentMorale);
            }
        }

        public float OverallProgress
        {
            get
            {
                float popScore = Mathf.Clamp01((float)Population / _maxPopulationForProgress);
                float portalScore = Mathf.Clamp01((float)_closedPortals / _maxPortalsForProgress);
                float buildingScore = Mathf.Clamp01((float)_buildingsConstructed / _maxBuildingsForProgress);
                float territoryScore = _territoryControlled;
                float moraleScore = AverageNPCMorale / 100f;

                return popScore * _populationWeight
                     + portalScore * _portalsWeight
                     + buildingScore * _buildingsWeight
                     + territoryScore * _territoryWeight
                     + moraleScore * _moraleWeight;
            }
        }

        #endregion

        #region Events

        public event Action<string, float> OnStatChanged;

        #endregion

        public void RegisterNPC(NPCController npc)
        {
            if (!_npcs.Contains(npc))
            {
                _npcs.Add(npc);
                npc.OnNPCDeath += HandleNPCDeath;
                RaiseStatChanged("Population", Population);
            }
        }

        public void UnregisterNPC(NPCController npc)
        {
            if (_npcs.Contains(npc))
            {
                npc.OnNPCDeath -= HandleNPCDeath;
                _npcs.Remove(npc);
                RaiseStatChanged("Population", Population);
            }
        }

        private void HandleNPCDeath(NPCController npc)
        {
            RaiseStatChanged("Population", Population);
        }

        private void RaiseStatChanged(string statName, float value)
        {
            OnStatChanged?.Invoke(statName, value);
        }

        #region Save/Load

        public SettlementStatsSaveData GetSaveData()
        {
            return new SettlementStatsSaveData
            {
                ClosedPortals = _closedPortals,
                BuildingsConstructed = _buildingsConstructed,
                TerritoryControlled = _territoryControlled,
                DefenseRating = _defenseRating
            };
        }

        public void LoadSaveData(SettlementStatsSaveData data)
        {
            _closedPortals = data.ClosedPortals;
            _buildingsConstructed = data.BuildingsConstructed;
            _territoryControlled = data.TerritoryControlled;
            _defenseRating = data.DefenseRating;
        }

        #endregion
    }

    [Serializable]
    public class SettlementStatsSaveData
    {
        public int ClosedPortals;
        public int BuildingsConstructed;
        public float TerritoryControlled;
        public float DefenseRating;
    }
}
```

### 6.3 NPCArrivalManager

**File:** `Assets/_Project/Scripts/NPC/NPCArrivalManager.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;
using VoxelRPG.Core;

namespace VoxelRPG.NPC
{
    /// <summary>
    /// Manages NPC spawning based on settlement attractiveness.
    /// </summary>
    public class NPCArrivalManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SettlementStats _settlementStats;
        [SerializeField] private NPCDefinition _defaultNPCDefinition;
        [SerializeField] private GameObject _npcPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private int _maxNPCs = 20;
        [SerializeField] private List<ArrivalThreshold> _arrivalThresholds = new();

        private int _nextThresholdIndex;
        private readonly List<NPCController> _spawnedNPCs = new();

        private void Start()
        {
            if (_settlementStats != null)
            {
                _settlementStats.OnStatChanged += OnSettlementChanged;
            }
        }

        private void OnDestroy()
        {
            if (_settlementStats != null)
            {
                _settlementStats.OnStatChanged -= OnSettlementChanged;
            }
        }

        private void OnSettlementChanged(string stat, float value)
        {
            CheckForArrival();
        }

        private void CheckForArrival()
        {
            if (_spawnedNPCs.Count >= _maxNPCs) return;
            if (_nextThresholdIndex >= _arrivalThresholds.Count) return;

            var threshold = _arrivalThresholds[_nextThresholdIndex];
            float progress = _settlementStats.OverallProgress;

            if (progress >= threshold.RequiredProgress)
            {
                SpawnNPC(threshold);
                _nextThresholdIndex++;
            }
        }

        private void SpawnNPC(ArrivalThreshold threshold)
        {
            Vector3 spawnPos = _spawnPoint != null
                ? _spawnPoint.position
                : transform.position + Random.insideUnitSphere * 5f;

            spawnPos.y = GetGroundHeight(spawnPos);

            var npcObj = Instantiate(_npcPrefab, spawnPos, Quaternion.identity);
            var npc = npcObj.GetComponent<NPCController>();

            var definition = threshold.NPCDefinition != null
                ? threshold.NPCDefinition
                : _defaultNPCDefinition;

            string npcId = System.Guid.NewGuid().ToString();
            npc.Initialize(definition, npcId);

            _spawnedNPCs.Add(npc);
            _settlementStats.RegisterNPC(npc);
        }

        private float GetGroundHeight(Vector3 position)
        {
            if (Physics.Raycast(position + Vector3.up * 50f, Vector3.down, out var hit, 100f))
            {
                return hit.point.y;
            }
            return position.y;
        }
    }

    [System.Serializable]
    public class ArrivalThreshold
    {
        public float RequiredProgress;
        public NPCDefinition NPCDefinition;
        public string ArrivalMessage;
    }
}
```

### Estimated Lines: ~250 lines

### Dependencies: Task 1

---

## Task 7: Personality System

**Goal:** Add personality traits and relationships to NPCs

### 7.1 PersonalityTrait ScriptableObject

**File:** `Assets/_Project/Scripts/NPC/Personality/PersonalityTrait.cs`

```csharp
using UnityEngine;

namespace VoxelRPG.NPC.Personality
{
    /// <summary>
    /// Defines a personality trait that affects NPC behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTrait", menuName = "VoxelRPG/NPC/Personality Trait")]
    public class PersonalityTrait : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea] private string _description;

        [Header("Work Modifiers")]
        [SerializeField] private float _miningSpeedModifier = 1f;
        [SerializeField] private float _buildingSpeedModifier = 1f;
        [SerializeField] private float _haulingSpeedModifier = 1f;

        [Header("Social Modifiers")]
        [SerializeField] private float _moraleDecayModifier = 1f;
        [SerializeField] private float _socialNeedModifier = 1f;

        [Header("Preferences")]
        [SerializeField] private TaskType _preferredTask = TaskType.Mining;
        [SerializeField] private TaskType _dislikedTask = TaskType.Hauling;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public float MiningSpeedModifier => _miningSpeedModifier;
        public float BuildingSpeedModifier => _buildingSpeedModifier;
        public float HaulingSpeedModifier => _haulingSpeedModifier;
        public float MoraleDecayModifier => _moraleDecayModifier;
        public float SocialNeedModifier => _socialNeedModifier;
        public TaskType PreferredTask => _preferredTask;
        public TaskType DislikedTask => _dislikedTask;
    }
}
```

### 7.2 NPCPersonality

**File:** `Assets/_Project/Scripts/NPC/Personality/NPCPersonality.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelRPG.NPC.Tasks;

namespace VoxelRPG.NPC.Personality
{
    /// <summary>
    /// Combines traits and manages personality-based behavior.
    /// </summary>
    public class NPCPersonality : MonoBehaviour
    {
        [SerializeField] private List<PersonalityTrait> _traits = new();
        [SerializeField] private string _generatedName;

        public string GeneratedName => _generatedName;
        public IReadOnlyList<PersonalityTrait> Traits => _traits;

        public void Initialize(List<PersonalityTrait> traits, string name)
        {
            _traits = traits ?? new List<PersonalityTrait>();
            _generatedName = name;
        }

        public float GetWorkSpeedModifier(TaskType taskType)
        {
            float modifier = 1f;
            foreach (var trait in _traits)
            {
                switch (taskType)
                {
                    case TaskType.Mining:
                        modifier *= trait.MiningSpeedModifier;
                        break;
                    case TaskType.Building:
                        modifier *= trait.BuildingSpeedModifier;
                        break;
                    case TaskType.Hauling:
                        modifier *= trait.HaulingSpeedModifier;
                        break;
                }
            }
            return modifier;
        }

        public int GetTaskPreference(TaskType taskType)
        {
            int preference = 0;
            foreach (var trait in _traits)
            {
                if (trait.PreferredTask == taskType) preference += 10;
                if (trait.DislikedTask == taskType) preference -= 10;
            }
            return preference;
        }

        public float GetMoraleDecayModifier()
        {
            float modifier = 1f;
            foreach (var trait in _traits)
            {
                modifier *= trait.MoraleDecayModifier;
            }
            return modifier;
        }

        #region Save/Load

        public NPCPersonalitySaveData GetSaveData()
        {
            var traitIds = new List<string>();
            foreach (var trait in _traits)
            {
                traitIds.Add(trait.Id);
            }

            return new NPCPersonalitySaveData
            {
                GeneratedName = _generatedName,
                TraitIds = traitIds
            };
        }

        #endregion
    }

    [Serializable]
    public class NPCPersonalitySaveData
    {
        public string GeneratedName;
        public List<string> TraitIds;
    }
}
```

### 7.3 NPCNameGenerator

**File:** `Assets/_Project/Scripts/NPC/Personality/NPCNameGenerator.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace VoxelRPG.NPC.Personality
{
    /// <summary>
    /// Generates random NPC names.
    /// </summary>
    public static class NPCNameGenerator
    {
        private static readonly List<string> FirstNames = new()
        {
            "Ada", "Bjorn", "Clara", "Dorn", "Elena", "Finn",
            "Greta", "Harald", "Ingrid", "Jarl", "Kira", "Leif",
            "Mira", "Nils", "Olga", "Pavel", "Runa", "Sven",
            "Thora", "Ulf", "Vera", "Wolf", "Xena", "Yuri", "Zara"
        };

        private static readonly List<string> LastNames = new()
        {
            "Ironhand", "Stoneheart", "Woodcutter", "Farmborn", "Hillwalker",
            "Riverfolk", "Meadowson", "Forestkin", "Rockhammer", "Fieldworker",
            "Duskborn", "Dawnseeker", "Stormwatch", "Nightshade", "Sunbringer"
        };

        public static string Generate()
        {
            string firstName = FirstNames[Random.Range(0, FirstNames.Count)];
            string lastName = LastNames[Random.Range(0, LastNames.Count)];
            return $"{firstName} {lastName}";
        }
    }
}
```

### 7.4 NPCRelationships

**File:** `Assets/_Project/Scripts/NPC/Personality/NPCRelationships.cs`

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelRPG.NPC.Personality
{
    /// <summary>
    /// Tracks relationships between NPCs.
    /// </summary>
    public class NPCRelationships : MonoBehaviour
    {
        private readonly Dictionary<string, float> _relationships = new();

        private const float MIN_RELATIONSHIP = -100f;
        private const float MAX_RELATIONSHIP = 100f;
        private const float NEUTRAL = 0f;

        public float GetRelationship(string npcId)
        {
            return _relationships.TryGetValue(npcId, out float value) ? value : NEUTRAL;
        }

        public void ModifyRelationship(string npcId, float delta)
        {
            if (!_relationships.ContainsKey(npcId))
            {
                _relationships[npcId] = NEUTRAL;
            }

            _relationships[npcId] = Mathf.Clamp(
                _relationships[npcId] + delta,
                MIN_RELATIONSHIP,
                MAX_RELATIONSHIP
            );
        }

        public bool IsFriendly(string npcId)
        {
            return GetRelationship(npcId) >= 30f;
        }

        public bool IsHostile(string npcId)
        {
            return GetRelationship(npcId) <= -30f;
        }

        #region Save/Load

        public NPCRelationshipsSaveData GetSaveData()
        {
            return new NPCRelationshipsSaveData
            {
                Relationships = new Dictionary<string, float>(_relationships)
            };
        }

        public void LoadSaveData(NPCRelationshipsSaveData data)
        {
            _relationships.Clear();
            foreach (var kvp in data.Relationships)
            {
                _relationships[kvp.Key] = kvp.Value;
            }
        }

        #endregion
    }

    [Serializable]
    public class NPCRelationshipsSaveData
    {
        public Dictionary<string, float> Relationships;
    }
}
```

### Assets to Create

```
Assets/_Project/ScriptableObjects/NPCs/Traits/
├── Trait_Hardworker.asset (miningSpeed: 1.2, moraleDecay: 0.8)
├── Trait_Lazy.asset (miningSpeed: 0.8, moraleDecay: 1.2)
├── Trait_Social.asset (socialNeed: 1.5, moraleDecay: 0.9)
├── Trait_Loner.asset (socialNeed: 0.5, moraleDecay: 1.1)
├── Trait_Strong.asset (haulingSpeed: 1.3)
└── Trait_Clumsy.asset (buildingSpeed: 0.8)
```

### Estimated Lines: ~250 lines

### Dependencies: Task 1

---

## Task 8: Bootstrap Integration

**Goal:** Wire all Phase 2 systems into GameBootstrap

### 8.1 Update GameBootstrap

**File:** `Assets/_Project/Scripts/Bootstrap/GameBootstrap.cs`

**Add new methods after existing Phase 1 setup:**

```csharp
// Add to using statements
using VoxelRPG.NPC;
using VoxelRPG.NPC.Tasks;
using VoxelRPG.Building;

// Add fields
[Header("Phase 2 References")]
[SerializeField] private NPCDefinition _defaultNPCDefinition;
[SerializeField] private GameObject _npcPrefab;
[SerializeField] private GameObject _stockpilePrefab;

// Add to InitializeSystems() or similar method
private void SetupPhase2Systems()
{
    SetupTaskSystem();
    SetupBuildingOrchestrator();
    SetupSettlementStats();
    SetupNPCArrival();
}

private void SetupTaskSystem()
{
    var taskManagerObj = new GameObject("TaskManager");
    var taskManager = taskManagerObj.AddComponent<TaskManager>();
    var interruptHandler = taskManagerObj.AddComponent<TaskInterruptHandler>();

    ServiceLocator.Register<ITaskManager>(taskManager);
}

private void SetupBuildingOrchestrator()
{
    var orchestratorObj = new GameObject("BuildingOrchestrator");
    var stockpileManager = orchestratorObj.AddComponent<StockpileManager>();
    var orchestrator = orchestratorObj.AddComponent<BuildingOrchestrator>();

    ServiceLocator.Register<IStockpileManager>(stockpileManager);
    ServiceLocator.Register<IBuildingOrchestrator>(orchestrator);
}

private void SetupSettlementStats()
{
    var statsObj = new GameObject("SettlementStats");
    var stats = statsObj.AddComponent<SettlementStats>();

    ServiceLocator.Register<ISettlementStats>(stats);
}

private void SetupNPCArrival()
{
    var arrivalObj = new GameObject("NPCArrivalManager");
    var arrivalManager = arrivalObj.AddComponent<NPCArrivalManager>();

    // Configure with references
}
```

### 8.2 Update ServiceLocator

Ensure ServiceLocator supports Phase 2 interfaces:

```csharp
// These should already work with existing ServiceLocator, but verify:
ServiceLocator.Register<ITaskManager>(taskManager);
ServiceLocator.Register<IStockpileManager>(stockpileManager);
ServiceLocator.Register<IBuildingOrchestrator>(orchestrator);
ServiceLocator.Register<ISettlementStats>(stats);
```

### 8.3 NavMesh Setup

**Manual Step:** Configure NavMesh for NPC pathfinding

1. Add `NavMeshSurface` component to terrain/voxel world
2. Configure agent settings for NPC movement
3. Bake NavMesh for walkable surfaces

### Estimated Lines: ~100 lines

### Dependencies: All previous tasks

---

## Implementation Order

```
Task 1: NPC Core System (Foundation)
├── No dependencies
└── ~450 lines

Task 2: NPC State Implementations
├── Depends on: Task 1
└── ~400 lines

Task 3: Task System
├── Depends on: Task 1, Task 2
└── ~500 lines

Task 4: Stockpile System (can run parallel with Task 2)
├── Depends on: Task 1
└── ~350 lines

Task 5: Building Orchestrator
├── Depends on: Task 3, Task 4
└── ~300 lines

Task 6: Settlement Stats & NPC Arrival
├── Depends on: Task 1
└── ~250 lines

Task 7: Personality System (can run parallel with Tasks 5-6)
├── Depends on: Task 1
└── ~250 lines

Task 8: Bootstrap Integration
├── Depends on: All tasks
└── ~100 lines
```

---

## Exit Criteria

Phase 2 is complete when:

- [ ] NPCs spawn based on settlement progress thresholds
- [ ] NPCs autonomously seek and claim tasks
- [ ] Mining designation creates tasks that NPCs complete
- [ ] Mined resources are hauled to stockpiles
- [ ] Construction sites generate build tasks
- [ ] NPCs place blocks for construction
- [ ] NPC needs (hunger, energy) drain and affect behavior
- [ ] NPCs flee from combat and resume tasks when safe
- [ ] Task interruptions handled gracefully (NPC death, target removed, etc.)
- [ ] Stockpiles store resources with filters
- [ ] Settlement stats track progress metrics
- [ ] `ISettlementStats.OverallProgress` returns meaningful 0-1 value
- [ ] NPCs have generated names and personality traits
- [ ] All state persists through save/load

### Testing Checklist

| Scenario | Expected Result |
|----------|-----------------|
| Designate mining area | Tasks created, NPCs claim and mine |
| Kill NPC mid-task | Task returns to queue, items dropped |
| Fill all stockpiles | NPC drops items, notifies player |
| Place construction blueprint | Build tasks created with dependencies |
| Attack working NPC | NPC flees, task suspended, resumes later |
| Save/load with active tasks | All task states preserved |
| 10+ NPCs working simultaneously | No performance issues, tasks distributed |

---

## Configuration Reference

### NPC Stats

| Stat | Default Value |
|------|---------------|
| Max Health | 100 |
| Max Hunger | 100 |
| Max Energy | 100 |
| Move Speed | 3.5 units/sec |
| Run Speed | 6 units/sec |
| Work Speed | 1.0 (multiplier) |
| Carry Capacity | 4 slots |
| Hunger Drain Rate | 0.5 units/min |
| Energy Drain Rate | 0.2 units/min |

### Task Priority Order

| Priority Level | Tasks |
|----------------|-------|
| Critical (3) | Defense, flee triggers |
| High (2) | Construction (finishing buildings) |
| Normal (1) | Mining, building |
| Low (0) | Hauling, stockpiling |

### Settlement Progress Thresholds

| NPC Count | Required Progress |
|-----------|-------------------|
| 1st NPC | 0.05 (first shelter built) |
| 2nd NPC | 0.10 |
| 3rd NPC | 0.15 |
| 4th NPC | 0.20 |
| 5th NPC | 0.30 |
| 10th NPC | 0.50 |
| 15th NPC | 0.70 |
| 20th NPC | 0.90 |

---

## References

- [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) - Full development roadmap
- [PHASE1_IMPLEMENTATION_PLAN.md](PHASE1_IMPLEMENTATION_PLAN.md) - Previous phase details
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Code standards
- [VISION.md](VISION.md) - Game design vision

---

**Document Version:** 1.0
**Last Updated:** November 2025
