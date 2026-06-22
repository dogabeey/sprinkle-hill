# Unity Project Coding Instructions

## Core Objective

Always prefer the simplest solution that:

1. Solves the problem correctly.
2. Keeps code maintainable.
3. Minimizes file size and complexity.
4. Minimizes future technical debt.
5. Minimizes AI token usage during edits.

Do not introduce architecture, abstractions, patterns, packages, files, or systems unless they provide immediate and measurable value.

---

# General Rules

## KISS First

Prefer simple code over clever code.

Bad:

* Reflection
* Generic frameworks
* Service locators
* Event buses for small features
* Premature optimization
* Deep inheritance trees

Good:

* Small focused classes
* Explicit dependencies
* Straightforward control flow
* Clear naming

---

## YAGNI

Do not implement features that are not currently required.

Never create:

* Future-proof systems
* "Just in case" abstractions
* Extension points without actual use cases

Implement only what is needed now.

---

## SOLID Principles

### Single Responsibility Principle

Each class should have one reason to change.

Good:

PlayerHealth

* Stores health
* Handles damage

PlayerMovement

* Handles movement

Bad:

PlayerController

* Health
* Movement
* Inventory
* UI
* Audio

---

### Open Closed Principle

Prefer extension through composition.

Use interfaces only when multiple implementations are expected.

Do not create interfaces with a single implementation unless there is a clear future need.

---

### Liskov Substitution Principle

Derived classes must behave like their base classes.

If inheritance creates special-case behavior, prefer composition.

---

### Interface Segregation Principle

Prefer small interfaces.

Good:

IDamageable

IMovable

IInteractable

Bad:

IGameEntityWithEverything

---

### Dependency Inversion Principle

Depend on abstractions only when multiple implementations exist or are expected.

Do not create interfaces solely to satisfy DIP.

---

# Unity Specific Rules

## MonoBehaviours

MonoBehaviours should be thin.

Use MonoBehaviours for:

* Unity lifecycle methods
* Scene references
* Input wiring
* View logic

Move game logic into plain C# classes when complexity grows.

---

## Avoid Update()

Do not use Update() unless necessary.

Prefer:

* Events
* Coroutines
* Timers
* Animation events

If Update() is required:

* Exit early
* Avoid allocations
* Keep logic minimal

---

## Serialization

Prefer:

[SerializeField] private

instead of public fields.

Example:

[SerializeField] private float speed;

Avoid public mutable fields.

Expose read-only properties if needed.

---

## Inspector Design

Expose only values designers need.

Hide implementation details.

Group fields logically.

Avoid inspector clutter.

---

## GetComponent Usage

Cache components.

Good:

private Rigidbody _rb;

Awake()
{
_rb = GetComponent<Rigidbody>();
}

Bad:

GetComponent<Rigidbody>()
inside Update()

---

## Find Usage

Never use:

* GameObject.Find
* FindObjectOfType
* Resources.FindObjectsOfTypeAll

during gameplay unless absolutely necessary.

Prefer serialized references or dependency injection.

---

## Coroutines

Use coroutines for:

* Delays
* Sequencing
* Timed actions

Avoid nested coroutine chains.

Keep them short and readable.

---

## ScriptableObjects

Use ScriptableObjects for:

* Configuration
* Static data
* Shared settings

Do not store runtime state in ScriptableObjects unless explicitly required.

---

## Events

Prefer C# events for simple communication.

Example:

public event Action Died;

Avoid large global event systems.

---

# Performance Rules

## Avoid Garbage Collection

Avoid runtime allocations in hot paths.

Common offenders:

* LINQ in Update
* String concatenation in loops
* New lists every frame
* Boxing

Prefer cached collections.

---

## LINQ

Avoid LINQ in gameplay code.

Acceptable:

* Editor scripts
* Tools
* Initialization

Not acceptable:

* Per-frame execution

---

## Collections

Reuse collections when possible.

Prefer:

Clear()

instead of creating new lists repeatedly.

---

## String Usage

Use cached strings when possible.

Avoid generating UI strings every frame.

---

# Architecture

## Composition Over Inheritance

Prefer:

Player
├ Health
├ Movement
└ Inventory

instead of:

Character
└ Player
└ Mage
└ FireMage

Keep inheritance shallow.

---

## Folder Structure

Assets/
Scripts/
Gameplay/
UI/
Systems/
Data/
Editor/

Avoid excessive nesting.

---

## Class Size

Target:

* Under 300 lines preferred.
* Over 500 lines requires justification.

Large classes should be split.

---

## Method Size

Prefer methods under 30 lines.

Extract only when it improves readability.

Do not create trivial wrappers.

---

# Naming

Use clear names.

Good:

CalculateDamage()

Bad:

Calc()

Good:

remainingHealth

Bad:

rh

Avoid abbreviations unless universally known.

Examples:

UI
ID
HP
XP

---

# Error Handling

Fail loudly during development.

Use:

Debug.LogError

for invalid setup.

Validate references in Awake or OnValidate.

Avoid silent failures.

---

# Code Style

Use early returns.

Good:

if (!target)
return;

Attack(target);

Bad:

if (target)
{
Attack(target);
}

Prefer reduced nesting.

---

## Comments

Do not comment obvious code.

Bad:

// Set health
health = 10;

Comment only:

* Why
* Non-obvious decisions
* Workarounds

---

# Dependencies

Before introducing:

* New package
* Framework
* Plugin
* Manager
* System

Ask:

"Can this be solved with existing code?"

Prefer fewer dependencies.

---

# Refactoring Guidelines

When modifying existing code:

1. Preserve behavior.
2. Make the smallest safe change.
3. Do not rewrite entire systems.
4. Avoid unrelated cleanup.
5. Minimize diff size.

Small diffs are preferred.

---

# AI Cost Optimization

When editing files:

* Change only necessary lines.
* Preserve existing formatting.
* Avoid rewriting entire files.
* Avoid creating files unless required.
* Reuse existing systems.
* Keep responses concise.

Always prefer the smallest correct implementation.

---

# Decision Hierarchy

When choosing between solutions:

1. Correctness
2. Simplicity
3. Maintainability
4. Performance
5. Extensibility

Never sacrifice simplicity for hypothetical future requirements.

The best code is often the code that does not exist.

# Project specific instructions

1. Do not try to build the project after the changes.
2. If you're not sure if the change you're about to make is the right decision, ask and wait for the user prompt.