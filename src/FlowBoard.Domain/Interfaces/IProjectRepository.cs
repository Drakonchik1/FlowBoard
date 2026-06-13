using FlowBoard.Domain.Entities;

namespace FlowBoard.Domain.Interfaces;

public interface IProjectRepository : IRepository<Project>
{
    /// <summary>Returns all non-deleted projects in a workspace, newest first.</summary>
    Task<IReadOnlyList<Project>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}
