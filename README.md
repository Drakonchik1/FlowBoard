# FlowBoard

[![CI](https://github.com/Drakonchik1/FlowBoard/actions/workflows/ci.yml/badge.svg)](https://github.com/Drakonchik1/FlowBoard/actions/workflows/ci.yml)

ASP.NET Core 10 API portfolio project: **Clean Architecture**, **JWT auth with family-based refresh rotation**, **workspace RBAC**, **104 unit tests**, and **Docker** local dev stack.

**Current scope (Sprints 1–2):** user authentication and multi-tenant workspaces with role-based access control.

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
| API docs | Scalar (OpenAPI) with JWT Bearer scheme |
| Tests | xUnit + Moq (104 unit tests) |

## Project Structure

```
src/
  FlowBoard.Domain/          Entities, value objects, domain events. Zero NuGet dependencies.
  FlowBoard.Application/     CQRS commands/queries, validators, MediatR behaviors.
  FlowBoard.Infrastructure/  EF Core, repositories, JWT, BCrypt, migrations.
  FlowBoard.API/             Controllers, middleware, security headers, health checks.

tests/
  FlowBoard.UnitTests/       Handler and domain tests with mocked repositories.
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
```

### 2. Start SQL Server

```pwsh
docker compose up -d sqlserver
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

**104 unit tests** — mocked repositories, sub-second feedback. TestContainers integration tests planned for Sprint 3.

## Roadmap

| Sprint | Status | Deliverable |
|---|---|---|
| 1 | Done | Auth |
| 2 | Done | Workspaces + RBAC |
| 3 | Planned | Boards + Cards + Dapper + TestContainers |
| 4 | Planned | SignalR real-time |
| 5 | Planned | Redis caching |
| 6 | Planned | Comments + Tags |
| 7 | Planned | Hangfire + activity log |
| 8 | Planned | Production CI/CD hardening |

## License

MIT — see [LICENSE](LICENSE).