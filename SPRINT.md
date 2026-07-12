# FlowBoard — sprint state (update after every task)

> **AI:** Read this file at the start of every new chat. Update the "Last task" and checklist when done.

## Current focus

| Field | Value |
|-------|-------|
| **Active phase** | **MVP complete** — council verified; published to GitHub (2026-07-12) |
| **Roadmap sprint** | 8 (Production deploy) — council verify done |
| **Branch** | `master` |
| **Last updated** | 2026-07-12 |
| **Unit tests** | 277 passing |
| **Integration tests** | 21 passing (Docker / TestContainers) |

## Sprint status

| Sprint | Status | Notes |
|--------|--------|-------|
| 1 Auth | Done | JWT + refresh rotation |
| 2 Workspaces RBAC | Done | 404 for non-members |
| 3 Boards + Cards | Done | Domain, EF, Dapper read, API, unit + integration tests |
| 4 SignalR | Done | CardMoved; stale groups (`close-02`); hub tests (`close-08`); broadcast path (`close-09`) |
| 5 Redis | Done | Backplane + compose wiring; Redis tests (`close-08`); polish (`close-11`) |
| 6 Comments + Email | Done | Comments CRUD, tags, MailKit email queue, integration tests |
| 7 Hangfire + activity | Done | Hangfire jobs, activity log + GetCardActivity |
| 8 Production deploy | Done | Prod compose, CI, write rate limits, deploy docs; council fixes (`s8-council-fixes`); council verified (`s8-council-verify`) |

## Sprint 1 — delivered

- [x] Solution: Domain, Application, Infrastructure, API, UnitTests, IntegrationTests
- [x] EF Core 10 + SQL Server, initial migrations
- [x] `User` entity + `Email` value object
- [x] `RegisterCommand`, `LoginCommand`, `RefreshTokenCommand`, `LogoutCommand` + validators
- [x] JWT access (15 min) + refresh (7 d) with **family_id** rotation and reuse detection
- [x] `PasswordService` — BCrypt wf 12; timing-safe login path
- [x] MediatR pipeline: `LoggingBehavior`, `ValidationBehavior`
- [x] `ExceptionHandlingMiddleware` — RFC 7807 Problem Details + traceId
- [x] Rate limiting on `/api/auth` (5/min/IP)
- [x] docker-compose SQL Server
- [x] Unit tests: auth handlers + refresh reuse detection
- [x] `close-10` Refresh rotation — transaction + `UPDLOCK`; concurrent refresh integration test

## Sprint 2 — delivered

- [x] `Workspace` + `WorkspaceMember` entities + EF configuration
- [x] Workspace CRUD + invite / remove / change member role
- [x] Roles: Owner, Admin, Member, Viewer
- [x] `WorkspaceAccess` + `ResourceGuard` — 404 non-members, 403 Viewer writes
- [x] `ICurrentUserService` from JWT claims
- [x] Soft delete global query filters
- [x] Migration `CouncilReviewFixes` — filtered unique indexes, FK Restrict
- [x] Unit tests: workspace command/query handlers
- [x] `close-10` InviteMember — missing invitee returns workspace 404 (anti-enumeration)

## Sprint 6 — delivered

- [x] `s6-01` `Comment` entity + EF migration; Create/Get/Update/Delete handlers with workspace authz; `CommentsController`; unit tests (+10)
- [x] `s6-02` `CommentAddedEvent` → `CommentAddedEventHandler` → `IBoardRealtimeNotifier` → SignalR `CommentAdded` to board group; unit tests (+3)
- [x] `s6-03` `Tag` + `CardTag` entities + migration; workspace tag CRUD; apply/remove on cards; `TagsController`; unit tests (+18)
- [x] `s6-04` `IEmailService` + MailKit `SmtpEmailService`; `SmtpSettings` via config/secrets; unit tests (+2)
- [x] `s6-05` `EmailQueue` + `QueuedEmailService` + `EmailBackgroundService`; `AssignCard` + `CardAssignedEvent` → queued assignment email; migration `AddCardAssignee`; unit tests (+8)
- [x] `s6-06` `CleanupExpiredRefreshTokensService` — daily scan revokes expired refresh tokens; `IRefreshTokenRepository.RevokeExpiredAsync`; unit tests (+2)
- [x] `s6-07` Integration tests — comment CRUD + soft-delete + `CommentAdded` notifier; tag CRUD + apply/remove + delete cascades; `CommentsAndTagsWorkflowTests` (+5 integration)
- [x] `s6-council-fixes` Council High/Medium remediation — `Repository.GetByIdAsync` soft-delete filter; `AssigneeId` in Dapper `GetBoard`; assign TOCTOU transaction + clear assignee on `RemoveMember`; comment author-only edit/delete; HTML-encode comment bodies; bounded email queue; assignment email throttle + membership re-check; `CancellationToken.None` post-commit publish; authz/tag/assign tests (+12 unit, +4 integration). **Deferred:** write-endpoint rate limits → `s8-02`; SMTP retry/dead-letter → `s7-02`; `AssigneeId` FK → migration approval (runtime cleanup instead)
- [x] `s6-council` Live Council — report at `docs/council/sprint-6-report.md`
- [x] `s6-council-verify` Remediation verified — report at `docs/council/sprint-6-verify-report.md`; council sign-off for publish
- [x] `s6-docs` Docs sync — Sprint 6 closed; README test counts + API surface; active phase → Sprint 7

