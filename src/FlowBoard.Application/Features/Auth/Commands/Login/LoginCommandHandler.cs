using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using MediatR;
using DomainRefreshToken = FlowBoard.Domain.Entities.RefreshToken;

namespace FlowBoard.Application.Features.Auth.Commands.Login;

/// <summary>
/// Verifies credentials and issues a new JWT + refresh token.
/// Returns a generic 401 on both "user not found" and "wrong password" to prevent user enumeration.
/// </summary>
public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IPasswordService passwordService,
    IJwtService jwtService) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Always verify to avoid timing side-channel revealing whether the email exists
        var hashToVerify = user?.PasswordHash ?? passwordService.TimingSafeDummyHash;
        var passwordValid = passwordService.Verify(request.Password, hashToVerify);

        if (user is null || !passwordValid)
            throw new UnauthorizedException("Invalid email or password.");

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
