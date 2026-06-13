using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Persistence.Repositories;

internal sealed class ProjectRepository(FlowBoardDbContext context)
    : Repository<Project>(context), IProjectRepository
{
    public async Task<IReadOnlyList<Project>> GetByWorkspaceAsync(
        Guid workspaceId, CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(p => p.WorkspaceId == workspaceId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
}
