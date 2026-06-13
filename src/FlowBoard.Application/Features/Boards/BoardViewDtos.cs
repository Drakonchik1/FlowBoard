namespace FlowBoard.Application.Features.Boards;

/// <summary>
/// Read model for the full Kanban board, assembled by the Dapper read service.
/// This is intentionally separate from the domain entities: it is a flat, query-shaped projection
/// optimized for the GetBoard endpoint, with no change tracking.
/// </summary>
public sealed record BoardViewDto(
    Guid Id,
    Guid ProjectId,
    Guid WorkspaceId,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<BoardListViewDto> Lists);

public sealed record BoardListViewDto(
    Guid Id,
    string Name,
    string Position,
    IReadOnlyList<CardViewDto> Cards);

public sealed record CardViewDto(
    Guid Id,
    Guid BoardListId,
    string Title,
    string? Description,
    string Position,
    string Priority,
    DateTime CreatedAt,
    DateTime UpdatedAt);
