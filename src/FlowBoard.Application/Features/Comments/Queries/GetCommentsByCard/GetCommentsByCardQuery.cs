using FlowBoard.Application.Features.Comments;
using MediatR;

namespace FlowBoard.Application.Features.Comments.Queries.GetCommentsByCard;

public sealed record GetCommentsByCardQuery(Guid CardId) : IRequest<IReadOnlyList<CommentDto>>;
