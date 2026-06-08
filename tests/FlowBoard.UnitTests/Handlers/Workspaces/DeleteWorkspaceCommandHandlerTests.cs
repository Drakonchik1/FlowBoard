using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Workspaces.Commands.DeleteWorkspace;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Workspaces;

public sealed class DeleteWorkspaceCommandHandlerTests
{
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private DeleteWorkspaceCommandHandler CreateHandler() =>
        new(_workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Owner_SoftDeletesWorkspace()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId);

        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await CreateHandler().Handle(new DeleteWorkspaceCommand(workspace.Id), CancellationToken.None);

        Assert.True(workspace.IsDeleted);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Admin_Returns404ToPreventEnumeration()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId);
        workspace.InviteMember(adminId, WorkspaceMemberRole.Admin);

        _currentUser.Setup(c => c.UserId).Returns(adminId);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeleteWorkspaceCommand(workspace.Id), CancellationToken.None));
    }
}
