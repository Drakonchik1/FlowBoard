using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Cards;
using FlowBoard.Application.Features.Cards.Commands.AssignCard;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Cards;

public sealed class AssignCardCommandHandlerTests
{
    private readonly Mock<ICardRepository> _cardRepo = new();
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    public AssignCardCommandHandlerTests()
    {
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<CardDto>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<CardDto>>, CancellationToken>(
                (operation, ct) => operation(ct));
    }

    private AssignCardCommandHandler CreateHandler() =>
        new(
            _cardRepo.Object,
            _boardRepo.Object,
            _workspaceRepo.Object,
            _userRepo.Object,
            _unitOfWork.Object,
            _currentUser.Object);

    private (Workspace workspace, Board board, Card card) SetupCard(Guid ownerId)
    {
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");
        var list = BoardList.Create(board.Id, "To Do", FractionalIndex.Start());
        var card = Card.Create(board.Id, list.Id, "Card", FractionalIndex.Start());

        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        _cardRepo.Setup(r => r.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);

        return (workspace, board, card);
    }

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var (_, _, card) = SetupCard(Guid.NewGuid());
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new AssignCardCommand(card.Id, Guid.NewGuid()), CancellationToken.None));

        _cardRepo.Verify(r => r.Update(It.IsAny<Card>()), Times.Never);
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
            CreateHandler().Handle(new AssignCardCommand(card.Id, ownerId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AssigneeNotWorkspaceMember_Throws404()
    {
        var ownerId = Guid.NewGuid();
        var (_, _, card) = SetupCard(ownerId);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new AssignCardCommand(card.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidAssignee_UpdatesCard()
    {
        var ownerId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var (workspace, _, card) = SetupCard(ownerId);
        workspace.InviteMember(assigneeId, WorkspaceMemberRole.Member);

        var assignee = User.Create("member@example.com", "Member", "hash");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(assignee, assigneeId);

        _userRepo.Setup(r => r.GetByIdAsync(assigneeId, It.IsAny<CancellationToken>())).ReturnsAsync(assignee);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);

        var result = await CreateHandler().Handle(
            new AssignCardCommand(card.Id, assigneeId),
            CancellationToken.None);

        Assert.Equal(assigneeId, result.AssigneeId);
        _cardRepo.Verify(r => r.Update(card), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Unassign_ClearsAssignee()
    {
        var ownerId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var (workspace, _, card) = SetupCard(ownerId);
        workspace.InviteMember(assigneeId, WorkspaceMemberRole.Member);
        card.Assign(assigneeId, ownerId);

        _currentUser.Setup(c => c.UserId).Returns(ownerId);

        var result = await CreateHandler().Handle(
            new AssignCardCommand(card.Id, null),
            CancellationToken.None);

        Assert.Null(result.AssigneeId);
        _cardRepo.Verify(r => r.Update(card), Times.Once);
    }

    [Fact]
    public async Task Handle_IdempotentReassign_DoesNotUpdate()
    {
        var ownerId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var (workspace, _, card) = SetupCard(ownerId);
        workspace.InviteMember(assigneeId, WorkspaceMemberRole.Member);
        card.Assign(assigneeId, ownerId);

        var assignee = User.Create("member@example.com", "Member", "hash");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(assignee, assigneeId);

        _userRepo.Setup(r => r.GetByIdAsync(assigneeId, It.IsAny<CancellationToken>())).ReturnsAsync(assignee);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);

        var result = await CreateHandler().Handle(
            new AssignCardCommand(card.Id, assigneeId),
            CancellationToken.None);

        Assert.Equal(assigneeId, result.AssigneeId);
        _cardRepo.Verify(r => r.Update(card), Times.Once);
    }
}
