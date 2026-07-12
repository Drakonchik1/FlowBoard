using FlowBoard.Application.Features.Comments;
using MediatR;

namespace FlowBoard.Application.Features.Comments.Commands.UpdateComment;

public sealed record UpdateCommentCommand(Guid CommentId, string Body) : IRequest<CommentDto>;
