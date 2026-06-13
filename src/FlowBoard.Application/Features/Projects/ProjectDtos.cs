namespace FlowBoard.Application.Features.Projects;

public sealed record ProjectDto(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);
