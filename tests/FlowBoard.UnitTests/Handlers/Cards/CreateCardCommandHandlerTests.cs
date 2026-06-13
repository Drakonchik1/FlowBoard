using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Cards.Commands.CreateCard;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;
using CardPriority = FlowBoard.Application.Features.Cards.CardPriority;

namespace FlowBoard.UnitTests.Handlers.Cards;

public sealed class CreateCardCommandHandlerTests
{
    private readonly Mock<ICardRepository> _cardRepo = new();
    private readonly Mock<IBoardListRepository> _listRepo = new();
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private CreateCardCommandHandler CreateHandler() =>
        new(_cardRepo.Object, _listRepo.Object, _boardRepo.Object,
            _workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Member_CreatesCardAtEndOfList()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");
        var list = BoardList.Create(board.Id, "To Do", FractionalIndex.Start());

        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _listRepo.Setup(r => r.GetByIdAsync(list.Id, It.IsAny<CancellationToken>())).ReturnsAsync(list);
        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        _cardRepo.Setup(r => r.GetLastPositionAsync(list.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FractionalIndex?)null);

        var result = await CreateHandler().Handle(
            new CreateCardCommand(list.Id, "Write tests", "desc", CardPriority.Medium), CancellationToken.None);

        Assert.Equal("Write tests", result.Title);
        Assert.Equal("Medium", result.Priority);
        Assert.Equal(list.Id, result.BoardListId);
        Assert.Equal(board.Id, result.BoardId);
        _cardRepo.Verify(r => r.AddAsync(It.IsAny<Card>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AppendsAfterExistingCard()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");
        var list = BoardList.Create(board.Id, "To Do", FractionalIndex.Start());
        var existingPosition = FractionalIndex.Start();

        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _listRepo.Setup(r => r.GetByIdAsync(list.Id, It.IsAny<CancellationToken>())).ReturnsAsync(list);
        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        _cardRepo.Setup(r => r.GetLastPositionAsync(list.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPosition);

        var result = await CreateHandler().Handle(
            new CreateCardCommand(list.Id, "Second", null), CancellationToken.None);

        Assert.True(string.CompareOrdinal(existingPosition.Value, result.Position) < 0);
    }

    [Fact]
    public async Task Handle_ListMissing_Throws404()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _listRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BoardList?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new CreateCardCommand(Guid.NewGuid(), "Card", null), CancellationToken.None));
    }
}
