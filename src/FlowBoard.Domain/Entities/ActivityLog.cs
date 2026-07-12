using FlowBoard.Domain.Common;
using FlowBoard.Domain.Exceptions;

namespace FlowBoard.Domain.Entities;

/// <summary>
/// Append-only audit entry for workspace and card activity. Written by domain-event handlers after commit.
/// </summary>
public sealed class ActivityLog : Entity
{
    public Guid WorkspaceId { get; private set; }
    public Guid? BoardId { get; private set; }
    public Guid? CardId { get; private set; }
    public Guid ActorId { get; private set; }
    public ActivityType Type { get; private set; }
    public Guid? TargetUserId { get; private set; }
    public string? TargetRole { get; private set; }
    public Guid? FromListId { get; private set; }
    public Guid? ToListId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ActivityLog() { }

    public static ActivityLog CardCreated(Guid workspaceId, Guid boardId, Guid cardId, Guid actorId)
    {
        ValidateWorkspace(workspaceId);
        ValidateBoard(boardId);
        ValidateCard(cardId);
        ValidateActor(actorId);

        return new ActivityLog
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            BoardId = boardId,
            CardId = cardId,
            ActorId = actorId,
            Type = ActivityType.CardCreated,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static ActivityLog CardMoved(
        Guid workspaceId,
        Guid boardId,
        Guid cardId,
        Guid actorId,
        Guid fromListId,
        Guid toListId)
    {
        ValidateWorkspace(workspaceId);
        ValidateBoard(boardId);
        ValidateCard(cardId);
        ValidateActor(actorId);

        if (fromListId == Guid.Empty)
            throw new DomainException("From list is required for card move activity.");
        if (toListId == Guid.Empty)
            throw new DomainException("To list is required for card move activity.");

        return new ActivityLog
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            BoardId = boardId,
            CardId = cardId,
            ActorId = actorId,
            Type = ActivityType.CardMoved,
            FromListId = fromListId,
            ToListId = toListId,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static ActivityLog MemberInvited(
        Guid workspaceId,
        Guid actorId,
        Guid targetUserId,
        WorkspaceMemberRole role)
    {
        ValidateWorkspace(workspaceId);
        ValidateActor(actorId);

        if (targetUserId == Guid.Empty)
            throw new DomainException("Invited user is required for member invite activity.");

        return new ActivityLog
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            ActorId = actorId,
            Type = ActivityType.MemberInvited,
            TargetUserId = targetUserId,
            TargetRole = role.ToString(),
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static void ValidateWorkspace(Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
            throw new DomainException("Activity log must belong to a workspace.");
    }

    private static void ValidateBoard(Guid boardId)
    {
        if (boardId == Guid.Empty)
            throw new DomainException("Card activity must belong to a board.");
    }

    private static void ValidateCard(Guid cardId)
    {
        if (cardId == Guid.Empty)
            throw new DomainException("Card activity must reference a card.");
    }

    private static void ValidateActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
            throw new DomainException("Activity actor is required.");
    }
}
