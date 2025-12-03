# Shovel Monster 3D - Claude Code Context

> *Rise from ruin. Build something worth protecting. You're not alone.*

## Project Overview

A voxel-based RPG survival base-building game where an ordinary farmer, guided by a mystical companion, rebuilds civilization in a world torn by divine conflict—with autonomous NPCs who feel like companions, not tools.

**Engine:** Unity (C#)
**Platform:** PC (Windows, Mac, Linux)

## Core Pillars (Priority Order)

1. **Build** - Transform wilderness into a fortified village, block by block
2. **Grow** - Attract NPCs with personalities who live, work, and fight alongside you
3. **Reclaim** - Close portals, clear monsters, and take back the world
4. **Choose** - Engage the story when you want; the sandbox is always there
5. **Connect** - Play solo with your companion, or build together with friends

## Key Design Principles

- **NPCs Should Feel Helpful, Never Like a Chore** - NPCs work autonomously, no micromanagement
- **The Player is a Participant, Not Just a Manager** - Players work alongside NPCs
- **Progression Should Feel Earned** - Gate advancement behind meaningful achievements
- **The World Should Feel Alive** - NPCs have schedules, preferences, relationships
- **Complexity Should Be Discoverable** - Start simple, reveal depth over time

## Project Structure

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
│   │   ├── UI/            # All UI scripts
│   │   └── Utilities/     # Helpers, Extensions, Object Pools
│   ├── ScriptableObjects/
│   │   ├── Blocks/        # BlockType definitions
│   │   ├── Items/         # ItemDefinition, Weapons
│   │   ├── NPCs/          # NPCDefinition, Traits
│   │   ├── Monsters/      # MonsterDefinition
│   │   ├── Recipes/       # Crafting recipes
│   │   ├── Events/        # EventChannel SOs
│   │   └── ...
│   ├── Prefabs/
│   ├── Scenes/
│   └── ...
└── Plugins/
```

## C# Coding Standards

### Naming Conventions

```csharp
public class VoxelChunk { }           // Classes: PascalCase
public interface IInteractable { }    // Interfaces: I + PascalCase
public void PlaceBlock() { }          // Methods: PascalCase
public int BlockCount { get; }        // Properties: PascalCase
private int _currentHealth;           // Private fields: _camelCase
int blockIndex = 0;                   // Local variables: camelCase
public const int MAX_CHUNK_SIZE = 16; // Constants: UPPER_SNAKE_CASE
public enum BlockType { Stone, Wood } // Enums: PascalCase
```

### Code Organization (in order)

1. Constants
2. Serialized fields (`[SerializeField]`)
3. Public properties
4. Private fields
5. Unity lifecycle methods (Awake, Start, Update, etc.)
6. Public methods
7. Private methods
8. Event handlers

### Best Practices

**Do:**
- Use `[SerializeField]` instead of public fields
- Cache component references in `Awake()`
- Use object pooling for frequently spawned objects
- Prefer composition over inheritance
- Use events/delegates for decoupling
- Use interfaces for multiplayer-ready patterns (e.g., `RequestBlockChange` not `SetBlock`)
- Use injectable event channels, not static events

**Don't:**
- Use `Find()` or `GetComponent()` in `Update()`
- Create garbage in hot paths
- Use magic numbers; define constants
- Leave empty Unity callbacks
- Use singletons in core systems

## Development Phases

| Phase | Name | Status |
|-------|------|--------|
| 0 | Foundation | Complete |
| 1 | Playable Prototype | In Progress |
| 2 | Colony Alpha | Planned |
| 3 | Combat & Threats | Planned |
| 4 | The Companion | Planned |
| 5 | Content & Polish | Planned |
| 6 | Multiplayer | Planned |
| 7 | Launch Prep | Planned |

## Key Architecture Patterns

### Authority Model (Multiplayer-Aware)

```csharp
// GOOD: Authority-agnostic interface
public interface IVoxelWorld
{
    BlockType GetBlock(Vector3Int worldPos);
    void RequestBlockChange(Vector3Int worldPos, BlockType type);
}
```

### Event Channels

Use ScriptableObject-based event channels for decoupling:
```csharp
[SerializeField] private BlockChangedEventChannel _onBlockChanged;
```

### Content Registry

Use `IContentRegistry<T>` interface for all content lookups:
```csharp
public interface IContentRegistry<T> where T : ScriptableObject
{
    T Get(string id);
    IEnumerable<T> GetAll();
}
```

## Documentation References

- `docs/VISION.md` - Game design vision and principles
- `docs/ROADMAP.md` - Development timeline and milestones
- `docs/DEVELOPMENT_PLAN.md` - Detailed implementation guide
- `docs/NPC_SYSTEM_DESIGN.md` - NPC behavior architecture
- `docs/PHASE1_IMPLEMENTATION_PLAN.md` - Current phase tasks
- `docs/PHASE2_IMPLEMENTATION_PLAN.md` - Colony Alpha planning
- `CONTRIBUTING.md` - Full coding standards and contribution guide

## Commit Message Format

```
type: Brief description

- What changed
- Why it changed
```

**Types:** `feat:`, `fix:`, `refactor:`, `docs:`, `chore:`

## Testing Requirements

Before committing:
- No compiler errors
- No console warnings
- Tested in Editor
- Code follows standards above
