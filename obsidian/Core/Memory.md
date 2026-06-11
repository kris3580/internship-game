---
type: AbstractFrame
---
# Memory

All agent state lives in `.md` files. No JSON. No database. Obsidian renders the vault; Codex reads it directly.

## File Registry
| File | Owner | Write pattern |
|---|---|---|
| [[STATUS]] | [[PM]] | Overwrite header, append body |
| [[TASKS]] | [[PM]] | Append rows only — never delete |
| [[CONTEXT]] | [[PM]] | Full regeneration each session |
| [[GDD]] | [[GameDesigner]] | Fill template sections |
| `Sessions/YYYY-MM-DD` | all | Append-only daily log |
| `Agents/*-Memory` | each agent | Private append-only state |

## Why .md not JSON
See [[decisions]] — D00.

## Concurrent Write Rules
- Multiple agents may append to `Sessions/` simultaneously (each appends, never overwrites)
- Only [[PM]] writes [[STATUS]], [[TASKS]], [[CONTEXT]]
- Each agent writes only its own `*-Memory` file
