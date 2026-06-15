using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Persistence.Repositories;

internal sealed class BoardListRepository(FlowBoardDbContext context)
    : Repository<BoardList>(context), IBoardListRepository
{
    public async Task<IReadOnlyList<BoardList>> GetByBoardAsync(
        Guid boardId, CancellationToken cancellationToken = default)
    {
        var lists = await DbSet
            .Where(l => l.BoardId == boardId)
            .ToListAsync(cancellationToken);

        return lists
            .OrderBy(l => l.Position.Value, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<FractionalIndex?> GetLastPositionAsync(
        Guid boardId, CancellationToken cancellationToken = default)
    {
        var lists = await DbSet
            .Where(l => l.BoardId == boardId)
            .ToListAsync(cancellationToken);

        return lists
            .OrderBy(l => l.Position.Value, StringComparer.Ordinal)
            .LastOrDefault()
            ?.Position;
    }
}
