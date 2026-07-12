# Live Council Report — Sprint 8

**Date:** 2026-07-12  
**Task:** s8-council-verify — Live Council — Sprint 8 / MVP remediation verification

## Executive summary

Sprint 8 remediation (`s8-council-fixes`) successfully closed all **actionable High and Medium** findings from the initial `sprint-8-report.md`. Production migration fail-fast, SMTP misconfiguration fail-fast with Hangfire retry, post-commit eviction resilience, removal of unreadable `MemberInvited` activity rows, hub join throttling, Production registration gate, Hangfire global locks, HSTS/HTTPS gating, CI prod-compose validation, and the three integration gaps (activity log E2E, RemoveMember assignee cleanup, write-limit 429) are verified fixed with code evidence and expanded tests. **`dotnet test` is green: 277 unit + 21 integration (0 skipped with Docker available).** No new Critical or High regressions were introduced.

The **Critical** multi-replica SignalR eviction gap remains **explicitly deferred** with single-replica documentation — a real-time IDOR risk only if operators scale API beyond one replica. Residual High items (query-string hub token, SQL `sa` superuser, forwarded-header rate-limit weakness, live deploy URL, transitive vulnerable packages) and Medium deferrals (read rate limits, `AssigneeId` FK, activity eventual consistency) are documented and acceptable for **single-replica portfolio/MVP publish**. **Council signs off MVP for GitHub publish.**

## Findings (prioritized)