## Sprint 7 — delivered

- [x] `s7-01` Hangfire + SQL Server storage — `AddHangfireWithSqlServer`, dashboard at `/jobs` with `Admin` policy (`Hangfire:DashboardAdminEmails`); unit tests (+4)
- [x] `s7-02` Email + token cleanup → Hangfire — `SendEmailJob` (per-message enqueue with `[AutomaticRetry(Attempts = 3)]`); `CleanupExpiredRefreshTokensJob` (daily recurring); removed `EmailBackgroundService`, `CleanupExpiredRefreshTokensService`, `EmailQueue`; unit tests (+3)
- [x] `s7-03` `ActivityLog` entity + migration; `ActivityLogEventHandler` writes on `CardCreated`, `CardMoved`, `MemberInvited`; Dapper `GetCardActivity` query + `GET /api/cards/{id}/activity`; unit tests (+7)
- [x] `s7-docs` Docs sync — Sprint 7 closed; README test counts + API surface; active phase → Sprint 8

## Sprint 8 — delivered

- [x] `s8-01` Production docker-compose (`docker-compose.prod.yml`) — internal SQL/Redis (no host ports), Redis AUTH, `ASPNETCORE_ENVIRONMENT=Production`, `AllowedOrigins` required; `.env.example` + `Program.cs` startup guard
- [x] `s8-02` Write-endpoint rate limiting — `writes` policy (60/min per user, IP fallback); MVC convention on `[Authorize]` mutations; auth policy unchanged (+7 unit)
- [x] `s8-03` CI — split `unit-tests` / `integration-tests` jobs; Docker requirement documented; Coverlet cobertura artifact on unit job
- [x] `s8-04` Deploy docs — README Live API + Railway/Azure/self-hosted guides; `railway.toml`; prod compose migration note
- [x] `s8-docs` Docs sync — Sprint 8 closed; MVP complete; README roadmap updated; active phase → council (`s8-council`)
- [x] `s8-council` Live Council — report at `docs/council/sprint-8-report.md`
- [x] `s8-council-fixes` Council High/Medium remediation — Production pending-migration fail-fast; SMTP misconfig throws (`SmtpNotConfiguredException`); eviction catch/log (close-03 pattern); stop unreadable `MemberInvited` activity rows; `BoardHubJoinRateLimiter` (30/min); `Auth:AllowRegistration` gated in Production; Hangfire `DisableGlobalLocks=false`; HSTS/HTTPS gated on TLS/forwarded headers; single-replica SignalR docs; CI prod compose validate; activity log + RemoveMember assignee + write 429 integration tests (+4 integration, +3 unit). **Deferred:** Critical #1 distributed SignalR eviction (documented single-replica); High #2 query-string hub token; High #3 SQL `sa` least-privilege; High #8 live deploy URL; Medium #11 activity eventual consistency; Medium #18 read rate limits; Medium #22 `AssigneeId` FK (migration approval); Low items

## Sprint 5 — delivered

- [x] `s5-01` Redis in docker-compose (`redis:7-alpine`, port 6379, healthcheck, volume)
- [x] `s5-02` StackExchange.Redis + SignalR backplane packages (optional when connection string set)
- [x] `s5-03` Redis connection from configuration + health check (`GetRedisConnectionString`; optional `redis` ready check)
- [x] `s5-04` Unit test for backplane registration (`SignalRRedisExtensionsTests`)
- [x] `s5-05` Docs sync — SPRINT.md, README roadmap, session log
- [x] `close-01` Compose API `ConnectionStrings__Redis=redis:6379` — SignalR backplane enabled in dev stack
- [x] `close-11` Redis connection trim; single resolve at startup; `EventHandlers/` convention documented

