using FlowBoard.Application.Common.Security;
using FlowBoard.Application.Features.Auth.Commands.Logout;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Auth;

public sealed class LogoutCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private LogoutCommandHandler CreateHandler() =>
        new(_refreshTokenRepo.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_ActiveToken_RevokesAndSaves()
    {
        var token = RefreshToken.CreateNew(Guid.NewGuid(), TokenHasher.Hash("plain"), DateTime.UtcNow.AddDays(7));
        _refreshTokenRepo.Setup(r => r.GetActiveByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        await CreateHandler().Handle(new LogoutCommand("plain"), CancellationToken.None);

        Assert.True(token.IsRevoked);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TokenNotFound_DoesNotThrowOrSave()
    {
        // Logout must be idempotent and silent, never reveals whether a token existed
        _refreshTokenRepo.Setup(r => r.GetActiveByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var ex = await Record.ExceptionAsync(() =>
            CreateHandler().Handle(new LogoutCommand("any"), CancellationToken.None));

        Assert.Null(ex);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}