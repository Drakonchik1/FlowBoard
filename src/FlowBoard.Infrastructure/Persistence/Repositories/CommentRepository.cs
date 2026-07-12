using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Persistence.Repositories;

internal sealed class CommentRepository(FlowBoardDbContext context)
    : Repository<Comment>(context), ICommentRepository
{
    public async Task<IReadOnlyList<Comment>> GetByCardIdAsync(
        Guid cardId, CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(c => c.CardId == cardId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
}
