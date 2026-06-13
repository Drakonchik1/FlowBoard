using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Boards.Commands.DeleteBoard;

public sealed class DeleteBoardCommandHandler(
    IBoardRepository boardRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<DeleteBoardCommand>
{
    public async Task Handle(DeleteBoardCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var board = await boardRepository.GetByIdAsync(request.BoardId, cancellationToken)
            ?? throw new NotFoundException("Board", request.BoardId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Board", request.BoardId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        board.SoftDelete();
        boardRepository.Update(board);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
