using MediatR;

namespace FlowBoard.Application.Features.Auth.Commands.Login;

/// <summary>Authenticates a user by email and password. Returns JWT + refresh token.</summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
