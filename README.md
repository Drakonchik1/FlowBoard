# FlowBoard

[![CI](https://github.com/Drakonchik1/FlowBoard/actions/workflows/ci.yml/badge.svg)](https://github.com/Drakonchik1/FlowBoard/actions/workflows/ci.yml)

Real-time multi-user project management SaaS — a full-stack flagship portfolio project. **Shipped today:** Clean Architecture, MediatR pipelines, JWT auth with family-based refresh-token rotation, EF Core, Workspaces + RBAC, Docker, CI.

**Planned (roadmap):** SignalR, Redis, Hangfire, Dapper read model, TestContainers, production deploy.

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
| API docs | Scalar (OpenAPI) |
| Tests | xUnit + Moq |
| CI | GitHub Actions (build + test) |

## Project Structure

```
src/
├── FlowBoard.Domain/          Entities, value objects, domain events, repository interfaces. Zero NuGet dependencies.
├── FlowBoard.Application/     CQRS commands/queries/handlers, validators, MediatR behaviors. Depends on Domain only.
├── FlowBoard.Infrastructure/  EF Core, repositories, JWT, BCrypt, migrations. Depends on Application + Domain.
└── FlowBoard.API/             Controllers, middleware, security headers, health checks, Program.cs.

tests/
└── FlowBoard.UnitTests/       Handler tests with mocked repositories + domain entity tests.
```

## Running Locally

### Prerequisites

- .NET 10 SDK
- Docker Desktop (only for SQL Server in Docker)
- `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef`

### 1. Set local secrets

The repo intentionally ships **no** credentials. Set them via `dotnet user-secrets`:

```pwsh
dotnet user-secrets init --project src/FlowBoard.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=FlowBoard;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;" --project src/FlowBoard.API
dotnet user-secrets set "Jwt:SecretKey" "REPLACE_WITH_AT_LEAST_32_RANDOM_CHARS" --project src/FlowBoard.API
```

Alternatively, copy `src/FlowBoard.API/secrets.example.json` to `secrets.json` (git-ignored) and fill in values.

The startup guard rejects JWT keys shorter than 32 characters with a clear error — do not hand-edit `appsettings.json` to "fix" that error; it lives in user-secrets by design.

### 2. Start SQL Server

```pwsh
docker compose up -d sqlserver
```

### 3. Run

```pwsh
dotnet run --project src/FlowBoard.API
```

Migrations apply automatically in Development. Browse `/scalar/v1` for the API explorer.

Use `src/FlowBoard.API/FlowBoard.API.http` for a ready-made register → login → workspace flow (VS Code / Rider REST Client).

## Running with Docker Compose

Copy `.env.example` to `.env` and replace the values:

```pwsh
Copy-Item .env.example .env
# Edit .env
docker compose up
```

Compose substitutes `${MSSQL_SA_PASSWORD}` and `${JWT_SECRET_KEY}` from `.env`. The API container runs migrations and starts on `http://localhost:5000`.

> Docker Compose is for **local development only** — it runs the API in Development mode with permissive CORS and exposed SQL port.

## Tests

```pwsh
dotnet test
```

Unit tests use mocked dependencies — no Docker required, sub-second feedback. TestContainers integration tests start in Sprint 3.

## Auth Flow

`POST /api/auth/register` — creates a user, returns access token + refresh token + user details.
`POST /api/auth/login` — verifies credentials, returns the same payload.
`POST /api/auth/refresh` — rotates the refresh token. Reuse of a previously-rotated token triggers full family revocation (all sessions on that login chain are invalidated).
`POST /api/auth/logout` — revokes the current refresh token. Idempotent.

All four endpoints are protected by a 5-requests-per-minute fixed-window rate limit per IP.

## Security Properties

- **Passwords** never stored in plaintext (BCrypt, work factor 12).
- **Refresh tokens** never stored in plaintext (SHA-256 hash; only the hash hits the DB).
- **Token rotation** with family-based reuse detection: a leaked-and-rotated token presented again invalidates the entire login session.
- **Generic 401** on login failure and **generic message** on duplicate registration prevent user enumeration.
- **Rate limiting** on every auth endpoint defends against brute force and credential stuffing.
- **JWT key length** enforced at startup (>= 32 chars).
- **CORS** wildcard only in Development; production reads `AllowedOrigins` from configuration.
- **Security headers** on every response: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`.
- **Forwarded headers** middleware for correct client IP / scheme behind a reverse proxy.
- **HSTS** enabled in non-development environments.
- **Trace ID** included in every error response for log correlation.

See [SECURITY.md](SECURITY.md) for the vulnerability reporting policy.

## Health Checks

- `GET /health/live` — process liveness, no dependencies checked.
- `GET /health/ready` — readiness, includes DB connectivity.

## Workspaces API

All endpoints require a valid JWT (`[Authorize]`).

| Method | Endpoint | Description | Required role |
|--------|----------|-------------|---------------|
| `POST` | `/api/workspaces` | Create workspace (caller becomes Owner) | Authenticated |
| `GET` | `/api/workspaces` | List workspaces the current user belongs to | Authenticated |
| `GET` | `/api/workspaces/{id}` | Get workspace details + members | Member |
| `PATCH` | `/api/workspaces/{id}` | Update name | Admin+ |
| `DELETE` | `/api/workspaces/{id}` | Soft-delete | Owner |
| `POST` | `/api/workspaces/{id}/members` | Invite member by `userId` | Admin+ |
| `DELETE` | `/api/workspaces/{id}/members/{userId}` | Remove member or leave workspace | Admin+ (or self) |
| `PATCH` | `/api/workspaces/{id}/members/{userId}` | Change member role | Admin+ |

Roles: **Owner** > **Admin** > **Member** > **Viewer**. Authorization is enforced in the `Workspace` domain aggregate (`EnsureMember`, `EnsureAdmin`, `EnsureOwner`, `EnsureCanWrite`). Non-members receive **404** (not 403) on workspace lookup to prevent ID enumeration.

## Sprint Status

**Completed:** Sprint 1 (Auth), Sprint 2 (Workspaces + RBAC), Docker local stack, GitHub Actions CI.

**Remaining sprints:**

3. Boards + Cards + Dapper read model + TestContainers
4. Real-time SignalR
5. Redis caching
6. Comments + Tags + async email
7. Hangfire migration + activity log
8. Production deploy + hardened ops config