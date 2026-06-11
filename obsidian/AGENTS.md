# AGENTS.md — [YOUR PROJECT NAME]

> Replace [brackets] with your values.
> This file is read by Codex before every task.
> **Before starting any task: identify which agent you are acting as, then find your row in the Context Manifests section and read those files first.**

---

## Project

[One paragraph. What is the game? What is the core mechanic? What platform?]

Engine: Unity 2022 LTS · Language: C# · Platform: Android
Scope: [MVP — e.g. "1 mechanic, 3 systems, playable in 3 sessions"]

---

## Environment

- MCPs: [[UnityMCP]] (localhost:8080) · [[MobileMCP]]
- Art pipeline: ComfyUI FLUX → `Assets/Generated/`
- Unity project path: `[path]`

---

## Session Protocol

Every agent, every session — before doing anything else:

1. Identify which agent you are acting as
   — if the prompt does not specify an agent, **default to PM**: read the PM manifest, check `Tasks/Open/`, and route the work to the correct agent
2. Find your row in the **Context Manifests** table below
3. Read every file in that row's Load column — in order
4. Execute exactly one task
5. Write output to your declared targets
6. Drop handoff in `Tasks/Open/` if another agent needs to act next
7. Append one entry to `Sessions/YYYY-MM-DD`

---

## Context Manifests

> **This is the entry point for every agent.**
> Read these files in order before acting. Do not skip steps.
> If your `*-Memory.md ## Lessons` section contains knowledge that conflicts with your agent frame, **the Lesson overrides the frame** — it is project-validated, the frame is general.

### PM
1. `Agents/PM.md` — role knowledge + routing rules
2. `STATUS.md` — what is done, in progress, blocked
3. `TASKS.md` — full task queue
4. `Tasks/Open/` — list all files here to find what needs action
5. `Agents/PM-Memory.md` — session history
6. `CONTEXT.md` — current sprint summary
7. `Core/Pipeline.md` — only if a routing decision is needed

### GameDesigner
1. `Agents/GameDesigner.md` — role knowledge + scoping rules
2. `STATUS.md` — current project phase
3. `Agents/GameDesigner-Memory.md` — previous GDD decisions
4. `GDD.md` — read current state before writing anything
5. `Templates/GDD-Template.md` — only if starting from scratch

### BA
1. `Agents/BA.md` — role knowledge + acceptance criteria rules
2. `GDD.md` — the document to validate
3. `STATUS.md` — current project phase
4. `Agents/BA-Memory.md` — known ambiguity patterns

### Architect
1. `Agents/Architect.md` — role knowledge + component design rules
2. `GDD.md` — validated game design
3. `Knowledge/decisions.md` — index first, full entry only if relevant
4. `Agents/Architect-Memory.md` — component map and prefab registry
5. `Core/Conventions.md` — Unity rules all task cards must follow

### Coder
1. `Agents/Coder.md` — role knowledge + Unity lifecycle rules
2. `Tasks/Open/[your task card]` — the single task to implement
3. `Core/Conventions.md` — non-negotiable coding rules
4. `Agents/Coder-Memory.md` — scripts written, bugs fixed
5. `Knowledge/decisions.md` — only if an architecture question arises

### ArtDirector
1. `Agents/ArtDirector.md` — role knowledge + FLUX prompt rules
2. `Tasks/Open/[your art task card]` — the asset to generate
3. `GDD.md` — art style section
4. `Agents/ArtDirector-Memory.md` — existing palette, density, prompts

### QA
1. `Agents/QA.md` — role knowledge + test case rules
2. `Tasks/Open/[handoff from Coder]` — what was built and how to test it
3. `GDD.md` — acceptance criteria for this feature
4. `Agents/QA-Memory.md` — known flaky tests, regression checklist

---

## Agents Overview

| Agent | writes to | triggers |
|-------|-----------|----------|
| [[PM]] | [[STATUS]], [[TASKS]], [[CONTEXT]], `Tasks/Open→Done` | all agents |
| [[GameDesigner]] | [[GDD]] | [[BA]] |
| [[BA]] | [[GDD]], `Tasks/Open/` | [[GameDesigner]] or [[Architect]] |
| [[Architect]] | `Tasks/Open/`, [[decisions]] | [[Coder]], [[ArtDirector]] |
| [[Coder]] | C# via [[UnityMCP]] | [[QA]] |
| [[ArtDirector]] | `Assets/Generated/` | [[Coder]] |
| [[QA]] | `Tasks/Open/` (bug) or `Tasks/Done/` (approved) | [[PM]] |
