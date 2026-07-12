using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Tags.Commands.RemoveTagFromCard;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Tags;

public sealed class RemoveTagFromCardCommandHandlerTests
{
    private readonly Mock<ICardRepository> _cardRepo = new();
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<ITagRepository> _tagRepo = new();
    private readonly Mock<ICardTagRepository> _cardTagRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private RemoveTagFromCardCommandHandler CreateHandler() =>
        new(_cardRepo.Object, _boardRepo.Object, _tagRepo.Object, _cardTagRepo.Object,
            _workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    private (Workspace workspace, Card card, Tag tag, CardTag cardTag) Setup(Guid ownerId)
    {
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");
        var list = BoardList.Create(board.Id, "To Do", FractionalIndex.Start());
        var card = Card.Create(board.Id, list.Id, "Card", FractionalIndex.Start());
        var tag = Tag.Create(workspace.Id, "Bug", null);
        var cardTag = CardTag.Create(card.Id, tag.Id);

        _cardRepo.Setup(r => r.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        _tagRepo.Setup(r => r.GetByIdAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tag);
        _cardTagRepo.Setup(r => r.GetByCardAndTagAsync(card.Id, tag.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cardTag);

        return (workspace, card, tag, cardTag);
    }

    [Fact]
    public async Task Handle_Member_RemovesTag()
    {
        var ownerId = Guid.NewGuid();
        var (_, card, tag, cardTag) = Setup(ownerId);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);

        await CreateHandler().Handle(new RemoveTagFromCardCommand(card.Id, tag.Id), CancellationToken.None);

        _cardTagRepo.Verify(r => r.Delete(cardTag), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotApplied_Throws404()
    {
        var ownerId = Guid.NewGuid();
        var (_, card, tag, _) = Setup(ownerId);
        _cardTagRepo.Setup(r => r.GetByCardAndTagAsync(card.Id, tag.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CardTag?)null);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new RemoveTagFromCardCommand(card.Id, tag.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Viewer_Throws403()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var (workspace, card, tag, _) = Setup(ownerId);
        workspace.InviteMember(viewerId, WorkspaceMemberRole.Viewer);
        _currentUser.Setup(c => c.UserId).Returns(viewerId);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new RemoveTagFromCardCommand(card.Id, tag.Id), CancellationToken.None));
    }
}
