using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using MediatR;
using DomainRefreshToken = FlowBoard.Domain.Entities.RefreshToken;

namespace FlowBoard.Application.Features.Auth.Commands.Register;

/// <summary>
/// Creates a new user, hashes the password, issues JWT + refresh token.
/// Returns 409 with a generic message if the email is already taken (anti-enumeration).
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
            throw new ConflictException(
                "Registration could not be completed. If you already have an account, try logging in.");

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
