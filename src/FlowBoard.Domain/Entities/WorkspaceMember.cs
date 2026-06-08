using FlowBoard.Domain.Common;

namespace FlowBoard.Domain.Entities;

/// <summary>
/// Junction entity between User and Workspace with a role.
/// Composite key: (WorkspaceId, UserId). A user can be a member of many workspaces with one role each.
/// </summary>
public sealed class WorkspaceMember : Entity
{
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public WorkspaceMemberRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    // Required by EF Core
    private WorkspaceMember() { }

    internal static WorkspaceMember Create(Guid workspaceId, Guid userId, WorkspaceMemberRole role)
    {
        var member = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow,
        };
        return member;
    }

    internal void ChangeRole(WorkspaceMemberRole newRole) => Role = newRole;
}