| # | Sev | Category | Location | Issue | Action |
|---|-----|----------|----------|-------|--------|
| 1 | Critical | Security / Bug | `src/FlowBoard.API/Services/BoardGroupMembershipRegistry.cs:8`, `src/FlowBoard.API/Services/BoardRealtimeGroupEvictor.cs:12-24`, `docker-compose.prod.yml:5-7` | **Deferred #1 — not fixed.** Registry and eviction remain process-local while prod enables Redis backplane. Removed/downgraded users on another replica keep receiving `CardMoved` / `CommentAdded`. Docs warn single-replica (`Program.cs:137-138`) but do not enforce it. | Implement distributed eviction (Redis registry or cross-node fan-out) before multi-replica deploy; add multi-instance smoke test. Until then, enforce `replicas: 1` in prod orchestration. |
| 2 | High | Security | `src/FlowBoard.API/Configuration/JwtBearerSignalRExtensions.cs:21-25` | **Deferred #2.** SignalR JWT still read from `?access_token=` query string — token leakage via proxy logs, Referer, browser history (CWE-598). | Move to header/subprotocol auth or short-lived hub tokens; redact query strings in prod proxy logs. |
| 3 | High | Deploy | `docker-compose.prod.yml:58` | **Deferred #3.** Production API still connects as SQL `sa` superuser. | Least-privilege SQL login for runtime; restrict `sa` to break-glass migrations. |
| 4 | High | Security / Deploy | `src/FlowBoard.API/Program.cs:64-72`, `src/FlowBoard.API/Configuration/RateLimitPartitionKeys.cs:16-17` | **Deferred #6.** Auth limit partitions by `RemoteIpAddress`; prod compose exposes `ForwardedHeaders__Enabled` but does not template `KnownProxies`. Misconfigured ingress can collapse clients into one bucket or wrong IPs. | Document and template KnownProxies per Railway/Azure; verify `RemoteIpAddress` in staging. |
| 5 | High | Dependencies | `src/FlowBoard.API/FlowBoard.API.csproj:16`, `src/FlowBoard.Infrastructure/FlowBoard.Infrastructure.csproj:11-12` | Transitive **High** severity advisories: `Microsoft.OpenApi` 2.0.0 (via OpenAPI/Scalar — dev-facing), `Newtonsoft.Json` 11.0.1 (via Hangfire). No confirmed exploit path; supply-chain risk remains. | Track and upgrade when patched versions available; add `dotnet list package --vulnerable` to CI. |
| 6 | High | Arch / Deploy | `README.md:20-24` | **Deferred #8.** Live API section still uses placeholder URLs; no verified production endpoint. | Deploy and replace placeholders after `/health/ready` passes. |
| 7 | Medium | Security / Scale-out | `src/FlowBoard.API/Configuration/WriteRateLimitingConvention.cs:13-42`, `src/FlowBoard.API/Services/BoardHubJoinRateLimiter.cs:8-9` | Write (60/min) and hub join (30/min) limiters are **in-memory per process**. Ineffective globally if API is scaled despite docs. | Use distributed rate limiting (Redis) before multi-replica deploy; keep single-replica enforcement until then. |
| 8 | Medium | Security | `src/FlowBoard.Infrastructure/Persistence/Repositories/BoardReadService.cs:13-24` | **Deferred #18.** Write limits apply only to HTTP mutations; `GetBoard` and other GETs are unbounded expensive reads (OWASP API4). | Add per-user read rate limits or pagination on aggregate reads. |
| 9 | Medium | Deploy | `src/FlowBoard.API/appsettings.json:38`, `src/FlowBoard.API/Program.cs:19` | **`AllowedHosts: "*"`** accepts any Host header — increases host-header attack surface if DNS/proxy routing is misconfigured. | Set explicit `AllowedHosts__0` in production env; document in deploy checklist. |
| 10 | Medium | Deploy | `docker-compose.prod.yml:58`, `SECURITY.md:18` | SQL connection uses **`TrustServerCertificate=True`**. Acceptable on isolated Docker network; risky on managed cloud SQL. | Use CA-trusted certs and `TrustServerCertificate=False` on Azure SQL; keep True only for internal compose network. |
| 11 | Medium | Data integrity | `src/FlowBoard.Infrastructure/Persistence/Configurations/CardConfiguration.cs:35` | **Deferred #22.** `AssigneeId` has no FK to `users`; orphaned assignee IDs possible outside `RemoveMember` path. Runtime cleanup works (`CardRepository.cs:36-50`; integration test at `ActivityLogWorkflowTests.cs:88-108`). | Add FK migration when schema changes are approved. |
| 12 | Medium | Bug | `src/FlowBoard.Application/EventHandlers/ActivityLogEventHandler.cs:48-49`, `src/FlowBoard.Infrastructure/Persistence/UnitOfWork.cs:35-45` | **Deferred #11.** Activity log uses a second `SaveChangesAsync` outside the triggering transaction. Failure after main commit yields committed card state without matching activity row (error swallowed). | Accept with monitoring, or co-locate activity insert in the original transaction. |
| 13 | Medium | Auth | `src/FlowBoard.Application/Features/Auth/Commands/Register/RegisterCommandHandler.cs:27-33` | When registration is **enabled** (Development default), register returns **409** hinting email existence vs login generic **401** — asymmetric enumeration (OWASP API6). **Mitigated in Production** by `AllowRegistration=false`. | Keep registration disabled in all prod envs; unify error messages if open registration is ever enabled. |
| 14 | Medium | Background jobs | `src/FlowBoard.Infrastructure/Hangfire/HangfireServiceExtensions.cs:27-34`, `src/FlowBoard.API/Program.cs:192-193` | Hangfire server runs on **every API replica**. Multiple replicas can duplicate recurring jobs (idempotent today, unsafe for future jobs). | Run a single Hangfire worker role or use distributed locks when scaling. |
| 15 | Medium | Bug / RBAC | `src/FlowBoard.Application/Features/BoardLists/Commands/MoveBoardList/MoveBoardListCommandHandler.cs:61-65` | Inaccessible neighbour list throws `DomainException` → **400**; `MoveCardCommandHandler.cs:86-90` throws `NotFoundException` → **404** for same class of error. Pre-existing inconsistency. | Align `MoveBoardList` neighbour resolution with `MoveCard` — return `NotFoundException` for missing/cross-board neighbours. |
| 16 | Medium | Arch | `src/FlowBoard.Application/Common/Interfaces/IBoardRealtimeGroupEvictor.cs:6-8`, `src/FlowBoard.API/Services/BoardRealtimeGroupEvictor.cs:7-10` | Application handlers depend on a SignalR delivery port whose **implementation lives in API**. Pre-dates Sprint 8; acceptable for MVP. | Post-MVP: move evictor/notifier behind Infrastructure adapter or invert via domain events only. |
| 17 | Medium | Tests | `tests/FlowBoard.UnitTests/Hubs/BoardHubTests.cs:13-63` | `BoardHubJoinRateLimiter` shipped but hub tests do not assert 31st `JoinBoard` throws `HubException` (limiter unit-tested only). | Add `JoinBoard_ExceedsRateLimit_ThrowsHubException` to `BoardHubTests`. |
| 18 | Medium | Tests | `tests/FlowBoard.UnitTests/Handlers/Workspaces/ChangeMemberRoleCommandHandlerTests.cs:46-65` | `RemoveMember` has eviction-failure test; `ChangeMemberRole` downgrade-to-Viewer path lacks equivalent catch/log test. | Mirror `Handle_EvictionFailsAfterCommit_ReturnsSuccess` for Viewer downgrade. |
| 19 | Low | Memory | `src/FlowBoard.API/Services/BoardHubJoinRateLimiter.cs:9`, `src/FlowBoard.Application/EventHandlers/AssignmentEmailThrottle.cs:10-21` | In-memory dictionaries never prune idle keys; slow memory growth under high churn. | Periodic prune or sliding-window cleanup. |
| 20 | Low | Validation | `src/FlowBoard.Application/Features/BoardLists/Commands/MoveBoardList/` | `MoveBoardListCommand` has no FluentValidation rules (unlike `MoveCardCommandValidator`). Empty-GUID neighbours not rejected at pipeline layer. | Add validator mirroring `MoveCard` empty-GUID rules. |
| 21 | Low | Auth | `src/FlowBoard.Application/Features/Auth/Commands/Register/RegisterCommandValidator.cs:19-22` | Password policy is minimum 8 characters only — no complexity or breach list (ASVS 2.1.7). | Add complexity rules before public SaaS launch. |
| 22 | Low | Reconnaissance | `src/FlowBoard.API/Program.cs:195-203` | `/health/ready` is unauthenticated and reports DB + Redis readiness. | Restrict ready probe to internal network or return minimal public body. |
| 23 | Low | XSS | `src/FlowBoard.API/Services/BoardRealtimeNotifier.cs:31` | Comment bodies HTML-encoded on SignalR broadcast but returned **raw in REST** DTOs. Stored XSS risk shifted to frontend. | Document client escaping requirement; optionally encode on read. |
| 24 | Info | Tests | `tests/FlowBoard.IntegrationTests/` | Auth rate limit (5/min/IP) has no HTTP integration test. | Add integration test asserting 429 on 6th auth request from same IP. |
| 25 | Info | Docs | `HANDOFF.md:17` | Session handoff records **274 unit + 17 integration** from pre-fixes run; live suite is **277 + 21**. | Sync HANDOFF on next docs task. |

