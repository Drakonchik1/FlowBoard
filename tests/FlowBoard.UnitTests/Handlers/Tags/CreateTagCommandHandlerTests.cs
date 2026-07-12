using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Tags.Commands.CreateTag;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Tags;

public sealed class CreateTagCommandHandlerTests
{
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<ITagRepository> _tagRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private CreateTagCommandHandler CreateHandler() =>
        new(_workspaceRepo.Object, _tagRepo.Object, _unitOfWork.Object, _currentUser.Object);

    private Workspace SetupWorkspace(Guid ownerId)
    {
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        _workspaceRepo
            .Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        return workspace;
    }

    [Fact]
    public async Task Handle_Member_CreatesTag()
    {
        var ownerId = Guid.NewGuid();
        var workspace = SetupWorkspace(ownerId);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _tagRepo.Setup(r => r.GetByNameInWorkspaceAsync(
                workspace.Id, "Bug", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tag?)null);

        var result = await CreateHandler().Handle(
            new CreateTagCommand(workspace.Id, "Bug", "#FF0000"), CancellationToken.None);

        Assert.Equal("Bug", result.Name);
        Assert.Equal("#FF0000", result.Color);
        Assert.Equal(workspace.Id, result.WorkspaceId);
        _tagRepo.Verify(r => r.AddAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var workspace = SetupWorkspace(Guid.NewGuid());
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new CreateTagCommand(workspace.Id, "Bug", null), CancellationToken.None));

        _tagRepo.Verify(r => r.AddAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()), Times.Never);
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
            CreateHandler().Handle(new CreateTagCommand(workspace.Id, "Bug", null), CancellationToken.None));
    }
}
