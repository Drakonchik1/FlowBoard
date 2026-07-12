using FlowBoard.Application.Common.Events;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.EventHandlers;
using FlowBoard.Domain.Events;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlowBoard.UnitTests.EventHandlers;

public sealed class CommentAddedEventHandlerTests
{
    private readonly Mock<IBoardRealtimeNotifier> _notifier = new();
    private readonly Mock<ILogger<CommentAddedEventHandler>> _logger = new();

    [Fact]
    public async Task Handle_CommentAddedEvent_NotifiesRealtimeClients()
    {
        var added = new CommentAddedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Looks good",
            DateTime.UtcNow);

        var handler = new CommentAddedEventHandler(_notifier.Object, _logger.Object);

        await handler.Handle(DomainEventNotification.Wrap(added), CancellationToken.None);

        _notifier.Verify(
            n => n.NotifyCommentAddedAsync(added, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_OtherDomainEvent_DoesNotNotify()
    {
        var other = new CardCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var handler = new CommentAddedEventHandler(_notifier.Object, _logger.Object);

        await handler.Handle(DomainEventNotification.Wrap(other), CancellationToken.None);

        _notifier.Verify(
            n => n.NotifyCommentAddedAsync(It.IsAny<CommentAddedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NotifierThrows_DoesNotPropagate()
    {
        var added = new CommentAddedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Looks good",
            DateTime.UtcNow);

        _notifier
            .Setup(n => n.NotifyCommentAddedAsync(added, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SignalR unavailable"));

        var handler = new CommentAddedEventHandler(_notifier.Object, _logger.Object);

        var exception = await Record.ExceptionAsync(() =>
            handler.Handle(DomainEventNotification.Wrap(added), CancellationToken.None));

        Assert.Null(exception);
    }
}
