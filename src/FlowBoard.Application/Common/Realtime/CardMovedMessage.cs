namespace FlowBoard.Application.Common.Realtime;

/// <summary>Payload broadcast to clients when a card is moved on a board.</summary>
public sealed record CardMovedMessage(
    Guid CardId,
    Guid BoardId,
    Guid FromListId,
    Guid ToListId,
    string Position);
