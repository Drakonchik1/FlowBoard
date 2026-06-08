using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;

namespace FlowBoard.Application.Features.Workspaces;

/// <summary>
/// Centralizes workspace authorization with anti-enumeration semantics.
/// Non-members and insufficient roles receive 404 (not 403) so callers cannot probe workspace IDs.
/// </summary>
internal static class WorkspaceAccess
{
    public static void EnsureMemberOrNotFound(Workspace workspace, Guid userId, Guid workspaceId)
    {
        if (!workspace.HasMember(userId))
            throw new NotFoundException("Workspace", workspaceId);
    }

    public static void EnsureAdminOrNotFound(Workspace workspace, Guid userId, Guid workspaceId)
    {
        if (!workspace.HasMember(userId) || !workspace.IsAdmin(userId))
            throw new NotFoundException("Workspace", workspaceId);
    }

    public static void EnsureOwnerOrNotFound(Workspace workspace, Guid userId, Guid workspaceId)
    {
        if (!workspace.HasMember(userId) || !workspace.IsOwner(userId))
            throw new NotFoundException("Workspace", workspaceId);
    }

    /// <summary>
    /// Self-actions require membership; managing other members requires Admin or Owner.
    /// </summary>
    public static void EnsureCanManageMemberOrNotFound(
        Workspace workspace, Guid actorId, Guid targetUserId, Guid workspaceId)
    {
        if (!workspace.HasMember(actorId))
            throw new NotFoundException("Workspace", workspaceId);

        if (actorId != targetUserId && !workspace.IsAdmin(actorId))
            throw new NotFoundException("Workspace", workspaceId);
    }
}
