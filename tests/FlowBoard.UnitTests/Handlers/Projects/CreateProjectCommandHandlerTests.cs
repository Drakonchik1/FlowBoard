using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Projects.Commands.CreateProject;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Projects;

public sealed class CreateProjectCommandHandlerTests
{
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private CreateProjectCommandHandler CreateHandler() =>
        new(_workspaceRepo.Object, _projectRepo.Object, _unitOfWork.Object, _currentUser.Object);

    private Workspace SetupWorkspace(Guid ownerId)
    {
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        _workspaceRepo
            .Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        return workspace;
    }

    [Fact]
    public async Task Handle_Member_CreatesProject()
    {
        var ownerId = Guid.NewGuid();
        var workspace = SetupWorkspace(ownerId);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);

        var result = await CreateHandler().Handle(
            new CreateProjectCommand(workspace.Id, "Apollo", "desc"), CancellationToken.None);

        Assert.Equal("Apollo", result.Name);
        Assert.Equal(workspace.Id, result.WorkspaceId);
        _projectRepo.Verify(r => r.AddAsync(
            It.Is<Project>(p => p.Name == "Apollo" && p.WorkspaceId == workspace.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var workspace = SetupWorkspace(Guid.NewGuid());
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid()); // not a member

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new CreateProjectCommand(workspace.Id, "Apollo", null), CancellationToken.None));

        _projectRepo.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Viewer_Throws403()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var workspace = SetupWorkspace(ownerId);
        workspace.InviteMember(viewerId, WorkspaceMemberRole.Viewer);
        _currentUser.Setup(c => c.UserId).Returns(viewerId);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new CreateProjectCommand(workspace.Id, "Apollo", null), CancellationToken.None));
    }
}
