# Live Council Report — Sprint 6

**Date:** 2026-06-18  
**Task:** s6-council — Live Council — Sprint 6 review (bugs + security + architecture)

## Executive summary

Sprint 6 delivers comments, tags, card assignment email, and refresh-token cleanup with **241 unit + 13 integration tests passing** and consistent workspace RBAC on new mutation handlers. No **Critical** vulnerabilities were found; credential hygiene per `SECURITY.md` remains intact. The highest-impact issues are a **shared `FindAsync` soft-delete bypass** in `Repository.GetByIdAsync` (affecting comments, tags, and cards on single-entity paths), **read-model drift** (`AssigneeId` on EF/DTO paths but absent from Dapper `GetBoard`), and **assignee lifecycle holes** (TOCTOU on assign, no cleanup when a member is removed). Security residuals include stored XSS in comment bodies, in-workspace comment edit/delete without author checks, unbounded email queue growth, and no rate limits on new write endpoints. Clean Architecture boundaries are preserved; remediation is queued in `s6-council-fixes` before publish.

## Findings (prioritized)

| # | Sev | Category | Location | Issue | Action |
|---|-----|----------|----------|-------|--------|
| 1 | High | Bug | `src/FlowBoard.Infrastructure/Persistence/Repositories/Repository.cs:17-18` | `DbSet.FindAsync` **does not apply** global soft-delete query filters (`CommentConfiguration.cs:35`, `TagConfiguration.cs:34`, `CardConfiguration.cs:53`). Single-entity loads return deleted rows; list endpoints hide them. Affects `GetCommentById`, `UpdateComment`, `DeleteComment` (re-delete returns 204), `GetTagById`, `UpdateTag`, `ApplyTagToCard` (orphan `card_tags`), and `CreateComment` on soft-deleted cards. | Replace `FindAsync` with filtered `FirstOrDefaultAsync` or explicit `!IsDeleted` guard; add integration test delete → `GetCommentById` → 404. |
| 2 | High | Bug / Arch | `src/FlowBoard.Infrastructure/Persistence/Repositories/BoardReadService.cs:22-24`, `BoardViewDtos.cs:23-31`, `GetCardByIdQueryHandler.cs:30-32` | Dapper `GetBoard` **omits `AssigneeId`** from cards SELECT; `CardViewDto` has no assignee field. EF `GetCardById` and mutation responses include it — inconsistent API contract within Sprint 6. | Extend SQL, `CardRow`, `CardViewDto`, projection; add integration test assign → `GetBoard` shows assignee. |
| 3 | High | Bug | `src/FlowBoard.Application/Features/Cards/Commands/AssignCard/AssignCardCommandHandler.cs:33-44` | **TOCTOU:** assignee membership validated on in-memory workspace snapshot, then saved without transaction or re-check. Concurrent `RemoveMember` can leave card assigned to ex-member. | Re-check membership in same transaction as card update, or enforce via FK/trigger. |
| 4 | High | Bug | `src/FlowBoard.Application/Features/Workspaces/Commands/RemoveMember/RemoveMemberCommandHandler.cs:29-31`, `CardConfiguration.cs:35`, `20260618153937_AddCardAssignee.cs:14-18` | Member removal does **not** clear `cards.AssigneeId`; no FK on assignee. Stale GUIDs persist and surface on `GetCardById`. | Null assignee on member removal; or add FK `AssigneeId → users.Id` with `OnDelete(SetNull)`. |
| 5 | Medium | Security / Bug | `UpdateCommentCommandHandler.cs:34-37`, `DeleteCommentCommandHandler.cs:32-35` | Any workspace **writer** can update or soft-delete **any** comment — no `AuthorId == userId` check (OWASP API1 / BOLA). | Restrict to author (403) or Admin/Owner; document and test chosen policy. |
| 6 | Medium | Security | `Comment.cs:39`, `GetCommentsByCardQueryHandler.cs:34-36`, `BoardRealtimeNotifier.cs:25-31` | Comment `Body` stored and returned/broadcast verbatim over REST and SignalR. HTML/script payloads execute if clients render as HTML (CWE-79, ASVS 5.3). | HTML-encode on output; document client escaping; consider server-side allowlist sanitization. |
| 7 | Medium | Security / Arch | `EmailQueue.cs:11-16`, `QueuedEmailService.cs:17` | Email queue uses `Channel.CreateUnbounded` — burst assignments grow process memory without backpressure (OWASP API4). | Bounded channel + metrics; or replace with Hangfire in Sprint 7 (`s7-02`). |
| 8 | Medium | Security | `Program.cs:46-67`, `CommentsController.cs:26-38`, `TagsController.cs:29-44`, `CardsController.cs:72-81` | Rate limiting (`5/min/IP`) applies only to `AuthController`. Sprint 6 write endpoints (comments, tags, assign) have no HTTP throttle — comment/tag spam and assignment abuse unbounded. | Per-user or per-IP policies on authenticated mutations (`s8-02`); at minimum limit comment create and assign. |
| 9 | Medium | Security / Bug | `Card.cs:107-114`, `CardAssignedEventHandler.cs:37-42` | Every assignee **change** to a non-null user raises `CardAssignedEvent` and queues SMTP. Malicious writer can cycle assignees to spam inbox (API6). Combines with assign TOCTOU to notify ex-members. | Debounce (one email per card/assignee per N minutes); suppress no-op churn; re-check assignee membership before send. |
| 10 | Medium | Bug | `UnitOfWork.cs:45`, `QueuedEmailService.cs:17` | Domain events published with **request `CancellationToken`**. Client disconnect can cancel queue write after commit. | Use `CancellationToken.None` for post-commit side effects (email, SignalR). |
| 11 | Medium | Arch | `CardConfiguration.cs:35`, `CommentConfiguration.cs:30-33` | `AssigneeId` nullable with no FK to `users`; `Comment.AuthorId` correctly FKs with `Restrict`. Orphan assignee GUIDs possible; integrity relies on runtime checks only. | Add optional FK with `OnDelete(SetNull)` or document intentional denormalization + cleanup on removal. |
| 12 | Medium | Bug | `EmailBackgroundService.cs:16-34` | SMTP failures logged and **discarded**; host shutdown abandons queued mail. No retry or dead-letter. | Retry policy or persist queue — Sprint 7 Hangfire scope (`s7-02`). |
| 13 | Medium | Tests | `CommentsAndTagsWorkflowTests.cs`, `tests/FlowBoard.UnitTests/Handlers/Tags/` | **Zero integration authz tests** (no second user, Viewer 403, outsider 404). Tag **query** handlers (`GetTagById`, `GetTagsByWorkspace`, `GetTagsByCard`) have no dedicated authz unit tests (comment queries covered). | Add multi-user integration smoke per feature; 404 non-member tests for tag queries per close-07 matrix. |
| 14 | Medium | Tests | `AssignCardCommandHandlerTests.cs`, `CommentsAndTagsWorkflowTests.cs` | No integration test for `AssignCard`, email queue path, or assignee round-trip. Unit tests missing unassign, `Guid.Empty`, idempotent re-assign, assignee-not-found email branch. | Add workflow test assign → `GetCardById`; extend handler and `CardAssignedEventHandlerTests`. |
| 15 | Medium | Bug | `AssignCardCommandValidator.cs:7-10` | Validator only checks `CardId`; **`Guid.Empty` assignee** not rejected (handler 404s via `HasMember`; domain accepts empty without event at `Card.cs:113-114`). | Add `Must(id => id is null \|\| id != Guid.Empty)`. |
| 16 | Low | Validation | `CreateCommentCommandValidator.cs:11-13`, `CreateTagCommandValidator.cs:15-17`, `DeleteCommentCommand.cs:5`, `DeleteTagCommand.cs:5` | Whitespace-only comment body and invalid hex color pass FluentValidation; domain throws → **400** not 422. ID-only delete commands lack validators. | Add whitespace/hex rules; minimal `NotEmpty()` validators on delete commands. |
| 17 | Low | Security | `SmtpEmailService.cs:65`, `SmtpSettings.cs:13`, `SmtpEmailService.cs:68-76` | Successful sends log recipient at **Information** (PII, ASVS 7.1). `UseSsl = false` allows cleartext SMTP — risky if misconfigured in production. | Redact recipient in logs; fail startup in Production when TLS not enforced. |
| 18 | Low | Realtime | `Comment.cs:50-61`, `IBoardRealtimeNotifier.cs`, `BoardRealtimeNotifier.cs:12-31` | Comment update/delete raise **no domain events** — clients only see creates. **No SignalR for assignee changes.** Notifier methods accept but do not forward `cancellationToken`. | Document product gaps or add broadcasts; pass token into hub invocation. |
| 19 | Low | Arch | `CardAssignedEventHandler.cs:37-42`, `SmtpEmailService.cs:10-12`, `DependencyInjection.cs:48-49` | Assignment email HTML embedded in Application event handler. `SmtpEmailService` implements `IEmailService` but only `QueuedEmailService` is registered — dual port on one class invites DI misuse. | Extract email template builder to Infrastructure; drop `IEmailService` from SMTP sender or use keyed registration. |
| 20 | Low | Bug / API | `RemoveTagFromCardCommandHandler.cs:37-38`, `SmtpEmailService.cs:23-38`, `SmtpEmailService.cs:43` | Not-applied tag throws `NotFoundException("Tag", …)` — misleading resource name. Unconfigured SMTP silently skips send; corrupt address dropped in background worker. | Distinct not-found key; validate email before enqueue; document ops requirement. |
| 21 | Low | Bug | `RefreshTokenRepository.cs:45-56` | `RevokeExpiredAsync` loads **all** expired tokens into memory — unbounded growth on long-lived deployments. | Batch with `Take` or set-based SQL UPDATE (`s7-02` or follow-up). |
| 22 | Info | Email / Tests | `BoardEvents.cs` (CardAssignedEvent), `CardAssignedEventHandler.cs:27-42`, `CommentsAndTagsWorkflowTests.cs:97-103` | `AssignedByUserId` captured but unused (self-assignment same email as third-party). `CommentAdded` integration test omits `CreatedAt` assertion. | Personalize/suppress self-assign email; extend payload assertion. |
| 23 | Info | Docs | `tasks/queue.json:44-46` vs `SPRINT.md:56-64` | Queue marks Sprint 6 roadmap `"planned"` while SPRINT checklist shows s6-01…s6-07 done. | Resolve in `s6-docs` task. |

