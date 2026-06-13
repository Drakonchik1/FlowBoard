using FlowBoard.Domain.Entities;
using FlowBoard.Domain.ValueObjects;

namespace FlowBoard.Domain.Interfaces;

public interface IBoardListRepository : IRepository<BoardList>
{
    /// <summary>Returns all non-deleted lists on a board, ordered by position.</summary>
    Task<IReadOnlyList<BoardList>> GetByBoardAsync(Guid boardId, CancellationToken cancellationToken = default);

    /// <summary>Returns the position of the last (highest) list on a board, or null when the board has none.</summary>
    Task<FractionalIndex?> GetLastPositionAsync(Guid boardId, CancellationToken cancellationToken = default);
}
