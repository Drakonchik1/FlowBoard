# FlowBoard

[![CI](https://github.com/Drakonchik1/FlowBoard/actions/workflows/ci.yml/badge.svg)](https://github.com/Drakonchik1/FlowBoard/actions/workflows/ci.yml)

ASP.NET Core 10 API portfolio project: **Clean Architecture**, **JWT auth with family-based refresh rotation**, **workspace RBAC**, **boards + cards with Dapper reads**, **SignalR real-time**, **optional Redis SignalR backplane**, **206 tests**, and **Docker** local dev stack.

**Delivered (Sprints 1–5):** authentication, multi-tenant workspaces with RBAC, Kanban boards with fractional-index ordering, SignalR card-move broadcasts, and optional Redis scale-out for SignalR. Next: Sprint 6 (Comments + Tags + Email).

## Tech Stack

| Layer | Choice |
|---|---|
| Backend | ASP.NET Core 10 |
| Architecture | Clean Architecture (Domain / Application / Infrastructure / API) |
| Mediator | MediatR 14 with pipeline behaviors (Logging, Validation) |
| Validation | FluentValidation 12 |
| ORM | EF Core 10 (Code-First, Fluent API) |
| Database | SQL Server 2022 |
| Auth | JWT (15 min access) + Refresh tokens (7 days, family-based rotation) |
| Hashing | BCrypt (work factor 12), SHA-256 for refresh-token storage |
| Rate limiting | ASP.NET Core fixed-window limiter on auth endpoints |
| Real-time | SignalR (`BoardHub`) — `CardMoved` events; optional Redis backplane for multi-instance scale-out |
| Cache / scale-out | Redis 7 (optional) — SignalR backplane only when connection string configured |
| API docs | Scalar (OpenAPI) with JWT Bearer scheme |
| Tests | xUnit + Moq (198 unit) + TestContainers SQL Server (8 integration) |

## Project Structure

```
src/
  FlowBoard.Domain/          Entities, value objects, domain events. Zero NuGet dependencies.
  FlowBoard.Application/     CQRS commands/queries, validators, MediatR behaviors.
  FlowBoard.Infrastructure/  EF Core, repositories, JWT, BCrypt, migrations.
  FlowBoard.API/             Controllers, SignalR hubs, middleware, security headers, health checks.

tests/
  FlowBoard.UnitTests/       Handler and domain tests with mocked repositories.
  FlowBoard.IntegrationTests/ Board workflow tests against real SQL Server (TestContainers).
```

## Running Locally

### Prerequisites

- .NET 10 SDK
- Docker Desktop (for SQL Server)
- Optional: `dotnet tool install --global dotnet-ef` (manual migrations only)

### 1. Set local secrets

```pwsh
dotnet user-secrets init --project src/FlowBoard.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=FlowBoard;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;" --project src/FlowBoard.API
dotnet user-secrets set "Jwt:SecretKey" "REPLACE_WITH_AT_LEAST_32_RANDOM_CHARS" --project src/FlowBoard.API
# Optional — SignalR Redis backplane (requires `docker compose up -d redis`):
# dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379" --project src/FlowBoard.API
```

### 2. Start SQL Server (and optional Redis)

```pwsh
docker compose up -d sqlserver
# Optional — for SignalR backplane / multi-instance testing:
docker compose up -d redis
```

### 3. Run

```pwsh
dotnet run --project src/FlowBoard.API
```

- Local URL: `http://localhost:5248` (see `launchSettings.json`)
- API explorer: `/scalar/v1`
- Migrations apply automatically in Development

## Docker Compose (dev stack)

```pwsh
Copy-Item .env.example .env
# Edit .env with strong passwords
docker compose up
```

- API URL: `http://localhost:5000`
- **Redis** on port 6379 — compose sets `ConnectionStrings__Redis=redis:6379` on the API (SignalR backplane enabled)
- **Development mode** in compose (OpenAPI, auto-migrate, permissive CORS) — not for production

## Auth API

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/register` | Create account, return tokens |
| POST | `/api/auth/login` | Login (generic 401 on failure) |
| POST | `/api/auth/refresh` | Rotate refresh token |
| POST | `/api/auth/logout` | Revoke refresh token (idempotent) |

All auth endpoints: **5 requests/minute per IP**.

## Workspaces API

All endpoints require `Authorization: Bearer <access_token>`.

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/workspaces` | Create workspace (caller becomes Owner) |
| GET | `/api/workspaces` | List my workspaces |
| GET | `/api/workspaces/{id}` | Get workspace + members (members only; 404 for outsiders) |
| PATCH | `/api/workspaces/{id}` | Rename workspace — **name only** (Admin+) |
| DELETE | `/api/workspaces/{id}` | Soft-delete (Owner only) |
| POST | `/api/workspaces/{id}/members` | Invite by **UserId** + role (Admin+) |
| DELETE | `/api/workspaces/{id}/members/{userId}` | Remove member or leave workspace |
| PATCH | `/api/workspaces/{id}/members/{userId}` | Change role (Admin+) |

**Roles:** Owner > Admin > Member > Viewer

**Invite payload:** `{ "userId": "<guid>", "role": "Member" }`

Non-members receive **404** (not 403) on all workspace endpoints to prevent ID enumeration.

## Boards API

All endpoints require `Authorization: Bearer <access_token>`.

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/projects/{projectId}/boards` | List boards in project |
| POST | `/api/projects/{projectId}/boards` | Create board |
| GET | `/api/boards/{id}` | Full board view (Dapper — lists + cards in position order) |
| POST | `/api/boards/{boardId}/lists` | Create list |
| PATCH | `/api/lists/{id}` | Rename list |
| POST | `/api/lists/{id}/move` | Reorder list |
| DELETE | `/api/lists/{id}` | Soft-delete list |
| POST | `/api/lists/{listId}/cards` | Create card |
| GET | `/api/cards/{id}` | Get card |
| PATCH | `/api/cards/{id}` | Update card |
| DELETE | `/api/cards/{id}` | Soft-delete card |
| POST | `/api/cards/{id}/move` | Move card within or across lists (fractional index) |

## Real-time (SignalR)

Hub: `/hubs/board` — authenticate with JWT via query string: `?access_token=<access_token>`.

| Client → server | Description |
|---|---|
| `JoinBoard(boardId)` | Join group `board:{boardId}` (workspace member required; 404 for outsiders) |
| `LeaveBoard(boardId)` | Leave board group |

| Server → client | Description |
|---|---|
| `CardMoved` | Fired after successful card move — payload mirrors move result |

**Scale-out:** set `ConnectionStrings:Redis`, `Redis:ConnectionString`, or `REDIS_CONNECTION` to enable the SignalR Redis backplane. Docker Compose sets `ConnectionStrings__Redis=redis:6379` automatically. Without Redis, SignalR works on a single API instance.

## Security Properties

- BCrypt passwords, SHA-256 hashed refresh tokens
- Family-based refresh rotation with reuse detection (revoked tokens only)
- Constant-time login (dummy BCrypt verify when user missing)
- Rate limiting on auth endpoints
- RFC 7807 Problem Details + traceId on all errors (including JWT challenges)
- Security headers, HSTS in non-Development
- Forwarded headers **disabled by default** — enable via `ForwardedHeaders:Enabled` in production config

## Tests

```pwsh
dotnet test
```

**198 unit tests** — mocked repositories, sub-second feedback.

**8 integration tests** — board workflow, auth refresh concurrency, and CardMoved notifier path via TestContainers + SQL Server. Require Docker; skipped automatically when Docker is unavailable.

```pwsh
# Integration tests only (checks Docker, uses TestContainers by default)
pwsh scripts/run-integration-tests.ps1

# Reuse a compose SQL Server on port 1434 (faster repeated runs)
pwsh scripts/run-integration-tests.ps1 -UseCompose
```

## Roadmap

| Sprint | Status | Deliverable |
|---|---|---|
| 1 | Done | Auth |
| 2 | Done | Workspaces + RBAC |
| 3 | Done | Boards + Cards + Dapper + TestContainers |
| 4 | Done | SignalR real-time (`BoardHub`, `CardMoved`, group eviction) |
| 5 | Done | Redis SignalR backplane (optional) + health check |
| Closeout | Done | Sprints 1–5 hardened (`close-01`…`close-docs`); council verify pending |
| 6 | Planned | Comments + Tags + Email |
| 7 | Planned | Hangfire + activity log |
| 8 | Planned | Production deploy + CI hardening |

## License

MIT — see [LICENSE](LICENSE).