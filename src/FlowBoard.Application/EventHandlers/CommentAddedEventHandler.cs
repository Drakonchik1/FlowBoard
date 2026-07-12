using FlowBoard.Application.Common.Events;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowBoard.Application.EventHandlers;

/// <summary>
/// Bridges domain <see cref="CommentAddedEvent"/> to real-time clients after commit.
/// Notification failures are logged and swallowed so a committed comment does not return HTTP 500.
/// </summary>
public sealed class CommentAddedEventHandler(
    IBoardRealtimeNotifier notifier,
    ILogger<CommentAddedEventHandler> logger)
    : INotificationHandler<DomainEventNotification>
{
    public async Task Handle(DomainEventNotification notification, CancellationToken cancellationToken)
    {
        if (notification.DomainEvent is not CommentAddedEvent added)
            return;

        try
        {
            await notifier.NotifyCommentAddedAsync(added, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to broadcast CommentAdded for comment {CommentId} on board {BoardId}",
                added.CommentId, added.BoardId);
        }
    }
}