## Sprint 4 — delivered

- [x] `BoardHub` at `/hubs/board` — JWT via `?access_token=` query param
- [x] Connection groups `board:{boardId}` — `JoinBoard` / `LeaveBoard` with workspace membership check (404 semantics)
- [x] `CardMovedEvent` → `DomainEventNotification` → `CardMovedEventHandler` → `IBoardRealtimeNotifier` → SignalR `CardMoved` to group
- [x] Unit tests: `CardMovedEventHandlerTests` (2 cases)
- [x] `close-02` SignalR group eviction — `BoardGroupMembershipRegistry` + `IBoardRealtimeGroupEvictor`; evict on `RemoveMember` / downgrade to Viewer
- [x] `close-03` Domain-event notifications — catch/log after commit; committed move does not return HTTP 500 on SignalR failure
- [x] Unit tests: `BoardRealtimeGroupEvictorTests` (stale-group case) + handler eviction wiring
- [x] Unit tests: `CardMovedEventHandlerTests` (3 cases — includes notifier failure swallow)
- [x] `dotnet test` green

## Sprint 3 — delivered

- [x] Boards, lists, cards — domain entities + EF repositories
- [x] `FractionalIndex` ordering for lists and cards
- [x] Dapper `IBoardReadService` — single round-trip board aggregate read
- [x] API: projects → boards → lists → cards + move
- [x] Unit tests for handlers (Create/Move card, GetBoard, CreateBoardList, etc.)
- [x] Integration tests: `BoardWorkflowTests` (TestContainers SQL Server)
- [x] Integration tests skip gracefully when Docker unavailable (local dev without Docker Desktop)
- [x] `close-09` Integration tests — soft-deleted cards in GetBoard; CardMoved notifier pipeline; concurrent move smoke (7 cases)
- [x] README synced (147 tests, Boards API, roadmap)
- [x] `close-04` UnitOfWork — clear domain events only after successful SaveChanges; `UnitOfWorkTests` (2 cases)
- [x] `close-05` BoardHub access checks via `EnsureBoardAccessQuery` (MediatR + `ResourceGuard`); `EnsureBoardAccessQueryHandlerTests` (4 cases)
- [x] `close-06` MoveCard hardening — empty-GUID validator rules; 404 for inaccessible list/neighbour; `UpdatedAt` concurrency token + unique `(BoardListId, Position)` index; retry on conflict; migration `AddCardMoveConcurrency`; `MoveCardCommandHandlerTests` (+4 cases)
- [x] `close-07` Handler authz matrix — 404 non-member + 403 Viewer on 13 mutation handlers (cards, boards, lists, projects); +26 unit tests

## Architecture decisions (do not change without asking)

- **Clean Architecture:** Domain → Application → Infrastructure → API
- **CQRS:** MediatR commands/queries + FluentValidation
- **Writes:** EF Core repositories
- **Reads:** Dapper via `IBoardReadService` / `BoardReadService` (GetBoard aggregate view)
- **Ordering:** `FractionalIndex` value object — ordinal string comparison for sort
- **Security:** Non-members get **404** (not 403) on workspace-scoped resources
- **Real-time:** `CardMovedEvent` + `CommentAddedEvent`; hub groups keyed by `board:{boardId}`; membership tracked in `BoardGroupMembershipRegistry`; evicted on member removal or Viewer downgrade
- **Redis:** Optional SignalR backplane when `ConnectionStrings:Redis` / `Redis:ConnectionString` / `REDIS_CONNECTION` set; docker-compose sets `ConnectionStrings__Redis=redis:6379` on the API; app runs without Redis for local `dotnet run`
- **DB:** SQL Server 2022 — `UseSqlServer()`, no Postgres migration
- **Migrations:** Auto-apply only in Development (`Program.cs`)
- **Tests:** xUnit + Moq (unit); TestContainers SQL Server (integration, skip without Docker)

## Project layout (where new code goes)

```
src/FlowBoard.Domain/           Entities, value objects, domain events, repo interfaces
src/FlowBoard.Application/      Features/{Area}/Commands|Queries, EventHandlers/, validators, DTOs
src/FlowBoard.Infrastructure/     EF configs, repos, Dapper, JWT, migrations
src/FlowBoard.API/              Controllers, Hubs, SignalR notifier impl
tests/FlowBoard.UnitTests/      Handler + domain tests (mocked repos)
tests/FlowBoard.IntegrationTests/ Full workflow vs real SQL Server
```

