# FlowBoard

[![CI](https://github.com/Drakonchik1/FlowBoard/actions/workflows/ci.yml/badge.svg)](https://github.com/Drakonchik1/FlowBoard/actions/workflows/ci.yml)

ASP.NET Core 10 API portfolio project: **Clean Architecture**, **JWT auth with family-based refresh rotation**, **workspace RBAC**, **boards + cards with Dapper reads**, **SignalR real-time**, **comments + tags + email notifications**, **Hangfire background jobs**, **card activity log**, **optional Redis SignalR backplane**, **277 unit tests**, and **Docker** local + production stacks.

**MVP complete (Sprints 1–8):** authentication, multi-tenant workspaces with RBAC, Kanban boards with fractional-index ordering, SignalR card-move and comment broadcasts, optional Redis scale-out for SignalR, comments/tags/email via Hangfire, card activity log, production compose hardening, write rate limits, CI integration parity, deployment docs (Railway / Azure / self-hosted), and council-verified security remediation.

## Live API

Production health checks (no auth):

| Check | Path |
|---|---|
| Liveness | `GET /health/live` |
| Readiness (SQL + Redis) | `GET /health/ready` |

**Base URL** depends on where you deploy:

| Target | Typical base URL |
|---|---|
| Self-hosted (`docker-compose.prod.yml`) | `http://<host>:8080` (or `API_PORT` from `.env`) |
| [Railway](#railway) | `https://<service-name>.up.railway.app` |
| [Azure Container Apps](#azure-container-apps) | `https://<app-name>.<region>.azurecontainerapps.io` |

Set `AllowedOrigins__0` to your frontend origin and `ForwardedHeaders__Enabled=true` when the API sits behind the platform reverse proxy. EF migrations must be applied once before traffic — see [Production deployment](#production-deployment).

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
| Rate limiting | ASP.NET Core fixed-window limiter — auth (5/min/IP); authenticated writes (60/min/user, IP fallback) |
| Real-time | SignalR (`BoardHub`) — `CardMoved` events; optional Redis backplane for multi-instance scale-out |
| Cache / scale-out | Redis 7 (optional) — SignalR backplane only when connection string configured |
| API docs | Scalar (OpenAPI) with JWT Bearer scheme |
| Background jobs | Hangfire (SQL Server storage) — email send + refresh-token cleanup |
| Tests | xUnit + Moq (277 unit) + TestContainers SQL Server (21 integration) |

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

## Production deployment

FlowBoard requires **SQL Server 2022**, **Redis 7** (with password in prod), and the **API** container. Use `docker-compose.prod.yml` for a single-host stack, or split services across Railway / Azure.

### Production Docker Compose (self-hosted / VPS)

`docker-compose.prod.yml` runs SQL Server and Redis **without host ports** (internal network only), enables Redis AUTH, sets `ASPNETCORE_ENVIRONMENT=Production`, and requires CORS origins.

```pwsh
Copy-Item .env.example .env
# Set MSSQL_SA_PASSWORD, JWT_SECRET_KEY (>= 32 chars), REDIS_PASSWORD, ALLOWED_ORIGINS__0
docker compose -f docker-compose.prod.yml up -d --build
```

- API URL: `http://localhost:8080` (override with `API_PORT` in `.env`)
- Health: `/health/live`, `/health/ready`
- Hangfire dashboard: `/jobs` (JWT + `Admin` policy; set `Hangfire__DashboardAdminEmails__0`)
- OpenAPI/Scalar are **disabled** in Production
- **Single API replica** when using Redis backplane — group eviction is process-local (see SECURITY.md)
- Production startup **fails fast** if EF migrations are pending
- HSTS/HTTPS redirect apply only when `ForwardedHeaders__Enabled=true` or `UseTls=true`

**Migrations** (once per environment — not auto-applied in Production):

```pwsh
$env:ConnectionStrings__DefaultConnection = "Server=<sql-host>,1433;Database=FlowBoard;User Id=sa;Password=<password>;TrustServerCertificate=True;"
dotnet ef database update --project src/FlowBoard.Infrastructure --startup-project src/FlowBoard.API
```

For the prod compose stack, expose SQL temporarily or run the command from the same Docker network.

### Railway

Best for the **API** when SQL Server and Redis are hosted elsewhere (e.g. Azure SQL + Railway Redis).

1. Create a Railway project and connect this repo (or `railway up` from the repo root).
2. Railway uses [`railway.toml`](railway.toml) → builds `src/FlowBoard.API/Dockerfile`.
3. Add a **Redis** plugin (or external Redis) and set `ConnectionStrings__Redis` to `host:port,password=<secret>` (StackExchange.Redis format).
4. Point `ConnectionStrings__DefaultConnection` at **Azure SQL** or another SQL Server instance (Railway has no native SQL Server).
5. Set required variables:

| Variable | Example |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Jwt__SecretKey` | `>= 32 random chars` |
| `AllowedOrigins__0` | `https://your-frontend.example.com` |
| `ForwardedHeaders__Enabled` | `true` |
| `ForwardedHeaders__KnownProxies__0` | Railway/Azure ingress IP (see platform docs) |
| `Smtp__Host` / `Smtp__FromEmail` | Required for assignment emails |
| `Hangfire__DashboardAdminEmails__0` | Admin email for `/jobs` dashboard |
| `Auth__AllowRegistration` | `false` (default in Production) unless open signup intended |

6. Deploy — Railway assigns a public URL like `https://flowboard-api.up.railway.app`. Verify `GET /health/ready`.

### Azure Container Apps

Recommended managed path for this stack (native SQL Server + Redis options):

1. **Azure SQL Database** — create server + database; allow Azure services / your Container Apps subnet.
2. **Azure Cache for Redis** — Basic tier is enough for SignalR backplane; note hostname, port, and access key.
3. **Container Apps** — deploy from the repo Dockerfile (`src/FlowBoard.API/Dockerfile`), port **8080**, ingress **external**.
4. Configure environment variables (same as Railway table), plus:

| Variable | Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | `Server=tcp:<server>.database.windows.net,1433;Database=FlowBoard;User Id=<user>;Password=<password>;Encrypt=True;` |
| `ConnectionStrings__Redis` | `<cache>.redis.cache.windows.net:6380,password=<key>,ssl=True,abortConnect=False` |
| `ForwardedHeaders__Enabled` | `true` |
| `ForwardedHeaders__KnownProxies__0` | Ingress/proxy IP when applicable |
| `Smtp__Host` / `Smtp__FromEmail` | Required for assignment emails |

5. Apply EF migrations against Azure SQL (see command above). Use `Encrypt=True` and `TrustServerCertificate=False` when the server presents a CA-trusted certificate.
6. Public URL: `https://<app-name>.<region>.azurecontainerapps.io` — verify `/health/ready`.

Optional: run the full three-service stack on an **Azure VM** with Docker using `docker-compose.prod.yml` instead of Container Apps.

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
| POST | `/api/cards/{id}/assign` | Assign card to workspace member (or clear assignee); queues assignment email |
| GET | `/api/cards/{id}/activity` | Card activity log (Dapper read — create, move, member invite) |

## Comments API

All endpoints require `Authorization: Bearer <access_token>`.

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/cards/{cardId}/comments` | List comments on a card |
| POST | `/api/cards/{cardId}/comments` | Add comment (triggers `CommentAdded` SignalR push) |
| GET | `/api/comments/{id}` | Get comment |
| PATCH | `/api/comments/{id}` | Update comment (author only) |
| DELETE | `/api/comments/{id}` | Soft-delete comment (author only) |

## Tags API

All endpoints require `Authorization: Bearer <access_token>`.

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/workspaces/{workspaceId}/tags` | List workspace tags |
| POST | `/api/workspaces/{workspaceId}/tags` | Create tag |
| GET | `/api/tags/{id}` | Get tag |
| PATCH | `/api/tags/{id}` | Update tag |
| DELETE | `/api/tags/{id}` | Soft-delete tag (removed from all cards) |
| GET | `/api/cards/{cardId}/tags` | List tags on a card |
| PUT | `/api/cards/{cardId}/tags/{tagId}` | Apply tag to card |
| DELETE | `/api/cards/{cardId}/tags/{tagId}` | Remove tag from card |

## Real-time (SignalR)

Hub: `/hubs/board` — authenticate with JWT via query string: `?access_token=<access_token>`.

| Client → server | Description |
|---|---|
| `JoinBoard(boardId)` | Join group `board:{boardId}` (workspace member required; 404 for outsiders) |
| `LeaveBoard(boardId)` | Leave board group |

| Server → client | Description |
|---|---|
| `CardMoved` | Fired after successful card move — payload mirrors move result |
| `CommentAdded` | Fired after new comment — includes comment id, card id, author, body |

**Scale-out:** set `ConnectionStrings:Redis`, `Redis:ConnectionString`, or `REDIS_CONNECTION` to enable the SignalR Redis backplane. Docker Compose sets `ConnectionStrings__Redis=redis:6379` automatically. Without Redis, SignalR works on a single API instance.

## Background jobs (Hangfire)

- Dashboard: `/jobs` — requires JWT + `Admin` policy (configure `Hangfire:DashboardAdminEmails`)
- **SendEmailJob** — per-message enqueue with automatic retry (3 attempts); replaces in-process email queue
- **CleanupExpiredRefreshTokensJob** — daily recurring scan revokes expired refresh tokens

## Security Properties

- BCrypt passwords, SHA-256 hashed refresh tokens
- Family-based refresh rotation with reuse detection (revoked tokens only)
- Constant-time login (dummy BCrypt verify when user missing)
- Rate limiting on auth endpoints (5/min/IP) and authenticated mutations (60/min/user)
- RFC 7807 Problem Details + traceId on all errors (including JWT challenges)
- Security headers, HSTS in non-Development
- Forwarded headers **disabled by default** — enable via `ForwardedHeaders:Enabled` in production config

## Tests

```pwsh
dotnet test
```

**274 unit tests** — mocked repositories, sub-second feedback.

**17 integration tests** — board workflow, auth refresh concurrency, comments/tags, and CardMoved/CommentAdded notifier paths via TestContainers + SQL Server. Require Docker; skipped automatically when Docker is unavailable.

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
| Closeout | Done | Sprints 1–5 hardened (`close-01`…`close-docs`); council verified (`close-council`) |
| 6 | Done | Comments + Tags + Email (MailKit queue, assignment notifications) |
| 7 | Done | Hangfire background jobs + card activity log |
| 8 | Done | Production compose, CI, write rate limits, Railway/Azure deploy docs |
| **MVP** | **Complete** | All 8 sprints delivered; council verify pending (`s8-council`) |

## License

MIT — see [LICENSE](LICENSE).