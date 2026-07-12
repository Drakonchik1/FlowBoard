using FlowBoard.Application.Common.Events;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.EventHandlers;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlowBoard.UnitTests.EventHandlers;

public sealed class CardAssignedEventHandlerTests
{
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IBoardRepository> _boardRepository = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepository = new();
    private readonly Mock<ILogger<CardAssignedEventHandler>> _logger = new();

    private CardAssignedEventHandler CreateHandler() =>
        new(
            _emailService.Object,
            _userRepository.Object,
            _boardRepository.Object,
            _workspaceRepository.Object,
            _logger.Object);

    private (Workspace workspace, Board board) SetupBoard(Guid assigneeId)
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        workspace.InviteMember(assigneeId, WorkspaceMemberRole.Member);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");

        _boardRepository
            .Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(board);
        _workspaceRepository
            .Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        return (workspace, board);
    }

    [Fact]
    public async Task Handle_CardAssignedEvent_QueuesEmailToAssignee()
    {
        var assigneeId = Guid.NewGuid();
        var (_, board) = SetupBoard(assigneeId);

        var assignee = User.Create("assignee@example.com", "Assignee", "hash");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(assignee, assigneeId);

        _userRepository
            .Setup(r => r.GetByIdAsync(assigneeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignee);

        var assigned = new CardAssignedEvent(
            Guid.NewGuid(),
            board.Id,
            assigneeId,
            Guid.NewGuid(),
            "Fix bug");

        await CreateHandler().Handle(DomainEventNotification.Wrap(assigned), CancellationToken.None);

        _emailService.Verify(
            e => e.SendEmailAsync(
                "assignee@example.com",
                "You've been assigned to a card",
                It.Is<string>(body => body.Contains("Fix bug")),
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_OtherDomainEvent_DoesNotSendEmail()
    {
        var other = new CardCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await CreateHandler().Handle(DomainEventNotification.Wrap(other), CancellationToken.None);

        _emailService.Verify(
            e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AssigneeNotWorkspaceMember_DoesNotSendEmail()
    {
        var assigneeId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");

        _boardRepository
            .Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(board);
        _workspaceRepository
            .Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        var assigned = new CardAssignedEvent(
            Guid.NewGuid(),
            board.Id,
            assigneeId,
            ownerId,
            "Fix bug");

        await CreateHandler().Handle(DomainEventNotification.Wrap(assigned), CancellationToken.None);

        _emailService.Verify(
            e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AssigneeNotFound_DoesNotSendEmail()
    {
        var assigneeId = Guid.NewGuid();
        var (_, board) = SetupBoard(assigneeId);

        _userRepository
            .Setup(r => r.GetByIdAsync(assigneeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var assigned = new CardAssignedEvent(
            Guid.NewGuid(),
            board.Id,
            assigneeId,
            Guid.NewGuid(),
            "Fix bug");

        await CreateHandler().Handle(DomainEventNotification.Wrap(assigned), CancellationToken.None);

        _emailService.Verify(
            e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_EmailServiceThrows_DoesNotPropagate()
    {
        var assigneeId = Guid.NewGuid();
        var (_, board) = SetupBoard(assigneeId);

        var assignee = User.Create("assignee@example.com", "Assignee", "hash");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(assignee, assigneeId);

        _userRepository
            .Setup(r => r.GetByIdAsync(assigneeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignee);

        _emailService
            .Setup(e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Queue full"));

        var assigned = new CardAssignedEvent(
            Guid.NewGuid(),
            board.Id,
            assigneeId,
            Guid.NewGuid(),
            "Fix bug");

        var exception = await Record.ExceptionAsync(() =>
            CreateHandler().Handle(DomainEventNotification.Wrap(assigned), CancellationToken.None));

        Assert.Null(exception);
    }
}
