---
type: AbstractFrame
---
# Agent

Abstract base for all autonomous roles. Session protocol and context loading live in [[AGENTS]] — that is always the entry point.

## Memory Contract
- All state lives in [[Memory]] — never in code or JSON
- Append rows to [[TASKS]], never delete
- Only [[PM]] overwrites [[STATUS]] and [[CONTEXT]]

## Handoff Format
```
Tasks/Open/T{ID}_{TargetAgent}.md
---
task: one-line title
input: list of files the agent must read
criteria: testable, observable acceptance conditions
```
When done: [[PM]] moves file from `Tasks/Open/` → `Tasks/Done/`.

## Subclasses
[[PM]] · [[GameDesigner]] · [[BA]] · [[Architect]] · [[Coder]] · [[ArtDirector]] · [[QA]]
