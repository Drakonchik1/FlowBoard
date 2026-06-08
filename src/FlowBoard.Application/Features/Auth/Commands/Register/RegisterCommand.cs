using MediatR;

namespace FlowBoard.Application.Features.Auth.Commands.Register;

/// <summary>Creates a new user account and returns authentication tokens.</summary>
public sealed record RegisterCommand(
    string Email,
    string FullName,
    string Password,
    string ConfirmPassword) : IRequest<AuthResponse>;
