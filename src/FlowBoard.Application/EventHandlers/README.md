# EventHandlers

MediatR **notification** handlers for domain events published after commit (`DomainEventNotification`).

## Convention

| Kind | Location |
|------|----------|
| Commands / queries (`IRequest`) | `Features/{Area}/Commands/` or `Features/{Area}/Queries/` |
| Domain-event notifications (`INotification`) | `EventHandlers/` |

Handlers here bridge `IDomainEvent` payloads to side effects (e.g. SignalR via `IBoardRealtimeNotifier`). They must not fail the originating HTTP command after a successful commit — catch, log, and swallow infrastructure errors.
