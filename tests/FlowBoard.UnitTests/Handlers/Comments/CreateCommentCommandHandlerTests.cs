using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Comments.Commands.CreateComment;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Comments;

public sealed class CreateCommentCommandHandlerTests
{
    private readonly Mock<ICommentRepository> _commentRepo = new();
    private readonly Mock<ICardRepository> _cardRepo = new();
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private CreateCommentCommandHandler CreateHandler() =>
        new(_commentRepo.Object, _cardRepo.Object, _boardRepo.Object,
            _workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    private (Workspace workspace, Board board, Card card) SetupCard(Guid ownerId)
    {
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");
        var list = BoardList.Create(board.Id, "To Do", FractionalIndex.Start());
        var card = Card.Create(board.Id, list.Id, "Card", FractionalIndex.Start());

        _cardRepo.Setup(r => r.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        return (workspace, board, card);
    }

    [Fact]
    public async Task Handle_Member_CreatesComment()
    {
        var ownerId = Guid.NewGuid();
        var (_, _, card) = SetupCard(ownerId);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);

        var result = await CreateHandler().Handle(
            new CreateCommentCommand(card.Id, "Looks good"), CancellationToken.None);

        Assert.Equal("Looks good", result.Body);
        Assert.Equal(card.Id, result.CardId);
        Assert.Equal(ownerId, result.AuthorId);
        _commentRepo.Verify(r => r.AddAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CardMissing_Throws404()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _cardRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Card?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new CreateCommentCommand(Guid.NewGuid(), "Hi"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var ownerId = Guid.NewGuid();
        var (_, _, card) = SetupCard(ownerId);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new CreateCommentCommand(card.Id, "Hi"), CancellationToken.None));

        _commentRepo.Verify(r => r.AddAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Viewer_Throws403()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var (workspace, _, card) = SetupCard(ownerId);
        workspace.InviteMember(viewerId, WorkspaceMemberRole.Viewer);
        _currentUser.Setup(c => c.UserId).Returns(viewerId);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new CreateCommentCommand(card.Id, "Hi"), CancellationToken.None));
    }
}
