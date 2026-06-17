# Live Council reports

Multi-agent sprint-end review orchestrated by `agent-runner` (`kind: "council"` tasks in `tasks/queue.json`).

## Sprint security cycle (and GitHub publish)

Each sprint follows this flow. **Nothing goes to GitHub until step 4 passes.**

```
1. Feature tasks (sN-01…)     → local
2. sN-council                 → report findings (local)
3. sN-council-fixes           → implement fixes (local)
4. sN-council-verify          → council re-checks → git push if OK
5. sN-docs                    → docs sync (local)
```

Closeout (Sprints 1–5) uses the same pattern:

```
close-01…close-11  → fixes from sprint-5-report (local)
close-docs         → SPRINT/README (local)
close-council      → re-verification → git push if OK
```

Agent-runner publishes on **`close-council`** and **`sN-council-verify`** only. If verify council marks the task `failed`, changes stay local until fixes and re-run.

Templates: `tasks/council-task.template.json`, `tasks/council-fixes-task.template.json`, `tasks/council-verify-task.template.json`.

## What runs

1. **Parallel reviewers** (separate agent sessions) write drafts to `docs/council/.work/`.
2. **Council chair** (synthesizer) merges into `docs/council/sprint-N-report.md` (or `*-verify-report.md`).

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

1. Feature tasks (`sN-01`…)
2. **`sN-council`** — initial review (`tasks/council-task.template.json`)
3. **`sN-council-fixes`** — implement findings (`tasks/council-fixes-task.template.json`)
4. **`sN-council-verify`** — re-verification + git publish (`tasks/council-verify-task.template.json`)
5. **`sN-docs`** — docs sync

## Manual preview

```powershell
powershell scripts/run-next-task.ps1 -DryRun
```

When the next task is a council task, dry-run prints all member prompts plus the synthesizer prompt.

## `.work/` directory

Transient member drafts. Safe to delete after the final report exists. Not required for CI.
