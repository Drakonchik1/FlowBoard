# FlowBoard - how to start a coding chat

Copy one block into a **new Agent chat** with FlowBoard as workspace root.

---

## Current queue (2026-07-12)

| Status | Task ID | What |
|--------|---------|------|
| in_progress | **s6-council-fixes** | Fix Sprint 6 council High/Medium findings |
| pending | s6-council-verify | Re-verify after fixes (read-only council) |
| pending | s6-docs | Sync README/SPRINT after verify passes |

**Start here:** s6-council-fixes (not s6-docs).

---

## Coding prompt (copy below)

```
@HANDOFF.md @PROJECT_CONTEXT.md @tasks/queue.json @docs/council/sprint-6-report.md

Task: s6-council-fixes - fix remaining High/Medium findings from sprint-6-report.md
Constraints: no auth rotation changes, no migrations without approval, minimal diff
Done when: dotnet test green; High/Medium fixed or deferred with reason in SPRINT.md
After: update HANDOFF.md and queue.json status
```

---

## Council verify (separate chat, after fixes)

```
@HANDOFF.md @docs/council/sprint-6-report.md

Task: s6-council-verify - read-only council, output docs/council/sprint-6-verify-report.md
Constraints: report only unless blocker; no auth/migration changes
Done when: verify report exists; sign-off or list blockers
```

---

## Docs sync (separate chat, after verify)

```
@HANDOFF.md @SPRINT.md @README.md

Task: s6-docs - mark Sprint 6 done, update session log
Done when: SPRINT.md + README reflect delivered Sprint 6
```