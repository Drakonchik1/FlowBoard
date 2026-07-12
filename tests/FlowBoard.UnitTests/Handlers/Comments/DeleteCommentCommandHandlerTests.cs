using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Comments.Commands.DeleteComment;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Comments;

public sealed class DeleteCommentCommandHandlerTests
{
    private readonly Mock<ICommentRepository> _commentRepo = new();
    private readonly Mock<ICardRepository> _cardRepo = new();
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private DeleteCommentCommandHandler CreateHandler() =>
        new(_commentRepo.Object, _cardRepo.Object, _boardRepo.Object,
            _workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    private (Workspace workspace, Comment comment) SetupComment(Guid ownerId)
    {
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");
        var list = BoardList.Create(board.Id, "To Do", FractionalIndex.Start());
        var card = Card.Create(board.Id, list.Id, "Card", FractionalIndex.Start());
        var comment = Comment.Create(card.Id, board.Id, ownerId, "Original");

        _commentRepo.Setup(r => r.GetByIdAsync(comment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(comment);
        _cardRepo.Setup(r => r.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);
        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        return (workspace, comment);
    }

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var (_, comment) = SetupComment(Guid.NewGuid());
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeleteCommentCommand(comment.Id), CancellationToken.None));

        _commentRepo.Verify(r => r.Update(It.IsAny<Comment>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Viewer_Throws403()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var (workspace, comment) = SetupComment(ownerId);
        workspace.InviteMember(viewerId, WorkspaceMemberRole.Viewer);
        _currentUser.Setup(c => c.UserId).Returns(viewerId);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new DeleteCommentCommand(comment.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonAuthor_Throws403()
    {
        var ownerId = Guid.NewGuid();
        var otherMemberId = Guid.NewGuid();
        var (workspace, comment) = SetupComment(ownerId);
        workspace.InviteMember(otherMemberId, WorkspaceMemberRole.Member);
        _currentUser.Setup(c => c.UserId).Returns(otherMemberId);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new DeleteCommentCommand(comment.Id), CancellationToken.None));

        _commentRepo.Verify(r => r.Update(It.IsAny<Comment>()), Times.Never);
    }
}
