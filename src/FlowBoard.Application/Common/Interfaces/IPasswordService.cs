namespace FlowBoard.Application.Common.Interfaces;

/// <summary>
/// BCrypt password hashing and constant-time verification.
/// Defined in Application, implemented in Infrastructure with BCrypt.Net-Next.
/// </summary>
public interface IPasswordService
{
    /// <summary>Returns a BCrypt hash of the plaintext password.</summary>
    string Hash(string password);

    /// <summary>
    /// Verifies a plaintext password against a stored BCrypt hash.
    /// Uses constant-time comparison to prevent timing attacks.
    /// </summary>
    bool Verify(string password, string hash);

    /// <summary>
    /// Pre-computed BCrypt hash used when the user record does not exist,
    /// so login always performs a verify and avoids user-enumeration via timing.
    /// </summary>
    string TimingSafeDummyHash { get; }
}
