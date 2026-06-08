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
}
