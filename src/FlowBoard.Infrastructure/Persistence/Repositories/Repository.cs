using FlowBoard.Domain.Common;
using FlowBoard.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic EF Core implementation of IRepository. Specialized repositories extend this.
/// Does NOT call SaveChangesAsync — that is the UnitOfWork's responsibility.
/// </summary>
internal class Repository<T>(FlowBoardDbContext context) : IRepository<T>
    where T : Entity
{
    protected readonly FlowBoardDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.FindAsync([id], cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await DbSet.AddAsync(entity, cancellationToken);

    public void Update(T entity) => DbSet.Update(entity);

    public void Delete(T entity) => DbSet.Remove(entity);
}
