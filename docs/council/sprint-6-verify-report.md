# Live Council Report — Sprint 6

**Date:** 2026-07-12  
**Task:** s6-council-verify — Live Council — Sprint 6 remediation verification

## Executive summary

Sprint 6 remediation (`s6-council-fixes`) successfully closed all **High** findings from the initial council review and all **Medium** items that were in scope for immediate fix. The shared `Repository.GetByIdAsync` soft-delete bypass, Dapper/EF assignee read-model drift, assign-card TOCTOU, stale assignee on member removal, in-workspace comment BOLA, stored XSS on comment output, unbounded email queue, post-commit `CancellationToken` coupling, and assignment-email abuse vectors are verified fixed with code evidence and expanded tests. **`dotnet test` is green: 253 unit + 17 integration (0 skipped with Docker available).** No **Critical** or new **High** regressions were found. Remaining risk is **Medium/Low** and either **explicitly deferred** with queue tasks (`s8-02` write rate limits, `s7-02` email retry/dead-letter, `AssigneeId` FK pending migration approval) or acceptable for pre-production dev posture. **Council signs off Sprint 6 for GitHub publish.**

## Findings (prioritized)

| # | Sev | Category | Location | Issue | Action |
|---|-----|----------|----------|-------|--------|
| 1 | Medium | Security | `src/FlowBoard.API/Program.cs:46-67` | Rate limiting (`5/min/IP`) applies only to `/api/auth`. Sprint 6 write endpoints (comments, tags, assign) remain unthrottled — comment/tag spam and assignment churn unbounded at API layer. Original #8. | **Deferred** → `s8-02`. Implement per-user or per-IP mutation limits before production. |
| 2 | Medium | Bug / Arch | `src/FlowBoard.Infrastructure/Persistence/Configurations/CardConfiguration.cs:35` | `AssigneeId` nullable with **no FK** to `users`. Runtime checks + `ClearAssigneeForUserInWorkspaceAsync` mitigate removal; orphan GUIDs possible via direct DB tampering or future code paths. Original #11. | **Deferred** (migration approval). Add optional FK `OnDelete(SetNull)` when approved. |
| 3 | Medium | Bug | `src/FlowBoard.Infrastructure/Services/EmailBackgroundService.cs:31-34` | SMTP failures logged and **discarded**; host shutdown abandons queued mail. No retry or dead-letter. Original #12. | **Deferred** → `s7-02`. Migrate to Hangfire with retry/backoff and dead-letter visibility. |
| 4 | Medium | Security / Perf | `src/FlowBoard.Infrastructure/Services/EmailQueue.cs:13-19`, `QueuedEmailService.cs:17` | Bounded channel (`Capacity = 1000`) uses `BoundedChannelFullMode.Wait` — when full, `WriteAsync` blocks post-commit handlers, tying up thread-pool capacity under burst load. Original #7 fix adds backpressure but not non-blocking failure. | Prefer `TryWrite`/`DropWrite` with metric+log, or external durable queue in `s7-02`. |
| 5 | Medium | Tests | `tests/FlowBoard.IntegrationTests/CommentsAndTagsWorkflowTests.cs` | Integration authz matrix **partial**: outsider 404 on tag queries and Viewer 403 on comment create covered; missing outsider on comment create/get, Viewer 403 on tag apply/update, non-author comment update/delete integration, assign outsider 404. Original #13. | Extend multi-user integration smoke per close-07 matrix. |
| 6 | Medium | Tests | `tests/FlowBoard.IntegrationTests/CommentsAndTagsWorkflowTests.cs`, `tests/FlowBoard.UnitTests/Handlers/Workspaces/RemoveMemberCommandHandlerTests.cs` | No integration test that `RemoveMember` clears `cards.AssigneeId` and `GetBoard`/`GetCardById` reflect null assignee. Original #4 fix verified at unit/mock level only. | Add assign → remove member → assert assignee cleared on board read. |
| 7 | Medium | Tests | `src/FlowBoard.Application/EventHandlers/AssignmentEmailThrottle.cs:10-22` | No unit tests for `ShouldSend` dedup window; throttle is static process-wide state — regression risk and parallel-test pollution. | Add throttle unit tests; consider instance-scoped throttle for testability. |
| 8 | Medium | Tests | `tests/FlowBoard.IntegrationTests/CommentsAndTagsWorkflowTests.cs` | No integration test for assignment email queue path. `CardAssignedEventHandler` email branch untested end-to-end. Original #14 residual. | Capture/enqueue assertion via test double for `IEmailService` in integration fixture. |
| 9 | Low | Security | `src/FlowBoard.Application/EventHandlers/AssignmentEmailThrottle.cs:13-22`, `src/FlowBoard.Domain/Entities/Card.cs:113-114` | Throttle suppresses duplicate emails per `(cardId, assigneeId)` within 5 min, but writer can assign **distinct** members in sequence — residual assignee-churn vector, much reduced vs. pre-fix. Original #9 residual. | Consider per-card throttle across all assignees, or cap emails per card per hour. |
| 10 | Low | Validation | `src/FlowBoard.Application/Features/Comments/Commands/CreateComment/CreateCommentCommandValidator.cs:11-13` | FluentValidation `NotEmpty()` allows whitespace-only body; domain throws → HTTP 400 not 422. Original #16. | Add `.Must(b => !string.IsNullOrWhiteSpace(b))`. |
| 11 | Low | Bug / API | `src/FlowBoard.Application/Features/Tags/Commands/RemoveTagFromCard/RemoveTagFromCardCommandHandler.cs:37-38` | Not-applied tag throws `NotFoundException("Tag", …)` — misleading resource name. Original #20. | Use distinct key e.g. `"CardTag"` or 404 on card scope. |
| 12 | Low | Security | `src/FlowBoard.Infrastructure/Services/SmtpEmailService.cs:65` | Successful sends log full recipient at Information (PII, ASVS 7.1). Original #17. | Redact/hash recipient in logs. |
| 13 | Low | Security | `src/FlowBoard.Infrastructure/Services/SmtpEmailService.cs:68-71`, `SmtpSettings.cs:13` | `UseSsl = false` allows cleartext SMTP if misconfigured in production. | Fail startup in Production when TLS not enforced (`s8-01`). |
| 14 | Low | Realtime | `src/FlowBoard.Domain/Entities/Comment.cs:50-61`, `src/FlowBoard.API/Services/BoardRealtimeNotifier.cs:23-32` | Comment update/delete and assignee changes emit no SignalR events; clients only see creates + moves. Original #18. | Document product gap or add broadcasts in backlog. |
| 15 | Low | Arch | `src/FlowBoard.API/Services/BoardRealtimeNotifier.cs:13-32` | Notifier methods accept `cancellationToken` but do not forward it to hub invocations. | Pass token into hub calls or remove unused parameter. |
| 16 | Low | Arch | `src/FlowBoard.Application/EventHandlers/CardAssignedEventHandler.cs:68-71` | Assignment email HTML embedded in Application event handler. | Extract template builder to Infrastructure in `s7-02`. |
| 17 | Low | Arch | `src/FlowBoard.Infrastructure/DependencyInjection.cs:47-49`, `SmtpEmailService.cs:12` | `SmtpEmailService` implements `IEmailService` but only `QueuedEmailService` registered as port; worker injects concrete SMTP class. | Split transport from queue adapter in `s7-02`. |
| 18 | Low | Bug | `src/FlowBoard.Infrastructure/Persistence/Repositories/RefreshTokenRepository.cs:48-51` | `RevokeExpiredAsync` loads all expired tokens into memory — unbounded on neglected deployments. Original #21. | Batch `Take` loop or set-based SQL UPDATE (`s7-02`). |
| 19 | Low | Memory | `src/FlowBoard.Application/EventHandlers/AssignmentEmailThrottle.cs:10-21` | Static `ConcurrentDictionary` never prunes stale keys — unbounded growth; ineffective under Redis scale-out. | TTL eviction or distributed cache key in `s7-02`. |
| 20 | Low | Security | `src/FlowBoard.API/Configuration/JwtBearerSignalRExtensions.cs:21-25` | JWT via `?access_token=` query string for WebSocket negotiate — may appear in proxy logs (Sprint 4 carryover). | Document client hygiene; short-lived hub tokens in production. |
| 21 | Info | Docs | `tasks/queue.json:44-46` vs `SPRINT.md:25-26,56-65` | Queue marks Sprint 6 roadmap `"planned"` while SPRINT checklist shows s6-01…s6-council-fixes done. Original #23. | Resolve in pending `s6-docs`. |
| 22 | Info | Tests | `tests/FlowBoard.IntegrationTests/CommentsAndTagsWorkflowTests.cs:95-111` | `CommentAdded` integration test asserts raw domain-event body, not HTML-encoded wire format. | Assert encoded payload or document domain vs API difference. |

