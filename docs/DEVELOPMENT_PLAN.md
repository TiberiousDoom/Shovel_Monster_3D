# Development Plan

**Last Updated:** November 2025
**Status:** Active
**Purpose:** Detailed implementation guide for coding the Voxel RPG Game

---

## Table of Contents

1. [Overview](#overview)
2. [Phase 0: Foundation](#phase-0-foundation)
3. [Phase 1: Playable Prototype](#phase-1-playable-prototype)
4. [Phase 2: Colony Alpha](#phase-2-colony-alpha)
5. [Phase 3: Combat & Threats](#phase-3-combat--threats)
6. [Phase 4: The Companion](#phase-4-the-companion)
7. [Phase 5: Content & Polish](#phase-5-content--polish)
8. [Phase 6: Multiplayer](#phase-6-multiplayer)
9. [Phase 7: Launch Preparation](#phase-7-launch-preparation)
10. [Implementation Summary](#implementation-summary)

---

## Overview

This document provides a detailed breakdown of each development phase with specific coding tasks, file locations, and exit criteria. It supplements the [ROADMAP.md](ROADMAP.md) with implementation-level details.

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
│   │   └── Utilities/     # Helpers, Extensions
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

## Phase 0: Foundation

**Duration:** 2-3 months
**Goal:** Establish core Unity systems

### 0.1 Project Setup

| Task | Description | Location |
|------|-------------|----------|
| Create Unity project | Unity 2022 LTS with URP | Root |
| Configure folder structure | Per CONTRIBUTING.md standards | `Assets/_Project/` |
| Git configuration | .gitignore, .gitattributes for Unity | Root |
| Assembly definitions | Separate assemblies for core systems | `Scripts/` subdirs |
| Input System setup | New Input System package | `_Project/Scripts/Core/` |

### 0.2 Voxel World System

| Task | Description | Location |
|------|-------------|----------|
| `BlockType` ScriptableObject | Define block properties (hardness, drops, transparency) | `ScriptableObjects/Blocks/` |
| `BlockRegistry` | Central registry for all block types | `Scripts/Voxel/` |
| `VoxelChunk` | 16x16x16 chunk data structure | `Scripts/Voxel/` |
| `ChunkMeshBuilder` | Greedy meshing for chunk rendering | `Scripts/Voxel/` |
| `VoxelWorld` | Chunk management, loading/unloading | `Scripts/Voxel/` |
| `WorldGenerator` | Interface for procedural generation | `Scripts/Voxel/` |
| Block modification events | Event system for block changes | `Scripts/Voxel/` |

**Key Classes:**

```csharp
// BlockType.cs - ScriptableObject
public class BlockType : ScriptableObject
{
    public string Id;
    public float Hardness;
    public ResourceDrop[] Drops;
    public bool IsTransparent;
    public bool IsSolid;
}

// VoxelChunk.cs
public class VoxelChunk : MonoBehaviour
{
    public const int SIZE = 16;
    private BlockType[,,] _blocks;

    public BlockType GetBlock(Vector3Int localPos);
    public void SetBlock(Vector3Int localPos, BlockType type);
    public void RebuildMesh();
}

// VoxelWorld.cs
public class VoxelWorld : MonoBehaviour
{
    public BlockType GetBlock(Vector3Int worldPos);
    public void SetBlock(Vector3Int worldPos, BlockType type);
    public VoxelChunk GetOrCreateChunk(Vector3Int chunkPos);
}
```

### 0.3 Player Controller

| Task | Description | Location |
|------|-------------|----------|
| `PlayerController` | WASD movement, jumping, gravity | `Scripts/Player/` |
| `PlayerCamera` | First/third person camera system | `Scripts/Player/` |
| `BlockInteraction` | Raycasting, place/remove blocks | `Scripts/Player/` |
| `PlayerInput` | Input action asset configuration | `_Project/Input/` |

### 0.4 Save/Load Architecture

| Task | Description | Location |
|------|-------------|----------|
| `ISaveable` interface | Contract for saveable objects | `Scripts/Core/` |
| `SaveManager` | Serialize/deserialize game state | `Scripts/Core/` |
| `ChunkSerializer` | Binary serialization for chunks | `Scripts/Voxel/` |
| Save file versioning | Handle save format migrations | `Scripts/Core/` |

**Key Interfaces:**

```csharp
// ISaveable.cs
public interface ISaveable
{
    string SaveId { get; }
    object CaptureState();
    void RestoreState(object state);
}
```

### 0.5 Core Framework

| Task | Description | Location |
|------|-------------|----------|
| `GameManager` | Singleton, game state management | `Scripts/Core/` |
| `EventChannel` ScriptableObjects | Decoupled event system | `ScriptableObjects/Events/` |
| `ServiceLocator` | Dependency injection alternative | `Scripts/Core/` |
| Object pooling system | Reusable pool for frequent objects | `Scripts/Utilities/` |

**Event Channel Pattern:**

```csharp
// VoidEventChannel.cs
[CreateAssetMenu(menuName = "Events/Void Event Channel")]
public class VoidEventChannel : ScriptableObject
{
    public event Action OnEventRaised;
    public void RaiseEvent() => OnEventRaised?.Invoke();
}

// BlockChangedEventChannel.cs
[CreateAssetMenu(menuName = "Events/Block Changed Event Channel")]
public class BlockChangedEventChannel : ScriptableObject
{
    public event Action<Vector3Int, BlockType> OnEventRaised;
    public void RaiseEvent(Vector3Int pos, BlockType type) => OnEventRaised?.Invoke(pos, type);
}
```

### Exit Criteria

- [ ] Player can move through a voxel world
- [ ] Blocks can be placed and removed
- [ ] Game state can be saved and loaded

---

## Phase 1: Playable Prototype

**Duration:** 2-3 months
**Goal:** Core survival gameplay loop
**Deliverable:** Playable survival demo

### 1.1 World Generation

| Task | Description | Location |
|------|-------------|----------|
| `TerrainGenerator` | Noise-based terrain heightmaps | `Scripts/Voxel/Generation/` |
| `BiomeDefinition` ScriptableObject | Biome parameters (blocks, vegetation) | `ScriptableObjects/Biomes/` |
| `BiomeManager` | Biome selection per region | `Scripts/Voxel/Generation/` |
| `OreGenerator` | Ore vein placement | `Scripts/Voxel/Generation/` |
| `VegetationGenerator` | Trees, plants, grass | `Scripts/Voxel/Generation/` |
| `StructureGenerator` | Ruins, caves | `Scripts/Voxel/Generation/` |
| World seed system | Deterministic generation | `Scripts/Voxel/` |

**Key Classes:**

```csharp
// BiomeDefinition.cs
[CreateAssetMenu(menuName = "World/Biome Definition")]
public class BiomeDefinition : ScriptableObject
{
    public string BiomeName;
    public BlockType SurfaceBlock;
    public BlockType SubsurfaceBlock;
    public float MinHeight;
    public float MaxHeight;
    public float Temperature;
    public float Humidity;
    public VegetationConfig[] Vegetation;
    public OreConfig[] Ores;
}
```

### 1.2 Player Survival Systems

| Task | Description | Location |
|------|-------------|----------|
| `HealthSystem` | HP, damage, healing, death | `Scripts/Player/` |
| `HungerSystem` | Food consumption, starvation effects | `Scripts/Player/` |
| `PlayerStats` | Central stat management | `Scripts/Player/` |
| `DamageSystem` | Unified damage handling | `Scripts/Combat/` |
| `DeathHandler` | Death, respawn logic | `Scripts/Player/` |

### 1.3 Inventory & Items

| Task | Description | Location |
|------|-------------|----------|
| `ItemDefinition` ScriptableObject | Item properties, stacking, categories | `ScriptableObjects/Items/` |
| `Inventory` | Generic container system | `Scripts/Core/` |
| `PlayerInventory` | Player-specific inventory | `Scripts/Player/` |
| `ItemDrop` | Dropped item in world | `Scripts/Core/` |
| `ToolItem` | Base class for tools | `Scripts/Player/` |

**Key Classes:**

```csharp
// ItemDefinition.cs
[CreateAssetMenu(menuName = "Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string ItemId;
    public string DisplayName;
    public Sprite Icon;
    public int MaxStackSize = 64;
    public ItemCategory Category;
    public bool IsConsumable;
}

// Inventory.cs
public class Inventory
{
    public int SlotCount { get; }
    public event Action<int> OnSlotChanged;

    public bool TryAddItem(ItemDefinition item, int amount);
    public bool TryRemoveItem(ItemDefinition item, int amount);
    public ItemStack GetSlot(int index);
}
```

### 1.4 Crafting System

| Task | Description | Location |
|------|-------------|----------|
| `Recipe` ScriptableObject | Ingredients, output, station required | `ScriptableObjects/Recipes/` |
| `RecipeRegistry` | All recipes, query by station/category | `Scripts/Core/` |
| `CraftingStation` | Workbench, furnace base class | `Scripts/Building/` |
| `CraftingManager` | Check requirements, execute craft | `Scripts/Core/` |

**Key Classes:**

```csharp
// Recipe.cs
[CreateAssetMenu(menuName = "Crafting/Recipe")]
public class Recipe : ScriptableObject
{
    public Ingredient[] Ingredients;
    public ItemStack Output;
    public CraftingStationType RequiredStation;
    public float CraftTime;
}

[System.Serializable]
public struct Ingredient
{
    public ItemDefinition Item;
    public int Amount;
}
```

### 1.5 Day/Night Cycle

| Task | Description | Location |
|------|-------------|----------|
| `TimeManager` | Game time, day/night state | `Scripts/Core/` |
| `SunController` | Directional light rotation | `Scripts/Core/` |
| `AmbientController` | Skybox, ambient color changes | `Scripts/Core/` |

### 1.6 Basic Threats

| Task | Description | Location |
|------|-------------|----------|
| `MonsterDefinition` ScriptableObject | Stats, behavior parameters | `ScriptableObjects/Monsters/` |
| `MonsterAI` | Basic chase/attack behavior | `Scripts/Combat/` |
| `MonsterSpawner` | Night spawning logic | `Scripts/Combat/` |
| `Hitbox`/`Hurtbox` | Combat collision system | `Scripts/Combat/` |

### 1.7 Basic UI

| Task | Description | Location |
|------|-------------|----------|
| `HUDController` | Health, hunger, hotbar display | `Scripts/UI/` |
| `InventoryUI` | Inventory screen | `Scripts/UI/` |
| `CraftingUI` | Crafting interface | `Scripts/UI/` |
| `PauseMenu` | Pause, settings, quit | `Scripts/UI/` |
| `UIManager` | Screen state management | `Scripts/UI/` |

### Exit Criteria

- [ ] Player can survive, craft tools, build shelter
- [ ] Day/night cycle functions with night dangers
- [ ] Complete survival gameplay loop

---

## Phase 2: Colony Alpha

**Duration:** 2-3 months
**Goal:** NPC settlement building
**Deliverable:** Colony management demo

### 2.1 NPC Core System

| Task | Description | Location |
|------|-------------|----------|
| `NPCDefinition` ScriptableObject | Base NPC stats, appearance | `ScriptableObjects/NPCs/` |
| `NPCController` | Movement, pathfinding integration | `Scripts/NPC/` |
| `NPCStateMachine` | State machine framework | `Scripts/NPC/` |
| `NPCNeeds` | Hunger, rest, morale | `Scripts/NPC/` |
| `NPCInventory` | Carrying capacity | `Scripts/NPC/` |

**State Machine Pattern:**

```csharp
// INPCState.cs
public interface INPCState
{
    void Enter(NPCController npc);
    void Update(NPCController npc, float deltaTime);
    void Exit(NPCController npc);
}

// NPCStateMachine.cs
public class NPCStateMachine
{
    private INPCState _currentState;
    private NPCController _npc;

    public void ChangeState(INPCState newState)
    {
        _currentState?.Exit(_npc);
        _currentState = newState;
        _currentState?.Enter(_npc);
    }

    public void Update(float deltaTime)
    {
        _currentState?.Update(_npc, deltaTime);
    }
}
```

### 2.2 Personality System

| Task | Description | Location |
|------|-------------|----------|
| `PersonalityTrait` ScriptableObject | Trait definitions and effects | `ScriptableObjects/NPCs/` |
| `NPCPersonality` | Trait combination, behavior modifiers | `Scripts/NPC/` |
| `NPCNameGenerator` | Random name generation | `Scripts/NPC/` |
| `NPCRelationships` | Relationship tracking between NPCs | `Scripts/NPC/` |

### 2.3 Task System

Based on [NPC_SYSTEM_DESIGN.md](NPC_SYSTEM_DESIGN.md):

| Task | Description | Location |
|------|-------------|----------|
| `ITask` interface | Base task contract | `Scripts/NPC/Tasks/` |
| `TaskQueue` | Priority queue for tasks | `Scripts/NPC/Tasks/` |
| `TaskManager` | Central task coordination | `Scripts/NPC/Tasks/` |
| `MiningTask` | Block extraction task | `Scripts/NPC/Tasks/` |
| `HaulTask` | Resource transport task | `Scripts/NPC/Tasks/` |
| `BuildTask` | Block placement task | `Scripts/NPC/Tasks/` |
| Task lifecycle handling | Claim, progress, complete, fail | `Scripts/NPC/Tasks/` |

**Key Classes:**

```csharp
// ITask.cs
public interface ITask
{
    string TaskId { get; }
    TaskType Type { get; }
    Vector3 Position { get; }
    int Priority { get; }
    TaskStatus Status { get; }
    string AssignedNpcId { get; }

    bool CanBeClaimed(NPCController npc);
    void Claim(NPCController npc);
    void Execute(NPCController npc, float deltaTime);
    void Complete();
    void Cancel();
}

// TaskManager.cs
public class TaskManager : MonoBehaviour
{
    private PriorityQueue<ITask> _taskQueue;

    public void AddTask(ITask task);
    public ITask FindBestTask(NPCController npc);
    public void CompleteTask(string taskId);
    public void CancelTask(string taskId);
}
```

### 2.4 NPC Worker States

| Task | Description | Location |
|------|-------------|----------|
| `IdleState` | Waiting for work | `Scripts/NPC/States/` |
| `SeekingTaskState` | Finding available work | `Scripts/NPC/States/` |
| `TravelingState` | Moving to task location | `Scripts/NPC/States/` |
| `MiningState` | Extracting blocks | `Scripts/NPC/States/` |
| `HaulingState` | Pickup and delivery | `Scripts/NPC/States/` |
| `BuildingState` | Placing blocks | `Scripts/NPC/States/` |
| `RestingState` | Sleep, recovery | `Scripts/NPC/States/` |

### 2.5 Stockpile System

| Task | Description | Location |
|------|-------------|----------|
| `Stockpile` | Storage zone definition | `Scripts/Building/` |
| `StockpileSlot` | Individual storage slot | `Scripts/Building/` |
| `StockpileManager` | Find nearest deposit/withdraw | `Scripts/Building/` |
| `ResourceFilter` | Filter allowed resources | `Scripts/Building/` |
| Zone designation UI | Player marks stockpile areas | `Scripts/UI/` |

**Key Classes:**

```csharp
// Stockpile.cs
public class Stockpile : MonoBehaviour
{
    public string StockpileId { get; }
    public Bounds Bounds { get; }
    public ResourceFilter AllowedResources;
    public int Capacity { get; }

    public StockpileSlot FindEmptySlot();
    public StockpileSlot FindSlotWith(ItemDefinition resource);
    public bool TryReserveSlot(int slotIndex, out StockpileSlot slot);
    public void ReleaseReservation(int slotIndex);
}
```

### 2.6 Building Orchestrator

| Task | Description | Location |
|------|-------------|----------|
| `BuildingOrchestrator` | Central coordinator | `Scripts/Building/` |
| `MiningManager` | Mining designation handling | `Scripts/Building/` |
| `HaulingManager` | Haul task coordination | `Scripts/Building/` |
| `ConstructionManager` | Construction site management | `Scripts/Building/` |
| Mining designation UI | Player marks blocks to mine | `Scripts/UI/` |

**Key Classes:**

```csharp
// BuildingOrchestrator.cs
public class BuildingOrchestrator : MonoBehaviour
{
    [SerializeField] private MiningManager _miningManager;
    [SerializeField] private HaulingManager _haulingManager;
    [SerializeField] private ConstructionManager _constructionManager;
    [SerializeField] private StockpileManager _stockpileManager;

    public void DesignateMining(Vector3Int position);
    public void DesignateMiningRegion(Vector3Int min, Vector3Int max);
    public void CreateStockpile(Bounds bounds, ResourceFilter filter);
    public void StartConstruction(Blueprint blueprint, Vector3 position);
    public void RegisterWorker(NPCController npc);
}
```

### 2.7 Construction System

| Task | Description | Location |
|------|-------------|----------|
| `Blueprint` ScriptableObject | Building templates | `ScriptableObjects/Blueprints/` |
| `ConstructionSite` | Active construction tracking | `Scripts/Building/` |
| `BuildOrderCalculator` | Structural build order | `Scripts/Building/` |
| Blueprint placement UI | Ghost preview, validation | `Scripts/UI/` |

**Key Classes:**

```csharp
// Blueprint.cs
[CreateAssetMenu(menuName = "Building/Blueprint")]
public class Blueprint : ScriptableObject
{
    public string BlueprintId;
    public string DisplayName;
    public BlockPlacement[] Blocks;
    public Dictionary<ItemDefinition, int> Requirements;

    public BlockPlacement[] GetBuildOrder();
}

[System.Serializable]
public struct BlockPlacement
{
    public Vector3Int Offset;
    public BlockType BlockType;
    public int Rotation;
}
```

### 2.8 NPC Arrival System

| Task | Description | Location |
|------|-------------|----------|
| `SettlementStats` | Track settlement attractiveness | `Scripts/Core/` |
| `NPCArrivalManager` | Spawn new settlers over time | `Scripts/NPC/` |
| NPC introduction events | Dialogue, joining animation | `Scripts/NPC/` |

### Exit Criteria

- [ ] NPCs arrive and autonomously work
- [ ] Mining, hauling, stockpiling, building all function
- [ ] NPCs have visible personalities

---

## Phase 3: Combat & Threats

**Duration:** 2-3 months
**Goal:** Meaningful conflict and portal system
**Deliverable:** Combat and defense demo

### 3.1 Player Combat

| Task | Description | Location |
|------|-------------|----------|
| `PlayerCombat` | Attack input, combos | `Scripts/Player/` |
| `WeaponDefinition` ScriptableObject | Weapon stats, attack patterns | `ScriptableObjects/Items/` |
| `MeleeWeapon` | Swing, hitbox timing | `Scripts/Combat/` |
| `RangedWeapon` | Projectile spawning, aiming | `Scripts/Combat/` |
| `DodgeSystem` | Dodge roll, i-frames | `Scripts/Player/` |
| `BlockSystem` | Shield blocking | `Scripts/Player/` |

### 3.2 NPC Combat

| Task | Description | Location |
|------|-------------|----------|
| `GuardRole` | Combat-focused NPC role | `Scripts/NPC/Roles/` |
| `CombatState` | NPC fighting state | `Scripts/NPC/States/` |
| `FleeState` | NPC fleeing state | `Scripts/NPC/States/` |
| `ThreatDetection` | Awareness of enemies | `Scripts/NPC/` |
| Patrol behavior | Guard patrol routes | `Scripts/NPC/` |

### 3.3 Monster System

| Task | Description | Location |
|------|-------------|----------|
| `MonsterTypes` ScriptableObjects | Varied monster definitions | `ScriptableObjects/Monsters/` |
| `MeleeMonsterAI` | Close-range attackers | `Scripts/Combat/` |
| `RangedMonsterAI` | Ranged attackers | `Scripts/Combat/` |
| `SwarmBehavior` | Group coordination | `Scripts/Combat/` |
| `BossMonster` | Boss encounter framework | `Scripts/Combat/` |

### 3.4 Portal System

| Task | Description | Location |
|------|-------------|----------|
| `Portal` | Portal entity, corruption radius | `Scripts/World/` |
| `PortalSpawner` | Monster spawning from portals | `Scripts/Combat/` |
| `PortalClosingMechanic` | Requirements to close portals | `Scripts/World/` |
| `CorruptionSystem` | Territory corruption spread | `Scripts/World/` |
| `PortalReopening` | Undefended portals reopen | `Scripts/World/` |

**Key Classes:**

```csharp
// Portal.cs
public class Portal : MonoBehaviour
{
    public PortalState State { get; private set; }
    public float CorruptionRadius;
    public float SpawnRate;

    public void StartClosingRitual(PlayerController player);
    public void Close();
    public void AttemptReopen();
}

public enum PortalState { Active, Closing, Closed, Reopening }
```

### 3.5 Territory Control

| Task | Description | Location |
|------|-------------|----------|
| `TerritoryManager` | Track claimed vs corrupted | `Scripts/World/` |
| `ClaimMarker` | Claim territory for settlement | `Scripts/Building/` |
| Territory visualization | Map UI, world indicators | `Scripts/UI/` |
| Border defense tracking | Monitor territory edges | `Scripts/World/` |

### 3.6 Defensive Structures

| Task | Description | Location |
|------|-------------|----------|
| `Wall` | Basic defensive wall | `Scripts/Building/Defense/` |
| `Gate` | Controllable gate | `Scripts/Building/Defense/` |
| `Trap` | Damage traps | `Scripts/Building/Defense/` |
| `Tower` | Archer tower placement | `Scripts/Building/Defense/` |
| Defensive structure blueprints | Pre-made defense templates | `ScriptableObjects/Blueprints/` |

### Exit Criteria

- [ ] Player and NPCs can fight monsters
- [ ] Portal system with closing/reopening mechanics
- [ ] Defensive structures protect settlements

---

## Phase 4: The Companion

**Duration:** 2-3 months
**Goal:** Mystical companion and magic system
**Deliverable:** Story and magic demo

### 4.1 Companion NPC

| Task | Description | Location |
|------|-------------|----------|
| `CompanionController` | Unique companion AI | `Scripts/NPC/Companion/` |
| `CompanionStateMachine` | Companion-specific states | `Scripts/NPC/Companion/` |
| `CompanionRecovery` | Healing tied to player progress | `Scripts/NPC/Companion/` |
| `CompanionPhase` | Track companion arc phase | `Scripts/NPC/Companion/` |
| Companion following behavior | Stay near player intelligently | `Scripts/NPC/Companion/` |

**Key Classes:**

```csharp
// CompanionController.cs
public class CompanionController : MonoBehaviour
{
    public CompanionPhase CurrentPhase { get; private set; }
    public float RecoveryProgress { get; private set; }

    public void UpdateRecovery(SettlementStats stats);
    public bool CanTeachSpell(SpellDefinition spell);
    public void TeachSpell(SpellDefinition spell, PlayerController player);
}

public enum CompanionPhase { Rescue, Recovery, Partnership, Revelation }
```

### 4.2 Dialogue System

| Task | Description | Location |
|------|-------------|----------|
| `DialogueNode` ScriptableObject | Conversation nodes | `ScriptableObjects/Dialogue/` |
| `DialogueTree` ScriptableObject | Conversation graphs | `ScriptableObjects/Dialogue/` |
| `DialogueManager` | Run conversations | `Scripts/Core/` |
| `DialogueUI` | Conversation display | `Scripts/UI/` |
| Companion dialogue content | Per-phase conversations | `ScriptableObjects/Dialogue/` |

**Key Classes:**

```csharp
// DialogueNode.cs
[CreateAssetMenu(menuName = "Dialogue/Node")]
public class DialogueNode : ScriptableObject
{
    public string SpeakerName;
    public string DialogueText;
    public DialogueChoice[] Choices;
    public DialogueNode NextNode;
    public UnityEvent OnNodeEnter;
}

[System.Serializable]
public struct DialogueChoice
{
    public string ChoiceText;
    public DialogueNode NextNode;
    public Condition[] Requirements;
}
```

### 4.3 Magic System

| Task | Description | Location |
|------|-------------|----------|
| `SpellDefinition` ScriptableObject | Spell parameters, effects | `ScriptableObjects/Magic/` |
| `SpellRegistry` | All available spells | `Scripts/Magic/` |
| `PlayerMagic` | Spell casting, mana | `Scripts/Player/` |
| `SpellEffect` base class | Base for spell effects | `Scripts/Magic/` |
| Individual spells | Light, Protection, Combat, Utility | `Scripts/Magic/Spells/` |

**Key Classes:**

```csharp
// SpellDefinition.cs
[CreateAssetMenu(menuName = "Magic/Spell Definition")]
public class SpellDefinition : ScriptableObject
{
    public string SpellId;
    public string DisplayName;
    public Sprite Icon;
    public float ManaCost;
    public float Cooldown;
    public float CastTime;
    public SpellEffect Effect;
    public CompanionPhase RequiredPhase;
}

// SpellEffect.cs
public abstract class SpellEffect : ScriptableObject
{
    public abstract void Execute(PlayerController caster, Vector3 targetPosition);
}
```

### 4.4 Teaching Mechanic

| Task | Description | Location |
|------|-------------|----------|
| `SpellUnlockCondition` | Requirements to learn spell | `Scripts/Magic/` |
| `TeachingDialogue` | Companion teaches spells | `ScriptableObjects/Dialogue/` |
| `MagicProgression` | Track learned spells | `Scripts/Player/` |
| Learning animation/ritual | Visual feedback for learning | `Scripts/Magic/` |

### 4.5 Story Hooks

| Task | Description | Location |
|------|-------------|----------|
| `LoreFragment` ScriptableObject | Discoverable lore pieces | `ScriptableObjects/Story/` |
| `LoreDiscovery` | Finding lore in world | `Scripts/Core/` |
| `StoryProgressTracker` | Track narrative state | `Scripts/Core/` |
| Lore UI (journal/codex) | View discovered lore | `Scripts/UI/` |

### Exit Criteria

- [ ] Companion functions with recovery arc
- [ ] Magic system with spell progression
- [ ] Dialogue system with companion conversations

---

## Phase 5: Content & Polish

**Duration:** 3-4 months
**Goal:** Full game experience
**Deliverable:** Feature-complete single-player

### 5.1 Full Narrative

| Task | Description | Location |
|------|-------------|----------|
| Main story quest chain | Complete optional story | `ScriptableObjects/Quests/` |
| Side quest content | Additional objectives | `ScriptableObjects/Quests/` |
| All companion dialogue | Full arc conversations | `ScriptableObjects/Dialogue/` |
| Endings/resolution | Story conclusion options | `Scripts/Core/` |

### 5.2 Quest System

| Task | Description | Location |
|------|-------------|----------|
| `Quest` ScriptableObject | Quest definition | `ScriptableObjects/Quests/` |
| `QuestObjective` | Individual objectives | `Scripts/Core/` |
| `QuestManager` | Track active quests | `Scripts/Core/` |
| `QuestUI` | Quest log, tracking | `Scripts/UI/` |
| Quest rewards | Items, unlocks, XP | `Scripts/Core/` |

**Key Classes:**

```csharp
// Quest.cs
[CreateAssetMenu(menuName = "Quests/Quest")]
public class Quest : ScriptableObject
{
    public string QuestId;
    public string Title;
    public string Description;
    public QuestObjective[] Objectives;
    public QuestReward[] Rewards;
    public Quest[] Prerequisites;
    public bool IsMainQuest;
}
```

### 5.3 Audio Implementation

| Task | Description | Location |
|------|-------------|----------|
| `AudioManager` | Central audio control | `Scripts/Core/` |
| `MusicController` | Dynamic music system | `Scripts/Audio/` |
| `AmbientController` | Environmental audio | `Scripts/Audio/` |
| `SFXPlayer` | Sound effect playback | `Scripts/Audio/` |
| Audio events | Event-driven audio | `ScriptableObjects/Audio/` |

### 5.4 Visual Effects

| Task | Description | Location |
|------|-------------|----------|
| Particle systems | Combat, magic, environment | `Prefabs/VFX/` |
| `WeatherSystem` | Rain, snow, fog | `Scripts/World/` |
| Post-processing profiles | Mood-based PP settings | `_Project/Settings/` |
| Block breaking/placing VFX | Feedback particles | `Prefabs/VFX/` |

### 5.5 Animation Polish

| Task | Description | Location |
|------|-------------|----------|
| Player animation blend tree | Smooth movement animations | `Animations/Player/` |
| NPC animation controller | Work, idle, combat anims | `Animations/NPC/` |
| Monster animations | Attack, move, death | `Animations/Monsters/` |
| Companion animations | Unique companion anims | `Animations/Companion/` |

### 5.6 Tutorial System

| Task | Description | Location |
|------|-------------|----------|
| `TutorialManager` | Tutorial flow control | `Scripts/UI/` |
| `TutorialStep` ScriptableObject | Individual tutorial steps | `ScriptableObjects/Tutorial/` |
| Context-sensitive hints | Helpful tooltips | `Scripts/UI/` |
| First-time experience | New player onboarding | `Scripts/Core/` |

### 5.7 Balancing Pass

| Task | Description | Location |
|------|-------------|----------|
| Difficulty settings | Easy/Normal/Hard | `Scripts/Core/` |
| Progression curve tuning | Resource scarcity, monster scaling | ScriptableObjects |
| Combat balance | Damage values, health pools | ScriptableObjects |
| Economy balance | Crafting costs, item values | ScriptableObjects |

### Exit Criteria

- [ ] Feature-complete single-player experience
- [ ] Full audio and visual polish
- [ ] Balanced difficulty progression

---

## Phase 6: Multiplayer

**Duration:** 2-3 months
**Goal:** Cooperative settlement building
**Deliverable:** Multiplayer beta

### 6.1 Network Architecture

| Task | Description | Location |
|------|-------------|----------|
| Networking solution selection | Netcode for GameObjects, Mirror, etc. | N/A |
| `NetworkManager` | Connection management | `Scripts/Networking/` |
| `SessionManager` | Host/join, lobbies | `Scripts/Networking/` |
| Player synchronization | Position, actions | `Scripts/Networking/` |

### 6.2 State Synchronization

| Task | Description | Location |
|------|-------------|----------|
| Voxel world sync | Chunk state across clients | `Scripts/Networking/` |
| NPC state sync | NPC positions, tasks | `Scripts/Networking/` |
| Inventory sync | Item movements | `Scripts/Networking/` |
| Combat sync | Damage, health | `Scripts/Networking/` |

### 6.3 Shared Settlement

| Task | Description | Location |
|------|-------------|----------|
| Shared task queue | Multiple players, one settlement | `Scripts/Networking/` |
| Simultaneous building | Multi-player construction | `Scripts/Networking/` |
| `PermissionSystem` | Who can build/destroy | `Scripts/Core/` |
| Shared resources | Stockpile access control | `Scripts/Networking/` |

### 6.4 Drop-in/Drop-out

| Task | Description | Location |
|------|-------------|----------|
| Player connection handling | Seamless join/leave | `Scripts/Networking/` |
| State catch-up | Sync joining players | `Scripts/Networking/` |
| Offline fallback | Host migration or pause | `Scripts/Networking/` |

### Exit Criteria

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
| Comprehensive bug tracking | GitHub Issues with labels |
| Regression testing | Automated test suite |
| Edge case testing | Unusual player behaviors |
| Save compatibility testing | Upgrade paths for saves |

### 7.2 Performance Optimization

| Task | Description |
|------|-------------|
| Profiling pass | Unity Profiler analysis |
| Chunk LOD system | Distance-based detail |
| Draw call batching | GPU instancing, atlasing |
| Memory optimization | Reduce GC allocations |
| Load time optimization | Async loading, streaming |

### 7.3 Platform Builds

| Task | Description |
|------|-------------|
| Windows build | x64, tested |
| Mac build | Universal binary |
| Linux build | x64, distro testing |
| Build automation | CI/CD pipeline |

### 7.4 Store Presence

| Task | Description |
|------|-------------|
| Steam page | Store page, capsules |
| itch.io page | Alternative storefront |
| Trailer creation | Launch trailer |
| Screenshot curation | Best screenshots |
| Marketing materials | Press kit |

### Exit Criteria

- [ ] Stable, performant builds on all platforms
- [ ] Store presence complete
- [ ] Ready for launch

---

## Implementation Summary

```
Phase 0: Foundation
├── Project setup & folder structure
├── Voxel world (chunks, blocks, meshing)
├── Player controller
├── Save/load architecture
└── Core framework (events, services)

Phase 1: Playable Prototype
├── World generation (terrain, biomes, resources)
├── Survival systems (health, hunger)
├── Inventory & items
├── Crafting system
├── Day/night cycle
├── Basic threats
└── Basic UI

Phase 2: Colony Alpha
├── NPC core (controller, states, needs)
├── Personality system
├── Task system (mining, hauling, building)
├── Stockpile system
├── Building orchestrator
├── Construction system
└── NPC arrival

Phase 3: Combat & Threats
├── Player combat (melee, ranged, dodge)
├── NPC combat (guards, fleeing)
├── Monster variety
├── Portal system (spawning, closing, reopening)
├── Territory control
└── Defensive structures

Phase 4: The Companion
├── Companion NPC (unique AI, recovery)
├── Dialogue system
├── Magic system (spells, mana)
├── Teaching mechanic
└── Story hooks

Phase 5: Content & Polish
├── Full narrative & quest system
├── Audio implementation
├── Visual effects & weather
├── Animation polish
├── Tutorial system
└── Balancing pass

Phase 6: Multiplayer
├── Network architecture
├── State synchronization
├── Shared settlement
└── Drop-in/drop-out

Phase 7: Launch
├── QA & bug fixing
├── Performance optimization
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
**Version:** 1.0
