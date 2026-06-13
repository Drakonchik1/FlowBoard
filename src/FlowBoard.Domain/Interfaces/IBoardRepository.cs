using FlowBoard.Domain.Entities;

namespace FlowBoard.Domain.Interfaces;

public interface IBoardRepository : IRepository<Board>
{
    /// <summary>Returns all non-deleted boards in a project, newest first.</summary>
    Task<IReadOnlyList<Board>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}