### Remediation verification (original sprint-6-report High/Medium)

| Original # | Sev | Status | Evidence |
|------------|-----|--------|----------|
| 1 | High | **Fixed** | `Repository.cs:17-18` filtered `FirstOrDefaultAsync`; `CommentsAndTagsWorkflowTests.cs:180-191` |
| 2 | High | **Fixed** | `BoardReadService.cs:22-24,50`; `BoardViewDtos.cs:30`; integration `AssignCard_GetBoardAndGetCardById_ShowAssignee` |
| 3 | High | **Fixed** | `AssignCardCommandHandler.cs:30-50` transactional membership re-check |
| 4 | High | **Fixed** | `RemoveMemberCommandHandler.cs:32`; `CardRepository.cs:36-50`; unit tests |
| 5 | Medium | **Fixed** | `ResourceGuard.cs:29-34`; author-only update/delete; unit tests |
| 6 | Medium | **Fixed** | `CommentMapper.cs:13`; `BoardRealtimeNotifier.cs:31` HTML-encode on egress |
| 7 | Medium | **Fixed** (bounded) | `EmailQueue.cs:11-19` capacity 1000; residual backpressure → finding #4 |
| 8 | Medium | **Deferred** | `s8-02` → finding #1 |
| 9 | Medium | **Mitigated** | `AssignmentEmailThrottle.cs`; `CardAssignedEventHandler.cs:48-56` debounce + membership re-check |
| 10 | Medium | **Fixed** | `UnitOfWork.cs:45` `CancellationToken.None` |
| 11 | Medium | **Deferred** | FK pending approval → finding #2 |
| 12 | Medium | **Deferred** | `s7-02` → finding #3 |
| 13 | Medium | **Partial** | +12 unit authz tests; integration partial → finding #5 |
| 14 | Medium | **Fixed** (unit + partial integration) | `AssignCardCommandHandlerTests.cs`; assign round-trip integration; email path gap → finding #8 |
| 15 | Medium | **Fixed** | `AssignCardCommandValidator.cs:10-12` rejects `Guid.Empty` |

