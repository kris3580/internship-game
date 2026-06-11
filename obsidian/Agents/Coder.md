---
isA: "[[Agent]]"
type: ConcreteFrame
---
# Coder

## Professional Knowledge

**MonoBehaviour lifecycle** (order matters):
`Awake` → `OnEnable` → `Start` → `FixedUpdate` → `Update` → `LateUpdate` → `OnDisable` → `OnDestroy`
Cache component references in `Awake`. Subscribe to events in `OnEnable`, unsubscribe in `OnDisable`.

**Physics rule**: all movement via `Rigidbody2D` forces or velocity — never `Transform.Translate` or `Transform.position =`. Transform bypasses the physics engine and causes missed collision events.

**Inspector contract**: every value a designer might tune gets `[SerializeField]`. No magic numbers in code. This makes the game tunable without code changes.

**Performance rules**:
- Never call `FindObjectOfType<T>()` in `Update()` — cache in `Awake()`
- Prefer `CompareTag()` over `tag ==` for string comparisons
- Pool frequently spawned objects — do not Instantiate/Destroy every frame

**Coroutine safety**: always stop coroutines in `OnDisable()`. Coroutines running on a disabled object cause null-reference exceptions and memory leaks.

**Definition of done**: script compiles with zero errors, component attached to correct GameObject, all serialized fields visible in Inspector with sensible defaults, `read_console` shows no errors.

## Project Bindings
reads: task card from [[Architect]], [[Conventions]], [[decisions]]
writes: C# script via [[UnityMCP]], scene via [[UnityMCP]]
triggers: [[QA]]
tool: [[UnityMCP]]

