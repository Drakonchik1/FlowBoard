using FlowBoard.Application.Features.Boards;

namespace FlowBoard.Application.Common.Interfaces;

/// <summary>
/// Read-side service for boards. Backed by Dapper raw SQL rather than EF Core so the hot
/// "load the whole board" path skips change-tracking and entity materialization overhead.
/// This is the first genuine read/write split in the codebase (writes still go through EF + repositories).
/// </summary>
public interface IBoardReadService
{
    /// <summary>
    /// Loads a board with its lists and cards (each ordered by position), or null when the board
    /// does not exist or has been soft-deleted.
    /// </summary>
    Task<BoardViewDto?> GetBoardAsync(Guid boardId, CancellationToken cancellationToken = default);
}
