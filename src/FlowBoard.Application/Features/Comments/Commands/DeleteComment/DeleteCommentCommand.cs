using MediatR;

namespace FlowBoard.Application.Features.Comments.Commands.DeleteComment;

public sealed record DeleteCommentCommand(Guid CommentId) : IRequest;
