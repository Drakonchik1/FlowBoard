namespace FlowBoard.Domain.Interfaces;

/// <summary>
/// Wraps DbContext.SaveChangesAsync and dispatches collected domain events after persisting.
/// Ensures events are only published once the data is safely committed.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all tracked changes, then dispatches domain events raised by entities.
    /// Returns the number of state entries written to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a database transaction on the same context as
    /// <see cref="SaveChangesAsync"/>. Commits on success; rolls back on any exception.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
