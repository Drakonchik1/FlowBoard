using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Application.Features.Comments;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Comments.Commands.UpdateComment;

public sealed class UpdateCommentCommandHandler(
    ICommentRepository commentRepository,
    ICardRepository cardRepository,
    IBoardRepository boardRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<UpdateCommentCommand, CommentDto>
{
    public async Task<CommentDto> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var comment = await commentRepository.GetByIdAsync(request.CommentId, cancellationToken)
            ?? throw new NotFoundException("Comment", request.CommentId);

        var card = await cardRepository.GetByIdAsync(comment.CardId, cancellationToken)
            ?? throw new NotFoundException("Comment", request.CommentId);

        var board = await boardRepository.GetByIdAsync(card.BoardId, cancellationToken)
            ?? throw new NotFoundException("Comment", request.CommentId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Comment", request.CommentId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);
        ResourceGuard.EnsureCommentAuthor(comment, userId);

        comment.Update(request.Body);
        commentRepository.Update(comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CommentMapper.ToDto(comment);
    }
}
