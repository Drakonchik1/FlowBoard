namespace FlowBoard.Domain.Events;

public sealed record ProjectCreatedEvent(Guid ProjectId, Guid WorkspaceId, string Name) : IDomainEvent;

public sealed record BoardCreatedEvent(Guid BoardId, Guid ProjectId, Guid WorkspaceId) : IDomainEvent;

public sealed record CardCreatedEvent(Guid CardId, Guid BoardId, Guid BoardListId) : IDomainEvent;

/// <summary>
/// Raised when a card changes list and/or position. Sprint 4 broadcasts this over SignalR to the
/// board group so other connected clients update in real time.
/// </summary>
public sealed record CardMovedEvent(
    Guid CardId,
    Guid BoardId,
    Guid FromListId,
    Guid ToListId,
    string Position) : IDomainEvent;
