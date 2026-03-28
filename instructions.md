# Sprinkle Hill — Development Instructions

## Purpose

This document defines the rules and conventions for writing and modifying code in this project. Follow these guidelines to prevent repeated code, avoid codebase bloating, and keep the project organized and maintainable.

---

## 1. Architecture Overview

| Layer | Location | Responsibility |
|---|---|---|
| **Framework** | `Assets/Scripts/Framework/` | Reusable systems (grid, events, managers, UI, sound, save). Must not reference project‑specific logic directly. |
| **Project** | `Assets/Project/Scripts/` | Game‑specific implementations (level editors, custom objectives). May reference Framework. |
| **ScriptableObjects** | Created via `CreateAssetMenu` | Data containers (`ElementData`, `ConstantManager`, `ObjectiveType`, etc.). No runtime logic beyond simple lookups. |

New scripts must be placed in the correct layer. Never put project-specific code inside `Framework/`.

---

## 2. Single Responsibility & Class Size

- **One purpose per class.** If a class handles two distinct concerns, split it.  
  - ✅ `Match3Grid` owns swap/match/gravity. `PowerUpHandler` owns power‑up detection/creation/activation. `GridHelper` owns shared renderer utilities.  
  - ❌ A single monolithic grid class that also manages UI, sound, and camera effects.
- **Helper/handler classes over mega‑methods.** When a method exceeds ~60 lines or a class exceeds ~400 lines, extract a helper class or break it into smaller private methods.
- **Nested classes are acceptable** for tightly coupled types (`SpawnRequest` inside `PowerUpHandler`, `GridCell` inside `Grid3D`), but they should remain small data holders — not behavior‑heavy.

---

## 3. No Code Duplication

Before writing new code, search for existing functionality:

- **Utility methods** — Check `ListExtensions`, `GeometryUtils`, `GridHelper`, and other files in `Framework/Util/`.
- **Event triggers** — Reuse `EventManager.TriggerEvent(GameEvent.X, ...)`. Do not create parallel notification systems.
- **Singleton access** — Use the existing `SingletonComponent<T>` pattern. Do not create new singleton implementations.
- **Constants & tuning values** — Store them in `ConstantManager` (the ScriptableObject), not as magic numbers scattered in code. If a new tuning value is needed, add it to `ConstantManager` with a `[Header]` group.

If you find yourself copying more than 3 lines of logic from another location, extract it into a shared method or utility class instead.

---

## 4. Event System Usage

All cross‑system communication goes through `EventManager`.

- **Declare events** in the `GameEvent` enum (`GameEvent.cs`). Group related events with comments.
- **Listen/unlisten symmetrically.** Every `StartListening` in `OnEnable` must have a matching `StopListening` in `OnDisable`.
- **Use `EventParam` fields.** Do not create new parameter classes. If `EventParam` lacks a needed field, prefer using `paramDictionary` before modifying the class.
- **Fire events at boundary moments**, not inside tight loops. One event per logical action (e.g., `MATCH_DETECTED` once per match cycle, not per cell).

---

## 5. Grid & Element Conventions

- **Grid data vs. visuals.** `GridCell` / `GridElementInfo` hold authoritative state. `GridElement` MonoBehaviours are visual representations. Always update data first, then sync visuals.
- **Position helpers.** Use `GetCell`, `GetElementAt`, `GetWorldPosition`, `IsValidPosition`, and `TryGetElementPosition` instead of manually indexing `gridCells` or `generatedTiles` from outside the grid.
- **Power‑up logic belongs in `PowerUpHandler`.** Detection, spawn‑request creation, visual creation, and activation are all routed through this class. Match3Grid calls into it — never the reverse pattern.
- **Coroutine ownership.** Coroutines that animate grid elements must be started on the `Match3Grid` MonoBehaviour (via `grid.StartCoroutine(...)` from helper classes) so they share the same lifecycle.

---

## 6. MonoBehaviour Patterns

- **Use `SerializeField` over public fields** for Inspector-exposed values that don't need external access.
- **Prefer composition over deep inheritance.** The hierarchy is intentionally shallow: `Grid3D → Match3Grid`, `GridElement → GridElement_Match3Game`. Avoid adding more inheritance levels.
- **Coroutine naming.** Name coroutines after the action they perform (`ApplyGravity`, `ClearMatches`, `AnimateBombFlight`). Avoid generic names like `DoStuff` or `Process`.
- **Cleanup.** Kill DOTween tweens (`DOKill`) and disable colliders before destroying elements to prevent animation-on-destroyed-object errors.

---

## 7. ScriptableObject Data

