# Phase 1 Implementation Plan

**Created:** November 2025
**Status:** Active
**Purpose:** Detailed implementation guide for completing Phase 1 (Playable Prototype)

---

## Executive Summary

**Current State:** Phase 1 scripts exist but are not wired into the scene
**Effort Required:** ~290 lines of code changes + asset creation
**Organized Into:** 7 implementation tasks, ordered by dependency

### Configuration Parameters

| Parameter | Value |
|-----------|-------|
| Art Style | Placeholder capsules/cubes |
| UI Style | Simple colored panels |
| Starting Items | Iron Pickaxe |
| Hunger Drain | 1 unit per minute |
| Monster Damage | 5% of max health per hit |

---

## Task 1: Player Survival Systems Integration

**Goal:** Add HealthSystem, HungerSystem, PlayerStats, and PlayerInventory to the player

**File to Modify:**
- `Assets/_Project/Scripts/Bootstrap/GameBootstrap.cs`

**Changes in `CreateDefaultPlayer()` method (after line 339):**

```csharp
// Add survival systems
var healthSystem = playerObject.AddComponent<HealthSystem>();
var hungerSystem = playerObject.AddComponent<HungerSystem>();
var playerStats = playerObject.AddComponent<PlayerStats>();
var playerInventory = playerObject.AddComponent<PlayerInventory>();
var deathHandler = playerObject.AddComponent<DeathHandler>();

// Configure hunger drain: 1 unit per minute = 1/60 per second
// HungerSystem._hungerDrainRate should be set to ~0.0167

// Tag player for easy finding
playerObject.tag = "Player";
```

**Estimated Lines:** ~80 lines added

**Dependencies:** None (first task)

---

## Task 2: Time & Weather System Setup

**Goal:** Create TimeManager and SunController in scene

**File to Modify:**
- `Assets/_Project/Scripts/Bootstrap/GameBootstrap.cs`

**New Method `SetupTimeSystem()`:**

```csharp
private void SetupTimeSystem()
{
    // Create TimeManager
    var timeObject = new GameObject("TimeManager");
    var timeManager = timeObject.AddComponent<TimeManager>();

    // Create SunController on directional light
    var light = FindFirstObjectByType<Light>();
    if (light != null && light.type == LightType.Directional)
    {
        var sunController = light.gameObject.AddComponent<SunController>();
        // Wire to TimeManager
    }

    // Add weather stub
    var weatherObject = new GameObject("WeatherSystem");
    weatherObject.AddComponent<StubWeatherSystem>();
}
```

**Estimated Lines:** ~50 lines added

**Dependencies:** None

---

## Task 3: Monster System Setup

**Goal:** Create MonsterSpawner and enable monster spawning at night

**File to Modify:**
- `Assets/_Project/Scripts/Bootstrap/GameBootstrap.cs`

**Assets to Create:**
- `Assets/_Project/ScriptableObjects/Monsters/Zombie.asset`
- `Assets/_Project/Prefabs/Monsters/Zombie.prefab`

**Monster Configuration:**
- Damage: 5 HP per hit (5% of 100 max health)
- Night-only spawning
- Basic chase/attack AI

**New Method `SetupMonsterSpawner()`:**

```csharp
private void SetupMonsterSpawner()
{
    var spawnerObject = new GameObject("MonsterSpawner");
    var spawner = spawnerObject.AddComponent<MonsterSpawner>();
    // Configure spawn rates and monster types
}
```

**Zombie Prefab Structure:**
```
Zombie (GameObject)
├── Capsule (MeshFilter + MeshRenderer) - Red material
├── BasicMonsterAI component
├── MonsterHealth component
├── CharacterController
└── Collider (CapsuleCollider)
```

**Estimated Lines:** ~40 lines + prefab setup

**Dependencies:** Task 2 (TimeManager needed for night detection)

---

## Task 4: Item System Assets

**Goal:** Create ItemDefinition assets for basic resources and tools

**Assets to Create:**

