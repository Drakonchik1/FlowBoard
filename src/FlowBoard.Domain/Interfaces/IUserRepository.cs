using FlowBoard.Domain.Entities;

namespace FlowBoard.Domain.Interfaces;

/// <summary>User-specific repository queries beyond the generic IRepository contract.</summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>Returns the user with the given email or null if not found.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Returns true if any non-deleted user has the given email.</summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
}
