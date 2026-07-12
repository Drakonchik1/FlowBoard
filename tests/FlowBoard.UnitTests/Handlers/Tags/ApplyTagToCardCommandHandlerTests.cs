using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Tags.Commands.ApplyTagToCard;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Tags;

public sealed class ApplyTagToCardCommandHandlerTests
{
    private readonly Mock<ICardRepository> _cardRepo = new();
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<ITagRepository> _tagRepo = new();
    private readonly Mock<ICardTagRepository> _cardTagRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private ApplyTagToCardCommandHandler CreateHandler() =>
        new(_cardRepo.Object, _boardRepo.Object, _tagRepo.Object, _cardTagRepo.Object,
            _workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    private (Workspace workspace, Board board, Card card, Tag tag) SetupCardAndTag(Guid ownerId)
    {
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");
        var list = BoardList.Create(board.Id, "To Do", FractionalIndex.Start());
        var card = Card.Create(board.Id, list.Id, "Card", FractionalIndex.Start());
        var tag = Tag.Create(workspace.Id, "Bug", "#FF0000");

        _cardRepo.Setup(r => r.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        _tagRepo.Setup(r => r.GetByIdAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tag);
        _cardTagRepo.Setup(r => r.GetByCardAndTagAsync(card.Id, tag.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CardTag?)null);

        return (workspace, board, card, tag);
    }

    [Fact]
    public async Task Handle_Member_AppliesTag()
    {
        var ownerId = Guid.NewGuid();
        var (_, _, card, tag) = SetupCardAndTag(ownerId);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);

        var result = await CreateHandler().Handle(
            new ApplyTagToCardCommand(card.Id, tag.Id), CancellationToken.None);

        Assert.Equal(tag.Id, result.Id);
        _cardTagRepo.Verify(r => r.AddAsync(It.IsAny<CardTag>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyApplied_IsIdempotent()
    {
        var ownerId = Guid.NewGuid();
        var (_, _, card, tag) = SetupCardAndTag(ownerId);
        var existing = CardTag.Create(card.Id, tag.Id);
        _cardTagRepo.Setup(r => r.GetByCardAndTagAsync(card.Id, tag.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);

        await CreateHandler().Handle(new ApplyTagToCardCommand(card.Id, tag.Id), CancellationToken.None);

        _cardTagRepo.Verify(r => r.AddAsync(It.IsAny<CardTag>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TagFromOtherWorkspace_Throws404()
    {
        var ownerId = Guid.NewGuid();
        var (_, _, card, tag) = SetupCardAndTag(ownerId);
        var otherTag = Tag.Create(Guid.NewGuid(), "Other", null);
        _tagRepo.Setup(r => r.GetByIdAsync(otherTag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(otherTag);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new ApplyTagToCardCommand(card.Id, otherTag.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var ownerId = Guid.NewGuid();
        var (_, _, card, tag) = SetupCardAndTag(ownerId);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new ApplyTagToCardCommand(card.Id, tag.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Viewer_Throws403()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var (workspace, _, card, tag) = SetupCardAndTag(ownerId);
        workspace.InviteMember(viewerId, WorkspaceMemberRole.Viewer);
        _currentUser.Setup(c => c.UserId).Returns(viewerId);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new ApplyTagToCardCommand(card.Id, tag.Id), CancellationToken.None));
    }
}
