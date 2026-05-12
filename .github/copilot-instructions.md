# Copilot Instructions
# Unity Coding Instructions

## Core Principles

- Follow **SOLID principles** at all times:
  - **Single Responsibility**: Each class should have one clear purpose.
  - **Open/Closed**: Extend behavior without modifying existing code where possible.
  - **Liskov Substitution**: Derived classes must be safely interchangeable.
  - **Interface Segregation**: Prefer small, specific interfaces over large ones.
  - **Dependency Inversion**: Depend on abstractions, not concrete implementations.

---

## Code Quality

- Write **clean, readable, and maintainable code**.
- Use **clear and descriptive naming** for classes, methods, and variables.
- Avoid large, monolithic classes (“God objects”).
- Keep methods **short and focused**.
- Prefer composition over inheritance unless inheritance is clearly justified.
- Avoid hardcoding values — use configuration or serialized fields where appropriate.
- Ensure code is easy to extend and refactor.
- Try to not use unnecessary null checks. Null checks are only for the moment when you're sure that the value being null does not break the gameplay.

---

## Architecture Guidelines

- Keep **game logic decoupled from Unity-specific components** where possible.
- Minimize logic inside `MonoBehaviour`; use it mainly as a bridge to Unity.
- Separate responsibilities into systems (e.g., input, gameplay, rendering).
- After all succesful changes, write a log in Assets/copilot-logs.txt with the format: [Date and Time] - [Short description of the change made].
- Change the codes that doesn't fit these instructions as long as It's related to the change you made. For example, if you added a new method to a class, you can also refactor the existing methods in that class to fit these instructions.

---

## Design Patterns

Use appropriate and commonly accepted design patterns where they improve clarity and flexibility:

- **Strategy Pattern** → interchangeable behaviors (e.g., movement, abilities)
- **Command Pattern** → actions, undo/redo systems
- **Observer Pattern** → event systems, decoupled communication
- **Factory Pattern** → object creation logic
- **State Pattern** → managing states (AI, game states, UI states)
- **Service Locator / Dependency Injection** → managing dependencies

Avoid overengineering — only apply patterns when they provide clear benefit.

---

## Maintainability

- Write code that is easy for others to understand and modify.
- Keep dependencies explicit and controlled.
- Document non-obvious logic with concise comments.
- Prefer extensible systems over quick hacks.

---

## When Unsure

- Choose the solution that maximizes **clarity, modularity, and extensibility**.
- Ask for clarification instead of making assumptions.

## Extra Notes

- When you need to create a new class, generate it inside and existing related class file.

---

## Gameplay Extension Instructions (Cell Features, Cell Types, Elements)

Use the following rules whenever implementing new cell features, new cell types, or new elements.

### 1) New Cell Feature (inherits `CellFeature`)

- Implement as a `ScriptableObject` with `[CreateAssetMenu(menuName = "Game/Cell Feature/<Feature Name>...")]`.
- Keep feature behavior inside the feature class:
  - `AcceptElements`
  - `OnElementMatchedOverTheCell(...)`
  - `OnElementMatchedAdjacentToTheCell(...)`
- If behavior has multiple states, use existing `GridCell` fields (`cellFeatureGroupHealth`, `cellFeatureGroupMaxHealth`, `cellFeatureGroupIndex`) before introducing new state containers.
- Use `tileSpriteSet` / `GetTileSpriteSet(...)` for visuals.
- Trigger events through `EventManager.TriggerEvent(...)` when created, damaged, activated, or destroyed.

Integration checklist:

- Add a serialized reference in `GameManager` under Cell Features if it must be globally selectable.
- Add Level Editor support:
  - Right-click context menu toggle.
  - Optional keyboard shortcut.
  - Feature preview icon rendering.
- Ensure tile generation/refresh applies feature sprite/material logic.
- Ensure compatibility with gravity, matching, and refill flow.

### 2) New Cell Type (extends `Grid3D.CellType`)

- Add the enum value in `Grid3D.CellType`.
- Define clear behavior rules:
  - Accepts elements or not.
  - Destructible or not.
  - Blocks movement/matching or not.
- Update generation and occupancy logic where `CellType` is switched/checked.
- Update `TileData` mapping to prefab/sprite/material path.
- Keep safe fallback behavior by reusing existing prefabs if dedicated visuals are missing.

Integration checklist:

- Level Editor:
  - Entry in Set Cell Type menu.
  - Color/preview rendering.
  - Any type-specific options.
- Runtime:
  - Generation data and tile spawn logic.
  - Match/damage/clear interactions.
  - Objective/event hooks if relevant.

### 3) New Element (`ElementData` + runtime handling)

- Add/extend using `ElementData` (display name, icon, mesh/material, behavior flags).
- Prefer `ElementBehaviorFlags` over scattered hardcoded checks for swap, match, shuffle, pass-through, and clear immunity.
- If unique behavior is needed, keep rules isolated and invoked through existing systems.
- Ensure visuals work in `GridElement` setup paths (mesh/sprite/material/animation).

Integration checklist:

- Add serialized reference in `GameManager` if globally known/special.
- Add Level Editor selection entries:
  - Normal pool selection.
  - Special/Power-up group if applicable.
- Verify interactions with:
  - Match detection
  - Swap validation
  - Shuffle validation
  - Gravity/refill
  - Objective/event tracking

### 4) Standards for all new gameplay content

- Keep `MonoBehaviour` thin; keep gameplay decisions in data/logic classes.
- Avoid duplicate rule checks across multiple files; centralize and reuse helpers.
- Avoid magic numbers; expose configuration in serialized fields or ScriptableObjects.
- Keep backward compatibility with existing levels unless migration is explicitly requested.
- Extend existing systems first before introducing new frameworks.

### 5) Final self-check

Before finishing, verify:

- New content is placeable from Level Editor.
- New content is visible with correct icon/sprite/material in editor and runtime.
- Core loop still works (spawn, move, match, clear, refill).
- Existing feature/type/element behavior has no regression.
- `Assets/copilot-logs.txt` has a new timestamped summary entry.
