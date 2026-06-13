using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;

namespace FlowBoard.Application.Common.Security;

/// <summary>
/// Authorization helpers for resources owned by a workspace (projects, boards, lists, cards).
///
/// Non-members get a 404 for the requested resource (anti-enumeration: they cannot tell an
/// existing resource they lack access to apart from one that does not exist). Members who lack
/// write access (Viewers) get a 403 on mutations, since they already know the resource exists.
/// </summary>
internal static class ResourceGuard
{
    /// <summary>Throws NotFound for the named resource when the workspace is missing or the user is not a member.</summary>
    public static void EnsureMember(Workspace? workspace, Guid userId, string resourceName, Guid resourceId)
    {
        if (workspace is null || !workspace.HasMember(userId))
            throw new NotFoundException(resourceName, resourceId);
    }

    /// <summary>Throws Forbidden when the (already-verified) member has read-only (Viewer) access.</summary>
    public static void EnsureCanWrite(Workspace workspace, Guid userId)
    {
        if (!workspace.CanWrite(userId))
            throw new ForbiddenException("This action requires write access. Viewer role is read-only.");
    }
}
