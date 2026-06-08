using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Application.Features.Auth.Commands.RefreshToken;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Auth;

/// <summary>
/// Tests for the most security-critical handler in the project: family-based reuse detection.
/// The reuse detection scenario (a revoked token presented again revokes the entire family) is
/// the whole reason family_id rotation exists. These tests lock that behavior in.
/// </summary>
public sealed class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJwtService> _jwtService = new();

    private RefreshTokenCommandHandler CreateHandler() =>
        new(_refreshTokenRepo.Object, _userRepo.Object, _unitOfWork.Object, _jwtService.Object);

    private static User CreateTestUser() =>
        User.Create("user@example.com", "Test User", "hash");

    [Fact]
    public async Task Handle_TokenNeverExisted_Throws401AndRevokesNothing()
    {
        _refreshTokenRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new RefreshTokenCommand("fabricated"), CancellationToken.None));

        _refreshTokenRepo.Verify(r => r.RevokeEntireFamilyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RevokedTokenPresented_RevokesEntireFamilyAndThrows()
    {
        var token = RefreshToken.CreateNew(Guid.NewGuid(), TokenHasher.Hash("any"), DateTime.UtcNow.AddDays(7));
        token.Revoke();

        _refreshTokenRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new RefreshTokenCommand("any"), CancellationToken.None));

        _refreshTokenRepo.Verify(r => r.RevokeEntireFamilyAsync(token.FamilyId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExpiredTokenPresented_RevokesFamilyAndThrows()
    {
        var expired = RefreshToken.CreateNew(Guid.NewGuid(), TokenHasher.Hash("any"), DateTime.UtcNow.AddDays(-1));

        _refreshTokenRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expired);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new RefreshTokenCommand("any"), CancellationToken.None));

        _refreshTokenRepo.Verify(r => r.RevokeEntireFamilyAsync(expired.FamilyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidActiveToken_IssuesRotatedReplacementInSameFamily()
    {
        var user = CreateTestUser();
        var existing = RefreshToken.CreateNew(user.Id, TokenHasher.Hash("any"), DateTime.UtcNow.AddDays(7));
        var originalFamily = existing.FamilyId;

        _refreshTokenRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jwtService.Setup(j => j.GenerateRefreshToken())
            .Returns(new RefreshTokenResult("new_plain", "new_hash", DateTime.UtcNow.AddDays(7)));
        _jwtService.Setup(j => j.GenerateAccessToken(user.Id, user.Email.Value))
            .Returns("new_access");

        var result = await CreateHandler().Handle(new RefreshTokenCommand("any"), CancellationToken.None);

        Assert.Equal("new_access", result.AccessToken);
        Assert.Equal("new_plain", result.RefreshToken);
        Assert.True(existing.IsRevoked, "consumed token must be revoked");

        _refreshTokenRepo.Verify(r => r.AddAsync(
            It.Is<RefreshToken>(rt => rt.FamilyId == originalFamily && rt.UserId == user.Id),
            It.IsAny<CancellationToken>()), Times.Once);

        _refreshTokenRepo.Verify(r => r.RevokeEntireFamilyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserNotFoundForActiveToken_Throws401()
    {
        var existing = RefreshToken.CreateNew(Guid.NewGuid(), TokenHasher.Hash("any"), DateTime.UtcNow.AddDays(7));

        _refreshTokenRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new RefreshTokenCommand("any"), CancellationToken.None));
    }
}