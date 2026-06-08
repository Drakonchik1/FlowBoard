namespace FlowBoard.Application.Common.Interfaces;

/// <summary>
/// Generates and validates JWT access tokens and raw refresh tokens.
/// Defined in Application so handlers stay decoupled from JWT library specifics.
/// Implemented in Infrastructure.
/// </summary>
public interface IJwtService
{
    /// <summary>Generates a signed JWT access token valid for the configured duration.</summary>
    string GenerateAccessToken(Guid userId, string email);

    /// <summary>
    /// Generates a cryptographically secure refresh token.
    /// Returns the plaintext token (sent to client), its SHA-256 hash (stored in DB),
    /// and the absolute UTC expiration time (config-driven).
    /// </summary>
    RefreshTokenResult GenerateRefreshToken();
}

/// <summary>Output of <see cref="IJwtService.GenerateRefreshToken"/>.</summary>
public sealed record RefreshTokenResult(string PlainToken, string Hash, DateTime ExpiresAt);
