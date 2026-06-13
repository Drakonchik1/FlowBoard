using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Persistence.Repositories;

internal sealed class BoardListRepository(FlowBoardDbContext context)
    : Repository<BoardList>(context), IBoardListRepository
{
    public async Task<IReadOnlyList<BoardList>> GetByBoardAsync(
        Guid boardId, CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(l => l.BoardId == boardId)
            .OrderBy(l => l.Position)
            .ToListAsync(cancellationToken);

    public async Task<FractionalIndex?> GetLastPositionAsync(
        Guid boardId, CancellationToken cancellationToken = default)
    {
        var last = await DbSet
            .Where(l => l.BoardId == boardId)
            .OrderByDescending(l => l.Position)
            .FirstOrDefaultAsync(cancellationToken);

        return last?.Position;
    }
}