- **`ElementData`** defines visual identity (sprite, mesh, material). It must not contain runtime state.
- **`ConstantManager`** stores all tuning parameters. Group related fields with `[Header("...")]`. Never hard-code durations, speeds, or magnitudes in C# — reference `ConstantManager` instead.
- **`ObjectiveType`** defines what event completes an objective. The objective system is generic; new objectives are added by creating new `ObjectiveType` assets, not by writing new listener code.

---

## 8. Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Classes | PascalCase | `PowerUpHandler`, `Match3Grid` |
| Public methods | PascalCase | `ActivateAt`, `SwapElements` |
| Private methods | PascalCase | `BuildColumnSections`, `ClearLineCellImmediate` |
| Private fields | camelCase | `currentComboCount`, `powerUpHandler` |
| Serialized fields | camelCase with `[SerializeField]` | `[SerializeField] private float minDragDistance` |
| Constants | PascalCase or UPPER_SNAKE in nested structs | `SortingOrderBoost`, `TAGS.PLAYER` |
| Enums | PascalCase members | `ElementPowerUpType.HorizontalRocket` |
| Events | UPPER_SNAKE_CASE | `GameEvent.ELEMENT_DESTROYED` |
| Coroutines | PascalCase, verb-first | `ApplyGravity()`, `BreakWallAt()` |

---

## 9. Comments & Documentation

- **XML summaries** (`/// <summary>`) on public classes and non-obvious public methods only.
- **No obvious comments.** Do not comment `// Destroy the element` above `Destroy(element.gameObject)`.
- **Section dividers** (`// ------`) are used in large files to group related methods. Follow the existing style in `Match3Grid.cs` when adding new sections.
- **`// TODO:` tags** are acceptable for known incomplete work, but must include a short description.

---

## 10. Adding New Features — Checklist

1. **Check if it already exists.** Search the codebase before writing anything new.
2. **Identify the correct layer.** Framework vs. Project. Shared utility vs. specific handler.
3. **Reuse existing infrastructure.**  
   - Need cross-system communication? → `EventManager`  
   - Need a tuning value? → `ConstantManager`  
   - Need a new grid behavior? → Extend or compose with `Match3Grid` / `PowerUpHandler`  
   - Need a new objective? → Create a `ObjectiveType` asset; no code changes required.
4. **Keep classes focused.** One class, one job. Extract helpers when complexity grows.
5. **No magic numbers.** If it's a value someone might want to tweak, put it in `ConstantManager`.
6. **Test the event lifecycle.** Ensure `StartListening` / `StopListening` are paired. Verify events fire at the right time — not too early, not too often.
7. **Clean up resources.** Kill tweens, disable colliders, destroy temporary GameObjects. Leaked objects cause subtle bugs.
8. **Newly created files are not loaded by the project without rebuilding the solution, which can lead to confusion and errors.** If you add a new class file, stop the process after creating and notify the developer to rebuild the solution by refreshing Unity.

---

## 11. Things to Avoid

| ❌ Don't | ✅ Do Instead |
|---|---|
| Copy-paste logic between classes | Extract a shared method or utility class |
| Hard-code numbers (durations, speeds, radii) | Add a field to `ConstantManager` |
| Create a new singleton pattern | Use `SingletonComponent<T>` |
| Build a parallel event/messaging system | Use `EventManager` and `GameEvent` |
| Add deep inheritance hierarchies | Compose with handler/helper classes |
| Put project-specific logic in Framework | Place it in `Assets/Project/Scripts/` |
| Leave `StartListening` without `StopListening` | Always pair them in `OnEnable` / `OnDisable` |
| Modify `EventParam` for one-off data | Use `paramDictionary` for ad-hoc payloads |
| Create MonoBehaviours for pure data | Use `[System.Serializable]` classes or ScriptableObjects |
| Use `public` fields for Inspector exposure | Use `[SerializeField] private` |

---

## 12. File Organization

```
Assets/
├── Libraries/              Third-party (I2, DOTween, etc.) — do not modify
├── Plugins/                Editor plugins (Odin, etc.) — do not modify
├── Project/
│   └── Scripts/
│       └── Core/           Game-specific scripts (LevelEditor, custom items)
├── Scripts/
│   └── Framework/
│       ├── Action Bar/     Action bar UI system
│       ├── Camera/         Camera bounds and effects
│       ├── Grid/           Grid3D, Match3Grid, elements, cell controllers
│       │   ├── Elements/   GridElement subclasses
│       │   └── ElementView/ElementData ScriptableObjects
│       ├── Level Management/ LevelScene hierarchy
│       ├── Objective System/ Objective tracking
│       ├── Save/           Save/load system
│       ├── ScreenManagement/ Screen flow (menus, win/lose)
│       ├── Sound/          Audio management
│       └── Util/           Extensions, singletons, geometry helpers
└── Shaders/                Custom shaders
```

Place new files in the folder that matches their responsibility. Create a new subfolder only when there are 3+ related files that don't fit an existing folder.
