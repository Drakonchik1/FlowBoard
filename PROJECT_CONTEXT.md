# FlowBoard — compact context (AI: read this OR HANDOFF, not full README)

## Stack
ASP.NET Core 10 · Clean Architecture · MediatR · FluentValidation · EF Core 10 · SQL Server · Dapper (reads) · JWT + refresh rotation · SignalR · optional Redis backplane · xUnit + Moq · TestContainers (integration)

## Layers
```
src/FlowBoard.Domain/         — entities, events, no NuGet
src/FlowBoard.Application/    — CQRS handlers, validators, behaviors
src/FlowBoard.Infrastructure/ — EF, repos, JWT, migrations, email queue
src/FlowBoard.API/            — controllers, hubs, middleware
tests/FlowBoard.UnitTests/
tests/FlowBoard.IntegrationTests/
```

## Conventions
- Handler folder: `Features/{Area}/Commands|Queries/{Name}/`
- Domain events: `EventHandlers/` as `INotificationHandler<DomainEventNotification>`
- Controllers: thin, `ISender.Send` only
- Workspace outsider -> 404 (not 403)
- Tests: unit in `Handlers/{Area}/`, integration needs Docker

## State files (read order)
1. `HANDOFF.md` — last session + next task
2. `tasks/queue.json` — pending work
3. `SPRINT.md` — sprint checklist + decisions

## Do not (unless asked)
- Postgres migration · deploy target change · drive-by refactors · README/SPRINT edits outside doc tasks

## Tests
277 unit + 21 integration (July 2026). Run: `dotnet test` from repo root.
