# Development Plan

**Last Updated:** November 2025
**Status:** Active
**Version:** 2.0
**Purpose:** Detailed implementation guide for coding the Voxel RPG Game

---

## Table of Contents

1. [Overview](#overview)
2. [Critical Architecture Decisions](#critical-architecture-decisions)
3. [Phase 0A: Minimal Foundation](#phase-0a-minimal-foundation)
4. [Phase 0B: Foundation Optimization](#phase-0b-foundation-optimization)
5. [Phase 1: Playable Prototype](#phase-1-playable-prototype)
6. [Vertical Slice Milestone](#vertical-slice-milestone)
7. [Phase 2: Colony Alpha](#phase-2-colony-alpha)
8. [Task Interruption & Recovery](#task-interruption--recovery)
9. [Phase 3: Combat & Threats](#phase-3-combat--threats)
10. [Phase 4.0: Integration Checkpoint](#phase-40-integration-checkpoint)
11. [Phase 4: The Companion](#phase-4-the-companion)
12. [Phase 5: Content & Polish](#phase-5-content--polish)
13. [Phase 6: Multiplayer](#phase-6-multiplayer)
14. [Phase 7: Launch Preparation](#phase-7-launch-preparation)
15. [Save System Evolution](#save-system-evolution)
16. [Multiplayer Compatibility Checklist](#multiplayer-compatibility-checklist)
17. [ScriptableObject Scaling Strategy](#scriptableobject-scaling-strategy)
18. [Implementation Summary](#implementation-summary)

---

## Overview

This document provides a detailed breakdown of each development phase with specific coding tasks, file locations, and exit criteria. It supplements the [ROADMAP.md](ROADMAP.md) with implementation-level details.

### Key Changes from v1.0

- **Split Phase 0** into minimal viable (0A) and optimization (0B) phases
- **Added per-phase save system evolution** instead of "done and forget"
- **Added task interruption and recovery** section for colony sim edge cases
- **Added Phase 4.0 integration checkpoint** before companion implementation
- **Added multiplayer compatibility checklist** for early phases
- **Added vertical slice milestone** for early integration testing
- **Added ScriptableObject scaling strategy** for content-rich scenarios
- **Added shared monster AI interface** to prevent Phase 1→3 rework
- **Moved WeatherSystem stub** earlier for gameplay balance testing
- **Added accessibility considerations** to Phase 7

### Project Structure Reference

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/          # GameManager, SaveManager, EventChannels
│   │   ├── Player/        # PlayerController, Combat, Inventory
│   │   ├── NPC/           # AI, States, Tasks, Companion
│   │   ├── Building/      # Construction, Stockpiles, Orchestrator
│   │   ├── Voxel/         # Chunks, World, Generation
│   │   ├── Combat/        # Damage, Monsters, Weapons
│   │   ├── Magic/         # Spells, Effects
│   │   ├── World/         # Portals, Territory, Weather
│   │   ├── Audio/         # Music, SFX, Ambient
│   │   ├── Networking/    # Multiplayer systems
│   │   ├── UI/            # All UI scripts
│   │   └── Utilities/     # Helpers, Extensions, Object Pools
│   ├── ScriptableObjects/
│   │   ├── Blocks/        # BlockType definitions
│   │   ├── Items/         # ItemDefinition, Weapons
│   │   ├── NPCs/          # NPCDefinition, Traits
│   │   ├── Monsters/      # MonsterDefinition
│   │   ├── Recipes/       # Crafting recipes
│   │   ├── Blueprints/    # Building blueprints
│   │   ├── Biomes/        # BiomeDefinition
│   │   ├── Magic/         # SpellDefinition
│   │   ├── Dialogue/      # DialogueTree, Nodes
│   │   ├── Quests/        # Quest definitions
│   │   ├── Events/        # EventChannel SOs
│   │   └── Audio/         # Audio events
│   ├── Prefabs/
│   ├── Scenes/
│   ├── Materials/
│   ├── Textures/
│   ├── Audio/
│   └── Animations/
├── Plugins/
└── Resources/
```

---

## Critical Architecture Decisions

These decisions must be made **before Phase 0A** because they affect everything downstream:

### 1. Authority Model (Multiplayer-Aware)

Even for single-player-first development, design with authority in mind:

```csharp
// BAD: Singleton that assumes single authority
public class VoxelWorld : MonoBehaviour
{
    public static VoxelWorld Instance; // Will need rewriting for multiplayer
}

// GOOD: Authority-agnostic interface
public interface IVoxelWorld
{
    BlockType GetBlock(Vector3Int worldPos);
    void RequestBlockChange(Vector3Int worldPos, BlockType type); // "Request" not "Set"
}

public class VoxelWorld : MonoBehaviour, IVoxelWorld
{
    // Single-player: requests are immediately applied
    // Multiplayer: requests go to server for validation
}
```

### 2. State Ownership

Define who owns what state from the start:

| State | Owner | Sync Strategy |
|-------|-------|---------------|
| Voxel World | Server/Host | Delta compression, chunk-based |
| Player Position | Each Client | Client-authoritative with validation |
| NPC State | Server/Host | State machine sync |
| Inventory | Server/Host | Transaction-based |
| Task Queue | Server/Host | Event-based updates |

### 3. Event System Design

Use injectable event channels, not static events:

```csharp
// GOOD: Injectable, testable, multiplayer-ready
public class BuildingOrchestrator : MonoBehaviour
{
    [SerializeField] private BlockChangedEventChannel _onBlockChanged;

    // Can be swapped for network-aware version later
}
```

---

## Phase 0A: Minimal Foundation

**Duration:** 4-6 weeks
**Goal:** Absolute minimum to place/remove blocks and move around

This phase intentionally defers optimization. Get something working first.

### 0A.1 Project Setup

| Task | Description | Location |
|------|-------------|----------|
| Create Unity project | Unity 2022 LTS with URP | Root |
| Configure folder structure | Per CONTRIBUTING.md standards | `Assets/_Project/` |
| Git configuration | .gitignore, .gitattributes for Unity | Root |
| Assembly definitions | Core, Player, Voxel assemblies | `Scripts/` subdirs |
| Input System setup | New Input System package | `_Project/Input/` |

### 0A.2 Naive Voxel World

**Goal:** Working voxel placement, NOT optimized rendering.

| Task | Description | Location |
|------|-------------|----------|
| `BlockType` ScriptableObject | Minimal: ID, color/material | `ScriptableObjects/Blocks/` |
| `VoxelChunk` | 16x16x16 data + **naive mesh** (one cube per block) | `Scripts/Voxel/` |
| `IVoxelWorld` interface | Authority-agnostic block access | `Scripts/Voxel/` |
| `VoxelWorld` | Chunk dictionary, block get/set | `Scripts/Voxel/` |
| Block change events | `BlockChangedEventChannel` | `ScriptableObjects/Events/` |

**Naive Meshing (intentionally simple):**

```csharp
// ChunkMeshBuilder.cs - Phase 0A version
public class ChunkMeshBuilder
{
    // Generate 6 faces per solid block, no optimization
    // This is slow but correct - optimize in Phase 0B
    public Mesh BuildNaiveMesh(VoxelChunk chunk)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        for (int x = 0; x < VoxelChunk.SIZE; x++)
        for (int y = 0; y < VoxelChunk.SIZE; y++)
        for (int z = 0; z < VoxelChunk.SIZE; z++)
        {
            if (chunk.GetBlockLocal(x, y, z).IsSolid)
            {
                AddCubeFaces(vertices, triangles, x, y, z, chunk);
            }
        }

        // ... build mesh
    }
}
```

### 0A.3 Basic Player Controller

| Task | Description | Location |
|------|-------------|----------|
| `PlayerController` | WASD, jump, gravity (CharacterController) | `Scripts/Player/` |
| `PlayerCamera` | First-person camera, mouse look | `Scripts/Player/` |
| `BlockInteraction` | Raycast, place/remove on click | `Scripts/Player/` |

### 0A.4 Minimal Save System

**Goal:** Save chunk data only. Expand per-phase (see [Save System Evolution](#save-system-evolution)).

| Task | Description | Location |
|------|-------------|----------|
| `ISaveable` interface | Base contract | `Scripts/Core/` |
| `SaveManager` | Orchestrate save/load | `Scripts/Core/` |
| `ChunkSaveData` | Serialize block arrays | `Scripts/Voxel/` |
| `SaveFileHeader` | Version number for migrations | `Scripts/Core/` |

```csharp
// SaveFileHeader.cs
[System.Serializable]
public class SaveFileHeader
{
    public int Version;
    public string GameVersion;
    public DateTime SavedAt;
    public List<string> LoadedSystems; // Track what's in this save
}
```

### 0A.5 Core Framework

| Task | Description | Location |
|------|-------------|----------|
| `GameManager` | Game state (Menu, Playing, Paused) | `Scripts/Core/` |
| `ServiceLocator` | Inject dependencies, avoid singletons | `Scripts/Core/` |
| Event channels | `VoidEventChannel`, `BlockChangedEventChannel` | `ScriptableObjects/Events/` |
| `IContentRegistry<T>` | **Abstract over SO loading strategy** | `Scripts/Core/` |
| `DirectRegistry<T>` | **Initial implementation (direct refs)** | `Scripts/Core/` |

**Content Registry (implement now, swap later):**

```csharp
// IContentRegistry.cs - Define the abstraction upfront
public interface IContentRegistry<T> where T : ScriptableObject
{
    T Get(string id);
    IEnumerable<T> GetAll();
    bool TryGet(string id, out T item);
}

// DirectRegistry.cs - Phase 0A implementation (simple, direct references)
public class DirectRegistry<T> : IContentRegistry<T> where T : ScriptableObject
{
    [SerializeField] private T[] _items;
    private Dictionary<string, T> _lookup;

    public void Initialize()
    {
        _lookup = _items.ToDictionary(GetId, item => item);
    }

    public T Get(string id) => _lookup[id];
    public IEnumerable<T> GetAll() => _items;
    public bool TryGet(string id, out T item) => _lookup.TryGetValue(id, out item);

    protected virtual string GetId(T item) => item.name; // Override per type
}

// Usage in Phase 0A - all registries use this pattern
public class BlockRegistry : DirectRegistry<BlockType>
{
    protected override string GetId(BlockType block) => block.Id;
}
```

**Why implement now:** Defining `IContentRegistry<T>` and using `DirectRegistry<T>` from the start means all call sites use the interface. When/if you need to swap to `AddressableRegistry<T>` in Phase 5+, you change the implementation, not every caller. The interface is trivial; the value is in consistent usage.

### Exit Criteria (Phase 0A)

- [ ] Player can walk around a flat voxel world
- [ ] Blocks can be placed and removed (even if rendering is slow)
- [ ] Chunk data saves and loads correctly
- [ ] No singletons in core systems (multiplayer-ready patterns)

### Multiplayer Compatibility Check (0A)

Before proceeding to 0B, verify:
- [ ] `IVoxelWorld` uses request pattern, not direct mutation
- [ ] No static state in core systems
- [ ] Event channels are injectable
- [ ] Save system has versioning

---

## Phase 0B: Foundation Optimization

**Duration:** 3-4 weeks
**Goal:** Performance-viable voxel rendering

Only start this after 0A exit criteria are met.

### 0B.1 Greedy Meshing

| Task | Description | Location |
|------|-------------|----------|
| `GreedyMeshBuilder` | Merge coplanar faces | `Scripts/Voxel/` |
| Face culling | Skip faces adjacent to solid blocks | `Scripts/Voxel/` |
| Chunk dirty flagging | Only rebuild changed chunks | `Scripts/Voxel/` |

```csharp
// IChunkMeshBuilder.cs - Allow swapping implementations
public interface IChunkMeshBuilder
{
    Mesh BuildMesh(VoxelChunk chunk);
}

// Can swap NaiveMeshBuilder for GreedyMeshBuilder
```

### 0B.2 Chunk Loading

| Task | Description | Location |
|------|-------------|----------|
| `ChunkLoader` | Load/unload based on player distance | `Scripts/Voxel/` |
| Async chunk generation | Don't block main thread | `Scripts/Voxel/` |
| Chunk pooling | Reuse chunk GameObjects | `Scripts/Voxel/` |

### 0B.3 Block Registry

| Task | Description | Location |
|------|-------------|----------|
| `BlockRegistry` | Centralized block type lookup | `Scripts/Voxel/` |
| Block properties | Hardness, drops, transparency | `ScriptableObjects/Blocks/` |
| UV mapping | Texture atlas coordinates | `Scripts/Voxel/` |

### Exit Criteria (Phase 0B)

- [ ] Stable 60 FPS with 16x16 chunk render distance
- [ ] Chunks load/unload smoothly as player moves
- [ ] Block textures render correctly

---

## Phase 1: Playable Prototype

**Duration:** 2-3 months
**Goal:** Core survival gameplay loop
**Deliverable:** Playable survival demo

### 1.1 World Generation

| Task | Description | Location |
|------|-------------|----------|
| `IWorldGenerator` | Interface for generation strategies | `Scripts/Voxel/Generation/` |
| `TerrainGenerator` | Noise-based heightmaps | `Scripts/Voxel/Generation/` |
| `BiomeDefinition` SO | Biome parameters | `ScriptableObjects/Biomes/` |
| `BiomeManager` | Select biome per region | `Scripts/Voxel/Generation/` |
| `OreGenerator` | Ore vein placement | `Scripts/Voxel/Generation/` |
| `VegetationGenerator` | Trees, plants | `Scripts/Voxel/Generation/` |
| World seed system | Deterministic generation | `Scripts/Voxel/` |

### 1.2 Player Survival Systems

| Task | Description | Location |
|------|-------------|----------|
| `HealthSystem` | HP, damage, healing | `Scripts/Player/` |
| `HungerSystem` | Food, starvation | `Scripts/Player/` |
| `PlayerStats` | Central stat container | `Scripts/Player/` |
| `IDamageable` interface | Shared damage contract | `Scripts/Combat/` |
| `DeathHandler` | Death, respawn | `Scripts/Player/` |

### 1.3 Inventory & Items

| Task | Description | Location |
|------|-------------|----------|
| `ItemDefinition` SO | Item properties | `ScriptableObjects/Items/` |
| `IInventory` interface | Generic container contract | `Scripts/Core/` |
| `Inventory` | Slot-based storage | `Scripts/Core/` |
| `PlayerInventory` | Player-specific logic | `Scripts/Player/` |
| `ItemDrop` | World item entity | `Scripts/Core/` |

```csharp
// IInventory.cs - Multiplayer-ready interface
public interface IInventory
{
    int SlotCount { get; }
    event Action<int, ItemStack> OnSlotChanged;

    // Returns success - server can reject in multiplayer
    bool TryAddItem(ItemDefinition item, int amount);
    bool TryRemoveItem(ItemDefinition item, int amount);
    ItemStack GetSlot(int index);
}
```

### 1.4 Crafting System

| Task | Description | Location |
|------|-------------|----------|
| `Recipe` SO | Ingredients, output | `ScriptableObjects/Recipes/` |
| `IRecipeRegistry` | Query recipes | `Scripts/Core/` |
| `CraftingStation` | Workbench base class | `Scripts/Building/` |
| `CraftingManager` | Execute crafting | `Scripts/Core/` |

### 1.5 Day/Night Cycle & Weather Stub

| Task | Description | Location |
|------|-------------|----------|
| `TimeManager` | Game time, day/night | `Scripts/Core/` |
| `SunController` | Light rotation | `Scripts/Core/` |
| `IWeatherSystem` | **Interface stub for weather** | `Scripts/World/` |
| `WeatherState` | Rain, clear, fog states | `Scripts/World/` |

**Weather Stub (implement fully in Phase 5):**

```csharp
// IWeatherSystem.cs - Stub now, implement later
public interface IWeatherSystem
{
    WeatherState CurrentWeather { get; }
    float Visibility { get; } // 0-1, affects NPC/monster behavior
    event Action<WeatherState> OnWeatherChanged;
}

// StubWeatherSystem.cs - Always clear, for Phase 1
public class StubWeatherSystem : MonoBehaviour, IWeatherSystem
{
    public WeatherState CurrentWeather => WeatherState.Clear;
    public float Visibility => 1f;
    public event Action<WeatherState> OnWeatherChanged;
}
```

### 1.6 Monster System (Shared Interface)

**Important:** Define `IMonsterAI` now to avoid rework in Phase 3.

| Task | Description | Location |
|------|-------------|----------|
| `IMonsterAI` | **Shared monster interface** | `Scripts/Combat/` |
| `MonsterDefinition` SO | Stats, behavior params | `ScriptableObjects/Monsters/` |
| `BasicMonsterAI` | Simple chase/attack | `Scripts/Combat/` |
| `MonsterSpawner` | Night spawning | `Scripts/Combat/` |
| `Hitbox`/`Hurtbox` | Combat collision | `Scripts/Combat/` |

```csharp
// IMonsterAI.cs - Define once, implement variants in Phase 3
public interface IMonsterAI
{
    MonsterDefinition Definition { get; }
    MonsterState CurrentState { get; }

    void Initialize(MonsterDefinition definition);
    void SetTarget(Transform target);
    void OnDamaged(float damage, Vector3 knockback);
    void OnDeath();

    // Phase 3 will add: SetSwarmGroup(), OnPortalCommand(), etc.
}

public enum MonsterState { Idle, Chasing, Attacking, Fleeing, Dead }

// BasicMonsterAI.cs - Phase 1 implementation
public class BasicMonsterAI : MonoBehaviour, IMonsterAI
{
    // Simple state machine: Idle → Chase → Attack → repeat
}
```

### 1.7 Basic UI

| Task | Description | Location |
|------|-------------|----------|
| `HUDController` | Health, hunger, hotbar | `Scripts/UI/` |
| `InventoryUI` | Inventory screen | `Scripts/UI/` |
| `CraftingUI` | Crafting interface | `Scripts/UI/` |
| `PauseMenu` | Pause, settings | `Scripts/UI/` |
| `UIManager` | Screen state | `Scripts/UI/` |

### Phase 1 Save System Additions

Add to save file:
- Player position, rotation
- Player stats (health, hunger)
- Player inventory
- Time of day
- World seed

```csharp
// Phase1SaveData.cs
[System.Serializable]
public class Phase1SaveData
{
    public Vector3 PlayerPosition;
    public float PlayerHealth;
    public float PlayerHunger;
    public InventorySaveData PlayerInventory;
    public float TimeOfDay;
    public int WorldSeed;
}
```

### Exit Criteria (Phase 1)

- [ ] Player can survive, craft tools, build shelter
- [ ] Day/night cycle with night monster spawns
- [ ] Save/load includes player state and inventory
- [ ] `IMonsterAI` interface ready for Phase 3 expansion
- [ ] `IWeatherSystem` stub in place

### Multiplayer Compatibility Check (Phase 1)

- [ ] `IInventory` uses request pattern
- [ ] Monster spawning can be server-authoritative
- [ ] Time synchronization considered

---

## Vertical Slice Milestone

**Timing:** After Phase 1, before Phase 2
**Duration:** 2-3 weeks
**Goal:** One fully polished slice proving integration works

### What's In the Slice

| Element | Quantity | Polish Level |
|---------|----------|--------------|
| Biome | 1 (forest) | Final art/audio |
| Block types | 10-15 | Final textures |
| Items | 15-20 | Final icons |
| Recipes | 10-15 | Balanced |
| Monster types | 1 | Full animations |
| NPCs | 0 (Phase 2) | N/A |

### Vertical Slice Goals

1. **Integration Test:** All Phase 0-1 systems work together
2. **Performance Baseline:** Establish target metrics
3. **Art Direction:** Lock visual style
4. **Feedback Ready:** Playable demo for external feedback
5. **Marketing Material:** Screenshots, short gameplay video

### Vertical Slice Checklist

- [ ] Forest biome with varied terrain
- [ ] Complete tool progression (wood → stone → iron)
- [ ] One monster type with full behavior and animations
- [ ] Day/night cycle with proper lighting
- [ ] All placeholder art replaced in slice area
- [ ] Sound effects for all interactions
- [ ] 60 FPS stable on target hardware
- [ ] Save/load tested extensively
- [ ] 30-minute guided playtest with external players

### Exit Criteria

- [ ] External playtester can survive 3 in-game days without major bugs
- [ ] Performance meets targets
- [ ] Visual style approved for full production

### Fail Criteria (Return to Phase 1)

These issues warrant going back rather than pushing forward:

| Issue | Symptom | Why It's a Blocker |
|-------|---------|-------------------|
| **Fundamental voxel bugs** | Chunks disappear, blocks desync, corruption on save/load | NPC pathfinding and construction will amplify these 10x |
| **Performance floor breach** | <30 FPS on target hardware with small world | Adding NPCs, tasks, and combat will only make it worse |
| **Player controller feel wrong** | Testers consistently describe movement as "floaty," "frustrating," or "unresponsive" | Core feel is hard to fix later; better to nail it now |
| **Survival loop not engaging** | Testers quit before night 2, describe game as "boring" or "pointless" | NPCs won't fix a broken core loop—they'll just be managing boredom |
| **Save system unreliable** | Data loss on >5% of save/load cycles | Phase 2+ saves are 10x more complex; fix it now |
| **Critical integration failure** | Systems fight each other (e.g., inventory and crafting have incompatible assumptions) | Architectural issues compound; rework is cheaper now |

**Decision Framework:**
1. If 1 blocker: Fix before proceeding
2. If 2+ blockers: Formal Phase 1 rework sprint
3. If core loop isn't fun: Stop and reassess design, not just code

The goal is to avoid sunk cost reasoning. It's cheaper to spend 2 extra weeks in Phase 1 than to build Phase 2-4 on a broken foundation.

---

## Phase 2: Colony Alpha

**Duration:** 2-3 months
**Goal:** NPC settlement building
**Deliverable:** Colony management demo

### 2.1 NPC Core System

| Task | Description | Location |
|------|-------------|----------|
| `NPCDefinition` SO | Base NPC stats | `ScriptableObjects/NPCs/` |
| `INPCController` | Interface for NPC control | `Scripts/NPC/` |
| `NPCController` | Movement, pathfinding | `Scripts/NPC/` |
| `NPCStateMachine` | State management | `Scripts/NPC/` |
| `NPCNeeds` | Hunger, rest, morale | `Scripts/NPC/` |
| `NPCInventory` | Carrying capacity | `Scripts/NPC/` |

### 2.2 Personality System

| Task | Description | Location |
|------|-------------|----------|
| `PersonalityTrait` SO | Trait definitions | `ScriptableObjects/NPCs/` |
| `NPCPersonality` | Trait combination | `Scripts/NPC/` |
| `NPCNameGenerator` | Name generation | `Scripts/NPC/` |
| `NPCRelationships` | Relationship tracking | `Scripts/NPC/` |

### 2.3 Task System

| Task | Description | Location |
|------|-------------|----------|
| `ITask` | Task contract | `Scripts/NPC/Tasks/` |
| `TaskQueue` | Priority queue | `Scripts/NPC/Tasks/` |
| `ITaskManager` | **Interface for task coordination** | `Scripts/NPC/Tasks/` |
| `TaskManager` | Implementation | `Scripts/NPC/Tasks/` |
| `MiningTask` | Block extraction | `Scripts/NPC/Tasks/` |
| `HaulTask` | Resource transport | `Scripts/NPC/Tasks/` |
| `BuildTask` | Block placement | `Scripts/NPC/Tasks/` |
| `TaskInterruptHandler` | **Handle task failures** | `Scripts/NPC/Tasks/` |

```csharp
// ITaskManager.cs - Multiplayer-ready interface
public interface ITaskManager
{
    void AddTask(ITask task);
    void RequestTaskClaim(string npcId, string taskId); // Request, not direct claim
    void ReportTaskProgress(string taskId, float progress);
    void ReportTaskComplete(string taskId);
    void ReportTaskFailed(string taskId, TaskFailureReason reason);
    ITask FindBestTaskFor(NPCController npc);
}
```

### 2.4 NPC Worker States

| Task | Description | Location |
|------|-------------|----------|
| `IdleState` | Waiting for work | `Scripts/NPC/States/` |
| `SeekingTaskState` | Finding work | `Scripts/NPC/States/` |
| `TravelingState` | Moving to task | `Scripts/NPC/States/` |
| `MiningState` | Extracting blocks | `Scripts/NPC/States/` |
| `HaulingState` | Pickup/delivery | `Scripts/NPC/States/` |
| `BuildingState` | Placing blocks | `Scripts/NPC/States/` |
| `RestingState` | Sleep, recovery | `Scripts/NPC/States/` |
| `ReactingState` | **Handle interruptions** | `Scripts/NPC/States/` |

### 2.5 Stockpile System

| Task | Description | Location |
|------|-------------|----------|
| `IStockpile` | Stockpile interface | `Scripts/Building/` |
| `Stockpile` | Storage zone | `Scripts/Building/` |
| `StockpileSlot` | Individual slot | `Scripts/Building/` |
| `IStockpileManager` | Manager interface | `Scripts/Building/` |
| `StockpileManager` | Find deposit/withdraw | `Scripts/Building/` |
| `ResourceFilter` | Filter resources | `Scripts/Building/` |

### 2.6 Building Orchestrator

| Task | Description | Location |
|------|-------------|----------|
| `IBuildingOrchestrator` | **Interface for coordination** | `Scripts/Building/` |
| `BuildingOrchestrator` | Central coordinator | `Scripts/Building/` |
| `MiningManager` | Mining designation | `Scripts/Building/` |
| `HaulingManager` | Haul coordination | `Scripts/Building/` |
| `ConstructionManager` | Construction sites | `Scripts/Building/` |

### 2.7 Construction System

| Task | Description | Location |
|------|-------------|----------|
| `Blueprint` SO | Building templates | `ScriptableObjects/Blueprints/` |
| `ConstructionSite` | Active construction | `Scripts/Building/` |
| `BuildOrderCalculator` | Structural order | `Scripts/Building/` |

### 2.8 NPC Arrival & Settlement Stats

| Task | Description | Location |
|------|-------------|----------|
| `ISettlementStats` | **Settlement metrics interface** | `Scripts/Core/` |
| `SettlementStats` | Track attractiveness | `Scripts/Core/` |
| `NPCArrivalManager` | Spawn settlers | `Scripts/NPC/` |

```csharp
// ISettlementStats.cs - Companion will need this in Phase 4
public interface ISettlementStats
{
    int Population { get; }
    int ClosedPortals { get; }
    int BuildingsConstructed { get; }
    float TerritoryControlled { get; }
    float AverageNPCMorale { get; }
    float DefenseRating { get; }

    // Composite score for companion recovery
    float OverallProgress { get; }

    event Action<string, float> OnStatChanged;
}
```

### Phase 2 Save System Additions

Add to save file:
- All NPC data (position, state, personality, inventory, needs)
- Task queue state
- Stockpile contents and reservations
- Construction site progress
- Settlement stats

```csharp
// Phase2SaveData.cs
[System.Serializable]
public class Phase2SaveData
{
    public List<NPCSaveData> NPCs;
    public List<TaskSaveData> PendingTasks;
    public List<StockpileSaveData> Stockpiles;
    public List<ConstructionSiteSaveData> ConstructionSites;
    public SettlementStatsSaveData SettlementStats;
}
```

### Exit Criteria (Phase 2)

- [ ] NPCs arrive and autonomously work
- [ ] Mining, hauling, stockpiling, building all function
- [ ] Task interruptions handled gracefully (see next section)
- [ ] `ISettlementStats` provides metrics for companion
- [ ] Save/load preserves all NPC and task state

### Multiplayer Compatibility Check (Phase 2)

- [ ] `ITaskManager` uses request pattern
- [ ] NPC state changes are server-authoritative
- [ ] Stockpile reservations handle race conditions

---

## Task Interruption & Recovery

**Critical Section:** Colony sims live or die by edge case handling.

### Interruption Scenarios

| Scenario | Detection | Recovery |
|----------|-----------|----------|
| NPC dies mid-task | `OnNPCDeath` event | Release task claim, return to queue |
| Stockpile fills during haul | Check on arrival | Find alternate stockpile or drop items |
| Block removed before mining | Validate on arrival | Cancel task, seek new task |
| Construction site cancelled | `OnSiteCancelled` event | Return materials to stockpile |
| Path blocked | Pathfinding failure | Reroute or abandon task |
| NPC attacked while working | `OnDamaged` event | Flee state, task suspended |
| Resource depleted mid-haul | Validate on pickup | Cancel haul, create new haul for partial |

### Task State Machine

```
                    ┌─────────────────────────────────────┐
                    │                                     │
                    ▼                                     │
┌─────────┐    ┌─────────┐    ┌─────────────┐    ┌──────┴────┐
│ PENDING │───▶│ CLAIMED │───▶│ IN_PROGRESS │───▶│ COMPLETED │
└─────────┘    └────┬────┘    └──────┬──────┘    └───────────┘
                    │                │
                    │                │ (interruption)
                    │                ▼
                    │         ┌───────────┐
                    │         │ SUSPENDED │ (can resume)
                    │         └─────┬─────┘
                    │               │
                    ▼               ▼
              ┌───────────┐  ┌──────────┐
              │ CANCELLED │  │  FAILED  │
              └───────────┘  └──────────┘
```

### Implementation

```csharp
// TaskInterruptHandler.cs
public class TaskInterruptHandler : MonoBehaviour
{
    [SerializeField] private TaskManager _taskManager;
    [SerializeField] private StockpileManager _stockpileManager;

    public void HandleNPCDeath(NPCController npc)
    {
        var task = _taskManager.GetTaskForNPC(npc.Id);
        if (task == null) return;

        // Release any reserved resources
        if (task is HaulTask haulTask)
        {
            _stockpileManager.ReleaseReservation(haulTask.SourceReservation);
            _stockpileManager.ReleaseReservation(haulTask.DestReservation);

            // Drop carried items at NPC location
            if (npc.Inventory.HasItems)
            {
                SpawnDroppedItems(npc.transform.position, npc.Inventory.Items);
            }
        }

        // Return task to queue or cancel
        if (task.CanBeReassigned)
        {
            task.Release();
            _taskManager.RequeueTask(task);
        }
        else
        {
            task.Cancel(TaskCancelReason.NPCDied);
        }
    }

    public void HandleStockpileFull(NPCController npc, HaulTask task)
    {
        // Try to find alternate stockpile
        var alternate = _stockpileManager.FindAlternateDeposit(
            npc.transform.position,
            task.ResourceType,
            excluding: task.DestinationStockpile
        );

        if (alternate != null)
        {
            task.RedirectTo(alternate);
        }
        else
        {
            // No space anywhere - drop items and notify player
            SpawnDroppedItems(npc.transform.position, npc.Inventory.Items);
            task.Fail(TaskFailureReason.NoStorageSpace);
            NotifyPlayer("Storage full! Resources dropped.");
        }
    }

    public void HandleBlockRemoved(Vector3Int position)
    {
        // Find any mining tasks targeting this block
        var miningTasks = _taskManager.GetTasksAtPosition<MiningTask>(position);
        foreach (var task in miningTasks)
        {
            task.Cancel(TaskCancelReason.TargetRemoved);
        }

        // Find any build tasks targeting this position
        var buildTasks = _taskManager.GetTasksAtPosition<BuildTask>(position);
        foreach (var task in buildTasks)
        {
            // Block was placed by someone else - task complete
            task.Complete();
        }
    }

    public void HandleNPCAttacked(NPCController npc, float damage)
    {
        var task = _taskManager.GetTaskForNPC(npc.Id);
        if (task == null) return;

        // Suspend task, NPC will flee
        task.Suspend();
        npc.StateMachine.ChangeState(new FleeState());

        // After fleeing, NPC can resume or abandon
        npc.OnSafe += () => HandleNPCReachedSafety(npc, task);
    }

    private void HandleNPCReachedSafety(NPCController npc, ITask task)
    {
        // Check if task is still valid
        if (task.IsStillValid())
        {
            task.Resume();
            // NPC will return to task
        }
        else
        {
            task.Cancel(TaskCancelReason.InvalidatedWhileFleeing);
        }
    }
}
```

### Testing Checklist

- [ ] Kill NPC mid-mining: task returns to queue
- [ ] Kill NPC mid-haul with items: items drop, reservations released
- [ ] Fill stockpile during haul: NPC finds alternate or drops
- [ ] Remove block NPC is walking to mine: task cancelled
- [ ] Cancel construction site: materials returned, tasks cancelled
- [ ] Block path with wall: NPC reroutes or gives up
- [ ] Attack NPC while building: NPC flees, resumes after safe
- [ ] Save/load with suspended task: task state preserved

---

## Phase 3: Combat & Threats

**Duration:** 2-3 months
**Goal:** Meaningful conflict and portal system
**Deliverable:** Combat and defense demo

### 3.1 Player Combat

| Task | Description | Location |
|------|-------------|----------|
| `PlayerCombat` | Attack input, combos | `Scripts/Player/` |
| `WeaponDefinition` SO | Weapon stats | `ScriptableObjects/Items/` |
| `MeleeWeapon` | Swing, hitbox timing | `Scripts/Combat/` |
| `RangedWeapon` | Projectile, aiming | `Scripts/Combat/` |
| `DodgeSystem` | Dodge roll, i-frames | `Scripts/Player/` |
| `BlockSystem` | Shield blocking | `Scripts/Player/` |

### 3.2 NPC Combat

| Task | Description | Location |
|------|-------------|----------|
| `GuardRole` | Combat NPC role | `Scripts/NPC/Roles/` |
| `CombatState` | NPC fighting | `Scripts/NPC/States/` |
| `FleeState` | NPC fleeing | `Scripts/NPC/States/` |
| `ThreatDetection` | Enemy awareness | `Scripts/NPC/` |
| `PatrolBehavior` | Guard routes | `Scripts/NPC/` |

### 3.3 Expanded Monster System

Build on `IMonsterAI` from Phase 1:

| Task | Description | Location |
|------|-------------|----------|
| `MeleeMonsterAI` | Close-range (implements `IMonsterAI`) | `Scripts/Combat/` |
| `RangedMonsterAI` | Ranged attacks (implements `IMonsterAI`) | `Scripts/Combat/` |
| `SwarmCoordinator` | Group behavior | `Scripts/Combat/` |
| `BossMonsterAI` | Boss encounters | `Scripts/Combat/` |
| `MonsterSpawnConfig` | Per-portal spawn rules | `ScriptableObjects/Monsters/` |

```csharp
// Extend IMonsterAI for Phase 3 features
public interface IMonsterAI
{
    // From Phase 1
    MonsterDefinition Definition { get; }
    MonsterState CurrentState { get; }
    void Initialize(MonsterDefinition definition);
    void SetTarget(Transform target);
    void OnDamaged(float damage, Vector3 knockback);
    void OnDeath();

    // Added in Phase 3
    void JoinSwarm(SwarmCoordinator swarm);
    void LeaveSwarm();
    void OnPortalCommand(PortalCommand command);
    void SetHomePortal(Portal portal);
}
```

### 3.4 Portal System

| Task | Description | Location |
|------|-------------|----------|
| `Portal` | Portal entity | `Scripts/World/` |
| `PortalSpawner` | Monster spawning | `Scripts/Combat/` |
| `PortalClosingMechanic` | Closing requirements | `Scripts/World/` |
| `CorruptionSystem` | Territory corruption | `Scripts/World/` |
| `PortalReopeningSystem` | Undefended reopening | `Scripts/World/` |

### 3.5 Territory Control

| Task | Description | Location |
|------|-------------|----------|
| `ITerritoryManager` | Territory interface | `Scripts/World/` |
| `TerritoryManager` | Track claimed/corrupted | `Scripts/World/` |
| `ClaimMarker` | Claim territory | `Scripts/Building/` |
| `TerritoryUI` | Map visualization | `Scripts/UI/` |

### 3.6 Defensive Structures

| Task | Description | Location |
|------|-------------|----------|
| `Wall` | Defensive wall | `Scripts/Building/Defense/` |
| `Gate` | Controllable gate | `Scripts/Building/Defense/` |
| `Trap` | Damage traps | `Scripts/Building/Defense/` |
| `Tower` | Archer tower | `Scripts/Building/Defense/` |

### Phase 3 Save System Additions

Add to save file:
- All portal states (active, closed, closing progress)
- Territory control map
- Monster spawner states
- Defensive structure states

### Exit Criteria (Phase 3)

- [ ] Player and NPCs can fight varied monsters
- [ ] Portal system with closing/reopening
- [ ] Territory control affects gameplay
- [ ] All monster types implement `IMonsterAI`

---

## Phase 4.0: Integration Checkpoint

**Timing:** After Phase 3, before Phase 4
**Duration:** 1-2 weeks
**Goal:** Verify Phase 1-3 systems produce companion-ready metrics

### Why This Checkpoint Exists

The companion's recovery is "tied to player progress" via `ISettlementStats`. Before implementing companion mechanics, verify the metrics exist and are meaningful.

### Integration Verification

| Metric | Source System | Verify |
|--------|---------------|--------|
| `Population` | NPCArrivalManager | NPCs arriving based on settlement quality |
| `ClosedPortals` | PortalSystem | Portals can be closed, count persists |
| `BuildingsConstructed` | ConstructionManager | Count increases, persists through save/load |
| `TerritoryControlled` | TerritoryManager | Territory claims work, corruption pushback works |
| `AverageNPCMorale` | NPCNeeds aggregation | Morale varies based on conditions |
| `DefenseRating` | Defensive structures + guards | Rating reflects actual defense capability |
| `OverallProgress` | Weighted composite | Produces 0-1 value that feels right |

### Balance Testing

Play through Phase 1-3 content and verify:

1. **Progression feels right:** Player naturally progresses from 0 → meaningful `OverallProgress`
2. **Metrics are balanced:** No single metric dominates
3. **Metrics are achievable:** Player can reach thresholds for companion phases

### Companion Phase Thresholds (Preliminary)

| Companion Phase | Required `OverallProgress` | Typical Player State |
|-----------------|---------------------------|---------------------|
| Rescue | 0.0 | Game start |
| Recovery | 0.1 | First shelter, some NPCs |
| Partnership | 0.4 | Established village, portal closed |
| Revelation | 0.7 | Strong settlement, multiple portals |

### Checkpoint Deliverables

- [ ] `ISettlementStats` fully implemented and tested
- [ ] Balance document with target thresholds
- [ ] Playtest confirming progression feels natural
- [ ] Any Phase 1-3 adjustments needed for companion balance

---

## Phase 4: The Companion

**Duration:** 2-3 months
**Goal:** Mystical companion and magic system
**Deliverable:** Story and magic demo

### 4.1 Companion NPC

| Task | Description | Location |
|------|-------------|----------|
| `CompanionController` | Unique companion AI | `Scripts/NPC/Companion/` |
| `CompanionStateMachine` | Companion states | `Scripts/NPC/Companion/` |
| `CompanionRecovery` | Healing via `ISettlementStats` | `Scripts/NPC/Companion/` |
| `CompanionPhase` | Arc phase tracking | `Scripts/NPC/Companion/` |
| `CompanionFollowing` | Intelligent following | `Scripts/NPC/Companion/` |

```csharp
// CompanionRecovery.cs - Uses ISettlementStats from Phase 2
public class CompanionRecovery : MonoBehaviour
{
    [SerializeField] private CompanionPhaseConfig[] _phaseConfigs;

    private ISettlementStats _settlementStats;
    private CompanionPhase _currentPhase;

    public void Initialize(ISettlementStats stats)
    {
        _settlementStats = stats;
        _settlementStats.OnStatChanged += OnSettlementChanged;
    }

    private void OnSettlementChanged(string stat, float value)
    {
        var progress = _settlementStats.OverallProgress;
        var nextPhase = GetPhaseForProgress(progress);

        if (nextPhase != _currentPhase)
        {
            TransitionToPhase(nextPhase);
        }
    }
}
```

### 4.2 Dialogue System

| Task | Description | Location |
|------|-------------|----------|
| `DialogueNode` SO | Conversation nodes | `ScriptableObjects/Dialogue/` |
| `DialogueTree` SO | Conversation graphs | `ScriptableObjects/Dialogue/` |
| `IDialogueManager` | Dialogue interface | `Scripts/Core/` |
| `DialogueManager` | Run conversations | `Scripts/Core/` |
| `DialogueUI` | Display UI | `Scripts/UI/` |

### 4.3 Magic System

| Task | Description | Location |
|------|-------------|----------|
| `SpellDefinition` SO | Spell parameters | `ScriptableObjects/Magic/` |
| `ISpellRegistry` | Spell lookup interface | `Scripts/Magic/` |
| `SpellRegistry` | Implementation | `Scripts/Magic/` |
| `PlayerMagic` | Casting, mana | `Scripts/Player/` |
| `SpellEffect` | Effect base class | `Scripts/Magic/` |
| Spell implementations | Light, Shield, Fireball, etc. | `Scripts/Magic/Spells/` |

### 4.4 Teaching Mechanic

| Task | Description | Location |
|------|-------------|----------|
| `SpellUnlockCondition` | Learning requirements | `Scripts/Magic/` |
| `TeachingSequence` | Companion teaches spells | `Scripts/NPC/Companion/` |
| `MagicProgression` | Track learned spells | `Scripts/Player/` |

### 4.5 Story Hooks

| Task | Description | Location |
|------|-------------|----------|
| `LoreFragment` SO | Discoverable lore | `ScriptableObjects/Story/` |
| `LoreDiscovery` | Finding lore | `Scripts/Core/` |
| `StoryProgressTracker` | Narrative state | `Scripts/Core/` |
| `JournalUI` | Lore codex | `Scripts/UI/` |

### Phase 4 Save System Additions

Add to save file:
- Companion phase and recovery progress
- Learned spells
- Dialogue history (for conditional dialogue)
- Discovered lore

### Exit Criteria (Phase 4)

- [ ] Companion recovery tied to actual settlement progress
- [ ] Magic system with spell progression
- [ ] Dialogue system with companion conversations
- [ ] Companion phases transition at appropriate thresholds

---

## Phase 5: Content & Polish

**Duration:** 3-4 months
**Goal:** Full game experience
**Deliverable:** Feature-complete single-player

### 5.1 Full Narrative

| Task | Description | Location |
|------|-------------|----------|
| Main story quest chain | Complete story | `ScriptableObjects/Quests/` |
| Side quests | Additional content | `ScriptableObjects/Quests/` |
| All companion dialogue | Full arc | `ScriptableObjects/Dialogue/` |
| Multiple endings | Story resolution | `Scripts/Core/` |

### 5.2 Quest System

| Task | Description | Location |
|------|-------------|----------|
| `Quest` SO | Quest definition | `ScriptableObjects/Quests/` |
| `QuestObjective` | Objectives | `Scripts/Core/` |
| `IQuestManager` | Quest interface | `Scripts/Core/` |
| `QuestManager` | Track quests | `Scripts/Core/` |
| `QuestUI` | Quest log | `Scripts/UI/` |

### 5.3 Audio Implementation

| Task | Description | Location |
|------|-------------|----------|
| `IAudioManager` | Audio interface | `Scripts/Core/` |
| `AudioManager` | Central control | `Scripts/Core/` |
| `MusicController` | Dynamic music | `Scripts/Audio/` |
| `AmbientController` | Environmental | `Scripts/Audio/` |
| `SFXPlayer` | Sound effects | `Scripts/Audio/` |

### 5.4 Full Weather System

Replace stub from Phase 1:

| Task | Description | Location |
|------|-------------|----------|
| `WeatherSystem` | Full implementation | `Scripts/World/` |
| `WeatherEffects` | Rain, snow, fog VFX | `Scripts/World/` |
| Weather gameplay effects | Visibility, NPC behavior | `Scripts/World/` |

### 5.5 Visual Effects

| Task | Description | Location |
|------|-------------|----------|
| Particle systems | Combat, magic, env | `Prefabs/VFX/` |
| Post-processing | Mood-based settings | `_Project/Settings/` |
| Block VFX | Break/place feedback | `Prefabs/VFX/` |

### 5.6 Animation Polish

| Task | Description | Location |
|------|-------------|----------|
| Player animations | Blend trees | `Animations/Player/` |
| NPC animations | Work, idle, combat | `Animations/NPC/` |
| Monster animations | Full sets | `Animations/Monsters/` |
| Companion animations | Unique anims | `Animations/Companion/` |

### 5.7 Tutorial System

| Task | Description | Location |
|------|-------------|----------|
| `TutorialManager` | Tutorial flow | `Scripts/UI/` |
| `TutorialStep` SO | Tutorial steps | `ScriptableObjects/Tutorial/` |
| Context hints | Helpful tooltips | `Scripts/UI/` |

### 5.8 Balancing Pass

| Task | Description | Location |
|------|-------------|----------|
| Difficulty settings | Easy/Normal/Hard | `Scripts/Core/` |
| Progression tuning | Resource/monster scaling | ScriptableObjects |
| Combat balance | Damage/health values | ScriptableObjects |
| Economy balance | Crafting costs | ScriptableObjects |

### Exit Criteria (Phase 5)

- [ ] Feature-complete single-player
- [ ] Full audio and visual polish
- [ ] Weather system affects gameplay
- [ ] Balanced difficulty progression

---

## Phase 6: Multiplayer

**Duration:** 2-3 months
**Goal:** Cooperative settlement building
**Deliverable:** Multiplayer beta

### 6.1 Network Architecture

| Task | Description | Location |
|------|-------------|----------|
| Networking solution | Netcode for GameObjects / Mirror | N/A |
| `NetworkManager` | Connection management | `Scripts/Networking/` |
| `SessionManager` | Host/join, lobbies | `Scripts/Networking/` |
| Authority implementation | Server-authoritative systems | `Scripts/Networking/` |

### 6.2 State Synchronization

| Task | Description | Location |
|------|-------------|----------|
| `NetworkedVoxelWorld` | Chunk sync | `Scripts/Networking/` |
| `NetworkedTaskManager` | Task sync | `Scripts/Networking/` |
| `NetworkedInventory` | Inventory sync | `Scripts/Networking/` |
| `NetworkedNPCController` | NPC sync | `Scripts/Networking/` |

### 6.3 Shared Settlement

| Task | Description | Location |
|------|-------------|----------|
| Multi-player task claiming | Conflict resolution | `Scripts/Networking/` |
| Simultaneous building | Block placement sync | `Scripts/Networking/` |
| `PermissionSystem` | Build/destroy perms | `Scripts/Core/` |

### 6.4 Drop-in/Drop-out

| Task | Description | Location |
|------|-------------|----------|
| Connection handling | Seamless join/leave | `Scripts/Networking/` |
| State catch-up | Sync new players | `Scripts/Networking/` |
| Host migration | Handle host disconnect | `Scripts/Networking/` |

### Exit Criteria (Phase 6)

- [ ] Multiple players can build together
- [ ] Stable network play
- [ ] Seamless connection handling

---

## Phase 7: Launch Preparation

**Duration:** 2-3 months
**Goal:** Ship-ready game
**Deliverable:** Version 1.0

### 7.1 QA & Bug Fixing

| Task | Description |
|------|-------------|
| Bug tracking | GitHub Issues |
| Regression testing | Automated tests |
| Edge case testing | Unusual behaviors |
| Save compatibility | Migration testing |

### 7.2 Performance Optimization

| Task | Description |
|------|-------------|
| Profiling pass | Unity Profiler |
| Chunk LOD | Distance-based detail |
| Draw call batching | GPU instancing |
| Memory optimization | GC reduction |
| Load time optimization | Async loading |

### 7.3 Accessibility

| Task | Description | Commitment |
|------|-------------|------------|
| Remappable controls | Full rebinding support | **Committed** |
| Colorblind modes | Deuteranopia, Protanopia, Tritanopia filters | **Committed** |
| Text scaling | UI scale options (75%-150%) | **Committed** |
| Subtitle options | Size, background, speaker labels | **Committed** |
| Reduce motion option | Disable screen shake, reduce particles | **Committed** |
| Screen reader (menus/UI) | Labels for menu navigation, inventory, dialogue | **Committed** |
| Screen reader (3D world) | Audio cues for nearby objects, navigation | **Investigate** |

**Screen Reader Scope:**

The menu/UI screen reader support is achievable—Unity UI elements can expose accessibility labels, and navigation is discrete (buttons, lists, grids).

However, **screen reader support for 3D voxel gameplay is genuinely hard** and may not be feasible:
- Spatial audio for "what's around me" is complex
- Voxel grids have thousands of elements
- Building/mining require precise 3D targeting
- No established patterns in the genre

**Recommendation:** Commit to menu/UI accessibility. For 3D gameplay, research what similar games have done (if anything), consult with accessibility experts, and decide post-Phase 5 whether to commit, scope down, or acknowledge as a limitation.

### 7.4 Platform Builds

| Task | Description |
|------|-------------|
| Windows build | x64 |
| Mac build | Universal binary |
| Linux build | x64 |
| Build automation | CI/CD |

### 7.5 Store Presence

| Task | Description |
|------|-------------|
| Steam page | Store page, capsules |
| itch.io page | Alternative store |
| Launch trailer | Polished video |
| Press kit | Screenshots, info |

### Exit Criteria (Phase 7)

- [ ] Stable builds on all platforms
- [ ] Accessibility features implemented
- [ ] Store presence complete
- [ ] Ready for launch

---

## Save System Evolution

The save system evolves with each phase. Don't try to design everything upfront.

### Per-Phase Save Data

| Phase | New Save Data | Migration Notes |
|-------|--------------|-----------------|
| 0A | Chunk data, save header | Base format |
| 0B | (no changes) | N/A |
| 1 | Player state, inventory, time, seed | Add default values for old saves |
| 2 | NPCs, tasks, stockpiles, construction, settlement | NPCs = empty list for old saves |
| 3 | Portals, territory, monsters | Portals = none for old saves |
| 4 | Companion, spells, dialogue, lore | Companion = rescue phase for old saves |
| 5 | Quests, weather | Quests = empty for old saves |
| 6 | Multiplayer state | Separate save format for MP |

### Migration Strategy

```csharp
// SaveMigrator.cs
public class SaveMigrator
{
    public SaveData Migrate(SaveData oldSave)
    {
        var version = oldSave.Header.Version;

        // Chain migrations
        if (version < 2) oldSave = MigrateV1ToV2(oldSave);
        if (version < 3) oldSave = MigrateV2ToV3(oldSave);
        if (version < 4) oldSave = MigrateV3ToV4(oldSave);
        // etc.

        return oldSave;
    }

    private SaveData MigrateV1ToV2(SaveData save)
    {
        // Add Phase 2 data with defaults
        save.NPCs = new List<NPCSaveData>();
        save.Tasks = new List<TaskSaveData>();
        save.Stockpiles = new List<StockpileSaveData>();
        save.Header.Version = 2;
        return save;
    }
}
```

### Backward Compatibility Policy

**Problem:** What happens when a player loads a newer save in an older game version (e.g., after rolling back an update)?

**Policy:** Refuse to load with clear messaging.

```csharp
// SaveLoader.cs
public class SaveLoader
{
    private const int CURRENT_VERSION = 4;

    public LoadResult TryLoad(string path)
    {
        var header = ReadHeader(path);

        // Forward compatibility: older save, newer game = OK (migrate)
        if (header.Version < CURRENT_VERSION)
        {
            return LoadAndMigrate(path);
        }

        // Exact match = OK
        if (header.Version == CURRENT_VERSION)
        {
            return LoadDirect(path);
        }

        // Backward compatibility: newer save, older game = REFUSE
        if (header.Version > CURRENT_VERSION)
        {
            return LoadResult.Failed(
                $"This save was created with a newer version of the game (save v{header.Version}, game v{CURRENT_VERSION}). " +
                $"Please update the game to load this save."
            );
        }
    }
}
```

**Rationale:**
- **Partial loading is dangerous:** Missing data can cause subtle bugs, corrupt the save further, or create inconsistent game state
- **Data loss warnings don't work:** Players click through warnings and then complain about lost progress
- **Rollbacks are rare:** Most players update forward; the few who rollback can update again
- **Clear messaging prevents support tickets:** "Update the game" is actionable

**Exception:** During Early Access, consider a "load anyway (DANGEROUS)" developer flag for debugging player-reported issues with specific saves.

### Save System Testing Per Phase

Each phase must include:
- [ ] Save/load round-trip test
- [ ] Migration test from previous version
- [ ] Corrupt save handling
- [ ] Large save performance test

---

## Multiplayer Compatibility Checklist

Check these items at the end of each phase to avoid painful rewrites.

### Phase 0A Checklist

- [ ] `IVoxelWorld` uses request pattern (`RequestBlockChange` not `SetBlock`)
- [ ] No singleton patterns in core systems
- [ ] Event channels are injectable (not static)
- [ ] Save system has versioning header

### Phase 0B Checklist

- [ ] Chunk loading is deterministic given same inputs
- [ ] No race conditions in async chunk generation

### Phase 1 Checklist

- [ ] `IInventory` uses request pattern
- [ ] `TimeManager` can be server-authoritative
- [ ] Monster spawning can be server-controlled
- [ ] Player actions are discrete events (not continuous state)

### Phase 2 Checklist

- [ ] `ITaskManager` uses request pattern
- [ ] Task claiming handles race conditions
- [ ] Stockpile reservations are atomic
- [ ] NPC state changes are events, not polling

### Phase 3 Checklist

- [ ] Portal state changes are authoritative
- [ ] Territory changes are events
- [ ] Combat damage is server-validated

### Phase 4 Checklist

- [ ] Companion state is shared correctly
- [ ] Spell casting is server-validated
- [ ] Dialogue state can be per-player or shared

### Phase 5 Checklist

- [ ] Quest progress sync strategy defined
- [ ] Weather is server-authoritative

---

## ScriptableObject Scaling Strategy

### Foundation (Implemented in Phase 0A)

`IContentRegistry<T>` and `DirectRegistry<T>` are implemented from Phase 0A (see [0A.5 Core Framework](#0a5-core-framework)). This means all code uses the interface from day one—no refactoring call sites later.

```csharp
// All registries inherit from DirectRegistry<T>
public class BlockRegistry : DirectRegistry<BlockType> { }
public class ItemRegistry : DirectRegistry<ItemDefinition> { }
public class RecipeRegistry : DirectRegistry<Recipe> { }
// etc.
```

### Current Approach (< 100 items per type)

`DirectRegistry<T>` works fine—simple array + dictionary lookup. No async, no complexity.

### Scaling Concerns (100+ items)

| Issue | Symptom | Solution |
|-------|---------|----------|
| Editor slowdown | Inspector lag | Use Addressables |
| Memory usage | All SOs loaded always | Lazy loading |
| Build size | Large initial download | Asset bundles |
| Load time | Slow startup | Async loading |

### Migration Path (Phase 5+ if needed)

Since all call sites use `IContentRegistry<T>`, migration is straightforward:

```csharp
// AddressableRegistry.cs - Swap in when needed
public class AddressableRegistry<T> : IContentRegistry<T> where T : ScriptableObject
{
    private readonly string _label;
    private Dictionary<string, T> _loaded = new();

    public T Get(string id)
    {
        if (!_loaded.TryGetValue(id, out var item))
        {
            // Sync load for immediate need (use sparingly)
            item = Addressables.LoadAssetAsync<T>(id).WaitForCompletion();
            _loaded[id] = item;
        }
        return item;
    }

    public async Task<T> GetAsync(string id)
    {
        if (!_loaded.TryGetValue(id, out var item))
        {
            item = await Addressables.LoadAssetAsync<T>(id);
            _loaded[id] = item;
        }
        return item;
    }

    public void Preload(IEnumerable<string> ids)
    {
        // Batch load for level start, etc.
    }
}

// Swap implementation via ServiceLocator or DI
services.Register<IContentRegistry<BlockType>>(new AddressableRegistry<BlockType>("blocks"));
```

### When to Migrate

Consider migration if:
- Editor becomes sluggish with SO inspector
- Build size exceeds 500MB+ from content alone
- Load times exceed 10 seconds on target hardware
- Memory usage exceeds comfortable margins

---

## Implementation Summary

```
Phase 0A: Minimal Foundation (4-6 weeks)
├── Project setup
├── Naive voxel world (works, not optimized)
├── Basic player controller
├── Minimal save system
└── Core framework (no singletons)

Phase 0B: Foundation Optimization (3-4 weeks)
├── Greedy meshing
├── Chunk loading/unloading
└── Block registry

Phase 1: Playable Prototype (2-3 months)
├── World generation
├── Survival systems
├── Inventory & crafting
├── Day/night + weather stub
├── Monster system (IMonsterAI interface)
└── Basic UI

** VERTICAL SLICE MILESTONE **
├── One biome fully polished
├── Integration tested
└── External playtest

Phase 2: Colony Alpha (2-3 months)
├── NPC core system
├── Task system + interruption handling
├── Stockpile system
├── Construction system
├── Settlement stats (ISettlementStats)
└── NPC arrival

Phase 3: Combat & Threats (2-3 months)
├── Player combat
├── NPC combat
├── Expanded monsters (IMonsterAI implementations)
├── Portal system
├── Territory control
└── Defensive structures

** PHASE 4.0 INTEGRATION CHECKPOINT **
├── Verify ISettlementStats metrics
├── Balance companion thresholds
└── Playtest progression

Phase 4: The Companion (2-3 months)
├── Companion NPC + recovery
├── Dialogue system
├── Magic system
├── Teaching mechanic
└── Story hooks

Phase 5: Content & Polish (3-4 months)
├── Full narrative + quests
├── Audio implementation
├── Full weather system
├── Visual effects + animation
├── Tutorial system
└── Balancing pass

Phase 6: Multiplayer (2-3 months)
├── Network architecture
├── State synchronization
├── Shared settlement
└── Drop-in/drop-out

Phase 7: Launch (2-3 months)
├── QA & bug fixing
├── Performance optimization
├── Accessibility
├── Platform builds
└── Store presence
```

---

## References

- [Game Vision](VISION.md) - Creative direction and design principles
- [Development Roadmap](ROADMAP.md) - Timeline and milestones
- [NPC System Design](NPC_SYSTEM_DESIGN.md) - NPC behavior architecture
- [Contributing Guide](../CONTRIBUTING.md) - Code standards

---

**Document Created:** November 2025
**Version:** 2.1
**Changelog:**
- v2.1: Added vertical slice fail criteria, save backward compatibility policy, moved IContentRegistry to Phase 0A, scoped screen reader accessibility
- v2.0: Split Phase 0, added integration checkpoints, task recovery, MP compatibility, accessibility
- v1.0: Initial development plan
