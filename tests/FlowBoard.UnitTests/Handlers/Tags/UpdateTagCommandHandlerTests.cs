using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Tags.Commands.UpdateTag;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Tags;

public sealed class UpdateTagCommandHandlerTests
{
    private readonly Mock<ITagRepository> _tagRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private UpdateTagCommandHandler CreateHandler() =>
        new(_tagRepo.Object, _workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    private (Workspace workspace, Tag tag) SetupTag(Guid ownerId)
    {
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var tag = Tag.Create(workspace.Id, "Bug", "#FF0000");

        _tagRepo.Setup(r => r.GetByIdAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tag);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        _tagRepo.Setup(r => r.GetByNameInWorkspaceAsync(workspace.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tag?)null);

        return (workspace, tag);
    }

    [Fact]
    public async Task Handle_Member_UpdatesTag()
    {
        var ownerId = Guid.NewGuid();
        var (_, tag) = SetupTag(ownerId);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);

        var result = await CreateHandler().Handle(
            new UpdateTagCommand(tag.Id, "Feature", "#00FF00"), CancellationToken.None);

        Assert.Equal("Feature", result.Name);
        Assert.Equal("#00FF00", result.Color);
        _tagRepo.Verify(r => r.Update(It.IsAny<Tag>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TagMissing_Throws404()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _tagRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tag?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new UpdateTagCommand(Guid.NewGuid(), "Bug", null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Viewer_Throws403()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var (workspace, tag) = SetupTag(ownerId);
        workspace.InviteMember(viewerId, WorkspaceMemberRole.Viewer);
        _currentUser.Setup(c => c.UserId).Returns(viewerId);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new UpdateTagCommand(tag.Id, "Feature", null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsConflict()
    {
        var ownerId = Guid.NewGuid();
        var (workspace, tag) = SetupTag(ownerId);
        var other = Tag.Create(workspace.Id, "Feature", null);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _tagRepo.Setup(r => r.GetByNameInWorkspaceAsync(workspace.Id, "Feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(other);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateHandler().Handle(new UpdateTagCommand(tag.Id, "Feature", null), CancellationToken.None));
    }
}
