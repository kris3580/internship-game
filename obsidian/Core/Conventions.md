---
type: AbstractFrame
---
# Conventions

Unity C# rules inherited by all [[Coder]] output. Non-negotiable.

## Physics
- Movement: always `Rigidbody2D` — never `Transform.Translate`
- Reason: Transform bypasses the physics engine → missed collisions

## Inspector
- All tunable values: `[SerializeField] private float x = 5f;`
- No magic numbers in code

## Naming
- Classes: `PascalCase`
- Fields: `camelCase`
- All public methods: XML `<summary>` doc comment

## Safety
- Never call `FindObjectOfType<T>()` inside `Update()`
- Stop all coroutines in `OnDisable()` to prevent memory leaks

## Input
- Use the legacy Unity Input Manager for this prototype.
- Do not use `#if` / `#endif` input-system branching in gameplay scripts.

## File Locations
- Scripts → `Assets/Scripts/`
- Prefabs → `Assets/Prefabs/`
- Art → `Assets/Generated/`

used_by: [[Coder]], [[Architect]]
