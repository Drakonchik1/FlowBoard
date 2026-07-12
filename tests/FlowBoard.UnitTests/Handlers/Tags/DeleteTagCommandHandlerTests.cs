using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Tags.Commands.DeleteTag;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Tags;

public sealed class DeleteTagCommandHandlerTests
{
    private readonly Mock<ITagRepository> _tagRepo = new();
    private readonly Mock<ICardTagRepository> _cardTagRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private DeleteTagCommandHandler CreateHandler() =>
        new(_tagRepo.Object, _cardTagRepo.Object, _workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    private (Workspace workspace, Tag tag) SetupTag(Guid ownerId)
    {
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var tag = Tag.Create(workspace.Id, "Bug", null);

        _tagRepo.Setup(r => r.GetByIdAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tag);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        return (workspace, tag);
    }

    [Fact]
    public async Task Handle_Member_DeletesTagAndRemovesCardLinks()
    {
        var ownerId = Guid.NewGuid();
        var (_, tag) = SetupTag(ownerId);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);

        await CreateHandler().Handle(new DeleteTagCommand(tag.Id), CancellationToken.None);

        _cardTagRepo.Verify(r => r.RemoveAllForTagAsync(tag.Id, It.IsAny<CancellationToken>()), Times.Once);
        _tagRepo.Verify(r => r.Update(It.Is<Tag>(t => t.Id == tag.Id)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var (_, tag) = SetupTag(Guid.NewGuid());
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeleteTagCommand(tag.Id), CancellationToken.None));
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
            CreateHandler().Handle(new DeleteTagCommand(tag.Id), CancellationToken.None));
    }
}
