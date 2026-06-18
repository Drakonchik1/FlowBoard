using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using MediatR;
using DomainRefreshToken = FlowBoard.Domain.Entities.RefreshToken;

namespace FlowBoard.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Token rotation with family-based reuse detection.
/// Flow:
///   1. Look up any token with this hash (active or revoked).
///   2. Not found → 401.
///   3. Found but revoked → REUSE DETECTED → revoke entire family → 401.
///   4. Found but expired → 401 (no family revocation — natural expiry is not reuse).
///   5. Found and active → revoke it → issue replacement in same family → return new pair.
/// Active rotation runs inside a transaction with a row update lock to prevent concurrent
/// double-issuance from the same refresh token.
/// </summary>
public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IJwtService jwtService) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private const string InvalidRefreshTokenMessage = "Invalid or expired refresh token.";

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.Hash(request.Token);

        var existing = await refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (existing is null)
            throw new UnauthorizedException(InvalidRefreshTokenMessage);

        if (existing.IsRevoked)
        {
            await refreshTokenRepository.RevokeEntireFamilyAsync(existing.FamilyId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException(InvalidRefreshTokenMessage);
        }

        if (existing.IsExpired)
            throw new UnauthorizedException(InvalidRefreshTokenMessage);

        return await unitOfWork.ExecuteInTransactionAsync(
            ct => RotateActiveTokenAsync(tokenHash, ct),
            cancellationToken);
    }

    private async Task<AuthResponse> RotateActiveTokenAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var existing = await refreshTokenRepository.GetByHashWithUpdateLockAsync(tokenHash, cancellationToken);

        if (existing is null || existing.IsRevoked || existing.IsExpired)
            throw new UnauthorizedException(InvalidRefreshTokenMessage);

        var user = await userRepository.GetByIdAsync(existing.UserId, cancellationToken);
        if (user is null)
            throw new UnauthorizedException(InvalidRefreshTokenMessage);

        existing.Revoke();

        var refresh = jwtService.GenerateRefreshToken();
        var newToken = DomainRefreshToken.CreateRotated(user.Id, refresh.Hash, existing.FamilyId, refresh.ExpiresAt);
        await refreshTokenRepository.AddAsync(newToken, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email.Value);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refresh.PlainToken,
            ExpiresAt: newToken.ExpiresAt,
            UserId: user.Id,
            Email: user.Email.Value,
            FullName: user.FullName);
    }
}
