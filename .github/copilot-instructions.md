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
