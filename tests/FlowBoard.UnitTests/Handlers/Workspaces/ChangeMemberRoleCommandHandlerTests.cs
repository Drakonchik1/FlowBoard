using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Workspaces;
using FlowBoard.Application.Features.Workspaces.Commands.ChangeMemberRole;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Workspaces;

public sealed class ChangeMemberRoleCommandHandlerTests
{
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IBoardRealtimeGroupEvictor> _groupEvictor = new();

    private ChangeMemberRoleCommandHandler CreateHandler() =>
        new(_workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object, _groupEvictor.Object);

    [Fact]
    public async Task Handle_Admin_ChangesRole()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId);
        workspace.InviteMember(memberId, WorkspaceMemberRole.Member);

        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        var result = await CreateHandler().Handle(
            new ChangeMemberRoleCommand(workspace.Id, memberId, WorkspaceRole.Admin),
            CancellationToken.None);

        Assert.Equal("Admin", result.Role);
        _groupEvictor.Verify(
            e => e.EvictUserFromBoardGroupsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_DowngradeToViewer_EvictsBoardGroups()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId);
        workspace.InviteMember(memberId, WorkspaceMemberRole.Member);

        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await CreateHandler().Handle(
            new ChangeMemberRoleCommand(workspace.Id, memberId, WorkspaceRole.Viewer),
            CancellationToken.None);

        _groupEvictor.Verify(
            e => e.EvictUserFromBoardGroupsAsync(memberId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NonAdmin_Returns404ToPreventEnumeration()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var otherMemberId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId);
        workspace.InviteMember(memberId, WorkspaceMemberRole.Member);
        workspace.InviteMember(otherMemberId, WorkspaceMemberRole.Member);

        _currentUser.Setup(c => c.UserId).Returns(memberId);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new ChangeMemberRoleCommand(workspace.Id, otherMemberId, WorkspaceRole.Admin),
                CancellationToken.None));
    }
}
