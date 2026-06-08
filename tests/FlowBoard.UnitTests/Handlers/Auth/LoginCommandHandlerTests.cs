using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Auth.Commands.Login;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Auth;

public sealed class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<IJwtService> _jwtService = new();

    private static RefreshTokenResult AnyRefresh() =>
        new("plain_refresh", "hashed_refresh", DateTime.UtcNow.AddDays(7));

    private LoginCommandHandler CreateHandler() =>
        new(_userRepo.Object, _refreshTokenRepo.Object, _unitOfWork.Object,
            _passwordService.Object, _jwtService.Object);

    private static User CreateTestUser(string email = "user@example.com", string passwordHash = "hash") =>
        User.Create(email, "Test User", passwordHash);

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAuthResponse()
    {
        var user = CreateTestUser("user@example.com", "hashed");
        var command = new LoginCommand("user@example.com", "Password123!");

        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordService.Setup(p => p.Verify(command.Password, user.PasswordHash))
            .Returns(true);
        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns(AnyRefresh());
        _jwtService.Setup(j => j.GenerateAccessToken(user.Id, user.Email.Value))
            .Returns("access_token");
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("plain_refresh", result.RefreshToken);
        Assert.Equal(user.Id, result.UserId);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedException()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new LoginCommand("ghost@example.com", "pass"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorizedException()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordService.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Same exception as "user not found" — prevents user enumeration
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new LoginCommand("user@example.com", "wrong"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WrongPassword_NeverCallsSaveChanges()
    {
        var user = CreateTestUser();
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordService.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new LoginCommand("user@example.com", "wrong"), CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
