using FlowBoard.Domain.Entities;

namespace FlowBoard.Domain.Interfaces;

public interface ICommentRepository : IRepository<Comment>
{
    /// <summary>Returns all non-deleted comments on a card, ordered by creation time.</summary>
    Task<IReadOnlyList<Comment>> GetByCardIdAsync(Guid cardId, CancellationToken cancellationToken = default);
}
