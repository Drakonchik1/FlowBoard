using FlowBoard.Application.Common.Events;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowBoard.Application.EventHandlers;

/// <summary>
/// Queues an email when a card is assigned to a workspace member.
/// Failures are logged and swallowed so a committed assignment does not return HTTP 500.
/// </summary>
public sealed class CardAssignedEventHandler(
    IEmailService emailService,
    IUserRepository userRepository,
    IBoardRepository boardRepository,
    IWorkspaceRepository workspaceRepository,
    ILogger<CardAssignedEventHandler> logger)
    : INotificationHandler<DomainEventNotification>
{
    public async Task Handle(DomainEventNotification notification, CancellationToken cancellationToken)
    {
        if (notification.DomainEvent is not CardAssignedEvent assigned)
            return;

        try
        {
            if (!AssignmentEmailThrottle.ShouldSend(assigned.CardId, assigned.AssigneeId))
            {
                logger.LogDebug(
                    "Skipping duplicate assignment email for card {CardId} to assignee {AssigneeId}",
                    assigned.CardId,
                    assigned.AssigneeId);
                return;
            }

            var board = await boardRepository.GetByIdAsync(assigned.BoardId, cancellationToken);
            if (board is null)
            {
                logger.LogWarning(
                    "Board {BoardId} not found for CardAssigned on card {CardId}",
                    assigned.BoardId,
                    assigned.CardId);
                return;
            }

            var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
            if (workspace is null || !workspace.HasMember(assigned.AssigneeId))
            {
                logger.LogWarning(
                    "Assignee {AssigneeId} is not a workspace member for CardAssigned on card {CardId}",
                    assigned.AssigneeId,
                    assigned.CardId);
                return;
            }

            var assignee = await userRepository.GetByIdAsync(assigned.AssigneeId, cancellationToken);
            if (assignee is null)
            {
                logger.LogWarning(
                    "Assignee {AssigneeId} not found for CardAssigned on card {CardId}",
                    assigned.AssigneeId,
                    assigned.CardId);
                return;
            }

            var subject = "You've been assigned to a card";
            var body = $"""
                <p>You have been assigned to the card <strong>{System.Net.WebUtility.HtmlEncode(assigned.CardTitle)}</strong>.</p>
                """;

            await emailService.SendEmailAsync(assignee.Email.Value, subject, body, isHtml: true, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to queue assignment email for card {CardId} to assignee {AssigneeId}",
                assigned.CardId,
                assigned.AssigneeId);
        }
    }
}