## Security posture

**Full-project baseline (Sprints 1–6) — not limited to Sprint 6 delta:**

| Area | Assessment |
|------|------------|
| **JWT access tokens** | HMAC-SHA256; full validation; 15-min TTL; 30s clock skew — unchanged, strong. |
| **Refresh tokens** | SHA-256 hash storage; rotation + family revocation on reuse; daily expired-token cleanup added (`CleanupExpiredRefreshTokensService`) — **strong**. |
| **Passwords** | BCrypt wf 12; constant-time login path — unchanged. |
| **Workspace RBAC (REST)** | Non-members get **404**; Viewer writes **403** — consistently applied on Sprint 6 handlers (comments, tags, `AssignCard`). |
| **Anti-enumeration** | Non-workspace assignees → card 404; cross-workspace tags → tag 404 — preserved. |
| **SignalR** | `[Authorize]` hub; join checks membership; group eviction on removal/Viewer downgrade (close-02). Sprint 6 adds `CommentAdded` to same `board:{boardId}` groups — **good**. Stale groups fixed in closeout; comment/assignee update broadcasts still absent. |
| **Stored XSS** | Comment bodies returned verbatim over REST + SignalR — **new gap** (finding #6). Card title HTML-encoded in assignment email — mitigated. |
| **BOLA (in-workspace)** | Comment edit/delete by any writer — **new gap** (finding #5). |
| **Resource consumption** | Auth rate-limited only; unbounded email queue; assignment email spam vector — **gaps** (findings #7–9). |
| **SQL injection** | EF LINQ + parameterized Dapper; no new raw SQL paths in Sprint 6. |
| **Validation** | FluentValidation on create/update commands; gaps on delete commands and some domain-edge cases (finding #16). |
| **Secrets** | Empty JWT/SMTP in committed `appsettings.json`; `.env.example` documents env vars — compliant with `SECURITY.md`. |
| **Post-commit resilience** | Event handlers swallow notifier/email failures — committed writes do not 500 (close-03 pattern). CancellationToken coupling remains (finding #10). |
| **Dev exposure** | SQL/Redis host ports; Development env — carryover from Sprints 1–5; production hardening deferred to Sprint 8. |

**OWASP API Top 10 (2023):**

| Risk | Status |
|------|--------|
| API1 Broken Object Level Authorization | **Strong** on workspace boundary (404 outsiders); **gap** on comment author checks and soft-delete bypass reads |
| API2 Broken Authentication | **Strong** JWT/refresh; SignalR query-token transport residual (closeout) |
| API3 Broken Object Property Level Authorization | **Good** — explicit DTOs |
| API4 Unrestricted Resource Consumption | **Gap** — auth-only rate limit; unbounded email queue |
| API5 Broken Function Level Authorization | **Good** — Viewer 403 on writes |
| API6 Unrestricted Access to Sensitive Business Flows | **Gap** — assignment email spam via assignee churn |
| API7 Server Side Request Forgery | **N/A** — no outbound URL fetch |
| API8 Security Misconfiguration | **Acceptable** dev; production compose/TLS/redis auth → Sprint 8 |
| API9 Improper Inventory Management | OpenAPI dev-only — acceptable |
| API10 Unsafe Consumption of APIs | **N/A** |

**ASVS L2 highlights:** V2 auth strong; V4 access control strong at workspace level, weak on comment ownership; V5 validation partial (422 vs 400 drift); V7 logging — email recipient PII at Information; V9 communication — SMTP TLS optional.

## Test & quality gaps

- **Integration authz:** No multi-user 404/403 scenarios in `CommentsAndTagsWorkflowTests` or elsewhere in IntegrationTests — cross-tenant IDOR regression risk on Sprint 6 endpoints.
- **Soft-delete regression:** Integration test deletes comment but never re-fetches by id; no unit tests for update/delete-after-soft-delete.
- **Tag query authz matrix:** Three tag query handlers untested; comment query handlers covered.
- **AssignCard coverage:** Unit tests lack unassign, empty GUID, card missing, idempotent re-assign, assignee-not-found email branch; no integration assign/email workflow.
- **Handler happy paths:** `UpdateCommentCommandHandlerTests` missing member success path; `CreateTagCommandHandlerTests` missing duplicate-name conflict; `RemoveTagFromCardCommandHandlerTests` missing NonMember 404.
- **GetCardById / tag list queries:** No dedicated unit tests for read handlers added in Sprint 6.
- **CI parity:** 241 unit + 13 integration green with Docker; 0 skipped when Docker available — document Docker requirement for CI (`s8-03`).

## Recommended follow-up tasks

1. **`s6-council-fixes` (immediate):** Fix `Repository.GetByIdAsync` soft-delete bypass (#1); add AssigneeId to Dapper board read (#2); assignee lifecycle — TOCTOU transaction + clear assignee on `RemoveMember` (#3–4); author guard or documented policy on comment edit/delete (#5); `CancellationToken.None` for post-commit publish (#10); `AssignCardCommandValidator` Guid.Empty (#15); soft-delete and assign integration/unit tests.
2. **`s6-council-fixes` (medium, same task or split):** Comment output encoding / XSS mitigation (#6); assignment email debounce + membership re-check before send (#9); bounded email channel or explicit defer to Sprint 7 (#7).
3. **`s6-council-verify`:** Re-run council after fixes; sign off for GitHub publish.
4. **`s6-docs`:** Mark Sprint 6 done in SPRINT/queue; sync README test counts.
5. **`s7-02`:** Migrate email + token cleanup to Hangfire with retry/dead-letter (#7, #12, #21).
6. **`s8-02`:** Rate limiting on authenticated write endpoints (#8).
7. **`s8-01` / `s8-03`:** Production TLS/SMTP enforcement (#17); CI Docker parity.
8. **Backlog (low):** FluentValidation consistency (#16); SignalR for comment update/delete and assignee (#18); email template extraction (#19); realtime `CreatedAt` assertion (#22).

## Sign-off

- **Bug hunter:** Sprint 6 logic is sound on happy paths and write authz; **241+13 tests green**. Top bugs: shared `FindAsync` soft-delete bypass, Dapper/EF assignee drift, assignee TOCTOU and stale assignee on member removal. Test matrix incomplete on queries, AssignCard, and integration authz.
- **Security:** No Critical findings; RBAC and credential hygiene maintained. New surfaces add stored XSS, in-workspace BOLA on comments, resource consumption (unbounded queue, no write rate limits), and assignment email abuse. Refresh cleanup improves session hygiene.
- **Architecture:** Clean Architecture boundaries intact; thin controllers; event-handler convention followed. Main debt: read-model drift on `AssigneeId`, missing FK on assignee, tag query test convention gap, and presentation logic in `CardAssignedEventHandler`.
