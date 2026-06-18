# Live Council Report — Sprint 6

**Date:** 2026-06-18  
**Task:** close-council — Live Council — closeout verification (Sprints 1–5)

## Executive summary

Sprints 1–5 are **signed off** for feature work: closeout tasks `close-01` through `close-11` substantively address the High/Medium findings from `docs/council/sprint-5-report.md`, and **`dotnet test` is green** (198 unit + 8 integration, 0 skipped with Docker available). Application-layer security is strong — JWT/refresh rotation with row locking, workspace RBAC with 404 anti-enumeration, parameterized data access, and REST IDOR controls are implemented consistently. **No Critical vulnerabilities** were found. The highest residual risks are **multi-instance SignalR stale groups** (process-local eviction registry vs Redis backplane), **JWT via `?access_token=`** on WebSocket negotiate, and **dev compose misconfiguration** if deployed unchanged. Several medium gaps remain in test coverage (cross-tenant IDOR integration, read-query authz unit tests, end-to-end SignalR broadcast) and in post-commit eviction reliability; production hardening (rate limits, Redis AUTH, prod compose) is explicitly deferred to Sprint 8.

## Findings (prioritized)

| # | Sev | Category | Location | Issue | Action |
|---|-----|----------|----------|-------|--------|
| 1 | High | Security / Real-time | `BoardGroupMembershipRegistry.cs:7-81`, `BoardRealtimeGroupEvictor.cs:12-24`, `SignalRRedisExtensions.cs:15-16` | **close-02 partial:** eviction uses a **process-local** registry. With Redis backplane (now wired in compose), a removed/downgraded user connected to another API instance stays in `board:{boardId}` and keeps receiving `CardMoved` until disconnect. Single-node eviction works; scale-out IDOR gap remains. | Store membership in Redis or broadcast eviction cluster-wide; re-validate before broadcast; or document single-instance-only real-time until Sprint 8. |
| 2 | High | Security | `JwtBearerSignalRExtensions.cs:21-25` | JWT for WebSocket negotiate is read from `?access_token=` query string. Tokens may appear in proxy logs, browser history, and Referer headers (OWASP API2, ASVS 3.3). | Prefer header-based auth where client stack allows; strip query strings from access logs; keep 15-min TTL; document secure client storage. **Deferred** — industry-standard WebSocket workaround; not in close scope. |
| 3 | High | Security / Ops | `docker-compose.yml:1-3,11-12,26-27,49,54-55` | Dev stack publishes SQL (`1433`) and Redis (`6379`) to host, runs `Development` env (OpenAPI, auto-migrate, permissive CORS). Catastrophic if deployed unchanged (OWASP API8). | Production compose: internal-only data stores, `Production` env, TLS, strong secrets, explicit `AllowedOrigins`. **Deferred → Sprint 8** (`s8-01`). |
| 4 | Medium | Arch / Real-time | `EnsureBoardAccessQueryHandler.cs:27`, `ChangeMemberRoleCommandHandler.cs:32-33` | Viewer-downgraded users are evicted from hub groups, but `EnsureBoardAccessQuery` only calls `ResourceGuard.EnsureMember` (Viewers pass). A downgraded user can immediately re-`JoinBoard` and receive `CardMoved` again — policy mismatch with close-02 intent. | Align policy: block Viewers in realtime gate (`EnsureCanWrite` or dedicated query) if they must not subscribe, or remove Viewer eviction if read-only clients should receive updates. Add Viewer `JoinBoard` unit test. |
| 5 | Medium | Bug / Real-time | `RemoveMemberCommandHandler.cs:31-32`, `ChangeMemberRoleCommandHandler.cs:30-33` | SignalR eviction runs after committed DB write **without try/catch**. Infrastructure failure returns HTTP 500 after successful membership change — inverse of close-03 pattern. | Catch/log eviction failures (mirror `CardMovedEventHandler` / `UnitOfWork`); optionally retry asynchronously. |
| 6 | Medium | Bug / Real-time | `DeleteWorkspaceCommandHandler.cs:23-26` | Workspace soft-delete does not call `IBoardRealtimeGroupEvictor`. Existing connections keep receiving `CardMoved` until disconnect. | Evict all workspace members' connections on delete, or broadcast forced-leave. |
| 7 | Medium | Bug | `MoveCardCommandHandler.cs:24-37` | Conflict retry re-enters handler logic but `GetByIdAsync` uses tracked `FindAsync` without detach/reload. After unique-index or concurrency conflict, stale neighbours may cause repeated 409 on hot same-slot moves. | Detach/reload entities before retry; add integration test for two moves into same neighbour gap. |
| 8 | Medium | Ops / Arch | `docker-compose.yml:46-47` vs `SPRINT.md:103` | **close-01 partial:** Redis connection is wired (`ConnectionStrings__Redis=redis:6379`), but `api` still **hard-depends** on healthy Redis. Compose cannot start API without Redis despite docs saying Redis is optional for `dotnet run`. | Remove `redis` from `depends_on` or update docs to state compose is always a full backplane stack. |
| 9 | Medium | Security | `Program.cs:46-66` | Rate limiting (5 req/min/IP) applies only to `/api/auth`. Authenticated mutations/reads are unlimited — DoS risk (OWASP API4). | Per-user or per-IP policies on write endpoints before production. **Deferred → Sprint 8** (`s8-02`). |
| 10 | Medium | Security | `docker-compose.yml:23-27` | Redis `7-alpine` has no `requirepass`/ACL; host port `6379` exposes pub/sub. | Enable Redis AUTH/TLS; internal network only. **Deferred → Sprint 8** (`s8-01`). |
| 11 | Medium | Security | `docker-compose.yml:50`, `secrets.example.json:3` | `TrustServerCertificate=True` on SQL connection strings — dev-only; MITM if reused in production. | Encrypted SQL + proper certs in production; document dev-only flag. **Deferred → Sprint 8** (`s8-01`). |
| 12 | Medium | Security | `Program.cs:58-60`, `Program.cs:87-100` | Auth rate limit partitions by `RemoteIpAddress`. If `ForwardedHeaders:Enabled` without strict `KnownProxies`, clients can spoof `X-Forwarded-For` to bypass 5/min limit. | Enable forwarded headers only with explicit known proxies in production. **Deferred → Sprint 8** (`s8-01`). |
| 13 | Medium | Tests | `tests/FlowBoard.IntegrationTests/` | No cross-tenant REST IDOR smoke — outsider 404 on another workspace's board/card/project endpoints (sprint-5-report P2). | Add integration case: seed workspace A, act as user B, assert 404 on `GetBoard` / `GetCardById` / `MoveCard`. |
| 14 | Medium | Tests | `GetCardByIdQueryHandler.cs:17-33` (+ 4 read query handlers) | **close-07 partial:** mutation authz matrix covered (13 handlers); **5 read query handlers** still lack unit tests for 404 non-member edge cases. | Add focused authz tests mirroring existing handler patterns (at minimum `GetCardById`). |
| 15 | Medium | Tests | `BoardWorkflowTests.cs:154-173` | **close-09 partial:** integration verifies MediatR → `IBoardRealtimeNotifier`, not SignalR group delivery. Hub wiring regression would not be caught. | Optional `WebApplicationFactory` + SignalR client test, or document as accepted deferral until Sprint 6 realtime features. |
| 16 | Medium | Tests / CI | `SqlServerFixture.cs:74-79`, `.github/workflows/ci.yml` | Local runs skip integration tests when Docker unavailable; CI requires Docker. Green local runs may have zero integration coverage. | Document divergence; dedicated integration job. **Deferred → Sprint 8** (`s8-03`). |
| 17 | Medium | Bug / Auth | _(no cleanup service)_ — `tasks/queue.json` (`s6-06`) | Expired refresh token rows are not purged. Hashes accumulate; widens offline breach window (low direct exploit risk). | Implement `CleanupExpiredRefreshTokensService` per Sprint 6 plan. |
| 18 | Medium | Bug | `UnitOfWork.cs:40-45` | Domain events publish after `SaveChangesAsync` but before `CommitAsync` inside `ExecuteInTransactionAsync`. If commit fails, notifications may fire for unrolled data. Currently only refresh rotation uses transactions and issues no domain events — latent footgun. | Defer publish until after explicit commit, or document constraint. |
| 19 | Low | Bug | `BoardRealtimeNotifier.cs:12-20` | **#25 still open:** `cancellationToken` accepted but not passed to SignalR `SendAsync`. | Forward token when hub client API supports it. |
| 20 | Low | Tests | `BoardHubTests.cs` | **close-08 partial:** missing test for invalid/missing JWT subject (`BoardHub.cs:64-71` → `"Authentication is required."`). | Add hub test with empty/malformed `sub` claim. |
| 21 | Low | Security | `RegisterCommandValidator.cs:19-22` | Password policy is minimum 8 characters only — no complexity or MFA (ASVS 2.1.1 L2 gap). | Strengthen for production. **Deferred → Sprint 8.** |
| 22 | Low | Security | `appsettings.json:23` | `AllowedHosts: "*"` — weak host-header protection. | Explicit allowlist in production config. **Deferred → Sprint 8** (`s8-01`). |
| 23 | Low | Security | `Program.cs:146-149` | `/health/ready` unauthenticated — infrastructure fingerprinting. | Restrict to internal network in production. **Deferred → Sprint 8.** |
| 24 | Low | Arch | `FlowBoard.API.csproj:15,24` | `AspNetCore.HealthChecks.Redis` pinned at 9.0.0 on net10.0; redundant `StackExchange.Redis` ref. | Align health-check package to 10.x; drop unused ref. |
| 25 | Low | Security / CI | `.github/workflows/ci.yml` | No automated dependency vulnerability scan. | Add `dotnet list package --vulnerable` or Dependabot. **Deferred → Sprint 8** (`s8-03`). |

