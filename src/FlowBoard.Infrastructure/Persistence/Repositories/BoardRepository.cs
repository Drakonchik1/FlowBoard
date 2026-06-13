using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Persistence.Repositories;

internal sealed class BoardRepository(FlowBoardDbContext context)
    : Repository<Board>(context), IBoardRepository
{
    public async Task<IReadOnlyList<Board>> GetByProjectAsync(
        Guid projectId, CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(b => b.ProjectId == projectId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
}
