# Live Council reports

Multi-agent sprint-end review orchestrated by `agent-runner` (`kind: "council"` tasks in `tasks/queue.json`).

## What runs

1. **Parallel reviewers** (separate agent sessions) write drafts to `docs/council/.work/`.
2. **Council chair** (synthesizer) merges into `docs/council/sprint-N-report.md`.

Default reviewers:

| Member | Focus |
|--------|--------|
| `bug-hunter` | Logic bugs, test gaps, EF/Dapper/SignalR edge cases |
| `security-auditor` | OWASP API Top 10 2023, ASVS L2, JWT, RBAC, secrets |

### Review scope per member

| `scope` | Meaning |
|---------|---------|
| `sprint` (default) | Changes from the current sprint |
| `project` | Full codebase baseline — use on `security-auditor` at milestones (Sprint 5 closeout audits Sprints 1–5) |

Set on each member in `tasks/queue.json`: `"scope": "project"`.
| `architecture-guard` | Clean Architecture, conventions, scope creep |

## Adding to a new sprint

1. Copy `tasks/council-task.template.json` → `sN-council` (read-only review).
2. Add **`sN-council-fixes`** after council with `"gitPublish": true` — implement findings; agent-runner pushes to GitHub when done.
3. Add `sN-docs` after fixes (docs only, no git publish).

Place council **after** feature tasks, **before** council-fixes.

## Manual preview

```powershell
powershell scripts/run-next-task.ps1 -DryRun
```

When the next task is a council task, dry-run prints all member prompts plus the synthesizer prompt.

## `.work/` directory

Transient member drafts. Safe to delete after the final report exists. Not required for CI.
