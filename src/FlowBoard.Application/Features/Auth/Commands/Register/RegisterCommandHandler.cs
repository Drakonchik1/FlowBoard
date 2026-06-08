using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using MediatR;
using DomainRefreshToken = FlowBoard.Domain.Entities.RefreshToken;

namespace FlowBoard.Application.Features.Auth.Commands.Register;

/// <summary>
/// Creates a new user, hashes the password, issues JWT + refresh token.
/// Returns a generic validation error if the email is already taken (no enumeration).
/// </summary>
public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IPasswordService passwordService,
    IJwtService jwtService) : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var emailExists = await userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
        if (emailExists)
            throw new ValidationException([new ValidationError("", "Registration could not be completed. Please try a different email or log in.")]);

        var passwordHash = passwordService.Hash(request.Password);
        var user = User.Create(request.Email, request.FullName, passwordHash);
        await userRepository.AddAsync(user, cancellationToken);

        var refresh = jwtService.GenerateRefreshToken();
        var refreshToken = DomainRefreshToken.CreateNew(user.Id, refresh.Hash, refresh.ExpiresAt);
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email.Value);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refresh.PlainToken,
            ExpiresAt: refresh.ExpiresAt,
            UserId: user.Id,
            Email: user.Email.Value,
            FullName: user.FullName);
    }
}
