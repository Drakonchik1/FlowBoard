# Live Council Report — Sprint 5

**Date:** 2026-06-17  
**Task:** s5-council — Live Council — Sprint 5 closeout (full-project security + sprint bugs/arch)

## Executive summary

FlowBoard is in **good shape** for a portfolio-grade Kanban API: Clean Architecture boundaries are intact, JWT/refresh rotation and workspace RBAC are consistently applied, and `dotnet test` is green (147 unit + 4 integration). No **Critical** vulnerabilities were found in the reviewed surface. The highest risks are **operational misconfiguration** (docker-compose runs Redis but never wires the API connection string; dev stack exposes SQL/Redis on host ports with `Development` mode) and **real-time authorization gaps** (SignalR group membership is checked only at join — revoked workspace members can keep receiving `CardMoved` broadcasts). Sprint 5 Redis backplane code is sound for optional local `dotnet run`, but the compose stack does not enable scale-out as documented. Before production deploy, close handler test gaps, harden rate limits beyond `/api/auth`, and address post-commit notification failures that can return 500 after a successful write.

## Findings (prioritized)

| # | Sev | Category | Location | Issue | Action |
|---|-----|----------|----------|-------|--------|
| 1 | High | Bug / Ops | `docker-compose.yml:46-54` | `api` waits for healthy Redis but `api.environment` never sets `ConnectionStrings__Redis`, `Redis__ConnectionString`, or `REDIS_CONNECTION`. Backplane and Redis ready health check stay inactive in compose. | Add `ConnectionStrings__Redis: "redis:6379"` to `api.environment`; align README/SPRINT wording with actual wiring. |
| 2 | High | Security / Bug | `BoardHub.cs:19-23`, `BoardRealtimeNotifier.cs:12-20`, `RemoveMemberCommandHandler.cs:28-30` | `JoinBoard` checks workspace membership once; removed or downgraded members remain in `board:{boardId}` and keep receiving `CardMoved` until disconnect. Highest exploitable IDOR gap in the real-time path. | Disconnect or remove from groups on member removal/role change; re-validate on hub heartbeat or before broadcast. |
| 3 | High | Bug | `UnitOfWork.cs:36-40`, `ExceptionHandlingMiddleware.cs:109-116` | Domain events publish after successful `SaveChangesAsync`. If `CardMovedEventHandler` or SignalR throws, the HTTP command already committed but the client receives 500 — DB and clients disagree. | Use outbox or fire-and-forget with retry; catch/log notification failures without failing the originating command. |
| 4 | High | Bug | `UnitOfWork.cs:29-34` | `ClearDomainEvents()` runs before `SaveChangesAsync`. If save fails, collected events are discarded and will not fire on retry within the same tracked entity instance. | Clear events only after successful commit (or restore on failure). Add `UnitOfWork` unit tests for save-failure paths. |
| 5 | High | Security | `JwtBearerSignalRExtensions.cs:21-25`, `README.md:142` | JWT passed via `?access_token=` on WebSocket negotiate. Tokens may appear in proxy/load-balancer logs, browser history, and Referer headers (OWASP API2, ASVS 3.3). | Prefer header-based auth where client stack allows; strip query strings from access logs; keep 15-minute access TTL; document secure client storage. |
| 6 | High | Security | `docker-compose.yml:1-3,11-12,26-27,49,54-55` | Dev stack publishes SQL Server (`1433`) and Redis (`6379`) to host; API runs `ASPNETCORE_ENVIRONMENT: Development` (OpenAPI/Scalar, auto-migrate, permissive CORS). Catastrophic if deployed unchanged to a reachable host. | Production compose: no host port binds for data stores, `Production` env, TLS termination, strong secrets, `AllowedOrigins`, no auto-migrate. |
| 7 | Medium | Arch / Ops | `docker-compose.yml:43-47` vs `SPRINT.md:99` | Sprint docs say Redis is optional for local dev; compose makes `api` hard-depend on Redis health. Single-instance dev without Redis cannot use `docker compose up api` as documented. | Remove `redis` from `api.depends_on` (keep Redis opt-in) or document compose as full backplane stack and wire the connection string. |
| 8 | Medium | Arch | `BoardHub.cs:15-16,31-44` | Hub injects `IBoardRepository` / `IWorkspaceRepository` and reimplements membership checks inline. Controllers route through MediatR and `WorkspaceAccess`; SignalR bypasses Application layer. | Extract Application query (e.g. `EnsureBoardAccessQuery`) or reuse `WorkspaceAccess.EnsureMemberOrNotFound`; keep hub transport-only. |
| 9 | Medium | Bug | `MoveCardCommandHandler.cs:49-53`, `CardConfiguration.cs:40` | No optimistic concurrency on card/list rows. Concurrent moves with same neighbour anchors can compute identical `FractionalIndex` values; index does not enforce uniqueness. Order becomes ambiguous. | Add `RowVersion` or unique constraint + retry; add integration test for concurrent `MoveCard`. |
| 10 | Medium | Bug | `MoveCardCommandValidator.cs:7-11`, `MoveCardCommandHandler.cs:61-68` | Validator does not reject `Guid.Empty` for `BeforeCardId` / `AfterCardId`. JSON `00000000-0000-0000-0000-000000000000` deserializes as non-null and is treated as a real neighbour → domain error instead of 422. | Add `Must(id => id is null or not Guid.Empty)` rules for optional neighbour IDs. |
| 11 | Medium | Security / Bug | `MoveCardCommandHandler.cs:34-38,67-68` | Cross-board `TargetListId` returns 400 ("different board"); missing neighbour returns 400 ("Neighbour card does not exist.") — both confirm UUIDs exist outside caller's workspace, diverging from 404 anti-enumeration pattern. | Return `NotFoundException` for lists/cards the caller cannot access via the card's board/workspace chain. |
| 12 | Medium | Security | `InviteMemberCommandHandler.cs:24-25` | Admin invite returns `NotFoundException("User", userId)` when invitee GUID does not exist vs workspace 404 — allows workspace admins to probe valid user IDs. | Return generic 404 for both missing workspace and missing user; or require email-based invite. |
| 13 | Medium | Security | `RefreshTokenCommandHandler.cs:28-57`, `UnitOfWork.cs:17-47` | Concurrent refresh with the same token can race: two requests may both see an active token before either commits, issuing two valid refresh tokens in one family without reuse detection. | Wrap rotation in serializable transaction or use optimistic concurrency / row lock on `TokenHash`; add integration test. |
| 14 | Medium | Security | `docker-compose.yml:23-36`, `SignalRRedisExtensions.cs:17-18` | Redis `7-alpine` has no `requirepass`/ACL; SignalR backplane uses connection string as-is. Any process on host/LAN can read/write pub/sub channels. | Enable Redis AUTH/TLS; bind to internal Docker network only; no host publish in shared environments. |
| 15 | Medium | Security | `Program.cs:44-65`, `AuthController.cs:15` | Rate limiting (5 req/min/IP) applies only to `api/auth`. Workspace/board/card mutations and reads are unlimited — DoS / resource exhaustion risk (OWASP API4). | Add per-user or per-IP policies on authenticated write endpoints before Sprint 8 deploy. |
| 16 | Medium | Security | `docker-compose.yml:50`, `.env.example:3`, `secrets.example.json:3` | `TrustServerCertificate=True` on SQL connection strings. Acceptable for local dev; enables MITM if reused in production without TLS to SQL Server. | Use encrypted SQL connections and proper server certificates in production; document dev-only flag. |
| 17 | Medium | Arch | `EventHandlers/CardMovedEventHandler.cs` vs `.cursor/rules/flowboard.mdc:22` | MediatR notification handler lives under `EventHandlers/`, not `Features/{Area}/` as mandated for handlers. Sprint 4 carryover. | Move to `Features/Cards/EventHandlers/` or document `EventHandlers/` as convention for domain-event notifications. |
| 18 | Medium | Tests | `tests/` (missing) | No `BoardHub` tests for `JoinBoard` 404 semantics, non-member rejection, `GetUserId` failure, or group naming. No tests for stale-group leakage after member removal. | Extract testable access guard or use `WebApplicationFactory` hub tests. |
| 19 | Medium | Tests | `RedisConnectionExtensions.cs:9-12`, `Program.cs:37-40` | `GetRedisConnectionString` fallback chain (`ConnectionStrings:Redis`, `Redis:ConnectionString`, `REDIS_CONNECTION`) and `/health/ready` Redis behaviour are untested. | Add `RedisConnectionExtensionsTests` covering all three sources, precedence, and health registration. |
| 20 | Medium | Tests | `NoOpBoardRealtimeNotifier.cs`, `BoardWorkflowTests.cs` | No integration test exercises `CardMovedEvent` → `IBoardRealtimeNotifier` → SignalR group broadcast. Fixture stubs notifier. | Add optional SignalR integration test or verify notifier invocation in MediatR pipeline test. |
| 21 | Medium | Tests | `tests/FlowBoard.UnitTests/Handlers/` | 14 of 31 MediatR handlers lack unit tests (e.g. `UpdateCard`, `DeleteCard`, `GetCardById`, board/list/project handlers). | Add focused authz tests (404 non-member, 403 Viewer) per existing handler test patterns. |
| 22 | Medium | Tests | `BoardWorkflowTests.cs:112-132`, `BoardReadService.cs:22-24` | Integration test covers soft-deleted lists in Dapper read, but not soft-deleted cards. | Add `GetBoard_RespectsSoftDeletedCards` integration case. |
| 23 | Medium | Tests / CI | `.github/workflows/ci.yml:20-21`, `SqlServerFixture.cs:73-76` | CI fails if Docker unavailable; locally integration tests skip and developers see green with 0 integration runs. | Document divergence; consider dedicated integration job or `Skip.IfNot` parity in CI. |
| 24 | Low | Bug | `BoardHub.cs:26-28` | `LeaveBoard` does not call `EnsureCanAccessBoardAsync`; any authenticated user can call `LeaveBoard(anyBoardId)` (no-op if not in group). | Document as intentional or mirror join check. |
| 25 | Low | Bug | `BoardRealtimeNotifier.cs:12-20` | `cancellationToken` accepted but not forwarded to SignalR send. Long-running shutdown may not cancel in-flight broadcasts promptly. | Pass token into SignalR client invocation if supported. |
| 26 | Low | Bug | `RedisConnectionExtensions.cs:9-12` | `GetRedisConnectionString` does not trim. Whitespace-only value could register a broken backplane/health check. | Trim and treat whitespace-only as unset. |
| 27 | Low | Security | `RegisterCommandValidator.cs:19-22` | Password policy is minimum 8 characters only — no complexity, breach list, or MFA (ASVS 2.1.1 L2 gap). | Strengthen policy for production; consider MFA at Sprint 8. |
| 28 | Low | Security | `appsettings.json:23` | `AllowedHosts: "*"` — weak host-header protection behind misconfigured reverse proxy. | Set explicit host allowlist in production configuration. |
| 29 | Low | Security | `Program.cs:139-147` | `/health/ready` is unauthenticated and reports database (and Redis when configured) status — infrastructure fingerprinting. | Restrict ready checks to internal network or protect with network policy. |
| 30 | Low | Arch | `Program.cs:30`, `SignalRRedisExtensions.cs:13`, `FlowBoard.API.csproj:15-24` | Redis connection resolved twice at startup; dual config keys increase drift risk; `StackExchange.Redis` referenced directly alongside SignalR package; `AspNetCore.HealthChecks.Redis` pinned at 9.0.0 on net10.0 host. | Resolve once; unify config key; remove unused package ref; align health-check package to 10.x. |
| 31 | Low | Tests | `SignalRRedisExtensionsTests.cs:26,48` | Backplane presence asserted via `HubLifetimeManager` type name containing `"Redis"` — fragile across package refactors. | Assert on options/configuration registration or `IConnectionMultiplexer` instead. |
| 32 | Info | Bug | `FractionalIndex.cs:56-136`, `CardConfiguration.cs:26` | Repeated inserts between same neighbours lengthen keys; `Position` capped at 100 chars. Theoretical exhaustion in extreme churn. | Monitor; add rebalance migration if needed. |
| 33 | Info | Arch | `.env.example:5-7` | `CURSOR_API_KEY` agent-runner entries bundled in s5-01 scope — unrelated to Redis. | Keep agent secrets in separate example file or scripts doc. |