## API surface (existing)

- `/api/auth/*` — public, rate-limited (5/min/IP)
- Authenticated mutations (POST/PUT/PATCH/DELETE) — rate-limited (60/min/user, IP fallback)
- `/api/workspaces/*` — workspace CRUD + members
- `/api/projects/*` — projects in workspace
- `/api/projects/{id}/boards` — boards in project
- `/api/boards/{id}` — full board view (Dapper)
- `/api/boards/{id}/lists` — board lists
- `/api/cards/*` — card CRUD + move
- `/api/cards/{cardId}/comments`, `/api/comments/{id}` — comment CRUD
- `/api/cards/{id}/activity` — card activity log (Dapper read)
- `/api/workspaces/{workspaceId}/tags`, `/api/tags/{id}` — workspace tag CRUD
- `/api/cards/{cardId}/tags/{tagId}` — apply/remove tag on card
- `/hubs/board` — SignalR: `JoinBoard(boardId)`, `LeaveBoard(boardId)`; server pushes `CardMoved`, `CommentAdded`
- `/jobs` — Hangfire dashboard (JWT + `Admin` policy; configure `Hangfire:DashboardAdminEmails`)

## Do NOT touch (unless task says so)

- Auth / refresh token rotation logic
- Workspace RBAC rules (`WorkspaceAccess`, role hierarchy)
- Existing migrations (add new migration only when schema changes)
- `docker-compose.yml` production assumptions (dev-only stack) — use `docker-compose.prod.yml` for production

## Local dev (quick ref)

```pwsh
docker compose up -d sqlserver redis   # full stack: compose wires Redis backplane on API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;..." --project src/FlowBoard.API
dotnet user-secrets set "Jwt:SecretKey" "REPLACE_WITH_AT_LEAST_32_RANDOM_CHARS" --project src/FlowBoard.API
# Optional for dotnet run (not needed when API runs in compose): ConnectionStrings:Redis localhost:6379
dotnet run --project src/FlowBoard.API
dotnet test
```

Integration tests require **Docker Desktop** running (skipped otherwise). Use `pwsh scripts/run-integration-tests.ps1` or `docker-compose.integration.yml` via `-UseCompose`.

## Next task (pick one per chat)

**Queue:** `tasks/queue.json` — Sprints 1–8 closed; MVP complete; council review next. `pwsh scripts/run-next-task.ps1 -Status`

**Recommended next:** `s8-council` — Live Council — Sprint 8 / MVP review (production + security + deploy).

## Closeout phase (Sprints 1–5 gaps)

- [x] `close-01` — Compose Redis connection wired on API
- [x] `close-02` — SignalR group eviction on member removal / Viewer downgrade
- [x] `close-03` — Domain-event notifications must not fail HTTP after successful commit
- [x] `close-04` — UnitOfWork clears domain events only after successful SaveChanges
- [x] `close-05` — BoardHub access checks via `EnsureBoardAccessQuery` (MediatR + `ResourceGuard`)
- [x] `close-06` — MoveCard validator, 404 semantics, concurrency (unique index + retry)
- [x] `close-07` — Handler authz matrix unit tests (404 non-member, 403 Viewer on mutation handlers)
- [x] `close-08` — Redis config precedence, BoardHub join denial, stable backplane assert
- [x] `close-09` — Integration tests: soft-deleted cards, CardMoved notifier path, concurrent move smoke
- [x] `close-10` — InviteMember anti-enumeration (workspace 404); refresh rotation transaction + row lock
- [x] `close-11` — Redis trim + single startup resolve; EventHandlers convention; LeaveBoard docs
- [x] `close-docs` — mark Sprints 1–5 fully closed in SPRINT.md + README
- [x] `close-council` — verify closeout report (`docs/council/closeout-report.md`)

## Session log

