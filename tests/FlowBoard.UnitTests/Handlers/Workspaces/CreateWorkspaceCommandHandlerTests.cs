using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Workspaces.Commands.CreateWorkspace;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Workspaces;

public sealed class CreateWorkspaceCommandHandlerTests
{
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private CreateWorkspaceCommandHandler CreateHandler() =>
        new(_workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Authenticated_CreatesWorkspaceWithUserAsOwner()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(userId);
        _workspaceRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateHandler().Handle(new CreateWorkspaceCommand("Acme Corp", null), CancellationToken.None);

        Assert.Equal("Acme Corp", result.Name);
        Assert.Equal(userId, result.OwnerId);
        Assert.Equal(1, result.MemberCount); // creator is auto-added as Owner

        _workspaceRepo.Verify(r => r.AddAsync(
            It.Is<Workspace>(w => w.OwnerId == userId && w.Name == "Acme Corp"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Anonymous_Throws401()
    {
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new CreateWorkspaceCommand("Acme", null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SlugTaken_ThrowsValidationException()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _workspaceRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateHandler().Handle(new CreateWorkspaceCommand("Acme", "acme"), CancellationToken.None));

        _workspaceRepo.Verify(r => r.AddAsync(It.IsAny<Workspace>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoSlugProvided_DerivesFromName()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(userId);
        _workspaceRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateHandler().Handle(new CreateWorkspaceCommand("My Big Project!", null), CancellationToken.None);

        Assert.Equal("my-big-project", result.Slug);
    }
}