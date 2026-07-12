namespace FlowBoard.Application.Features.Comments;

public sealed record CommentDto(
    Guid Id,
    Guid CardId,
    Guid AuthorId,
    string Body,
    DateTime CreatedAt,
    DateTime UpdatedAt);
