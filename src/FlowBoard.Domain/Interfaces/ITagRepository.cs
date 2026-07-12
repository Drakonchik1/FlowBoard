using FlowBoard.Domain.Entities;

namespace FlowBoard.Domain.Interfaces;

public interface ITagRepository : IRepository<Tag>
{
    Task<IReadOnlyList<Tag>> GetByWorkspaceIdAsync(
        Guid workspaceId, CancellationToken cancellationToken = default);

    Task<Tag?> GetByNameInWorkspaceAsync(
        Guid workspaceId, string name, CancellationToken cancellationToken = default);
}
