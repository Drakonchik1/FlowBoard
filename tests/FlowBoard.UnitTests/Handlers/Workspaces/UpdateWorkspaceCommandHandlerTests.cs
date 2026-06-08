using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Workspaces.Commands.UpdateWorkspace;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Workspaces;

public sealed class UpdateWorkspaceCommandHandlerTests
{
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private UpdateWorkspaceCommandHandler CreateHandler() =>
        new(_workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Admin_RenamesWorkspace()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId);

        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        var result = await CreateHandler().Handle(
            new UpdateWorkspaceCommand(workspace.Id, "Acme Corp"), CancellationToken.None);

        Assert.Equal("Acme Corp", result.Name);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonAdmin_Returns404ToPreventEnumeration()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId);
        workspace.InviteMember(memberId, WorkspaceMemberRole.Member);

        _currentUser.Setup(c => c.UserId).Returns(memberId);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new UpdateWorkspaceCommand(workspace.Id, "Hacked"), CancellationToken.None));
    }
}
