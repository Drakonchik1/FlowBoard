using FlowBoard.Domain.Entities;
using FlowBoard.Domain.ValueObjects;

namespace FlowBoard.Domain.Interfaces;

public interface ICardRepository : IRepository<Card>
{
    /// <summary>Returns all non-deleted cards in a list, ordered by position.</summary>
    Task<IReadOnlyList<Card>> GetByListAsync(Guid boardListId, CancellationToken cancellationToken = default);

    /// <summary>Returns the position of the last (highest) card in a list, or null when the list is empty.</summary>
    Task<FractionalIndex?> GetLastPositionAsync(Guid boardListId, CancellationToken cancellationToken = default);
}
