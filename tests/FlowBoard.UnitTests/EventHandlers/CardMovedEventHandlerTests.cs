using FlowBoard.Application.Common.Events;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.EventHandlers;
using FlowBoard.Domain.Events;
using Moq;

namespace FlowBoard.UnitTests.EventHandlers;

public sealed class CardMovedEventHandlerTests
{
    private readonly Mock<IBoardRealtimeNotifier> _notifier = new();

    [Fact]
    public async Task Handle_CardMovedEvent_NotifiesRealtimeClients()
    {
        var moved = new CardMovedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "a0");

        var handler = new CardMovedEventHandler(_notifier.Object);

        await handler.Handle(DomainEventNotification.Wrap(moved), CancellationToken.None);

        _notifier.Verify(
            n => n.NotifyCardMovedAsync(moved, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_OtherDomainEvent_DoesNotNotify()
    {
        var other = new CardCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var handler = new CardMovedEventHandler(_notifier.Object);

        await handler.Handle(DomainEventNotification.Wrap(other), CancellationToken.None);

        _notifier.Verify(
            n => n.NotifyCardMovedAsync(It.IsAny<CardMovedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
