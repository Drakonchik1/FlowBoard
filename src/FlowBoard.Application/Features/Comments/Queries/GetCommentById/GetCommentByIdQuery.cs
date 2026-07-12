using FlowBoard.Application.Features.Comments;
using MediatR;

namespace FlowBoard.Application.Features.Comments.Queries.GetCommentById;

public sealed record GetCommentByIdQuery(Guid CommentId) : IRequest<CommentDto>;
