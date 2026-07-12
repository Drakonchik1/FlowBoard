using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Persistence.Repositories;

internal sealed class TagRepository(FlowBoardDbContext context)
    : Repository<Tag>(context), ITagRepository
{
    public async Task<IReadOnlyList<Tag>> GetByWorkspaceIdAsync(
        Guid workspaceId, CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(t => t.WorkspaceId == workspaceId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

    public async Task<Tag?> GetByNameInWorkspaceAsync(
        Guid workspaceId, string name, CancellationToken cancellationToken = default) =>
        await DbSet
            .FirstOrDefaultAsync(
                t => t.WorkspaceId == workspaceId && t.Name == name.Trim(),
                cancellationToken);
}
