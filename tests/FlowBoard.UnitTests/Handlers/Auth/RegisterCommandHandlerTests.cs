using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Auth.Commands.Register;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Auth;

public sealed class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<IJwtService> _jwtService = new();

    private static RefreshTokenResult AnyRefresh() =>
        new("plain_token", "hashed_token", DateTime.UtcNow.AddDays(7));

    private RegisterCommandHandler CreateHandler() =>
        new(_userRepo.Object, _refreshTokenRepo.Object, _unitOfWork.Object,
            _passwordService.Object, _jwtService.Object);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAuthResponse()
    {
        var command = new RegisterCommand("test@example.com", "Test User", "Password123!", "Password123!");

        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordService.Setup(p => p.Hash(command.Password)).Returns("hashed_password");
        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns(AnyRefresh());
        _jwtService.Setup(j => j.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>())).Returns("access_token");
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("plain_token", result.RefreshToken);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.FullName);

        _userRepo.Verify(r => r.AddAsync(It.Is<User>(u => u.FullName == "Test User"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ThrowsConflictException()
    {
        var command = new RegisterCommand("existing@example.com", "Test User", "Password123!", "Password123!");

        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().Handle(command, CancellationToken.None));

        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_HashesPasswordBeforeStoringUser()
    {
        var command = new RegisterCommand("test@example.com", "Test User", "Password123!", "Password123!");
        const string hashedPassword = "bcrypt_hashed_result";

        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _passwordService.Setup(p => p.Hash(command.Password)).Returns(hashedPassword);
        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns(AnyRefresh());
        _jwtService.Setup(j => j.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>())).Returns("jwt");
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await CreateHandler().Handle(command, CancellationToken.None);

        _userRepo.Verify(r => r.AddAsync(
            It.Is<User>(u => u.PasswordHash == hashedPassword),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
