namespace FlowBoard.Application.Common.Realtime;

/// <summary>Payload broadcast to clients when a comment is added to a card.</summary>
public sealed record CommentAddedMessage(
    Guid CommentId,
    Guid CardId,
    Guid BoardId,
    Guid AuthorId,
    string Body,
    DateTime CreatedAt);
