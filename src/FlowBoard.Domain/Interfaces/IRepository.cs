using FlowBoard.Domain.Common;

namespace FlowBoard.Domain.Interfaces;

/// <summary>
/// Generic repository abstraction. Defined in Domain so Application can depend on it
/// without depending on Infrastructure. Implemented in Infrastructure by EF Core.
/// </summary>
public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
}
