namespace FlowBoard.Application.Features.Cards;

public sealed record CardDto(
    Guid Id,
    Guid BoardListId,
    Guid BoardId,
    string Title,
    string? Description,
    string Position,
    string Priority,
    DateTime CreatedAt,
    DateTime UpdatedAt);
