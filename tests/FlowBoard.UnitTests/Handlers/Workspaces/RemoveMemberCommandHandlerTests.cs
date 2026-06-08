using FlowBoard.Application.Common.Exceptions;
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

    private RemoveMemberCommandHandler CreateHandler() =>
        new(_workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_SelfLeave_RemovesMember()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId);
        workspace.InviteMember(memberId, WorkspaceMemberRole.Member);
        _currentUser.Setup(c => c.UserId).Returns(memberId);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await CreateHandler().Handle(new RemoveMemberCommand(workspace.Id, memberId), CancellationToken.None);

        Assert.False(workspace.HasMember(memberId));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminRemovesOther_RemovesMember()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId);
        workspace.InviteMember(adminId, WorkspaceMemberRole.Admin);
        workspace.InviteMember(memberId, WorkspaceMemberRole.Member);
        _currentUser.Setup(c => c.UserId).Returns(adminId);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await CreateHandler().Handle(new RemoveMemberCommand(workspace.Id, memberId), CancellationToken.None);

        Assert.False(workspace.HasMember(memberId));
    }

    [Fact]
    public async Task Handle_MemberRemovesOther_Throws403()
    {
        var ownerId = Guid.NewGuid();
        var memberA = Guid.NewGuid();
        var memberB = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId);
        workspace.InviteMember(memberA, WorkspaceMemberRole.Member);
        workspace.InviteMember(memberB, WorkspaceMemberRole.Member);
        _currentUser.Setup(c => c.UserId).Returns(memberA);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new RemoveMemberCommand(workspace.Id, memberB), CancellationToken.None));
    }
}
