# Player Skill System Design

## Overview

A progression system where players earn experience through gameplay actions and unlock passive bonuses and active abilities. The system integrates with existing PlayerStats, combat, and survival mechanics.

## Design Goals

1. **Feel Rewarding** - Every level should provide meaningful, noticeable improvements
2. **Encourage Varied Gameplay** - XP from combat, mining, crafting, and survival activities
3. **Stay Simple** - Start with core skills, expand later
4. **Integrate Seamlessly** - Hook into existing damage, health, hunger, and mining systems
5. **Support Save/Load** - Full persistence via ISaveable interface

---

## Skill Categories

### 1. Combat Skills
**Vitality** - Health and survivability
- Levels 1-10: +5 max HP per level (50 total bonus)
- Integration: `HealthSystem.MaxHealth` modifier

**Strength** - Melee damage output
- Levels 1-10: +5% melee damage per level (50% total bonus)
- Integration: `MeleeWeapon.SetDamageMultiplier()` / `Hitbox` damage calculation

**Toughness** - Damage reduction
- Levels 1-10: +3% damage reduction per level (30% total)
- Integration: `Hurtbox` or `HealthSystem.TakeDamage()` modifier

### 2. Gathering Skills
**Mining** - Block breaking speed and yields
- Levels 1-10: +8% mining speed per level (80% total bonus)
- Integration: `BlockInteraction` - add mining speed modifier

**Woodcutting** - Tree felling efficiency
- Levels 1-10: +8% chopping speed per level
- Integration: Same as mining, different block types

### 3. Survival Skills
**Endurance** - Hunger efficiency
- Levels 1-10: -5% hunger decay rate per level (50% slower decay)
- Integration: `HungerSystem.HungerDecayRate` modifier

**Fortitude** - Healing effectiveness
- Levels 1-10: +5% healing received per level (50% bonus)
- Integration: `HealthSystem.Heal()` modifier

---

## Experience System

### XP Sources
| Action | Base XP |
|--------|---------|
| Kill monster | 10-50 (based on monster tier) |
| Mine block | 1-5 (based on block hardness) |
| Chop tree | 2 |
| Craft item | 5-20 (based on recipe complexity) |
| Take damage and survive | 1 per 10 damage |
| Eat food | 2 |

### Leveling Formula
```
XP Required = BaseXP * (Level ^ ExponentFactor)
BaseXP = 100
ExponentFactor = 1.5

Level 1 → 2: 100 XP
Level 2 → 3: 283 XP
Level 3 → 4: 520 XP
...
Level 9 → 10: 2,700 XP
```

### Skill Points
- Gain 1 skill point per player level
- Spend points to level individual skills
- Each skill costs 1 point per level (1 point for level 1, 1 point for level 2, etc.)
- Max skill level: 10

---

## Architecture

### Core Components

```
Scripts/Player/Skills/
├── SkillSystem.cs           # Central skill manager, ISaveable
├── SkillDefinition.cs       # ScriptableObject for skill configuration
├── SkillData.cs             # Runtime data for a single skill
├── ExperienceSystem.cs      # XP tracking and level calculation
└── SkillModifiers.cs        # Static helper to apply skill bonuses

ScriptableObjects/Skills/
├── Vitality.asset
├── Strength.asset
├── Toughness.asset
├── Mining.asset
├── Woodcutting.asset
├── Endurance.asset
└── Fortitude.asset

Scripts/UI/
└── SkillsUI.cs              # Character screen skill display
```

### Class Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      SkillSystem                             │
│─────────────────────────────────────────────────────────────│
│ - _skills: Dictionary<string, SkillData>                    │
│ - _experienceSystem: ExperienceSystem                       │
│ - _availableSkillPoints: int                                │
│─────────────────────────────────────────────────────────────│
│ + GetSkillLevel(skillId): int                               │
│ + GetSkillBonus(skillId): float                             │
│ + TryLevelUpSkill(skillId): bool                            │
│ + AvailableSkillPoints: int                                 │
│ + OnSkillLevelChanged: Action<string, int>                  │
│ + Save(): SkillSystemSaveData                               │
│ + Load(data): void                                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    ExperienceSystem                          │
│─────────────────────────────────────────────────────────────│
│ - _currentXP: int                                           │
│ - _currentLevel: int                                        │
│─────────────────────────────────────────────────────────────│
│ + AddExperience(amount): void                               │
│ + CurrentLevel: int                                         │
│ + CurrentXP: int                                            │
│ + XPToNextLevel: int                                        │
│ + XPProgress: float (0-1)                                   │
│ + OnLevelUp: Action<int>                                    │
│ + OnXPGained: Action<int>                                   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                 SkillDefinition (SO)                         │
│─────────────────────────────────────────────────────────────│
│ + SkillId: string                                           │
│ + DisplayName: string                                       │
│ + Description: string                                       │
│ + Icon: Sprite                                              │
│ + Category: SkillCategory                                   │
│ + MaxLevel: int                                             │
│ + BonusPerLevel: float                                      │
│ + BonusType: SkillBonusType (Additive/Multiplicative)       │
└─────────────────────────────────────────────────────────────┘
```

### Integration Points

**HealthSystem.cs** - Add skill modifiers:
```csharp
// In TakeDamage():
float toughnessReduction = SkillModifiers.GetDamageReduction(skillSystem);
actualDamage *= (1f - toughnessReduction);

