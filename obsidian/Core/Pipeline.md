---
type: AbstractFrame
---
# Pipeline

The ordered flow of work through the 7-agent team. [[PM]] owns this flow.

## Flow
```
Idea → [[GameDesigner]] → [[GDD]] → [[BA]] → validated GDD
     → [[Architect]] → task cards → [[Coder]] + [[ArtDirector]]
     → [[QA]] → APK
```

## Trigger Table
| When this is ready | PM routes to |
|---|---|
| Game idea from human | [[GameDesigner]] |
| [[GDD]] written | [[BA]] |
| [[GDD]] validated | [[Architect]] |
| Task card in `Tasks/` | [[Coder]] or [[ArtDirector]] |
| Feature in Unity | [[QA]] |
| QA APPROVED | mark done in [[TASKS]] |
| QA BUG | [[Coder]] with bug report |

## Loop
QA bugs feed back to Coder without re-running BA or Architect.
Scope changes feed back to GameDesigner and restart from BA.
