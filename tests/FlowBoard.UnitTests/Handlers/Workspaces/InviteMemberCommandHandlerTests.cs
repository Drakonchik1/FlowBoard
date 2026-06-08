using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Workspaces;
using FlowBoard.Application.Features.Workspaces.Commands.InviteMember;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Workspaces;

public sealed class InviteMemberCommandHandlerTests
{
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private InviteMemberCommandHandler CreateHandler() =>
        new(_workspaceRepo.Object, _userRepo.Object, _unitOfWork.Object, _currentUser.Object);

    private static User TestUser(string email = "invitee@example.com") =>
        User.Create(email, "Invitee", "hash");

    private static Workspace WorkspaceWithOwner(Guid ownerId) =>
        Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId);

    [Fact]
    public async Task Handle_OwnerInvitesUser_AddsMember()
    {
        var ownerId = Guid.NewGuid();
        var workspace = WorkspaceWithOwner(ownerId);
        var invitee = TestUser();

        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _userRepo.Setup(r => r.GetByIdAsync(invitee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invitee);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        var result = await CreateHandler().Handle(
            new InviteMemberCommand(workspace.Id, invitee.Id, WorkspaceRole.Member),
            CancellationToken.None);

        Assert.Equal(invitee.Id, result.UserId);
        Assert.Equal("Member", result.Role);
        Assert.Equal(2, workspace.Members.Count); // owner + invitee
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonAdminInvites_ThrowsNotFoundToPreventEnumeration()
    {
        var ownerId = Guid.NewGuid();
        var nonAdminId = Guid.NewGuid();
        var workspace = WorkspaceWithOwner(ownerId);
        workspace.InviteMember(nonAdminId, WorkspaceMemberRole.Member);
        var invitee = TestUser();

        _currentUser.Setup(c => c.UserId).Returns(nonAdminId);
        _userRepo.Setup(r => r.GetByIdAsync(invitee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invitee);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new InviteMemberCommand(workspace.Id, invitee.Id, WorkspaceRole.Member),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InviteeNotFound_Throws404()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new InviteMemberCommand(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WorkspaceNotFound_Throws404()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestUser());
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new InviteMemberCommand(Guid.NewGuid(), Guid.NewGuid(), WorkspaceRole.Member),
                CancellationToken.None));
    }
}