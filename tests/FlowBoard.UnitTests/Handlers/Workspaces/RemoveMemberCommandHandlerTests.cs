using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Workspaces.Commands.RemoveMember;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Workspaces;

public sealed class RemoveMemberCommandHandlerTests
{
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IBoardRealtimeGroupEvictor> _groupEvictor = new();

    private RemoveMemberCommandHandler CreateHandler() =>
        new(_workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object, _groupEvictor.Object);

    [Fact]
    public async Task Handle_MemberLeaves_RemovesSelf()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId);
        workspace.InviteMember(memberId, WorkspaceMemberRole.Member);

        _currentUser.Setup(c => c.UserId).Returns(memberId);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await CreateHandler().Handle(new RemoveMemberCommand(workspace.Id, memberId), CancellationToken.None);

        Assert.DoesNotContain(workspace.Members, m => m.UserId == memberId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _groupEvictor.Verify(
            e => e.EvictUserFromBoardGroupsAsync(memberId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AdminRemovesOther_RemovesMember()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId);
        workspace.InviteMember(memberId, WorkspaceMemberRole.Member);

        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await CreateHandler().Handle(new RemoveMemberCommand(workspace.Id, memberId), CancellationToken.None);

        Assert.DoesNotContain(workspace.Members, m => m.UserId == memberId);
        _groupEvictor.Verify(
            e => e.EvictUserFromBoardGroupsAsync(memberId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NonAdminRemovesOther_Returns404()
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
            CreateHandler().Handle(new RemoveMemberCommand(workspace.Id, otherMemberId), CancellationToken.None));
    }
}