## Security posture

**Full-project baseline (Sprints 1–6) — not limited to Sprint 6 delta:**

| Area | Assessment |
|------|------------|
| **JWT access tokens** | HMAC-SHA256; full validation; 15-min TTL; 30s clock skew — strong. |
| **Refresh tokens** | SHA-256 hash storage; rotation + family revocation on reuse; daily expired-token cleanup (`CleanupExpiredRefreshTokensService`) — **strong**. |
| **Passwords** | BCrypt wf 12; constant-time login path — unchanged. |
| **Workspace RBAC (REST)** | Non-members get **404**; Viewer writes **403** — consistently applied on Sprint 6 handlers (comments, tags, `AssignCard`). |
| **Anti-enumeration** | Non-workspace assignees → card 404; cross-workspace tags → tag 404 — preserved. |
| **SignalR** | `[Authorize]` hub; join checks membership; group eviction on removal/Viewer downgrade (close-02). `CommentAdded` scoped to `board:{boardId}` with encoded body — **good**. Comment/assignee update broadcasts still absent (product gap). |
| **Stored XSS** | Comment bodies HTML-encoded on REST (`CommentMapper`) and SignalR (`BoardRealtimeNotifier`); stored raw in DB — **mitigated** on egress; add regression test with `<script>` payload. |
| **BOLA (in-workspace)** | Comment edit/delete restricted to author (`ResourceGuard.EnsureCommentAuthor`) — **fixed**. |
| **Soft-delete consistency** | `GetByIdAsync` respects global query filters — **fixed** (was High bypass). |
| **Resource consumption** | Auth rate-limited only; bounded email queue with Wait backpressure; assignment email throttled — **improved**; write rate limits deferred to Sprint 8. |
| **SQL injection** | EF LINQ + parameterized Dapper; no new raw SQL paths in Sprint 6. |
| **Validation** | FluentValidation on create/update commands; whitespace/delete-command gaps remain Low. |
| **Secrets** | Empty JWT/SMTP in committed `appsettings.json`; `.env` gitignored per `SECURITY.md` — compliant. |
| **Post-commit resilience** | Event handlers catch/log failures; `CancellationToken.None` on publish — committed writes do not 500. |
| **Dev exposure** | SQL/Redis host ports; Development env — carryover; production hardening deferred to Sprint 8. |

