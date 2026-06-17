using FlowBoard.Application.Common.Events;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Events;
using MediatR;

namespace FlowBoard.Application.EventHandlers;

/// <summary>
/// Bridges domain <see cref="CardMovedEvent"/> to real-time clients after commit.
/// </summary>
public sealed class CardMovedEventHandler(IBoardRealtimeNotifier notifier)
    : INotificationHandler<DomainEventNotification>
{
    public async Task Handle(DomainEventNotification notification, CancellationToken cancellationToken)
    {
        if (notification.DomainEvent is not CardMovedEvent moved)
            return;

        await notifier.NotifyCardMovedAsync(moved, cancellationToken);
    }
}
