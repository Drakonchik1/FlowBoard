namespace FlowBoard.Application.Features.BoardLists;

public sealed record BoardListDto(
    Guid Id,
    Guid BoardId,
    string Name,
    string Position,
    DateTime CreatedAt,
    DateTime UpdatedAt);
