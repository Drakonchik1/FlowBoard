using FlowBoard.Application.Features.Comments;
using MediatR;

namespace FlowBoard.Application.Features.Comments.Commands.CreateComment;

public sealed record CreateCommentCommand(Guid CardId, string Body) : IRequest<CommentDto>;
