using FlowBoard.Domain.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FlowBoard.Infrastructure.Hangfire.Jobs;

/// <summary>
/// Hangfire recurring job that revokes refresh tokens past expiry but never explicitly revoked.
/// </summary>
internal sealed class CleanupExpiredRefreshTokensJob(
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    ILogger<CleanupExpiredRefreshTokensJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        var revokedCount = await refreshTokenRepository.RevokeExpiredAsync(CancellationToken.None);
        if (revokedCount == 0)
            return;

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        logger.LogInformation("Revoked {RevokedCount} expired refresh tokens", revokedCount);
    }
}
