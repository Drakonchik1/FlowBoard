using FlowBoard.Domain.Entities;

namespace FlowBoard.Application.Features.Workspaces;

/// <summary>Wire-level workspace role enum. Maps to domain <see cref="WorkspaceMemberRole"/> at the application boundary.</summary>
public enum WorkspaceRole
{
    Owner,
    Admin,
    Member,
    Viewer
}

internal static class WorkspaceRoleMapper
{
    public static WorkspaceMemberRole ToDomain(WorkspaceRole role) => role switch
    {
        WorkspaceRole.Owner => WorkspaceMemberRole.Owner,
        WorkspaceRole.Admin => WorkspaceMemberRole.Admin,
        WorkspaceRole.Member => WorkspaceMemberRole.Member,
        WorkspaceRole.Viewer => WorkspaceMemberRole.Viewer,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown workspace role.")
    };

    public static WorkspaceRole FromDomain(WorkspaceMemberRole role) => role switch
    {
        WorkspaceMemberRole.Owner => WorkspaceRole.Owner,
        WorkspaceMemberRole.Admin => WorkspaceRole.Admin,
        WorkspaceMemberRole.Member => WorkspaceRole.Member,
        WorkspaceMemberRole.Viewer => WorkspaceRole.Viewer,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown workspace role.")
    };
}
