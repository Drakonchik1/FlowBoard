using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Cards.Commands.MoveCard;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using FluentValidation.TestHelper;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Cards;

public sealed class MoveCardCommandHandlerTests
{
    private readonly Mock<ICardRepository> _cardRepo = new();
    private readonly Mock<IBoardListRepository> _listRepo = new();
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private MoveCardCommandHandler CreateHandler() =>
        new(_cardRepo.Object, _listRepo.Object, _boardRepo.Object,
            _workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    private (Workspace ws, Board board) Arrange(Guid ownerId)
    {
        var ws = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), ws.Id, "Board");
        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(ws.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ws);
        return (ws, board);
    }

    [Fact]
    public async Task Handle_MovesCardToAnotherListOnSameBoard()
    {
        var ownerId = Guid.NewGuid();
        var (_, board) = Arrange(ownerId);

        var listA = BoardList.Create(board.Id, "A", FractionalIndex.Start());
        var listB = BoardList.Create(board.Id, "B", FractionalIndex.Between(listA.Position, null));
        var card = Card.Create(board.Id, listA.Id, "Card", FractionalIndex.Start());

        _cardRepo.Setup(r => r.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        _listRepo.Setup(r => r.GetByIdAsync(listB.Id, It.IsAny<CancellationToken>())).ReturnsAsync(listB);

        var result = await CreateHandler().Handle(
            new MoveCardCommand(card.Id, listB.Id, null, null), CancellationToken.None);

        Assert.Equal(listB.Id, result.BoardListId);
        _cardRepo.Verify(r => r.Update(It.Is<Card>(c => c.BoardListId == listB.Id)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PositionsBetweenTwoCards()
    {
        var ownerId = Guid.NewGuid();
        var (_, board) = Arrange(ownerId);

        var list = BoardList.Create(board.Id, "A", FractionalIndex.Start());
        var first = Card.Create(board.Id, list.Id, "First", FractionalIndex.Start());
        var third = Card.Create(board.Id, list.Id, "Third", FractionalIndex.Between(first.Position, null));
        var moving = Card.Create(board.Id, list.Id, "Moving", FractionalIndex.Between(third.Position, null));

        _cardRepo.Setup(r => r.GetByIdAsync(moving.Id, It.IsAny<CancellationToken>())).ReturnsAsync(moving);
        _cardRepo.Setup(r => r.GetByIdAsync(first.Id, It.IsAny<CancellationToken>())).ReturnsAsync(first);
        _cardRepo.Setup(r => r.GetByIdAsync(third.Id, It.IsAny<CancellationToken>())).ReturnsAsync(third);
        _listRepo.Setup(r => r.GetByIdAsync(list.Id, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var result = await CreateHandler().Handle(
            new MoveCardCommand(moving.Id, list.Id, first.Id, third.Id), CancellationToken.None);

        Assert.True(string.CompareOrdinal(first.Position.Value, result.Position) < 0);
        Assert.True(string.CompareOrdinal(result.Position, third.Position.Value) < 0);
    }

    [Fact]
    public async Task Handle_TargetListOnDifferentBoard_ThrowsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var (_, board) = Arrange(ownerId);

        var card = Card.Create(board.Id, Guid.NewGuid(), "Card", FractionalIndex.Start());
        var foreignList = BoardList.Create(Guid.NewGuid(), "Foreign", FractionalIndex.Start());

        _cardRepo.Setup(r => r.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        _listRepo.Setup(r => r.GetByIdAsync(foreignList.Id, It.IsAny<CancellationToken>())).ReturnsAsync(foreignList);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new MoveCardCommand(card.Id, foreignList.Id, null, null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MissingNeighbour_ThrowsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var (_, board) = Arrange(ownerId);

        var list = BoardList.Create(board.Id, "A", FractionalIndex.Start());
        var card = Card.Create(board.Id, list.Id, "Card", FractionalIndex.Start());
        var missingNeighbourId = Guid.NewGuid();

        _cardRepo.Setup(r => r.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        _cardRepo.Setup(r => r.GetByIdAsync(missingNeighbourId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Card?)null);
        _listRepo.Setup(r => r.GetByIdAsync(list.Id, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new MoveCardCommand(card.Id, list.Id, missingNeighbourId, null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NeighbourNotInTargetList_ThrowsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var (_, board) = Arrange(ownerId);

        var list = BoardList.Create(board.Id, "A", FractionalIndex.Start());
        var card = Card.Create(board.Id, list.Id, "Card", FractionalIndex.Start());
        var strayNeighbour = Card.Create(board.Id, Guid.NewGuid(), "Stray", FractionalIndex.Start());

        _cardRepo.Setup(r => r.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        _cardRepo.Setup(r => r.GetByIdAsync(strayNeighbour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(strayNeighbour);
        _listRepo.Setup(r => r.GetByIdAsync(list.Id, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(
                new MoveCardCommand(card.Id, list.Id, strayNeighbour.Id, null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ConcurrencyConflict_RetriesAndSucceeds()
    {
        var ownerId = Guid.NewGuid();
        var (_, board) = Arrange(ownerId);

        var listA = BoardList.Create(board.Id, "A", FractionalIndex.Start());
        var listB = BoardList.Create(board.Id, "B", FractionalIndex.Between(listA.Position, null));
        var card = Card.Create(board.Id, listA.Id, "Card", FractionalIndex.Start());

        _cardRepo.Setup(r => r.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        _listRepo.Setup(r => r.GetByIdAsync(listB.Id, It.IsAny<CancellationToken>())).ReturnsAsync(listB);

        var saveAttempts = 0;
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                saveAttempts++;
                if (saveAttempts == 1)
                    throw new ConflictException("A resource with this value already exists.");
                return Task.FromResult(1);
            });

        var result = await CreateHandler().Handle(
            new MoveCardCommand(card.Id, listB.Id, null, null), CancellationToken.None);

        Assert.Equal(listB.Id, result.BoardListId);
        Assert.Equal(2, saveAttempts);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var ownerId = Guid.NewGuid();
        var (_, board) = Arrange(ownerId);

        var list = BoardList.Create(board.Id, "A", FractionalIndex.Start());
        var card = Card.Create(board.Id, list.Id, "Card", FractionalIndex.Start());

        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _cardRepo.Setup(r => r.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new MoveCardCommand(card.Id, list.Id, null, null), CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Viewer_Throws403()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var (ws, board) = Arrange(ownerId);
        ws.InviteMember(viewerId, WorkspaceMemberRole.Viewer);

        var list = BoardList.Create(board.Id, "A", FractionalIndex.Start());
        var card = Card.Create(board.Id, list.Id, "Card", FractionalIndex.Start());

        _currentUser.Setup(c => c.UserId).Returns(viewerId);
        _cardRepo.Setup(r => r.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new MoveCardCommand(card.Id, list.Id, null, null), CancellationToken.None));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validator_RejectsEmptyGuidNeighbourIds(bool beforeCard)
    {
        var validator = new MoveCardCommandValidator();
        var command = beforeCard
            ? new MoveCardCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null)
            : new MoveCardCommand(Guid.NewGuid(), Guid.NewGuid(), null, Guid.Empty);

        var result = validator.TestValidate(command);

        if (beforeCard)
            result.ShouldHaveValidationErrorFor(x => x.BeforeCardId);
        else
            result.ShouldHaveValidationErrorFor(x => x.AfterCardId);
    }
}
