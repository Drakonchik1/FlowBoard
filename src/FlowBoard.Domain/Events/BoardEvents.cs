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

/// <summary>
/// Raised when a comment is added to a card. Sprint 6 broadcasts this over SignalR to the board group.
/// </summary>
public sealed record CommentAddedEvent(
    Guid CommentId,
    Guid CardId,
    Guid BoardId,
    Guid AuthorId,
    string Body,
    DateTime CreatedAt) : IDomainEvent;

/// <summary>
/// Raised when a card is assigned to a workspace member. Sprint 6 queues an email notification.
/// </summary>
public sealed record CardAssignedEvent(
    Guid CardId,
    Guid BoardId,
    Guid AssigneeId,
    Guid AssignedByUserId,
    string CardTitle) : IDomainEvent;
