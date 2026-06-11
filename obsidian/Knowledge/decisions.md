# Architecture & Design Decisions

> Append to **Index** first (one line), then add full entry below.
> Codex reads the Index only — follow a specific DXX link only if that decision is relevant to the current task.

## Index
| ID | Title | Owner | Summary |
|----|-------|-------|---------|
| D02 | Mobile loading screen | [[Architect]] | Loading scene is the first build scene and stays visible for at least 2 seconds while async-loading Game |
| D00 | .md memory system | [[PM]] | Chose .md over JSON — append-safe, Obsidian-native |
| D01 | Rigidbody2D for movement | [[Architect]] | Never Transform.Translate — physics engine handles collisions |

---

## D00 — Use .md-based memory system

**Decision**: All agent state in plain .md files — STATUS, TASKS, CONTEXT, per-agent Memory, Sessions logs.
**Rationale**: Codex reads .md natively. Append is safer than full-file JSON rewrite. No encoding issues. Obsidian renders it.
**Rejected**: MEMORY.json — full-file rewrites, no append, encoding failures with non-ASCII.
Related: [[STATUS]] · [[TASKS]] · [[CONTEXT]] · [[Memory]]

---

## D01 — Rigidbody2D for all movement

**Decision**: All moving GameObjects use `Rigidbody2D` forces/velocity. Never `Transform.Translate`.
**Rationale**: Transform bypasses the physics engine → missed collision events.
**Rejected**: Transform.Translate — visually works but breaks OnCollisionEnter2D.
Related: [[Conventions]] · [[Coder]]

---

## D02 - Mobile loading screen before Game

**Decision**: `Loading` is the first build scene. It async-loads `Game`, keeps the loading screen visible for at least 2 seconds, and fills the existing bar using both elapsed minimum time and actual scene load progress.
**Rationale**: Mobile players should see the branded loading screen, but the bar must still represent readiness of the next scene.
**Rejected**: Instant scene switch or fixed fake timer only.
Related: [[GDD]] - [[Coder]]

---

> Add new decisions here. One line in Index + full entry below the last `---`.
