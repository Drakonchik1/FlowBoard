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
///   2. Not found → expired or fabricated → 401.
///   3. Found but revoked/expired → REUSE DETECTED → revoke entire family → 401.
///   4. Found and active → revoke it → issue replacement in same family → return new pair.
/// Step 3 is the security property: a stolen token that has already been rotated triggers
/// full family invalidation, protecting the legitimate user.
/// </summary>
public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IJwtService jwtService) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.Hash(request.Token);

        // Look up any token — active OR revoked
        var existing = await refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (existing is null)
            throw new UnauthorizedException("Refresh token is invalid or has expired.");

        if (!existing.IsActive)
        {
            // Token exists but is already revoked or expired.
            // This is a reuse attempt — the token was already consumed and rotated.
            // Revoke the entire family to protect all sessions derived from this login.
            await refreshTokenRepository.RevokeEntireFamilyAsync(existing.FamilyId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Refresh token has already been used. All sessions have been invalidated for security.");
        }

        // Global query filter on User already excludes soft-deleted accounts — null means deleted or not found
        var user = await userRepository.GetByIdAsync(existing.UserId, cancellationToken);
        if (user is null)
            throw new UnauthorizedException("User account not found.");

        // Revoke the consumed token and issue a replacement in the same family
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
