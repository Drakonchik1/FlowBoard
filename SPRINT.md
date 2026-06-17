# FlowBoard — sprint state (update after every task)

> **AI:** Read this file at the start of every new chat. Update the "Last task" and checklist when done.

## Current focus

| Field | Value |
|-------|-------|
| **Active phase** | **closeout** — finish Sprints 1–5 before Sprint 6 features |
| **Roadmap sprint** | 6 (Comments + Tags + Email) — planned after closeout |
| **Branch** | `master` |
| **Last updated** | 2026-06-17 |
| **Unit tests** | 147 passing |
| **Integration tests** | 4 passing (Docker / TestContainers) |

## Sprint status

| Sprint | Status | Notes |
|--------|--------|-------|
| 1 Auth | Done | JWT + refresh rotation |
| 2 Workspaces RBAC | Done | 404 for non-members |
| 3 Boards + Cards | Done | Domain, EF, Dapper read, API, unit + integration tests |
| 4 SignalR | Done — closeout pending | CardMoved shipped; stale groups, tests → `close-02`, `close-08`, `close-09` |
| 5 Redis | Done — closeout pending | Backplane shipped; compose wiring → `close-01` |
| 6 Comments + Email | Planned | Original roadmap — starts after closeout phase |
| 7 Hangfire + activity | Planned | After Sprint 6 |
| 8 Production deploy | Planned | CI, prod compose, live URL |

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

## Sprint 2 — delivered

- [x] `Workspace` + `WorkspaceMember` entities + EF configuration
- [x] Workspace CRUD + invite / remove / change member role
- [x] Roles: Owner, Admin, Member, Viewer
- [x] `WorkspaceAccess` + `ResourceGuard` — 404 non-members, 403 Viewer writes
- [x] `ICurrentUserService` from JWT claims
- [x] Soft delete global query filters
- [x] Migration `CouncilReviewFixes` — filtered unique indexes, FK Restrict
- [x] Unit tests: workspace command/query handlers

## Sprint 5 — delivered

- [x] `s5-01` Redis in docker-compose (`redis:7-alpine`, port 6379, healthcheck, volume)
- [x] `s5-02` StackExchange.Redis + SignalR backplane packages (optional when connection string set)
- [x] `s5-03` Redis connection from configuration + health check (`GetRedisConnectionString`; optional `redis` ready check)
- [x] `s5-04` Unit test for backplane registration (`SignalRRedisExtensionsTests`)
- [x] `s5-05` Docs sync — SPRINT.md, README roadmap, session log

## Sprint 4 — delivered

- [x] `BoardHub` at `/hubs/board` — JWT via `?access_token=` query param
- [x] Connection groups `board:{boardId}` — `JoinBoard` / `LeaveBoard` with workspace membership check (404 semantics)
- [x] `CardMovedEvent` → `DomainEventNotification` → `CardMovedEventHandler` → `IBoardRealtimeNotifier` → SignalR `CardMoved` to group
- [x] Unit tests: `CardMovedEventHandlerTests` (2 cases)
- [x] `dotnet test` green

## Sprint 3 — delivered

- [x] Boards, lists, cards — domain entities + EF repositories
- [x] `FractionalIndex` ordering for lists and cards
- [x] Dapper `IBoardReadService` — single round-trip board aggregate read
- [x] API: projects → boards → lists → cards + move
- [x] Unit tests for handlers (Create/Move card, GetBoard, CreateBoardList, etc.)
- [x] Integration tests: `BoardWorkflowTests` (TestContainers SQL Server)
- [x] Integration tests skip gracefully when Docker unavailable (local dev without Docker Desktop)
- [x] README synced (147 tests, Boards API, roadmap)

## Architecture decisions (do not change without asking)

- **Clean Architecture:** Domain → Application → Infrastructure → API
- **CQRS:** MediatR commands/queries + FluentValidation
- **Writes:** EF Core repositories
- **Reads:** Dapper via `IBoardReadService` / `BoardReadService` (GetBoard aggregate view)
- **Ordering:** `FractionalIndex` value object — ordinal string comparison for sort
- **Security:** Non-members get **404** (not 403) on workspace-scoped resources
- **Real-time:** `CardMovedEvent` only (Sprint 4); hub groups keyed by `board:{boardId}`
- **Redis:** Optional SignalR backplane when `ConnectionStrings:Redis` / `Redis:ConnectionString` / `REDIS_CONNECTION` set; app runs without Redis for local dev
- **DB:** SQL Server 2022 — `UseSqlServer()`, no Postgres migration
- **Migrations:** Auto-apply only in Development (`Program.cs`)
- **Tests:** xUnit + Moq (unit); TestContainers SQL Server (integration, skip without Docker)

## Project layout (where new code goes)

```
src/FlowBoard.Domain/           Entities, value objects, domain events, repo interfaces
src/FlowBoard.Application/      Features/{Area}/Commands|Queries, validators, DTOs, event handlers
src/FlowBoard.Infrastructure/     EF configs, repos, Dapper, JWT, migrations
src/FlowBoard.API/              Controllers, Hubs, SignalR notifier impl
tests/FlowBoard.UnitTests/      Handler + domain tests (mocked repos)
tests/FlowBoard.IntegrationTests/ Full workflow vs real SQL Server
```

## API surface (existing)

- `/api/auth/*` — public, rate-limited
- `/api/workspaces/*` — workspace CRUD + members
- `/api/projects/*` — projects in workspace
- `/api/projects/{id}/boards` — boards in project
- `/api/boards/{id}` — full board view (Dapper)
- `/api/boards/{id}/lists` — board lists
- `/api/cards/*` — card CRUD + move
- `/hubs/board` — SignalR: `JoinBoard(boardId)`, `LeaveBoard(boardId)`; server pushes `CardMoved`

## Do NOT touch (unless task says so)

- Auth / refresh token rotation logic
- Workspace RBAC rules (`WorkspaceAccess`, role hierarchy)
- Existing migrations (add new migration only when schema changes)
- `docker-compose.yml` production assumptions (dev-only stack)

## Local dev (quick ref)

```pwsh
docker compose up -d sqlserver redis   # redis optional unless testing SignalR scale-out
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;..." --project src/FlowBoard.API
dotnet user-secrets set "Jwt:SecretKey" "REPLACE_WITH_AT_LEAST_32_RANDOM_CHARS" --project src/FlowBoard.API
# Optional: dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379" --project src/FlowBoard.API
dotnet run --project src/FlowBoard.API
dotnet test
```

Integration tests require **Docker Desktop** running (skipped otherwise). Use `pwsh scripts/run-integration-tests.ps1` or `docker-compose.integration.yml` via `-UseCompose`.

## Next task (pick one per chat)

**Queue:** `tasks/queue.json` — phase **closeout** first (`close-01`…), then Sprint 6–8 features. `pwsh scripts/run-next-task.ps1 -Status`

**Recommended next:** `close-01` — Wire Redis in docker-compose API (completes Sprint 5).

## Closeout phase (Sprints 1–5 gaps)

- [ ] `close-01` … `close-11` — Council + sprint completion fixes
- [ ] `close-docs` — mark Sprints 1–5 fully closed
- [ ] `close-council` — verify closeout report

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
