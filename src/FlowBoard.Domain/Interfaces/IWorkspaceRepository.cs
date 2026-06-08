using FlowBoard.Domain.Entities;

namespace FlowBoard.Domain.Interfaces;

public interface IWorkspaceRepository : IRepository<Workspace>
{
    /// <summary>Returns the workspace with its full Members collection loaded, or null.</summary>
    Task<Workspace?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns true if any non-deleted workspace has the given slug.</summary>
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Returns all workspaces the user is a member of.</summary>
    Task<IReadOnlyList<Workspace>> GetWorkspacesForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
