using FlowBoard.Application.Common.Events;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.EventHandlers;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlowBoard.UnitTests.EventHandlers;

public sealed class ActivityLogEventHandlerTests
{
    private readonly Mock<IActivityLogRepository> _activityLogRepo = new();
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ILogger<ActivityLogEventHandler>> _logger = new();

    private ActivityLogEventHandler CreateHandler() =>
        new(_activityLogRepo.Object, _boardRepo.Object, _unitOfWork.Object,
            _currentUser.Object, _logger.Object);

    [Fact]
    public async Task Handle_CardCreatedEvent_PersistsActivityLog()
    {
        var actorId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var created = new CardCreatedEvent(cardId, boardId, Guid.NewGuid());
        var board = Board.Create(Guid.NewGuid(), workspaceId, "Board");

        _currentUser.Setup(c => c.UserId).Returns(actorId);
        _boardRepo.Setup(r => r.GetByIdAsync(boardId, It.IsAny<CancellationToken>())).ReturnsAsync(board);

        await CreateHandler().Handle(DomainEventNotification.Wrap(created), CancellationToken.None);

        _activityLogRepo.Verify(
            r => r.AddAsync(
                It.Is<ActivityLog>(a =>
                    a.Type == ActivityType.CardCreated &&
                    a.CardId == cardId &&
                    a.BoardId == boardId &&
                    a.WorkspaceId == workspaceId &&
                    a.ActorId == actorId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CardMovedEvent_PersistsActivityLog()
    {
        var actorId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var fromListId = Guid.NewGuid();
        var toListId = Guid.NewGuid();
        var moved = new CardMovedEvent(cardId, boardId, fromListId, toListId, "a0");
        var board = Board.Create(Guid.NewGuid(), workspaceId, "Board");

        _currentUser.Setup(c => c.UserId).Returns(actorId);
        _boardRepo.Setup(r => r.GetByIdAsync(boardId, It.IsAny<CancellationToken>())).ReturnsAsync(board);

        await CreateHandler().Handle(DomainEventNotification.Wrap(moved), CancellationToken.None);

        _activityLogRepo.Verify(
            r => r.AddAsync(
                It.Is<ActivityLog>(a =>
                    a.Type == ActivityType.CardMoved &&
                    a.CardId == cardId &&
                    a.FromListId == fromListId &&
                    a.ToListId == toListId &&
                    a.ActorId == actorId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MemberInvitedEvent_DoesNotPersistActivityLog()
    {
        var actorId = Guid.NewGuid();
        var invited = new MemberInvitedEvent(Guid.NewGuid(), Guid.NewGuid(), WorkspaceMemberRole.Member);

        _currentUser.Setup(c => c.UserId).Returns(actorId);

        await CreateHandler().Handle(DomainEventNotification.Wrap(invited), CancellationToken.None);

        _activityLogRepo.Verify(
            r => r.AddAsync(It.IsAny<ActivityLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NoAuthenticatedActor_SkipsPersist()
    {
        var created = new CardCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);

        await CreateHandler().Handle(DomainEventNotification.Wrap(created), CancellationToken.None);

        _activityLogRepo.Verify(
            r => r.AddAsync(It.IsAny<ActivityLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SaveChangesThrows_DoesNotPropagate()
    {
        var actorId = Guid.NewGuid();
        var created = new CardCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), "Board");

        _currentUser.Setup(c => c.UserId).Returns(actorId);
        _boardRepo.Setup(r => r.GetByIdAsync(created.BoardId, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB unavailable"));

        var exception = await Record.ExceptionAsync(() =>
            CreateHandler().Handle(DomainEventNotification.Wrap(created), CancellationToken.None));

        Assert.Null(exception);
    }
}
