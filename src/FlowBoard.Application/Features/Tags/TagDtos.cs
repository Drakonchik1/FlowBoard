namespace FlowBoard.Application.Features.Tags;

public sealed record TagDto(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string? Color,
    DateTime CreatedAt,
    DateTime UpdatedAt);
