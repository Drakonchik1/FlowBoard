namespace FlowBoard.Application.Features.Auth.Commands;

/// <summary>Returned by all auth commands that issue tokens.</summary>
public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    Guid UserId,
    string Email,
    string FullName);
