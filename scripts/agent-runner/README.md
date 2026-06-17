# FlowBoard agent-runner

Orchestrates **one Cursor agent session per task** with fresh context. Aligned with [@cursor/sdk](https://cursor.com/docs/api/sdk/typescript) **v1.0.19** (latest on npm).

## Requirements

- **Node.js ≥ 22.13** (`node -v`)
- **CURSOR_API_KEY** in repo-root `.env` (git-ignored)
- Dependencies: `@cursor/sdk` + `@connectrpc/connect-node` (installed via `npm install`)

## Quick start

```powershell
# Preview prompt for a manual new chat (no API key)
powershell scripts/run-next-task.ps1 -DryRun

# Show queue
powershell scripts/run-next-task.ps1 -Status

# One-time: copy .env and add API key from Cursor Dashboard → API Keys
Copy-Item .env.example .env

# Automated run
powershell scripts/run-next-task.ps1 -Run

# Up to 3 tasks (new agent session each time)
powershell scripts/run-next-task.ps1 -Loop -Max 3
```

## Cursor SDK alignment

| Setting | Value | Why |
|---------|-------|-----|
| Model | `composer-2.5` | Current default per Cursor docs (`composer-2` reroutes to 2.5) |
| Runtime | `local: { cwd }` | Runs against your working tree |
| `settingSources` | `["project"]` | Loads `.cursor/rules/flowboard.mdc` from the repo |
| Agent lifecycle | `await using agent = await Agent.create()` | Per SDK resource-management docs |
| Peer dep | `@connectrpc/connect-node` | Required on Node; not bundled in SDK |

Docs: [cursor.com/docs/api/sdk/typescript](https://cursor.com/docs/api/sdk/typescript)

## How it works

Each `Agent.create()` = **new agent** (fresh session). State carries forward via `HANDOFF.md`, `SPRINT.md`, `tasks/queue.json`.

After a task is marked **done** and tests pass, the runner **commits and pushes** to `origin/<current-branch>`:

| Task kind | Commit prefix | Example |
|-----------|---------------|---------|
| Feature (`s6-01`, …) | `feat(task-id):` | `feat(s6-01): Comments entity + CRUD API` |
| Closeout (`close-01`, …) | `fix(task-id):` | `fix(close-01): Wire Redis in docker-compose API` |
| Docs / council | `docs(task-id):` | `docs(s5-council): Live Council — Sprint 5` |

Skip with `--skip-git` (PowerShell: `-SkipGit`). Sensitive paths (`.env`, `secrets.json`) block the commit.

### Live Council (sprint-end)

Tasks with `"kind": "council"` run **after** regular implementation tasks:

1. **3 reviewers in parallel** — bug hunter, security auditor, architecture guard (separate agent sessions).
2. **1 synthesizer** — merges drafts into `docs/council/sprint-N-report.md`.

Council tasks are **read-only** (no code fixes). Security checks use OWASP API Top 10 2023, ASVS L2, CWE Top 25, and project `SECURITY.md`.

Per-member `scope`: `"sprint"` (default) or `"project"` (full-repo security baseline — used for `s5-council`).

Template for new sprints: `tasks/council-task.template.json`. See `docs/council/README.md`.

```powershell
# Preview all council prompts
powershell scripts/run-next-task.ps1 -DryRun
```

## Files

| File | Role |
|------|------|
| `tasks/queue.json` | Task queue |
| `HANDOFF.md` | Last run summary |
| `SPRINT.md` | Sprint state |
| `.env` | `CURSOR_API_KEY` (never commit) |

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | Task completed |
| 1 | Config / SDK startup error |
| 2 | Run error or task not done |
| 75 | Transient SDK error (retry) |
