# AI workflow — FlowBoard

How to keep Cursor effective across sprint tasks without burning your AI plan.

## Golden rules

1. **One chat = one task** — close chat after commit lands on GitHub.
2. **Always attach `SPRINT.md`** — agent reads state from file, not memory.
3. **Precise `@` files** — e.g. `@CreateCardCommandHandler.cs` not `@Codebase`.
4. **One GitHub push per sprint** — only after council **re-verification** passes (`close-council`, `sN-council-verify`). Review → fixes → verify → publish.

## Task prompt template (copy-paste)

```
Task: [single concrete outcome]
Context: @SPRINT.md
Files: @[relevant files]
Done when: [tests / behavior]
Don't touch: [auth, migrations, etc.]
After: update SPRINT.md checklist + session log
After (manual chat): `git commit` + `git push` only when council **verify** task passes (`close-council`, `sN-council-verify`).
```

**Git publish** (one push per sprint — after full remediation verified):

| Task | When |
|------|------|
| `close-council` | Re-verified close-01…close-11 fixes (Sprints 1–5) |
| `sN-council-verify` | Re-verified Sprint N council fixes |

Cycle: `sN-council` → `sN-council-fixes` (local) → `sN-council-verify` → **push**.

Commit examples:

| Message |
|---------|
| `fix(closeout): Council verified — Sprints 1–5 remediation published` |
| `fix(s6): Council verified — Sprint 6 remediation published` |

### Example — fix integration tests

```
Task: Fix all 4 failing tests in BoardWorkflowTests
Context: @SPRINT.md
Files: @BoardWorkflowTests.cs @BoardReadService.cs @SqlServerFixture.cs
Done when: dotnet test tests/FlowBoard.IntegrationTests passes (Docker running)
Don't touch: auth, workspace RBAC
After: update SPRINT.md — check integration test item, add session log row
```

### Example — new feature (Sprint 4)

```
Task: Add SignalR hub for board card move events — hub + notify on MoveCard only
Context: @SPRINT.md
Files: @MoveCardCommandHandler.cs @Program.cs
Done when: unit test for handler still passes; manual hub test documented in PR description
Don't touch: Dapper read path, auth
Ask before: new NuGet packages
```

## Daily rhythm (~3 tasks)

| Step | Time | Action |
|------|------|--------|
| 1 | 5 min | Update `SPRINT.md` — pick next unchecked item |
| 2 | — | New chat → paste template → work → `dotnet test` |
| 3 | — | On council **verify** task: agent-runner commits + pushes if council signs off |
| 4 | — | **New chat** for next task (do not continue same thread) |

## When quality drops

| Symptom | Fix |
|---------|-----|
| Agent forgets decisions | `@SPRINT.md` + project rule already loaded |
| Repeats wrong approach | New chat with error output + exact files |
| Huge diff / scope creep | Narrow "Done when" + "Don't touch" |
| Burns credits | Shorter chats, fewer `@` files, no marathon threads |

## Chat types (separate threads)

| Type | When | Model tip |
|------|------|-----------|
| **Plan** | "How should I structure X?" | Short — no code |
| **Implement** | One handler / test / fix | Attach 2–5 files |
| **Debug** | Test failure | Paste error + `@` failing test + `@` implementation |

## Automated task runner (optional)

For **fresh agent session per task** without manual chat copy-paste:

```pwsh
pwsh scripts/run-next-task.ps1 -Status          # queue overview
pwsh scripts/run-next-task.ps1 -DryRun          # prompt for manual new chat
# Automated: set CURSOR_API_KEY in `.env` (see `.env.example`, git-ignored). Key: Cursor Dashboard → API Keys.
pwsh scripts/run-next-task.ps1 -Run             # push only when close-council / sN-council-verify passes
pwsh scripts/run-next-task.ps1 -Run -ForceGit   # push after any completed task (override)
pwsh scripts/run-next-task.ps1 -Run -SkipGit    # never push
pwsh scripts/run-next-task.ps1 -Loop -Max 3     # up to 3 tasks sequentially
```

**Sprint end:** last queue item is usually `sN-council` (Live Council) — multi-agent bug + security review. See `docs/council/README.md`.

See `scripts/agent-runner/README.md` for details.

## Files that persist context

| File | Purpose |
|------|---------|
| `SPRINT.md` | Current sprint, decisions, next task |
| `tasks/queue.json` | Machine-readable task queue for agent-runner |
| `HANDOFF.md` | Last session summary (updated after each task) |
| `.cursor/rules/flowboard.mdc` | Auto-loaded conventions |
| `~/.cursor/rules/pavlo-verify-no-sycophancy.mdc` | Your global style (user rules) |
| Git commits | What actually shipped |

## Open workspace

Open folder: `C:\Users\pashk\Projects\FlowBoard`

So project rules and `SPRINT.md` load automatically.
