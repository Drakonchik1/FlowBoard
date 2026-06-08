using MediatR;

namespace FlowBoard.Application.Features.Auth.Commands.Logout;

/// <summary>Revokes the current refresh token, invalidating this session.</summary>
public sealed record LogoutCommand(string RefreshToken) : IRequest<Unit>;
