using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
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

    private ChangeMemberRoleCommandHandler CreateHandler() =>
        new(_workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Admin_ChangesMemberRole()
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

        var result = await CreateHandler().Handle(
            new ChangeMemberRoleCommand(workspace.Id, memberId, WorkspaceMemberRole.Viewer),
            CancellationToken.None);

        Assert.Equal("Viewer", result.Role);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Member_Throws403()
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
            CreateHandler().Handle(
                new ChangeMemberRoleCommand(workspace.Id, memberB, WorkspaceMemberRole.Viewer),
                CancellationToken.None));
    }
}
