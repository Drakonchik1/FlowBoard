using FlowBoard.Domain.Entities;

namespace FlowBoard.Domain.Interfaces;

/// <summary>Refresh token queries for the auth flow.</summary>
public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>Finds an active (non-revoked, non-expired) token by its SHA-256 hash.</summary>
    Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds any token (active or revoked) by its SHA-256 hash.
    /// Used during reuse detection: if a revoked token is presented, we know the family is compromised.
    /// </summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a token by hash while holding an update lock until the surrounding transaction completes.
    /// Serializes concurrent refresh rotation on the same token.
    /// </summary>
    Task<RefreshToken?> GetByHashWithUpdateLockAsync(
        string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all tokens belonging to the same family.
    /// Called on token reuse detection to invalidate a potentially compromised session.
    /// </summary>
    Task RevokeEntireFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);
}
