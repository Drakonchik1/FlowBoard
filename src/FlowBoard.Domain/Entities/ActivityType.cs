namespace FlowBoard.Domain.Entities;

/// <summary>
/// Kind of workspace or card activity recorded in the activity log.
/// Stored as a string in the database for readable migrations.
/// </summary>
public enum ActivityType
{
    CardCreated,
    CardMoved,
    MemberInvited,
}