```
Assets/_Project/ScriptableObjects/Items/
├── Resources/
│   ├── Wood.asset (stackable, 64)
│   ├── Stone.asset (stackable, 64)
│   ├── Coal.asset (stackable, 64)
│   ├── IronOre.asset (stackable, 64)
│   ├── IronIngot.asset (stackable, 64)
│   ├── Planks.asset (stackable, 64)
│   └── Stick.asset (stackable, 64)
├── Tools/
│   ├── WoodPickaxe.asset (not stackable)
│   ├── StonePickaxe.asset (not stackable)
│   ├── IronPickaxe.asset (not stackable)
│   ├── WoodAxe.asset (not stackable)
│   ├── StoneAxe.asset (not stackable)
│   └── IronAxe.asset (not stackable)
└── Food/
    ├── Apple.asset (stackable, 16, consumable)
    └── CookedMeat.asset (stackable, 16, consumable)
```

**ItemRegistry Setup:**
- Create `ItemRegistry.asset` referencing all items
- Wire into GameBootstrap

**Dependencies:** None (can be done in parallel)

---

## Task 5: Recipe System Assets

**Goal:** Create crafting recipes connecting items

**Assets to Create:**

```
Assets/_Project/ScriptableObjects/Recipes/
├── Planks.asset (1 Wood → 4 Planks)
├── Stick.asset (2 Planks → 4 Sticks)
├── WoodPickaxe.asset (3 Planks + 2 Sticks → 1 Wood Pickaxe)
├── WoodAxe.asset (3 Planks + 2 Sticks → 1 Wood Axe)
├── StonePickaxe.asset (3 Stone + 2 Sticks → 1 Stone Pickaxe)
├── StoneAxe.asset (3 Stone + 2 Sticks → 1 Stone Axe)
├── IronIngot.asset (1 IronOre + 1 Coal → 1 Iron Ingot)
├── IronPickaxe.asset (3 IronIngot + 2 Sticks → 1 Iron Pickaxe)
└── IronAxe.asset (3 IronIngot + 2 Sticks → 1 Iron Axe)
```

**RecipeRegistry Setup:**
- Create `RecipeRegistry.asset` referencing all recipes
- Wire CraftingManager in GameBootstrap

**Dependencies:** Task 4 (needs ItemDefinitions)

---

## Task 6: UI Canvas & Screens

**Goal:** Create functional UI with HUD, inventory, crafting, and pause screens

**Prefabs to Create:**

```
Assets/_Project/Prefabs/UI/
├── GameUI.prefab (main canvas with all screens)
│   ├── HUDScreen
│   │   ├── HealthBar (Slider)
│   │   ├── HungerBar (Slider)
│   │   ├── Hotbar (9 slots)
│   │   └── TimeDisplay (Day X, HH:MM)
│   ├── PauseScreen
│   │   ├── Title "PAUSED"
│   │   ├── ResumeButton
│   │   └── QuitButton
│   ├── InventoryScreen
│   │   ├── Title "INVENTORY"
│   │   ├── SlotGrid (29 slots: 9 hotbar + 20 inventory)
│   │   └── CloseButton
│   ├── CraftingScreen
│   │   ├── Title "CRAFTING"
│   │   ├── RecipeList
│   │   └── CraftButton
│   └── DeathScreen
│       ├── Title "YOU DIED"
│       └── RespawnButton
```

**UI Style (Simple):**
- Dark semi-transparent panels
- White text
- Green health bar, orange hunger bar
- Yellow selection highlight for hotbar

**New Method `SetupUI()`:**

```csharp
private void SetupUI()
{
    // Create UI canvas
    var canvasObject = new GameObject("GameUI");
    var canvas = canvasObject.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvasObject.AddComponent<CanvasScaler>();
    canvasObject.AddComponent<GraphicRaycaster>();

    // Add UIManager
    var uiManager = canvasObject.AddComponent<UIManager>();

    // Create screens programmatically or instantiate prefab
    // Wire references
}
```

**Estimated Lines:** ~150 lines for programmatic UI creation

**Dependencies:** Tasks 1, 2, 4 (HUD needs player systems and time)

---

## Task 7: Block-to-Item Integration

**Goal:** When breaking blocks, drop corresponding items

**Files to Modify:**
- `Assets/_Project/Scripts/Voxel/BlockType.cs`
- `Assets/_Project/Scripts/Player/BlockInteraction.cs`

