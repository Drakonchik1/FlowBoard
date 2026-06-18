using FlowBoard.Application.Common.Events;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowBoard.Application.EventHandlers;

/// <summary>
/// Bridges domain <see cref="CardMovedEvent"/> to real-time clients after commit.
/// Notification failures are logged and swallowed so a committed move does not return HTTP 500.
/// </summary>
public sealed class CardMovedEventHandler(
    IBoardRealtimeNotifier notifier,
    ILogger<CardMovedEventHandler> logger)
    : INotificationHandler<DomainEventNotification>
{
    public async Task Handle(DomainEventNotification notification, CancellationToken cancellationToken)
    {
        if (notification.DomainEvent is not CardMovedEvent moved)
            return;

        try
        {
            await notifier.NotifyCardMovedAsync(moved, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to broadcast CardMoved for card {CardId} on board {BoardId}",
                moved.CardId, moved.BoardId);
        }
    }
}
