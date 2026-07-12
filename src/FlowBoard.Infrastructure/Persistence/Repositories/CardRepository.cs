using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Persistence.Repositories;

internal sealed class CardRepository(FlowBoardDbContext context)
    : Repository<Card>(context), ICardRepository
{
    public async Task<IReadOnlyList<Card>> GetByListAsync(
        Guid boardListId, CancellationToken cancellationToken = default)
    {
        var cards = await DbSet
            .Where(c => c.BoardListId == boardListId)
            .ToListAsync(cancellationToken);

        return cards
            .OrderBy(c => c.Position.Value, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<FractionalIndex?> GetLastPositionAsync(
        Guid boardListId, CancellationToken cancellationToken = default)
    {
        var cards = await DbSet
            .Where(c => c.BoardListId == boardListId)
            .ToListAsync(cancellationToken);

        return cards
            .OrderBy(c => c.Position.Value, StringComparer.Ordinal)
            .LastOrDefault()
            ?.Position;
    }

    public async Task ClearAssigneeForUserInWorkspaceAsync(
        Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        var boardIds = await Context.Boards
            .Where(b => b.WorkspaceId == workspaceId)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var cards = await DbSet
            .Where(c => boardIds.Contains(c.BoardId) && c.AssigneeId == userId)
            .ToListAsync(cancellationToken);

        foreach (var card in cards)
            card.Assign(null, userId);
    }
}