**BlockType.cs Additions:**

```csharp
[Header("Item Drop")]
[SerializeField] private ItemDefinition _droppedItem;
[SerializeField] private int _dropAmount = 1;

public ItemDefinition DroppedItem => _droppedItem;
public int DropAmount => _dropAmount;
```

**BlockInteraction.cs Additions:**

```csharp
// After successfully breaking a block:
if (brokenBlock.DroppedItem != null)
{
    var playerInventory = ServiceLocator.TryGet<PlayerInventory>(out var inv) ? inv : null;
    if (playerInventory != null)
    {
        playerInventory.TryAddItem(brokenBlock.DroppedItem, brokenBlock.DropAmount);
    }
}
```

**Block Asset Updates:**
- Stone.asset → drops Stone item (1)
- Grass.asset → drops Dirt item (1)
- Dirt.asset → drops Dirt item (1)
- Wood.asset → drops Wood item (1)
- CoalOre.asset → drops Coal item (1)
- IronOre.asset → drops IronOre item (1)

**Starting Inventory:**
- Player starts with 1x Iron Pickaxe

**Estimated Lines:** ~30 lines across files

**Dependencies:** Tasks 1, 4 (needs inventory and items)

---

## Implementation Order

```
Week 1: Foundation
├── Task 1: Player Survival Systems (Day 1-2)
├── Task 2: Time & Weather System (Day 2)
├── Task 4: Item System Assets (Day 3-4, parallel)
└── Task 3: Monster System (Day 4-5)

Week 2: Integration
├── Task 5: Recipe System Assets (Day 1)
├── Task 6: UI Canvas & Screens (Day 2-4)
└── Task 7: Block-to-Item Integration (Day 5)
```

---

## Exit Criteria

Phase 1 is complete when:

- [ ] Player has visible health bar (starts at 100)
- [ ] Player has visible hunger bar (starts at 100, drains 1/min)
- [ ] Starvation (hunger=0) causes health damage
- [ ] Day/Night cycle visible (sun movement, lighting changes)
- [ ] Time display shows "Day X, HH:MM"
- [ ] Monsters spawn at night around player
- [ ] Monsters deal 5 damage per hit (5% of 100 HP)
- [ ] Monsters despawn or burn at dawn
- [ ] Breaking blocks adds items to inventory
- [ ] Player starts with Iron Pickaxe
- [ ] Inventory screen opens with I/Tab key
- [ ] Inventory shows 29 slots (9 hotbar + 20 storage)
- [ ] Crafting screen shows available recipes
- [ ] Can craft items when ingredients are available
- [ ] Pause menu opens with ESC
- [ ] Pause menu has Resume and Quit buttons
- [ ] Player can die (health reaches 0)
- [ ] Death screen shows with Respawn button
- [ ] Respawn restores health to 100%, hunger to 50%

---

## Configuration Reference

### Player Stats
| Stat | Value |
|------|-------|
| Max Health | 100 |
| Starting Health | 100 |
| Max Hunger | 100 |
| Starting Hunger | 100 |
| Hunger Drain Rate | 1 unit/minute (0.0167/sec) |
| Starvation Damage | 1 HP/second when hunger=0 |
| Respawn Health | 100% |
| Respawn Hunger | 50% |

### Monster Stats (Zombie)
| Stat | Value |
|------|-------|
| Max Health | 50 |
| Damage | 5 (5% of player max HP) |
| Attack Range | 2 units |
| Attack Cooldown | 1.5 seconds |
| Detection Range | 15 units |
| Chase Speed | 5 units/sec |
| Night Only | Yes |
| Burns in Daylight | Yes |

### Time Settings
| Setting | Value |
|---------|-------|
| Day Length | 600 seconds (10 minutes) |
| Dawn Start | 0.20 (04:48) |
| Day Start | 0.25 (06:00) |
| Dusk Start | 0.70 (16:48) |
| Night Start | 0.75 (18:00) |
| Starting Time | 0.25 (sunrise) |

---

## References

- [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) - Full development roadmap
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Code standards
- [VISION.md](VISION.md) - Game design vision

---

**Document Version:** 1.0
**Last Updated:** November 2025
