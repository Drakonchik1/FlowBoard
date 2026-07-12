using System.Net;
using FlowBoard.Domain.Entities;

namespace FlowBoard.Application.Features.Comments;

/// <summary>Maps comment entities to API DTOs with HTML-encoded bodies for safe client rendering.</summary>
internal static class CommentMapper
{
    public static CommentDto ToDto(Comment comment) => new(
        comment.Id,
        comment.CardId,
        comment.AuthorId,
        WebUtility.HtmlEncode(comment.Body),
        comment.CreatedAt,
        comment.UpdatedAt);
}
