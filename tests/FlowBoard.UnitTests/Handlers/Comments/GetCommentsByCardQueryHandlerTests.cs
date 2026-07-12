using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Comments.Queries.GetCommentsByCard;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Comments;

public sealed class GetCommentsByCardQueryHandlerTests
{
    private readonly Mock<ICommentRepository> _commentRepo = new();
    private readonly Mock<ICardRepository> _cardRepo = new();
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private GetCommentsByCardQueryHandler CreateHandler() =>
        new(_commentRepo.Object, _cardRepo.Object, _boardRepo.Object,
            _workspaceRepo.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");
        var list = BoardList.Create(board.Id, "To Do", FractionalIndex.Start());
        var card = Card.Create(board.Id, list.Id, "Card", FractionalIndex.Start());

        _cardRepo.Setup(r => r.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new GetCommentsByCardQuery(card.Id), CancellationToken.None));

        _commentRepo.Verify(r => r.GetByCardIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