### Remediation verification (original sprint-8-report High/Medium)

| Original # | Sev | Status | Evidence |
|------------|-----|--------|----------|
| 1 | Critical | **Deferred** | `SECURITY.md:25`, `docker-compose.prod.yml:5-7`, `Program.cs:137-138` |
| 2 | High | **Deferred** | `JwtBearerSignalRExtensions.cs:21-25` |
| 3 | High | **Deferred** | `docker-compose.prod.yml:58` |
| 4 | High | **Fixed** | `Program.cs:160-168` — Production startup throws on pending EF migrations |
| 5 | High | **Fixed** | `SmtpEmailService.cs:23-33`, `SendEmailJob.cs:21-24` — `SmtpNotConfiguredException`; Hangfire retries |
| 6 | High | **Deferred** | `Program.cs:64-72`, `.env.example:34-37` — ForwardedHeaders not templated |
| 7 | High | **Fixed** | `ActivityLogWorkflowTests.cs:53-108` — create/move → DB → `GetCardActivity`; assign → RemoveMember → null assignee |
| 8 | High | **Deferred** | `README.md:20-24` — placeholder URLs only |
| 9 | Medium | **Fixed** | `RemoveMemberCommandHandler.cs:38-48`, `ChangeMemberRoleCommandHandler.cs:36-46` — catch/log eviction |
| 10 | Medium | **Fixed** | `ActivityLogEventHandler.cs:38-43` — `MemberInvitedEvent` no longer persisted to unreadable rows |
| 11 | Medium | **Deferred** | `ActivityLogEventHandler.cs:48-49` — second `SaveChangesAsync` |
| 12 | Medium | **Fixed** | `WriteRateLimitApiTests.cs:61-84` — 61st POST returns 429 + Problem Details |
| 13 | Medium | **Fixed** | `BoardHubJoinRateLimiter.cs:8-27`, `BoardHub.cs:27-28` — 30/min per user |
| 14 | Medium | **Fixed** | `HangfireServiceExtensions.cs:33` — `DisableGlobalLocks = false` |
| 15 | Medium | **Fixed** | `.github/workflows/ci.yml:72-78` — `docker compose -f docker-compose.prod.yml config` |
| 16 | Medium | **Deferred** | `docker-compose.prod.yml:58` — `TrustServerCertificate=True` documented in `SECURITY.md` |
| 17 | Medium | **Fixed (prod)** | `appsettings.Production.json:2-4`, `RegisterCommandHandler.cs:27-28` — `AllowRegistration=false` |
| 18 | Medium | **Deferred** | No read limiter added |
| 21 | Medium | **Fixed** | `Program.cs:146-147`, `170-171`, `183-184` — HSTS/HTTPS gated on TLS/forwarded headers |
| 22 | Medium | **Deferred** | `CardConfiguration.cs:35` — no FK migration |

