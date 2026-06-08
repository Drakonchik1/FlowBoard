using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Persistence.Repositories;

internal sealed class WorkspaceRepository(FlowBoardDbContext context)
    : Repository<Workspace>(context), IWorkspaceRepository
{
    public async Task<Workspace?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(w => w.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Workspace>> GetWorkspacesForUserAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await DbSet
            .Where(w => w.Members.Any(m => m.UserId == userId))
            .Include(w => w.Members)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
}