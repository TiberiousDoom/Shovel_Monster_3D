# Phase Implementation Plan Template

**Purpose:** Template for creating detailed phase implementation plans

---

## How to Use This Template

1. Copy this template to `PHASEX_IMPLEMENTATION_PLAN.md`
2. Fill in all sections with specific implementation details
3. Review against DEVELOPMENT_PLAN.md to ensure all tasks are covered
4. Review against VISION.md to ensure alignment with design principles
5. Review against CONTRIBUTING.md for code standards compliance

---

## Template Structure

```markdown
# Phase X Implementation Plan

**Created:** [Date]
**Status:** Active | In Progress | Complete
**Purpose:** Detailed implementation guide for completing Phase X ([Phase Name])
**Prerequisites:** Phase [X-1] must be complete

---

## Executive Summary

**Current State:** [What exists from previous phases]
**Goal:** [One-sentence description of phase goal]
**Effort Required:** [Estimated lines of code + asset creation]
**Organized Into:** [Number] implementation tasks, ordered by dependency

### Configuration Parameters

| Parameter | Value | Notes |
|-----------|-------|-------|
| [Key config] | [Value] | [Why this value] |
| ... | ... | ... |

---

## Phase Dependencies

### Required from Previous Phase
- [ ] [System/Feature] - used by [what in this phase]
- [ ] [System/Feature] - used by [what in this phase]

### Interfaces This Phase Must Implement
- [ ] `IExampleInterface` - defined in Phase X, implemented here

### Systems This Phase Must Define for Future Phases
- [ ] `IFutureInterface` - will be implemented in Phase Y

---

## Task X: [Task Name]

**Goal:** [What this task achieves]
**Estimated Effort:** [Lines of code] + [Asset count]
**Dependencies:** [None | Task X, Task Y]

### Files to Create

| File | Location | Purpose |
|------|----------|---------|
| `ClassName.cs` | `Scripts/Domain/` | [Brief description] |
| ... | ... | ... |

### Files to Modify

| File | Location | Changes |
|------|----------|---------|
| `ExistingFile.cs` | `Scripts/Domain/` | [What to add/change] |
| ... | ... | ... |

### ScriptableObject Assets to Create

```
Assets/_Project/ScriptableObjects/[Domain]/
├── AssetName.asset ([Configuration notes])
├── ...
```

### Prefabs to Create

```
Assets/_Project/Prefabs/[Domain]/
├── PrefabName.prefab
│   ├── Component1
│   ├── Component2
│   └── ...
```

### Code Implementation

#### [ClassName].cs

```csharp
using UnityEngine;

namespace VoxelRPG.[Domain]
{
    /// <summary>
    /// [What this class does]
    /// </summary>
    public class ClassName : MonoBehaviour
    {
        // Show key structure, not full implementation
        // Focus on:
        // - Serialized fields with Header attributes
        // - Public interface (properties/methods)
        // - Event definitions
        // - Key implementation patterns
    }
}
```

### Integration Steps

1. [Step-by-step how to wire this into the game]
2. [Include specific file locations and method names]
3. [Include configuration values]

### Verification

- [ ] [Specific testable condition]
- [ ] [Specific testable condition]

---

## Implementation Order

```
Week X: [Phase Name]
├── Task 1: [Name] (Day X-Y)
│   └── Dependencies: None
├── Task 2: [Name] (Day X-Y, can parallel with Task 1)
│   └── Dependencies: None
├── Task 3: [Name] (Day X-Y)
│   └── Dependencies: Task 1
└── ...
```

---

## Save System Additions

### New Save Data Classes

```csharp
[System.Serializable]
public class PhaseXSaveData
{
    public [Type] [Field];
    // ...
}
```

### Migration from Previous Version

```csharp
// In SaveMigrator.cs
private SaveData MigrateVXToVY(SaveData save)
{
    // Default values for new fields
    save.[NewField] = [DefaultValue];
    save.Header.Version = Y;
    return save;
}
```

---

## Event Channels to Create

| Event Channel | Type | Purpose |
|--------------|------|---------|
| `OnEventName` | `VoidEventChannel` | [When raised, who listens] |
| `OnValueChanged` | `FloatEventChannel` | [When raised, who listens] |
| ... | ... | ... |

---

## Exit Criteria

Phase X is complete when:

### Functional Requirements
- [ ] [Specific, testable condition]
- [ ] [Specific, testable condition]
- [ ] [Specific, testable condition]

### Integration Requirements
- [ ] [System] integrates with [System from previous phase]
- [ ] Save/load preserves all new state
- [ ] No console errors or warnings

### Multiplayer Compatibility
- [ ] [Interface] uses request pattern (returns success/failure)
- [ ] No static mutable state
- [ ] Event channels are injectable

---

## Configuration Reference

### [System Name] Configuration

| Setting | Value | Notes |
|---------|-------|-------|
| [Setting] | [Value] | [Reasoning] |
| ... | ... | ... |

---

## Testing Checklist

### Unit Tests
- [ ] [Component] handles [edge case]
- [ ] [Component] handles [edge case]

### Integration Tests
- [ ] [System A] correctly triggers [System B]
- [ ] [Flow] works end-to-end

### Playtest Scenarios
- [ ] [Scenario description] - expected result: [result]
- [ ] [Scenario description] - expected result: [result]

---

## References

- [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) - Full development roadmap
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Code standards
- [VISION.md](VISION.md) - Game design vision
- [Previous phase implementation plan if applicable]

---

**Document Version:** 1.0
**Last Updated:** [Date]
```

---

## Best Practices for Phase Plans

### Be Specific

**Bad:** "Add NPC movement"
**Good:** "Add NPCController component with NavMeshAgent, speed of 3.5 units/sec, stopping distance of 1.5 units"

### Include Code Patterns

Show how new code should follow existing patterns:
- Namespace conventions
- Event channel usage
- ServiceLocator registration
- Save data structure

### Define Configuration Values

Every magic number should be documented with reasoning:
- Why is hunger drain 0.0167/sec? (1 unit per minute feels right in playtesting)
- Why is NPC detection range 10 units? (Balances awareness vs performance)

### Map Dependencies Explicitly

For each task:
- What must exist before this task can start?
- What does this task enable for later tasks?

### Testable Exit Criteria

**Bad:** "NPCs work correctly"
**Good:** "NPC navigates around obstacles to reach stockpile within 10 seconds"

---

**Template Version:** 1.0
**Last Updated:** November 2025
