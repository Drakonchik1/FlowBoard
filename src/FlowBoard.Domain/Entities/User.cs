using FlowBoard.Domain.Common;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.ValueObjects;

namespace FlowBoard.Domain.Entities;

/// <summary>
/// Aggregate root for user authentication and identity.
/// All mutations go through factory/command methods — no public setters.
/// </summary>
public sealed class User : Entity
{
    public Email Email { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string? AvatarUrl { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<RefreshToken> _refreshTokens = [];
    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // Required by EF Core
    private User() { }

    /// <summary>Creates and validates a new user. Raises UserRegisteredEvent.</summary>
    public static User Create(string email, string fullName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Full name cannot be empty.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash cannot be empty.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = Email.Create(email),
            FullName = fullName.Trim(),
            PasswordHash = passwordHash,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.Raise(new UserRegisteredEvent(user.Id, user.Email.Value));
        return user;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
