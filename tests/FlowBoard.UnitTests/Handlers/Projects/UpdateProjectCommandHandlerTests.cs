using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Projects.Commands.UpdateProject;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Projects;

public sealed class UpdateProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private UpdateProjectCommandHandler CreateHandler() =>
        new(_projectRepo.Object, _workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    private (Workspace workspace, Project project) SetupProject(Guid ownerId)
    {
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var project = Project.Create(workspace.Id, "Apollo", "desc");

        _projectRepo.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        return (workspace, project);
    }

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var (_, project) = SetupProject(Guid.NewGuid());
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new UpdateProjectCommand(project.Id, "Renamed", null), CancellationToken.None));

        _projectRepo.Verify(r => r.Update(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Viewer_Throws403()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var (workspace, project) = SetupProject(ownerId);
        workspace.InviteMember(viewerId, WorkspaceMemberRole.Viewer);
        _currentUser.Setup(c => c.UserId).Returns(viewerId);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(
                new UpdateProjectCommand(project.Id, "Renamed", null), CancellationToken.None));
    }
}