**OWASP API Top 10 (2023):**

| Risk | Status |
|------|--------|
| API1 Broken Object Level Authorization | **Strong** on workspace boundary; comment author checks **fixed**; soft-delete bypass **fixed** |
| API2 Broken Authentication | **Strong** JWT/refresh; SignalR query-token transport residual (Low) |
| API3 Broken Object Property Level Authorization | **Good** — explicit DTOs |
| API4 Unrestricted Resource Consumption | **Gap** — auth-only rate limit (deferred `s8-02`); bounded email queue with Wait backpressure |
| API5 Broken Function Level Authorization | **Good** — Viewer 403 on writes |
| API6 Unrestricted Access to Sensitive Business Flows | **Mitigated** — assignment email throttle + membership re-check; residual churn Low |
| API7 Server Side Request Forgery | **N/A** |
| API8 Security Misconfiguration | **Acceptable** dev; production compose/TLS/redis auth → Sprint 8 |
| API9 Improper Inventory Management | OpenAPI dev-only — acceptable |
| API10 Unsafe Consumption of APIs | **N/A** |

**ASVS L2 highlights:** V2 auth strong; V4 access control strong at workspace level with comment ownership enforced; V5 validation partial (422 vs 400 drift Low); V7 logging — email recipient PII at Information (Low); V9 communication — SMTP TLS optional (Low, enforce in prod).

## Test & quality gaps

- **Integration authz matrix incomplete** — tag query outsider 404 and comment create Viewer 403 covered; comment update/delete author, tag mutation authz, assign outsider 404 missing (finding #5).
- **Assignee lifecycle integration** — assign round-trip covered; `RemoveMember` assignee-clear not integration-tested (finding #6).
- **Email path untested end-to-end** — `CardAssignedEventHandler` queue branch has no integration assertion (finding #8).
- **Throttle unit tests missing** — `AssignmentEmailThrottle` static state untested (finding #7).
- **XSS regression** — no integration test asserting encoded output for `<script>` payload (Info).
- **CI parity** — 253 unit + 17 integration green with Docker; integration tests skip silently without Docker — document in `s8-03`.

## Recommended follow-up tasks

1. **`s6-docs`:** Mark Sprint 6 done in SPRINT/queue/README; sync test counts; resolve docs drift (finding #21).
2. **`s7-02`:** Migrate email + token cleanup to Hangfire with retry/dead-letter (findings #3, #18); split SMTP/queue DI (finding #17); extract email templates (finding #16).
3. **`s8-02`:** Rate limiting on authenticated write endpoints (finding #1).
4. **`s8-01`:** Production TLS/SMTP enforcement (finding #13); Redis AUTH; no public SQL/Redis ports.
5. **`s8-03`:** CI Docker parity; integration job always runs with TestContainers.
6. **Backlog — integration tests:** Multi-user authz matrix for comments/tags/assign (finding #5); remove-member assignee clear (finding #6); assignment email enqueue (finding #8).
7. **Backlog — low:** FluentValidation whitespace (finding #10); `RemoveTagFromCard` not-found key (finding #11); log redaction (finding #12); SignalR for comment/assignee updates (finding #14); `AssigneeId` FK when migration approved (finding #2).
8. **Backlog — email queue:** Non-blocking `TryWrite`/`DropWrite` or Hangfire durable queue (finding #4).

## Sign-off

- **Bug hunter:** All **High** and in-scope **Medium** findings from `sprint-6-report.md` verified **fixed** with code and test evidence. **253 unit + 17 integration tests green.** No new Critical/High regressions. Residual Medium items are deferred (`s8-02`, `s7-02`, FK) or test-coverage gaps that do not block publish.
- **Security:** All **High** findings closed. RBAC, anti-enumeration, injection posture, credential hygiene, and post-commit resilience intact. XSS and comment BOLA remediated. Remaining Medium risk is explicitly deferred write rate limits, email reliability, and optional FK — acceptable for dev; must land before production (`s8-02`, `s7-02`).
- **Architecture:** Clean Architecture boundaries preserved; council fixes match conventions without scope creep. Thin controllers, CQRS layout, port placement, and `EventHandlers/` convention maintained. Deferred schema FK and Sprint 7/8 hardening tracked in queue.

**Council decision: Sprint 6 approved for GitHub publish.** No blockers remain on High/Medium remediation; deferred items are queued with task IDs.