### Sprint 5 closeout verification (High/Medium items)

| Report # | Topic | Verdict |
|----------|-------|---------|
| 1 | Compose Redis not wired | **Fixed** — `ConnectionStrings__Redis: "redis:6379"` |
| 2 | Stale SignalR groups | **Partial** — single-instance eviction; scale-out gap (#1) |
| 3 | HTTP 500 after committed write | **Fixed** — catch/log in UnitOfWork + CardMovedEventHandler |
| 4 | Domain events cleared before SaveChanges | **Fixed** — clear after save; UnitOfWorkTests |
| 5 | JWT via `?access_token=` | **Deferred** — documented (#2) |
| 6 | Dev compose exposure | **Deferred → Sprint 8** (#3) |
| 7 | Compose hard-depends on Redis | **Partial** (#8) |
| 8 | BoardHub bypasses Application layer | **Fixed** — EnsureBoardAccessQuery via MediatR |
| 9–11 | MoveCard concurrency / validation / 404 | **Fixed** — unique index, retry, validator; EF retry gap (#7) |
| 12 | InviteMember enumeration | **Fixed** — workspace 404 |
| 13 | Refresh rotation race | **Fixed** — UPDLOCK + integration test |
| 14–16 | Redis AUTH, rate limits, SQL TLS | **Deferred → Sprint 8** (#9–11) |
| 17 | EventHandlers convention | **Fixed** — documented in EventHandlers/README.md |
| 18–19 | BoardHub + Redis config tests | **Mostly fixed** — partial hub/health gaps (#20) |
| 20–22 | Integration test gaps | **Mostly fixed** — notifier partial (#15); soft-deleted cards fixed |
| 21 | Handler authz matrix | **Mostly fixed** — 5 read queries open (#14) |
| 23 | CI divergence | **Deferred → Sprint 8** (#16) |

## Security posture

**Full-project baseline (Sprints 1–5) vs OWASP API Top 10 / ASVS L2:**

| Area | Assessment |
|------|------------|
| **JWT access tokens** | HMAC-SHA256; full validation; 30s clock skew; 15-min TTL — **strong** |
| **Refresh tokens** | SHA-256 hash only; family reuse detection; **`UPDLOCK` row lock** under transaction — **exemplary** |
| **Passwords** | BCrypt wf 12; timing-safe dummy hash; generic 401 — **strong**; complexity/MFA gap (#21) |
| **Workspace RBAC** | Non-members → **404** consistently; Viewer writes → 403 — **strong** |
| **REST IDOR** | `ResourceGuard` on handlers; parameterized Dapper — **strong**; integration smoke missing (#13) |
| **SignalR** | `[Authorize]` hub; join via `EnsureBoardAccessQuery`; generic errors — **good join path**; stale groups on scale-out (#1) and Viewer policy mismatch (#4) |
| **SignalR token transport** | `?access_token=` — residual log/referrer risk (#2) |
| **Redis / compose** | Backplane wired in compose; no AUTH in dev; host ports exposed — **dev only** (#3, #10) |
| **SQL injection** | EF + parameterized Dapper; no string-concat SQL — **strong** |
| **Validation** | FluentValidation via MediatR pipeline — **strong** |
| **Secrets** | `.env`/`secrets.json` gitignored; JWT key length enforced — **no violations found** |
| **Error handling** | RFC 7807; generic 500 with traceId — **strong** |
| **Headers / transport** | HSTS non-Dev; security headers; OpenAPI dev-only — **good** |
| **Rate limiting** | Auth only — **gap** (#9) |
| **Container** | Non-root `appuser` in Dockerfile — **good** |

**OWASP API Top 10 (2023):**

| Risk | Status |
|------|--------|
| API1 Broken Object Level Authorization | **Strong** REST; **gap** SignalR scale-out stale groups |
| API2 Broken Authentication | **Strong** JWT/refresh; query-token transport residual |
| API3 Broken Object Property Level Authorization | **Good** |
| API4 Unrestricted Resource Consumption | **Gap** — auth-only rate limit |
| API5 Broken Function Level Authorization | **Good** |
| API6 Unrestricted Access to Sensitive Business Flows | **Acceptable** for MVP scope |
| API7 SSRF | N/A |
| API8 Security Misconfiguration | **Risk** in dev Docker defaults |
| API9 Improper Inventory Management | OpenAPI dev-only — acceptable |
| API10 Unsafe Consumption of APIs | N/A |

## Test & quality gaps

### Covered well (closeout additions)
- Post-commit notification failure swallow (UnitOfWork + CardMovedEventHandler)
- MoveCard hardening: empty GUID, 404 semantics, concurrency index + retry
- Refresh rotation under `UPDLOCK` with concurrent integration test
- InviteMember anti-enumeration
- BoardHub join denial + EnsureBoardAccessQuery (unit)
- Redis config precedence (8 cases) + stable backplane DI assert
- Handler authz matrix on 13 mutation handlers (+26 unit tests)
- Integration: soft-deleted cards, CardMoved notifier pipeline, concurrent move smoke
- UnitOfWork event clearing after successful save

### Remaining gaps (prioritized)
| Priority | Gap |
|----------|-----|
| P1 | Multi-instance SignalR stale groups after member removal/downgrade |
| P1 | Post-commit eviction failures can 500 after successful workspace mutation |
| P2 | Cross-tenant REST IDOR integration smoke |
| P2 | 5 read query handler authz unit tests |
| P2 | End-to-end SignalR broadcast test (not just notifier capture) |
| P2 | Viewer JoinBoard policy alignment with eviction intent |
| P3 | MoveCard EF retry stale-context edge case |
| P3 | Workspace delete — no group eviction |
| P3 | UnitOfWork publish-before-commit-in-transaction latent bug |
| P3 | BoardHub GetUserId failure test; UnitOfWork publish-failure test |

**CI divergence:** Local without Docker skips integration tests; CI requires Docker (#16).

## Recommended follow-up tasks

1. **Sprint 6 early** — Catch/log SignalR eviction failures in `RemoveMember` / `ChangeMemberRole` (#5); align Viewer realtime policy (#4).
2. **Sprint 6** — `s6-06` CleanupExpiredRefreshTokensService (#17).
3. **Sprint 6** — Cross-tenant IDOR integration smoke (#13); read-query authz unit tests (#14).
4. **Sprint 6 optional** — SignalR end-to-end integration test or document accepted deferral (#15).
5. **Sprint 8** — `s8-01` Production compose + Redis AUTH + SQL TLS + AllowedHosts (#3, #10, #11, #22).
6. **Sprint 8** — `s8-02` Rate limiting on authenticated write endpoints (#9).
7. **Sprint 8** — `s8-03` CI integration job parity + dependency vulnerability scan (#16, #25).
8. **Sprint 8** — Cluster-wide SignalR group eviction (Redis membership or backplane broadcast) (#1).
9. **Sprint 8** — Reconcile compose `depends_on` Redis vs optional-backplane docs (#8).
10. **Backlog** — MoveCard EF reload on conflict retry (#7); workspace-delete group eviction (#6); JWT header transport (#2).

## Sign-off

- **Bug hunter:** Closeout tasks `close-01`…`close-11` verified against sprint-5-report. Core logic fixes (UnitOfWork ordering, post-commit 500, MoveCard hardening, refresh lock, InviteMember) are solid. **`dotnet test` green** (198 + 8). Residual logic risk: process-local SignalR eviction under scale-out, EF stale retry on MoveCard conflict, eviction 500 after committed workspace mutations, workspace delete omitting eviction. Test gaps: cross-tenant IDOR integration, 5 read-query authz tests, no WebSocket broadcast assertion.
- **Security:** No Critical findings. Application auth/RBAC/anti-enumeration is consistently strong. Residual High items are operational (dev compose) and architectural (scale-out stale groups, query-token JWT). Production hardening explicitly deferred to Sprint 8. Credential hygiene clean.
- **Architecture:** Clean Architecture boundaries intact after BoardHub refactor and compose Redis wiring. Ports/adapters correct for `IBoardRealtimeNotifier` / `IBoardRealtimeGroupEvictor`. Main arch debt: process-local registry incompatible with Redis backplane scale-out; Viewer eviction vs `EnsureBoardAccessQuery` policy mismatch; compose docs vs `depends_on` contradiction.

**Council decision:** **Sprints 1–5 signed off.** Sprint 6 feature work (`s6-01`…) may proceed. High/Medium sprint-5-report items are fixed, partially fixed with documented residual risk, or deferred to Sprint 8 with reason. Git push authorized after this report (per agent-runner policy).
