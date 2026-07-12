using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Persistence.Repositories;

internal sealed class CardTagRepository(FlowBoardDbContext context)
    : Repository<CardTag>(context), ICardTagRepository
{
    public async Task<CardTag?> GetByCardAndTagAsync(
        Guid cardId, Guid tagId, CancellationToken cancellationToken = default) =>
        await DbSet
            .FirstOrDefaultAsync(ct => ct.CardId == cardId && ct.TagId == tagId, cancellationToken);

    public async Task<IReadOnlyList<Tag>> GetTagsForCardAsync(
        Guid cardId, CancellationToken cancellationToken = default) =>
        await Context.Set<Tag>()
            .Where(t => Context.Set<CardTag>().Any(ct => ct.CardId == cardId && ct.TagId == t.Id))
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

    public async Task RemoveAllForTagAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        var rows = await DbSet.Where(ct => ct.TagId == tagId).ToListAsync(cancellationToken);
        DbSet.RemoveRange(rows);
    }
}