## Security posture

**Full-project baseline (Sprints 1–5) — not limited to Sprint 5 delta:**

| Area | Assessment |
|------|------------|
| **JWT access tokens** | HMAC-SHA256; `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey`; 30s clock skew (`JwtService.cs`, `DependencyInjection.cs`). |
| **Refresh tokens** | Plaintext never persisted; SHA-256 hash stored; unique index on `TokenHash`; rotation + family revocation on reuse — **exemplary** (`TokenHasher.cs`, `RefreshTokenConfiguration.cs`). |
| **Passwords** | BCrypt work factor 12; constant-time login via dummy hash when user missing; generic 401 message (`PasswordService.cs`, `LoginCommandHandler.cs`). |
| **Workspace RBAC** | Non-members consistently get **404** (not 403) on scoped resources (`WorkspaceAccess.cs`, `ResourceGuard.cs`); Viewer writes return 403. |
| **Cards/Boards IDOR (REST)** | `ResourceGuard` enforced on reviewed handlers (`GetCardById`, `GetBoard`, etc.); no critical in-handler IDOR found. |
| **SignalR** | `[Authorize]` on hub; `JoinBoard` membership check with generic `HubException("Board not found.")` — good join semantics; **stale group membership** after removal is the main gap. |
| **SignalR token transport** | JWT via `?access_token=` — industry-standard WebSocket workaround with log/referrer leakage risk; mitigate with short TTL and log stripping. |
| **Redis** | Optional backplane correctly gated when connection string unset; no AUTH in dev compose; host port `6379` exposed — **dev only**. Compose does not wire API to Redis (finding #1). |
| **SQL injection** | EF Core + parameterized Dapper (`BoardReadService.cs`); no `FromSqlRaw` / string-interpolated SQL in `src/`. |
| **Validation** | MediatR `ValidationBehavior` runs FluentValidation before all commands (`ValidationBehavior.cs`). |
| **Secrets** | `.env`, `secrets.json`, `appsettings.Development.json` gitignored; startup enforces JWT key ≥ 32 chars; `appsettings.json` has empty secrets. |
| **Error handling** | RFC 7807 via `ExceptionHandlingMiddleware`; unhandled exceptions return generic 500 with `traceId` — no stack traces to clients. |
| **Headers / transport** | HSTS when not Development; security headers (`X-Content-Type-Options`, `X-Frame-Options`, etc.); forwarded headers opt-in with `KnownProxies`; OpenAPI/Scalar dev-only. |
| **Container** | API Dockerfile runs as non-root `appuser`. |
| **Dev exposure** | SQL `1433` + Redis `6379` on host; `Development` env with auto-migrate and `AllowAnyOrigin` CORS — **never deploy as-is**. |
| **Dependencies** | Packages pinned (EF Core 10.0.7, JwtBearer 10.0.7, etc.); no automated vulnerability scan in CI. |

**OWASP API Top 10 (2023):**

| Risk | Status |
|------|--------|
| API1 Broken Object Level Authorization | **Strong** on REST; **gap** on SignalR stale groups |
| API2 Broken Authentication | **Strong** JWT/refresh; SignalR query-token transport is residual risk |
| API3 Broken Object Property Level Authorization | **Good** — explicit DTOs, no entity binding |
| API4 Unrestricted Resource Consumption | **Gap** — rate limit only on `/api/auth` |
| API5 Broken Function Level Authorization | **Good** — Viewer 403 on writes |
| API6 Unrestricted Access to Sensitive Business Flows | **Acceptable** for current scope |
| API7 Server Side Request Forgery | N/A — no outbound URL fetching |
| API8 Security Misconfiguration | **Risk** in dev Docker/compose defaults |
| API9 Improper Inventory Management | OpenAPI in dev only — acceptable |
| API10 Unsafe Consumption of APIs | N/A |

**ASVS L2 highlights:** Auth session management and anti-enumeration on REST are solid. Gaps: password complexity/MFA (2.1.1), broader rate limiting (4.1), SignalR token in URL (3.3), production hardening checklist incomplete.

## Test & quality gaps

### Covered well
- Auth: register, login, logout, refresh (including reuse detection) — unit tests
- Workspaces: CRUD, invite, remove, change role — unit tests
- Cards: create, move (unit); board workflow with fractional-index ordering (integration)
- Domain: `FractionalIndex`, workspace rules, refresh token entity
- Sprint 5: `SignalRRedisExtensionsTests` — DI resolves with/without Redis backplane (2 cases)
- Event: `CardMovedEventHandler` (2 cases)

### Missing (prioritized)
| Priority | Gap |
|----------|-----|
| P1 | `BoardHub` authz and stale-group isolation after member removal |
| P1 | `UnitOfWork` save-failure and post-commit publish-failure paths |
| P1 | `GetRedisConnectionString` precedence + Redis health registration |
| P2 | 14 untested MediatR handlers — authz matrix (404 non-member, 403 Viewer) |
| P2 | `CardMovedEvent` → notifier → SignalR broadcast (integration or pipeline test) |
| P2 | Concurrent `MoveCard` and concurrent refresh rotation |
| P2 | Cross-tenant IDOR integration smoke (outsider 404 on cards/boards) |
| P3 | Soft-deleted cards in Dapper `GetBoard` read |
| P3 | All 17 `*Validator.cs` files — zero dedicated validator tests |
| P3 | Integration auth E2E: register → login → refresh → protected endpoint |

### CI divergence
Local runs without Docker skip integration tests (`SqlServerFixture.IsDockerAvailable`); CI requires Docker and fails the job if unavailable. Developers may see green unit-only runs while CI exercises a narrower path.

## Recommended follow-up tasks

1. **s6-01** — Wire `ConnectionStrings__Redis` in `docker-compose.yml` `api.environment`; reconcile `depends_on` with "optional Redis" docs.
2. **s6-02** — SignalR group lifecycle: disconnect/remove on `RemoveMember` / role change; re-validate membership on heartbeat or broadcast.
3. **s6-03** — Decouple domain-event notifications from HTTP response: outbox or catch/log without 500 after commit.
4. **s6-04** — Fix `UnitOfWork` event clearing order; add unit tests for save/publish failure paths.
5. **s6-05** — `BoardHub` refactor: route access checks through Application (`EnsureBoardAccessQuery` or `WorkspaceAccess`).
6. **s6-06** — `MoveCard` hardening: `RowVersion` or unique position constraint; empty-GUID validator rules; 404 semantics for inaccessible neighbours/lists.
7. **s6-07** — Unit tests: untested card/board/list/project handlers (authz-focused).
8. **s6-08** — Tests: `RedisConnectionExtensions` precedence, Redis ready health, `BoardHub` join denial.
9. **s6-09** — Integration: `CardMoved` broadcast path; soft-deleted cards; concurrent move smoke.
10. **s6-10** — Security: generic 404 on `InviteMember` for missing user; concurrent refresh rotation test + serializable transaction.
11. **s8-prep** — Production hardening: rate limits on write endpoints, Redis AUTH/TLS, no public SQL/Redis ports, `AllowedOrigins`, `AllowedHosts`, dependency vulnerability scan in CI.

## Sign-off

- **Bug hunter:** Sprint 5 Redis wiring is functionally sound for optional local `dotnet run`, but compose never enables the backplane. Core board/card logic and EF→Dapper consistency are solid. Main logic risks: SignalR group staleness, post-commit 500 on notification failure, concurrent fractional-index collisions, and large test gaps (hub, Redis health, broadcast path, 14 handlers).
- **Security:** Application-layer security is solid for Sprints 1–5 — no critical REST IDOR; refresh rotation exemplary. Residual risk concentrates in dev/ops misconfiguration, SignalR token-in-query-string, stale real-time group membership (highest exploitable gap), missing rate limits on non-auth endpoints, and enumeration leaks on invite/MoveCard error semantics.
- **Architecture:** Clean Architecture posture remains sound; Sprint 5 Redis work correctly confined to API composition root. Sprint 5 queue scope met. Main arch gaps: compose operational wiring vs documented optional Redis, `BoardHub` bypassing Application CQRS, `EventHandlers/` folder convention drift, and minor config/package tidying.