## Security posture

**Full-project baseline (Sprints 1–8) — not limited to Sprint 8 delta:**

| Area | Assessment |
|------|------------|
| **JWT access tokens** | HMAC-SHA256; issuer, audience, signing key, lifetime validation; 15-min TTL; 30s clock skew — **strong**. |
| **Refresh tokens** | SHA-256 hash at rest; rotation + family revocation on reuse; transactional `UPDLOCK`; concurrent integration test — **strong**. |
| **Passwords** | BCrypt wf 12; constant-time login dummy hash — **strong**; policy is min 8 chars only (finding #21). |
| **Workspace RBAC (REST)** | Non-members get **404**; Viewer writes **403** — consistently enforced via `ResourceGuard` / `WorkspaceAccess` on all mutation handlers. |
| **Anti-enumeration** | Workspace/board/card 404 for outsiders preserved; registration 409 hints email existence when enabled — **mitigated in Production** (finding #13). |
| **SignalR** | `[Authorize]` hub; join checks membership via MediatR; group eviction on removal/Viewer downgrade — **good on single instance**; **Critical gap** on multi-instance (finding #1); query-string token transport (finding #2). |
| **Stored XSS** | Comment bodies HTML-encoded on SignalR broadcast; raw in REST DTOs — **mitigated** for SignalR consumers; client must escape REST (finding #23). |
| **Rate limiting** | Auth 5/min/IP; writes 60/min/user; hub join 30/min/user — **improved**; proxy/forwarded-header config gap (finding #4); unlimited GET reads (finding #8); in-memory limits per-process (finding #7). |
| **SQL injection** | EF LINQ + parameterized Dapper; `FromSqlInterpolated` for refresh lock — **no vectors found**. |
| **Secrets** | No committed `.env` / secrets; `.gitignore` and `SECURITY.md` hygiene intact; prod compose uses env vars — **compliant**. |
| **Production hardening** | Internal SQL/Redis, Redis AUTH, mandatory CORS, write rate limits, security headers, HSTS gated on TLS, non-root Docker user, migration fail-fast, registration gate — **meaningful Sprint 8 gains**; SQL `sa`, `TrustServerCertificate`, `AllowedHosts: *`, and missing live URL remain gaps. |
| **Background jobs** | Hangfire dashboard requires JWT + email allowlist — **good**; SMTP fail-fast fixed; `DisableGlobalLocks` re-enabled; scale-out duplicate-job risk when multi-replica (finding #14). |
| **Post-commit resilience** | Domain events, activity log, assignment email, and SignalR eviction generally catch/log failures — committed mutations do not 500. |

**OWASP API Top 10 (2023):**

| Risk | Status |
|------|--------|
| API1 Broken Object Level Authorization | **Strong** on workspace boundary; comment author checks hold from Sprint 6 |
| API2 Broken Authentication | **Strong** JWT/refresh; **gaps**: SignalR query-token (finding #2), registration enumeration when enabled (finding #13) |
| API3 Broken Object Property Level Authorization | **Good** — explicit DTOs |
| API4 Unrestricted Resource Consumption | **Partial** — auth + write + hub join limits; unlimited GET/GetBoard (finding #8); per-process limits (finding #7) |
| API5 Broken Function Level Authorization | **Good** — Viewer 403 on writes; Hangfire admin email allowlist |
| API6 Unrestricted Access to Sensitive Business Flows | **Mitigated** — assignment email throttle; registration abuse mitigated in Production |
| API7 Server Side Request Forgery | **N/A** — no outbound URL fetch |
| API8 Security Misconfiguration | **Improved** prod compose; residuals: `sa` user, `TrustServerCertificate`, `AllowedHosts: *`, forwarded headers (findings #3, #4, #9, #10) |
| API9 Improper Inventory Management | OpenAPI/Scalar dev-only — acceptable |
| API10 Unsafe Consumption of APIs | **N/A** |

**ASVS L2 highlights:** V2 auth strong with registration/password policy gaps; V4 access control strong at workspace level; V5 validation good on mutations; V7 logging — email recipient PII at Information (carryover); V9 communication — SMTP TLS optional; V13 API — rate limits on auth/writes/hub joins but not reads.

## Test & quality gaps

- **Hub join rate limit:** Unit-tested in `BoardHubJoinRateLimiterTests` but not asserted in `BoardHubTests` (finding #17).
- **ChangeMemberRole eviction failure:** `RemoveMember` has catch/log test; Viewer downgrade path lacks mirror test (finding #18).
- **Auth rate limit 429:** No HTTP integration test for 6th auth request from same IP (finding #24).
- **SignalR hub paths:** Unit tests with stub evictor; no integration for negotiate, join denial, or post-removal eviction.
- **Multi-instance real-time:** No test for cross-replica stale-group behavior (finding #1).
- **MoveBoardList semantics:** Pre-existing 400 vs 404 inconsistency vs `MoveCard` (finding #15).
- **Local Docker skip:** Integration tests skip when Docker unavailable — CI mitigates via `docker info`; 21 passed, 0 skipped in this verification run.

## Recommended follow-up tasks

1. **Distributed SignalR eviction** — Redis-backed membership registry or cross-node fan-out; multi-instance smoke test; enforce single replica until done.
2. **SignalR auth hardening** — Move off `?access_token=` query string or issue short-lived hub tokens; redact query strings in prod proxy logs.
3. **Least-privilege SQL login** — Replace `sa` in production connection string; document in deploy runbook.
4. **ForwardedHeaders templates** — KnownProxies/KnownNetworks for Railway/Azure; staging verification of `RemoteIpAddress`.
5. **Deploy live API** — Railway or Azure Container Apps; replace README placeholders with verified URL.
6. **Read rate limits / pagination** — Protect expensive `GetBoard` aggregate reads.
7. **`AssigneeId` FK migration** — When schema changes approved.
8. **Dependency audit CI** — `dotnet list package --vulnerable` in CI; upgrade `Microsoft.OpenApi` / `Newtonsoft.Json` when patched.
9. **Production env hardening** — Explicit `AllowedHosts`; `TrustServerCertificate=False` on managed SQL.
10. **MoveBoardList alignment** — Return `NotFoundException` for inaccessible neighbours (match `MoveCard`).
11. **Test backlog** — Hub join 429 in `BoardHubTests`; ChangeMemberRole eviction failure test; auth rate-limit integration test.
12. **Hangfire scale-out** — Single worker role or distributed locks when scaling beyond one replica.

## Sign-off

- **Bug hunter:** All actionable **High** and **Medium** findings from `sprint-8-report.md` verified **fixed** or **deferred with documented reason**. **277 unit + 21 integration tests green.** Critical #1 (distributed eviction) remains deferred with single-replica docs — acceptable for MVP publish. Residual Medium items are pre-existing semantics (`MoveBoardList`), test coverage gaps, and deferred scale-out/deploy items that do not block single-replica GitHub publish.
- **Security:** Solid MVP baseline — JWT/refresh, RBAC 404/403, parameterized queries, prod CORS/Redis AUTH, write limits, hub join throttle, registration gate, migration fail-fast, SMTP fail-fast, no committed secrets. Residuals concentrate on deploy posture (`sa`, `TrustServerCertificate`, `AllowedHosts`, proxy config), SignalR token transport, transitive vulnerable packages, and multi-instance real-time IDOR (deferred). Sprint 6 XSS/BOLA remediations hold.
- **Architecture:** Clean Architecture boundaries intact; Sprint 8 deliverables and council fixes match scope without creep. Dev/prod compose separation explicit. Controllers remain thin MediatR facades. Gaps are operational completeness (live URL, prod env templates) and deferred layering/scale-out items tracked above.

**Council decision: MVP approved for GitHub publish.** No blockers remain on High/Medium remediation for single-replica deployment; Critical #1 and deferred High items are documented with operational guardrails (`SECURITY.md`, prod compose comments). Agent-runner may push full MVP work to GitHub.
