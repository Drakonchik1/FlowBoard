namespace FlowBoard.Application.Features.Boards;

/// <summary>Summary view of a board (no lists/cards). Used by list endpoints and create/update responses.</summary>
public sealed record BoardDto(
    Guid Id,
    Guid ProjectId,
    Guid WorkspaceId,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt);
