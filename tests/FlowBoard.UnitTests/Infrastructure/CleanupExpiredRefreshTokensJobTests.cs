using FlowBoard.Domain.Interfaces;
using FlowBoard.Infrastructure.Hangfire.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowBoard.UnitTests.Infrastructure;

public sealed class CleanupExpiredRefreshTokensJobTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTokensRevoked_SavesChanges()
    {
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        refreshTokenRepository
            .Setup(r => r.RevokeExpiredAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var unitOfWork = new Mock<IUnitOfWork>();

        var job = new CleanupExpiredRefreshTokensJob(
            refreshTokenRepository.Object,
            unitOfWork.Object,
            NullLogger<CleanupExpiredRefreshTokensJob>.Instance);

        await job.ExecuteAsync();

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNothingToRevoke_SkipsSaveChanges()
    {
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        refreshTokenRepository
            .Setup(r => r.RevokeExpiredAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var unitOfWork = new Mock<IUnitOfWork>();

        var job = new CleanupExpiredRefreshTokensJob(
            refreshTokenRepository.Object,
            unitOfWork.Object,
            NullLogger<CleanupExpiredRefreshTokensJob>.Instance);

        await job.ExecuteAsync();

        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
