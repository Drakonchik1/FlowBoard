using MediatR;

namespace FlowBoard.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Rotates a refresh token: validates the current token, revokes it,
/// issues a new token in the same family, and returns a new JWT pair.
/// </summary>
public sealed record RefreshTokenCommand(string Token) : IRequest<AuthResponse>;
