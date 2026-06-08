namespace FlowBoard.Application.Features.Workspaces;

public sealed record WorkspaceDto(
    Guid Id,
    string Name,
    string Slug,
    Guid OwnerId,
    DateTime CreatedAt,
    int MemberCount);

public sealed record WorkspaceDetailDto(
    Guid Id,
    string Name,
    string Slug,
    Guid OwnerId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<WorkspaceMemberDto> Members);

public sealed record WorkspaceMemberDto(
    Guid UserId,
    string Role,
    DateTime JoinedAt);