| Date | Task | Result |
|------|------|--------|
| 2026-06-17 | AI workflow setup | Added SPRINT.md, .cursor/rules, docs/AI-WORKFLOW.md |
| 2026-06-17 | End Sprint 3 | Integration tests skip without Docker; README + SPRINT synced; `dotnet test` green |
| 2026-06-17 | Sprint 5 s5-01 | Redis service in docker-compose + REDIS_CONNECTION in .env.example |
| 2026-06-17 | Sprint 5 s5-02 | StackExchange.Redis + SignalR backplane packages; optional backplane via `AddSignalRWithOptionalRedisBackplane` |
| 2026-06-17 | Sprint 5 s5-03 | `GetRedisConnectionString` (ConnectionStrings:Redis, Redis:ConnectionString, REDIS_CONNECTION); optional Redis ready health check |
| 2026-06-17 | Sprint 5 s5-04 | `SignalRRedisExtensionsTests` — BoardHub DI resolves with/without Redis backplane; 147 unit tests |
| 2026-06-17 | Sprint 5 s5-05 | Docs sync — Sprint 5 delivered in SPRINT.md; README roadmap (Sprints 4–5 Done); `dotnet test` green |
| 2026-06-17 | Sprint 5 Live Council | Report at `docs/council/sprint-5-report.md` |
| 2026-06-17 | Queue restructure | closeout phase; Sprint 6–8 feature roadmap restored in `tasks/queue.json` |
| 2026-06-17 | Notion sync | Sprint History page; closeout vs Sprint 6 separated |
| 2026-06-17 | closeout close-01 | `ConnectionStrings__Redis=redis:6379` on compose API; README/.env.example synced; `dotnet test` green |
| 2026-06-17 | closeout close-02 | SignalR stale-group fix — registry + evictor; RemoveMember/ChangeMemberRole handlers; 151 unit tests; `dotnet test` green |
| 2026-06-17 | closeout close-03 | Post-commit domain-event publish catch/log in UnitOfWork + CardMovedEventHandler; 152 unit tests; `dotnet test` green |
| 2026-06-17 | closeout close-04 | UnitOfWork clears domain events after commit only; `UnitOfWorkTests` (retain on failure, clear + publish on success); 154 unit tests; `dotnet test` green |
| 2026-06-17 | closeout close-05 | `EnsureBoardAccessQuery` + handler; BoardHub uses MediatR; `EnsureBoardAccessQueryHandlerTests` (4 cases); 158 unit tests; `dotnet test` green |
| 2026-06-17 | closeout close-06 | MoveCard hardening — validator rejects empty neighbour GUIDs; 404 for inaccessible list/neighbour; `UpdatedAt` concurrency + unique position index; retry on conflict; `AddCardMoveConcurrency` migration; 162 unit tests; `dotnet test` green |
| 2026-06-17 | closeout close-07 | Handler authz matrix — 404 non-member + 403 Viewer on 13 mutation handlers (Create/Update/Delete/Move card, Create/Update/Delete board, Create/Rename/Move/Delete list, Update/Delete project); +26 unit tests; 188 unit tests; `dotnet test` green |
| 2026-06-18 | closeout close-09 | Integration tests — `GetBoard_RespectsSoftDeletedCards`, `MoveCard_InvokesCardMovedNotifierAfterCommit`, concurrent move smoke; `CapturingBoardRealtimeNotifier`; 7 integration tests; 196 unit tests; `dotnet test` green |
| 2026-06-18 | closeout close-10 | InviteMember missing invitee → workspace 404; refresh rotation `ExecuteInTransactionAsync` + `UPDLOCK`; `AuthRefreshTests` concurrent refresh; 8 integration tests; 196 unit tests; `dotnet test` green |
| 2026-06-18 | closeout close-11 | `GetRedisConnectionString` trims whitespace; Redis resolved once in `Program.cs`; `EventHandlers/` convention in README + agent rules; `LeaveBoard` access-check docs; 198 unit tests; `dotnet test` green |
| 2026-06-18 | closeout close-docs | Sprints 1–5 marked fully closed; closeout phase complete (pending `close-council`); active phase → Sprint 6; README test counts synced (198 unit, 8 integration); `dotnet test` green |
| 2026-06-18 | Sprint 6 Live Council | Report at `docs/council/closeout-report.md` |
| 2026-06-18 | sprint6 s6-01 | Comment entity + CRUD API — migration `AddComments`, handlers with workspace authz, `CommentsController`; 208 unit tests; `dotnet test` green |
| 2026-06-18 | sprint6 s6-02 | `CommentAddedEvent` SignalR broadcast — `CommentAddedEventHandler`, `CommentAddedMessage`, `IBoardHubClient.CommentAdded`; 211 unit tests; `dotnet test` green |
| 2026-06-18 | sprint6 s6-03 | Tags + CardTags — migration `AddTags`, workspace tag CRUD, apply/remove on cards, `TagsController`; 229 unit tests; `dotnet test` green |
| 2026-06-18 | sprint6 s6-04 | `IEmailService` + MailKit `SmtpEmailService`; `SmtpSettings` via config/secrets; `SmtpEmailServiceTests` (2 cases); 231 unit tests; `dotnet test` green |
| 2026-06-18 | sprint6 s6-05 | `EmailQueue` + `QueuedEmailService` + `EmailBackgroundService`; `AssignCard` + `CardAssignedEventHandler`; migration `AddCardAssignee`; 239 unit tests; `dotnet test` green |
| 2026-06-18 | sprint6 s6-06 | `CleanupExpiredRefreshTokensService` — daily `RevokeExpiredAsync` scan; `IRefreshTokenRepository.RevokeExpiredAsync`; 241 unit tests; `dotnet test` green |
| 2026-06-18 | sprint6 s6-07 | Integration tests — `CommentsAndTagsWorkflowTests` (comment CRUD, CommentAdded notifier, tag apply/remove/delete); extended `CapturingBoardRealtimeNotifier`; 13 integration tests; `dotnet test` green |
| 2026-06-18 | Sprint 6 Live Council | Report at `docs/council/sprint-6-report.md` |
| 2026-07-12 | sprint6 s6-council-fixes | Council High/Medium fixes — soft-delete bypass, assignee lifecycle, comment author/XSS, bounded queue, post-commit token; 253 unit + 17 integration; `dotnet test` green |
| 2026-07-12 | sprint6 s6-council-verify | Live Council remediation verification — report at `docs/council/sprint-6-verify-report.md`; council sign-off for publish |
| 2026-07-12 | sprint6 s6-docs | Docs sync — Sprint 6 closed; README test counts + API docs; active phase → Sprint 7; 253 unit + 17 integration; `dotnet test` green |
| 2026-07-12 | sprint7 s7-01 | Hangfire + SQL Server storage — dashboard at `/jobs` with `Admin` policy; `HangfireServiceExtensions`; 257 unit + 17 integration; `dotnet test` green |
| 2026-07-12 | sprint7 s7-02 | Email + token cleanup → Hangfire jobs — `SendEmailJob`, `CleanupExpiredRefreshTokensJob` (daily recurring); removed IHostedService workers; 260 unit + 17 integration; `dotnet test` green |
| 2026-07-12 | sprint7 s7-03 | `ActivityLog` entity + event-driven writes — `ActivityLogEventHandler` for card create/move + member invite; Dapper `GetCardActivity`; `GET /api/cards/{id}/activity`; migration `AddActivityLog`; 267 unit + 17 integration; `dotnet test` green |
| 2026-07-12 | sprint7 s7-docs | Docs sync — Sprint 7 closed; README test counts + API docs; active phase → Sprint 8; 267 unit + 17 integration; `dotnet test` green |
| 2026-07-12 | sprint8 s8-01 | `docker-compose.prod.yml` — internal SQL/Redis, Redis AUTH, Production env + `AllowedOrigins`; `.env.example` + `Program.cs` guard; 267 unit + 17 integration; `dotnet test` green |
| 2026-07-12 | sprint8 s8-02 | Write-endpoint rate limiting — `writes` policy (60/min/user); `WriteRateLimitingConvention`; auth limit unchanged; 274 unit + 17 integration; `dotnet test` green |
| 2026-07-12 | sprint8 s8-03 | CI — separate unit/integration jobs; Docker documented for TestContainers; Coverlet cobertura artifact; 274 unit + 17 integration; `dotnet test` green |
| 2026-07-12 | sprint8 s8-04 | README Live API + Railway/Azure/self-hosted deploy guides; `railway.toml`; prod compose migration note; 274 unit + 17 integration; `dotnet test` green |
| 2026-07-12 | sprint8 s8-docs | Docs sync — Sprint 8 closed; MVP complete; README roadmap updated; active phase → council (`s8-council`); 274 unit + 17 integration; `dotnet test` green |
| 2026-07-12 | Sprint 8 Live Council | Report at `docs/council/sprint-8-report.md` |
| 2026-07-12 | sprint8 s8-council-fixes | Council High/Medium fixes — prod migration gate, SMTP fail-fast, eviction resilience, hub join throttle, registration gate, integration gaps; 277 unit + 21 integration; `dotnet test` green |
| 2026-07-12 | Sprint 8 Live Council — report at docs/council/sprint-8-verify-report.md |
| 2026-07-12 | publish-mvp | Sprints 6–8 + council verify published to GitHub; Notion synced; 277 unit + 21 integration |
