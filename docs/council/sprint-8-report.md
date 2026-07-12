# Live Council Report — Sprint 8

**Date:** 2026-07-12  
**Task:** s8-council — Live Council — Sprint 8 / MVP review (production + security + deploy)

## Executive summary

Sprint 8 delivers meaningful production hardening: internalized SQL/Redis in prod compose, Redis AUTH, mandatory CORS origins, write rate limits on authenticated mutations, split CI jobs, and deploy documentation. The test suite is green (274 unit + 17 integration). No committed secrets were found; JWT/refresh rotation, workspace RBAC (404/403), parameterized queries, and comment XSS encoding remain strong from prior sprints.

The **Critical** risk is **horizontal scale-out**: `BoardGroupMembershipRegistry` is process-local while prod compose enables a Redis SignalR backplane — removed or downgraded members on another API replica can keep receiving real-time events. Secondary **High** risks cluster around **production operations** (manual migrations, SQL `sa` account, SMTP silent drops, proxy/forwarded-header misconfiguration weakening auth rate limits), **SignalR token transport** (`?access_token=` in query strings), and **MVP completeness** (no verified live deployment URL). No blockers prevent local MVP use on a single replica; remediation is queued in `s8-council-fixes` before GitHub publish.

## Findings (prioritized)

| # | Sev | Category | Location | Issue | Action |
|---|-----|----------|----------|-------|--------|
| 1 | Critical | Security / Bug | `src/FlowBoard.API/Services/BoardGroupMembershipRegistry.cs:8`, `src/FlowBoard.API/Services/BoardRealtimeGroupEvictor.cs:12-24`, `docker-compose.prod.yml:55` | `BoardGroupMembershipRegistry` is **process-local**, but prod compose enables a **Redis SignalR backplane**. `EvictUserFromBoardGroupsAsync` only removes connections tracked on the **current** instance. After `RemoveMember` or Viewer downgrade, a user connected to another replica keeps receiving `CardMoved` / `CommentAdded` — the close-02 fix does not hold under horizontal scale. | Share eviction across instances (Redis-backed registry, cross-node fan-out, or `IUserIdProvider` + server broadcast). Add multi-instance integration/smoke test. Until fixed, document single-replica requirement. |
| 2 | High | Security | `src/FlowBoard.API/Configuration/JwtBearerSignalRExtensions.cs:21`, `src/FlowBoard.API/Program.cs:167` | SignalR JWT is read from `?access_token=` query string. Tokens appear in **proxy/access logs**, browser history, and Referer headers (OWASP API2 / CWE-598). | Prefer WebSocket subprotocol auth, negotiate `Authorization` header, or short-lived hub-specific tokens. Ensure production proxies redact query strings from logs. |
| 3 | High | Deploy | `docker-compose.prod.yml:54` | Production API connects as SQL **`sa`** superuser. Compromise of API container or connection string grants full database control. | Create least-privilege SQL login (DDL via migrations only); restrict `sa` to break-glass admin. Document in deploy runbook. |
| 4 | High | Bug / Deploy | `docker-compose.prod.yml:5-7`, `src/FlowBoard.API/Program.cs:138-146` | EF migrations **auto-apply only in Development**. Production startup does not verify schema version. A missed manual `dotnet ef database update` yields runtime SQL errors on first traffic. | Add startup schema-version check (fail fast) or CI/deploy gate on migration success; consider a one-shot migrate init container in prod compose. |
| 5 | High | Bug | `src/FlowBoard.Infrastructure/Services/SmtpEmailService.cs:23-29`, `src/FlowBoard.Infrastructure/Hangfire/Jobs/SendEmailJob.cs:17-19` | When `Smtp:Host` (or `FromEmail`) is empty, `SmtpEmailService` logs a warning and **returns without throwing**. Hangfire marks the job **succeeded** — no retry, no dead-letter. Assignment emails are silently dropped when SMTP env vars are omitted. | Throw a dedicated exception on misconfiguration so Hangfire retries/alerts; document required `Smtp__*` vars in prod compose/README; add test for enqueue + failure path. |
| 6 | High | Security / Deploy | `src/FlowBoard.API/Program.cs:63-71`, `src/FlowBoard.API/Configuration/RateLimitPartitionKeys.cs:16`, `README.md:152` | Auth rate limit partitions by `RemoteIpAddress` (5/min). Deploy docs enable `ForwardedHeaders__Enabled` but do not require `ForwardedHeaders:KnownProxies`. Behind Railway/Azure ingress without trusted proxy config, clients may share one IP bucket (weak brute-force protection) or limits may not reflect real client IPs. | Document and template `ForwardedHeaders__KnownProxies__*` per platform; verify `RemoteIpAddress` in staging; consider secondary limiter keyed by normalized email on login. |
| 7 | High | Tests | `tests/FlowBoard.IntegrationTests/` (no `GetCardActivity` / `ActivityLog` / `RemoveMember` assignee tests) | Sprint 7 **activity log** has unit tests only. No integration test proves create/move → DB row → `GetCardActivity` Dapper read. `RemoveMember` assignee cleanup verified only via mocked `ICardRepository`. | Add `ActivityLogWorkflowTests` and integration test: assign card → remove member → assert `GetBoard` / `GetCardById` assignee is null. |
| 8 | High | Arch | `README.md:20-24` | **Live API** section lists placeholder base URLs only; Sprint 8 `s8-04` CV goal implies a real deployed endpoint. | Deploy to Railway or Azure Container Apps, verify `/health/ready`, replace placeholders with actual production base URL. |
| 9 | Medium | Bug / Security | `src/FlowBoard.Application/Features/Workspaces/Commands/RemoveMember/RemoveMemberCommandHandler.cs:34-35`, `src/FlowBoard.Application/Features/Workspaces/Commands/ChangeMemberRole/ChangeMemberRoleCommandHandler.cs:30-33` | Group eviction runs **after** `SaveChangesAsync` **without** catch/log (unlike `UnitOfWork` post-commit domain events). SignalR failure returns **HTTP 500** after membership change is committed; user may stay in stale groups on the local instance. | Mirror close-03 pattern: catch/log eviction failures; return 200 for committed workspace mutations. |
| 10 | Medium | Bug | `src/FlowBoard.Application/EventHandlers/ActivityLogEventHandler.cs:42-43`, `src/FlowBoard.Domain/Entities/ActivityLog.cs:87-96`, `src/FlowBoard.Infrastructure/Persistence/Repositories/ActivityLogReadService.cs:15-16` | `MemberInvitedEvent` persists `ActivityLog` rows with **`CardId = null`**, but the only read API (`GetCardActivity`) filters `WHERE [CardId] = @CardId`. Invite audit entries are **written but never readable** via API. | Add workspace-level activity query or stop persisting card-scoped-incompatible events; align handler scope with API surface. |
| 11 | Medium | Bug | `src/FlowBoard.Application/EventHandlers/ActivityLogEventHandler.cs:50-51`, `src/FlowBoard.Infrastructure/Persistence/UnitOfWork.cs:35-45` | Activity log uses a **second** `SaveChangesAsync` in the event handler, outside the original transaction. Failure after main commit yields **committed card state without a matching activity row** (swallowed error). | Accept as eventual consistency with monitoring, or write activity in the same transaction as the triggering mutation. |
| 12 | Medium | Security / Tests | `src/FlowBoard.API/Configuration/WriteRateLimitingConvention.cs:13`, `tests/FlowBoard.UnitTests/Configuration/WriteRateLimitingConventionTests.cs` | Write limits (60/min/user) are convention-tested at MVC model level only. **No WebApplication/integration test** asserts `429` + Problem Details on the 61st mutation. | Add minimal API test hitting an authenticated POST endpoint 61 times. |
| 13 | Medium | Security | `src/FlowBoard.API/Hubs/BoardHub.cs:23-28`, `src/FlowBoard.API/Program.cs:51-82` | `JoinBoard` is authenticated but **not rate-limited**. A valid member can spam joins across many boards, stressing DB access checks (`EnsureBoardAccessQuery`) and group bookkeeping. | Add per-user hub rate limiting or connection-level join throttle. |
| 14 | Medium | Bug / Arch | `src/FlowBoard.Infrastructure/Hangfire/HangfireServiceExtensions.cs:27-34`, `src/FlowBoard.API/Program.cs:168-169` | `DisableGlobalLocks = true` with **Hangfire server on every API replica** can allow duplicate execution of recurring `CleanupExpiredRefreshTokensJob` under load (mostly idempotent today, but unsafe for future jobs). | Re-enable locks for recurring jobs, run a single Hangfire worker role, or document single-job-runner deployment. |
| 15 | Medium | Deploy / Tests | `.github/workflows/ci.yml:48-70` | CI runs unit + integration tests but **never builds/validates `docker-compose.prod.yml`** or runs a prod-config smoke test (CORS guard, Redis AUTH, health endpoints). | Add `docker compose -f docker-compose.prod.yml config` + optional prod image build/smoke job. |
| 16 | Medium | Security / Deploy | `docker-compose.prod.yml:54`, `src/FlowBoard.API/secrets.example.json:3` | `TrustServerCertificate=True` disables SQL Server certificate validation. Acceptable on isolated Docker network; risky on managed cloud SQL or untrusted networks (MITM). | Use `Encrypt=True` with valid server cert; set `TrustServerCertificate=False` when CA-trusted. |
| 17 | Medium | Security | `src/FlowBoard.Application/Features/Auth/Commands/Register/RegisterCommandHandler.cs:24-25`, `src/FlowBoard.API/Controllers/AuthController.cs:19` | **Open self-registration** with no email verification, CAPTCHA, or admin approval. Registration 409 message hints email existence, unlike login generic 401. Enables bulk fake accounts and resource consumption. | Gate registration behind invite/admin flag for production; unify register/login error messages; add email verification and bot protection. |
| 18 | Medium | Security | `src/FlowBoard.Infrastructure/Persistence/Repositories/BoardReadService.cs:13` | Write rate limit (60/min/user) applies only to POST/PUT/PATCH/DELETE. **GET endpoints are unlimited**, including `GetBoard` which loads all lists/cards in one Dapper round-trip — expensive unbounded reads (OWASP API4). | Add per-user read rate limits or pagination; cap board size; consider caching. |
| 19 | Medium | Deploy | `docker-compose.prod.yml:52-60`, `.env.example:1-32`, `src/FlowBoard.API/appsettings.json:24` | Prod compose sets JWT/CORS/SQL/Redis but omits `Hangfire__DashboardAdminEmails__*` and `Smtp__*` variables. Default empty admin array denies `/jobs` access (secure) but encourages misconfiguration; SMTP omission causes silent email loss (finding #5). | Extend `.env.example` and prod compose with commented Hangfire/SMTP keys and operational checklist in README. |
| 20 | Medium | Security | `src/FlowBoard.API/appsettings.json:35` | `AllowedHosts: "*"` accepts any Host header — host-header attack surface if DNS/reverse-proxy misconfigured. | Set explicit host allowlist in production (`AllowedHosts__0`). |
| 21 | Medium | Arch / Deploy | `src/FlowBoard.API/Program.cs:147-161`, `docker-compose.prod.yml:60-62` | Production pipeline always enables `UseHsts()` and `UseHttpsRedirection()`, while prod compose/Dockerfile expose **HTTP-only** on port 8080 with `ForwardedHeaders__Enabled` defaulting to `false`. Browser clients on plain HTTP may receive inconsistent HSTS/redirect behavior. | Gate HSTS/HTTPS redirection on TLS being configured or `ForwardedHeaders:Enabled=true`; document TLS-terminating reverse proxy requirement. |
| 22 | Medium | Arch | `src/FlowBoard.Infrastructure/Persistence/Configurations/CardConfiguration.cs:35`, `SPRINT.md:65` | `AssigneeId` still has **no FK** to `users` (deferred from Sprint 6). `RemoveMember` clears assignees, but direct DB edits or future code paths could leave orphan assignee IDs visible in Dapper reads. | Add FK migration when approved (`OnDelete(SetNull)`), or defensive read-side nulling for non-members. |
| 23 | Low | Bug | `src/FlowBoard.Application/EventHandlers/AssignmentEmailThrottle.cs:10-21` | Static `ConcurrentDictionary` records throttle keys but **never prunes** expired entries. Long-lived processes accumulate entries for every card/assignee pair ever throttled. | Periodic cleanup of entries older than the 5-minute window, or use a bounded cache. |
| 24 | Low | Security / CI | `dotnet test` (NU1903), `.github/workflows/ci.yml:40-46` | Transitive **NU1903** advisories on `Newtonsoft.Json` 11.0.1 (Hangfire) and `Microsoft.OpenApi` 2.0.0. Coverlet artifact uploads with `if-no-files-found: ignore` and **no coverage threshold**. | Track upstream upgrades or `dotnet list package --vulnerable` in CI; fail or warn when cobertura artifact is missing. |
| 25 | Low | Security | `docker-compose.yml:12`, `docker-compose.yml:27` | Dev compose exposes SQL Server (`1433`) and Redis (`6379`) on host **without Redis password**. Documented as dev-only; dangerous if deployed to internet-facing host. | Keep dev compose off public hosts; add compose profile or firewall note in README. |
| 26 | Low | Security | `docker-compose.integration.yml:10` | Default integration SQL password when env unset. Low risk (local/test, port `1434`) but predictable credential. | Require explicit `MSSQL_SA_PASSWORD` or bind to `127.0.0.1` only. |
| 27 | Low | Security | `src/FlowBoard.Application/Features/Auth/Commands/Register/RegisterCommandValidator.cs:19` | Password policy is **min 8 chars only** — no complexity, breach list, or max-age (ASVS 2.1.7). | Add complexity rules and optional HIBP/breach check for production. |
| 28 | Low | Security | `src/FlowBoard.API/Program.cs:152` | Security headers include `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` but **no Content-Security-Policy**. | Add CSP appropriate to API + Scalar (dev) / frontend origins (prod). |
| 29 | Low | Security | `src/FlowBoard.API/Program.cs:176` | `/health/ready` is unauthenticated and reports DB/Redis readiness — aids reconnaissance. | Restrict ready probe to internal network or return minimal body publicly. |
| 30 | Low | Tests | `tests/FlowBoard.IntegrationTests/NoOpBoardRealtimeGroupEvictor.cs:5`, `tests/FlowBoard.UnitTests/Hubs/BoardHubTests.cs` | Hub authorization has **unit tests only**; integration suite stubs group eviction and does not exercise live SignalR negotiate/`JoinBoard` paths. | Add integration tests for hub join denial and post-removal eviction (single-node baseline). |
| 31 | Low | Arch | `src/FlowBoard.API/Controllers/CardsController.cs:106-109`, `src/FlowBoard.API/Controllers/AuthController.cs:24-29` | Request binding inconsistent: most controllers use API-layer `*Payload` records; `AuthController` binds Application commands directly. | Standardize on one pattern in a post-MVP hygiene pass. |
| 32 | Low | Arch | `src/FlowBoard.Infrastructure/DependencyInjection.cs:58-79` | JWT authentication and Hangfire `WebApplication` registration live in Infrastructure rather than API composition root. Valid but couples auth wiring to persistence assembly. | Accept for MVP; extract API-facing composition extensions if stricter layering desired. |
| 33 | Info | Tests | `tests/FlowBoard.IntegrationTests/SqlServerFixture.cs:78-81` | Integration tests **skip** (not fail) when Docker unavailable — correct for local dev. CI mitigates via `docker info`. | Keep CI Docker preflight; document that local `Skipped: 17` means zero integration coverage. |
| 34 | Info | Arch | `src/FlowBoard.Application/EventHandlers/ActivityLogEventHandler.cs:38-45` | Activity log covers create/move/invite only — **not** assign, comment, or tag events. Matches Sprint 7 scope. | Extend activity coverage in a future sprint if full card audit trail is required. |
| 35 | Info | Security | `src/FlowBoard.API/Configuration/RateLimitPartitionKeys.cs:15-17` | Authenticated write fallback partition `"unknown"` when JWT `sub` and IP are both missing buckets all such clients together. Edge case behind misconfigured proxies. | Monitor 429 metrics; prefer always resolving client IP when behind known proxies. |
| 36 | Info | Docs | `SECURITY.md:1-18`, `docker-compose.prod.yml:47-51` | Security policy covers credential hygiene but not Sprint 8 write rate limits or prod compose separation. Prod compose **requires** healthy Redis (intentional for backplane). | Add rate-limiting and production deployment subsections during `s8-council-fixes` if security docs are in scope. |

## Security posture

**Full-project baseline (Sprints 1–8) — not limited to Sprint 8 delta:**

| Area | Assessment |
|------|------------|
| **JWT access tokens** | HMAC-SHA256; issuer, audience, signing key, lifetime validation; 15-min TTL; 30s clock skew — **strong**. |
| **Refresh tokens** | SHA-256 hash at rest; rotation + family revocation on reuse; transactional `UPDLOCK`; concurrent integration test — **strong**. |
| **Passwords** | BCrypt wf 12; constant-time login dummy hash — **strong**; policy is min 8 chars only (finding #27). |
| **Workspace RBAC (REST)** | Non-members get **404**; Viewer writes **403** — consistently enforced via `ResourceGuard` / `WorkspaceAccess` on all mutation handlers. |
| **Anti-enumeration** | Workspace/board/card 404 for outsiders preserved; registration 409 hints email existence (finding #17). |
| **SignalR** | `[Authorize]` hub; join checks membership via MediatR; group eviction on removal/Viewer downgrade — **good on single instance**; **Critical gap** on multi-instance (finding #1); query-string token transport (finding #2). |
| **Stored XSS** | Comment bodies HTML-encoded on API read and SignalR broadcast (Sprint 6 remediation) — **mitigated** for API consumers. |
| **Rate limiting** | Auth 5/min/IP; writes 60/min/user (Sprint 8) — **improved**; proxy/forwarded-header config gap (finding #6); unlimited GET reads (finding #18); hub joins unbounded (finding #13). |
| **SQL injection** | EF LINQ + parameterized Dapper; `FromSqlInterpolated` for refresh lock — **no vectors found**. |
| **Secrets** | No committed `.env` / secrets; `.gitignore` and `SECURITY.md` hygiene intact; prod compose uses env vars — **compliant**. |
| **Production hardening** | Internal SQL/Redis, Redis AUTH, mandatory CORS, write rate limits, security headers, HSTS in non-Development, non-root Docker user — **meaningful Sprint 8 gains**; SQL `sa`, `TrustServerCertificate`, manual migrations, and missing live URL remain gaps. |
| **Background jobs** | Hangfire dashboard requires JWT + email allowlist — **good**; SMTP silent success on misconfig (finding #5); `DisableGlobalLocks` scale-out risk (finding #14). |
| **Post-commit resilience** | Domain events, activity log, email queue generally swallow downstream failures — committed mutations do not 500; eviction path can still 500 (finding #9). |

**OWASP API Top 10 (2023):**

| Risk | Status |
|------|--------|
| API1 Broken Object Level Authorization | **Strong** on workspace boundary (404 outsiders); comment author checks fixed in Sprint 6 |
| API2 Broken Authentication | **Strong** JWT/refresh; **gaps**: SignalR query-token (finding #2), open registration (finding #17) |
| API3 Broken Object Property Level Authorization | **Good** — explicit DTOs |
| API4 Unrestricted Resource Consumption | **Partial** — auth + write limits; unlimited GET/GetBoard (finding #18); hub join spam (finding #13) |
| API5 Broken Function Level Authorization | **Good** — Viewer 403 on writes; Hangfire admin email allowlist |
| API6 Unrestricted Access to Sensitive Business Flows | **Mitigated** — assignment email throttle (Sprint 6); registration abuse remains (finding #17) |
| API7 Server Side Request Forgery | **N/A** — no outbound URL fetch |
| API8 Security Misconfiguration | **Improved** prod compose; residuals: `sa` user, `TrustServerCertificate`, `AllowedHosts: *`, forwarded headers (findings #3, #6, #16, #20) |
| API9 Improper Inventory Management | OpenAPI/Scalar dev-only — acceptable |
| API10 Unsafe Consumption of APIs | **N/A** |

**ASVS L2 highlights:** V2 auth strong with registration/password policy gaps; V4 access control strong at workspace level; V5 validation good on mutations; V7 logging — email recipient PII at Information (carryover); V9 communication — SMTP TLS optional; V13 API — rate limits on auth/writes but not reads/hub.

## Test & quality gaps

- **Activity log end-to-end:** Unit tests only; no integration proof of event → DB → `GetCardActivity` (finding #7).
- **RemoveMember assignee lifecycle:** Mocked unit test only; no SQL integration for assign → remove member → null assignee (finding #7).
- **Write rate limit 429:** Convention/partition-key unit tests only; no HTTP-level assertion (finding #12).
- **SignalR hub paths:** Unit tests with stub evictor; no integration for negotiate, join denial, or post-removal eviction (finding #30).
- **Multi-instance real-time:** No test for cross-replica stale-group behavior (finding #1).
- **Prod deploy validation:** CI does not build or smoke-test `docker-compose.prod.yml` (finding #15).
- **Coverage gate:** Coverlet artifact optional; no minimum threshold (finding #24).
- **Local Docker skip:** Integration tests skip when Docker unavailable — CI mitigates; local `Skipped: 17` means zero integration coverage (finding #33).

## Recommended follow-up tasks

1. **Distributed SignalR eviction** — Redis-backed membership registry or cross-node fan-out; multi-instance smoke test (`s8-council-fixes` blocker for multi-replica deploy).
2. **SignalR auth hardening** — Move off `?access_token=` query string or issue short-lived hub tokens.
3. **Production migration gate** — Schema-version check at startup or migrate init container in prod compose.
4. **SMTP fail-fast** — Throw on misconfiguration so Hangfire retries; document `Smtp__*` in prod env template.
5. **ForwardedHeaders templates** — KnownProxies/KnownNetworks for Railway/Azure; staging verification of `RemoteIpAddress`.
6. **ActivityLog integration tests** — Card create/move → DB row → `GetCardActivity` API.
7. **RemoveMember assignee integration test** — Assign → remove member → assert null assignee on `GetBoard` / `GetCardById`.
8. **Write rate limit 429 smoke test** — Authenticated POST hammer → 429 + Problem Details.
9. **Eviction error handling** — Catch/log `EvictUserFromBoardGroupsAsync` failures in RemoveMember/ChangeMemberRole (close-03 pattern).
10. **Deploy live API** — Railway or Azure Container Apps; replace README placeholders with verified URL.
11. **Least-privilege SQL login** — Replace `sa` in production connection string.
12. **Activity log API alignment** — Workspace-level activity query or stop persisting unreadable `MemberInvited` rows.
13. **Hub join rate limiting** — Per-user throttle on `JoinBoard`.
14. **Hangfire scale-out** — Single worker role or re-enable global locks for recurring jobs.
15. **CI prod compose validation** — `docker compose -f docker-compose.prod.yml config` + optional prod image build.
16. **Prod env completeness** — Hangfire admin emails, SMTP, `AllowedHosts` in `.env.example` and prod compose comments.
17. **HSTS/HTTPS gating** — Enable only when TLS or forwarded headers configured.
18. **Registration hardening** — Invite-only flag, unified error messages, optional email verification (pre-public-SaaS).
19. **Read rate limits / pagination** — Protect expensive `GetBoard` aggregate reads.
20. **`AssigneeId` FK migration** — When schema changes approved.

## Sign-off

- **Bug hunter:** Sprint 8 hardening is structurally sound and tests are green (274 unit + 17 integration). Critical gap is process-local SignalR eviction vs Redis backplane under horizontal scale. High operational risks: manual migrations, SMTP silent drops, forwarded-header rate-limit weakness. Test holes in activity log integration, write-limit 429, and RemoveMember assignee cleanup.
- **Security:** Solid MVP baseline — JWT/refresh, RBAC 404/403, parameterized queries, prod CORS/Redis AUTH, write limits, no committed secrets. Residuals concentrate on deploy posture (`sa`, `TrustServerCertificate`, proxy config), SignalR token transport, open registration, and multi-instance real-time IDOR. Sprint 6 XSS/BOLA remediations hold.
- **Architecture:** Clean Architecture boundaries intact; Sprint 8 deliverables match scope without creep. Dev/prod compose separation is explicit. Gaps are operational completeness (live URL, prod env templates, CI prod validation), deferred `AssigneeId` FK, and rate-limit/hub coverage at integration level only.
