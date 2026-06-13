using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Persistence.Repositories;

internal sealed class CardRepository(FlowBoardDbContext context)
    : Repository<Card>(context), ICardRepository
{
    public async Task<IReadOnlyList<Card>> GetByListAsync(
        Guid boardListId, CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(c => c.BoardListId == boardListId)
            .OrderBy(c => c.Position)
            .ToListAsync(cancellationToken);

    public async Task<FractionalIndex?> GetLastPositionAsync(
        Guid boardListId, CancellationToken cancellationToken = default)
    {
        var last = await DbSet
            .Where(c => c.BoardListId == boardListId)
            .OrderByDescending(c => c.Position)
            .FirstOrDefaultAsync(cancellationToken);

        return last?.Position;
    }
}
