using FlowBoard.Domain.Common;

namespace FlowBoard.Domain.Entities;

/// <summary>
/// Represents a hashed refresh token with family-based reuse detection.
/// FamilyId groups all tokens from the same login session — if any token in the family
/// is reused after revocation, the entire family is invalidated.
/// </summary>
public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;  // SHA-256 hash of the actual token
    public Guid FamilyId { get; private set; }               // All rotations of one session share this
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public User User { get; private set; } = null!;

    // Required by EF Core
    private RefreshToken() { }

    /// <summary>Creates a new refresh token for a new login session (new FamilyId).</summary>
    public static RefreshToken CreateNew(Guid userId, string tokenHash, DateTime expiresAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = Guid.NewGuid(),
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>Creates a rotated replacement token, preserving the FamilyId.</summary>
    public static RefreshToken CreateRotated(Guid userId, string tokenHash, Guid familyId, DateTime expiresAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

    public void Revoke() => IsRevoked = true;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>A token is usable only if it has not been explicitly revoked and has not expired.</summary>
    public bool IsActive => !IsRevoked && !IsExpired;
}
