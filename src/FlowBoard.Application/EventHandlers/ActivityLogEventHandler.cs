using FlowBoard.Application.Common.Events;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowBoard.Application.EventHandlers;

/// <summary>
/// Persists activity-log entries after successful commit for card and workspace member events.
/// Failures are logged and swallowed so committed operations do not return HTTP 500.
/// </summary>
public sealed class ActivityLogEventHandler(
    IActivityLogRepository activityLogRepository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ILogger<ActivityLogEventHandler> logger)
    : INotificationHandler<DomainEventNotification>
{
    public async Task Handle(DomainEventNotification notification, CancellationToken cancellationToken)
    {
        ActivityLog? entry = null;

        try
        {
            var actorId = currentUser.UserId;
            if (actorId is null)
            {
                logger.LogWarning(
                    "Skipping activity log for {DomainEventType}: no authenticated actor",
                    notification.DomainEvent.GetType().Name);
                return;
            }

            entry = notification.DomainEvent switch
            {
                CardCreatedEvent created => await BuildCardCreatedEntryAsync(created, actorId.Value, cancellationToken),
                CardMovedEvent moved => await BuildCardMovedEntryAsync(moved, actorId.Value, cancellationToken),
                _ => null,
            };

            if (entry is null)
                return;

            await activityLogRepository.AddAsync(entry, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to persist activity log for {DomainEventType}",
                notification.DomainEvent.GetType().Name);
        }
    }

    private async Task<ActivityLog?> BuildCardCreatedEntryAsync(
        CardCreatedEvent created,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var board = await boardRepository.GetByIdAsync(created.BoardId, cancellationToken);
        if (board is null)
        {
            logger.LogWarning(
                "Board {BoardId} not found for CardCreated activity on card {CardId}",
                created.BoardId,
                created.CardId);
            return null;
        }

        return ActivityLog.CardCreated(board.WorkspaceId, created.BoardId, created.CardId, actorId);
    }

    private async Task<ActivityLog?> BuildCardMovedEntryAsync(
        CardMovedEvent moved,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var board = await boardRepository.GetByIdAsync(moved.BoardId, cancellationToken);
        if (board is null)
        {
            logger.LogWarning(
                "Board {BoardId} not found for CardMoved activity on card {CardId}",
                moved.BoardId,
                moved.CardId);
            return null;
        }

        return ActivityLog.CardMoved(
            board.WorkspaceId,
            moved.BoardId,
            moved.CardId,
            actorId,
            moved.FromListId,
            moved.ToListId);
    }
}