// In Heal():
float fortitudeBonus = SkillModifiers.GetHealingBonus(skillSystem);
actualHeal *= (1f + fortitudeBonus);

// MaxHealth property:
public float MaxHealth => _baseMaxHealth + SkillModifiers.GetBonusHealth(skillSystem);
```

**HungerSystem.cs** - Add endurance modifier:
```csharp
// In Update() hunger decay:
float enduranceModifier = SkillModifiers.GetHungerDecayModifier(skillSystem);
_currentHunger -= _hungerDecayRate * enduranceModifier * Time.deltaTime;
```

**BlockInteraction.cs** - Add mining speed:
```csharp
// In mining calculation:
float miningBonus = SkillModifiers.GetMiningSpeedBonus(skillSystem);
float effectiveSpeed = baseMiningSpeed * (1f + miningBonus);
```

**Combat damage pipeline** - Add strength modifier:
```csharp
// In Hitbox or MeleeWeapon:
float strengthBonus = SkillModifiers.GetMeleeDamageBonus(skillSystem);
float finalDamage = baseDamage * (1f + strengthBonus);
```

---

## Save Data Structure

```csharp
[Serializable]
public class SkillSystemSaveData
{
    public int CurrentXP;
    public int CurrentLevel;
    public int AvailableSkillPoints;
    public List<SkillSaveData> Skills;
}

[Serializable]
public class SkillSaveData
{
    public string SkillId;
    public int Level;
}
```

---

## UI Design

### Character Screen Layout

```
┌────────────────────────────────────────────────────────────┐
│  CHARACTER                                           [X]   │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  Level 5                    XP: 450/520                    │
│  [████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░]            │
│                                                            │
│  Skill Points Available: 3                                 │
│                                                            │
├────────────────────────────────────────────────────────────┤
│  COMBAT                                                    │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ [⚔] Vitality      Lv. 2    +10 Max HP         [+]   │  │
│  │ [🗡] Strength      Lv. 1    +5% Melee Damage   [+]   │  │
│  │ [🛡] Toughness     Lv. 0    +0% Damage Resist  [+]   │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                            │
│  GATHERING                                                 │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ [⛏] Mining        Lv. 3    +24% Mining Speed   [+]   │  │
│  │ [🪓] Woodcutting   Lv. 0    +0% Chopping Speed  [+]   │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                            │
│  SURVIVAL                                                  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ [❤] Endurance     Lv. 1    -5% Hunger Decay    [+]   │  │
│  │ [✚] Fortitude     Lv. 0    +0% Healing Bonus   [+]   │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

---

## Implementation Phases

### Phase A: Core System (Priority)
1. Create `SkillDefinition` ScriptableObject
2. Create `ExperienceSystem` with XP and leveling
3. Create `SkillSystem` as central manager
4. Create `SkillModifiers` helper class
5. Implement `ISaveable` for persistence
6. Create 7 skill ScriptableObjects

### Phase B: Integration
1. Modify `HealthSystem` for Vitality, Toughness, Fortitude
2. Modify `HungerSystem` for Endurance
3. Modify `BlockInteraction` for Mining/Woodcutting
4. Modify combat pipeline for Strength

### Phase C: XP Sources
1. Add XP grants to monster kills
2. Add XP grants to block mining
3. Add XP grants to crafting
4. Add XP grants to survival actions

### Phase D: UI
1. Create `SkillsUI` component
2. Build skill display prefabs
3. Wire into Character screen
4. Add level-up notification/effects

---

## Future Expansion Ideas

- **Active abilities** at skill milestones (level 5, 10)
- **Skill trees** with branching specializations
- **Prestige system** for post-max-level progression
- **Skill synergies** (bonuses for related skill combos)
- **Tool proficiency** (better with specific tool types)

---

## Files to Create

1. `Scripts/Player/Skills/SkillDefinition.cs`
2. `Scripts/Player/Skills/SkillData.cs`
3. `Scripts/Player/Skills/ExperienceSystem.cs`
4. `Scripts/Player/Skills/SkillSystem.cs`
5. `Scripts/Player/Skills/SkillModifiers.cs`
6. `Scripts/Player/Skills/SkillSystemSaveData.cs`
7. `Scripts/UI/SkillsUI.cs`
8. `Scripts/UI/SkillSlotUI.cs`
9. `ScriptableObjects/Skills/*.asset` (7 skill definitions)

## Files to Modify

1. `Scripts/Player/HealthSystem.cs` - Add skill modifiers
2. `Scripts/Player/HungerSystem.cs` - Add endurance modifier
3. `Scripts/Player/BlockInteraction.cs` - Add mining speed
4. `Scripts/Combat/Hitbox.cs` or `MeleeWeapon.cs` - Add strength modifier
5. `Scripts/UI/RuntimeUIBuilder.cs` - Add skills display to Character screen
6. `Scripts/Core/SaveManager.cs` - Register SkillSystem as saveable
