using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.BoardLists.Commands.DeleteBoardList;

public sealed class DeleteBoardListCommandHandler(
    IBoardListRepository boardListRepository,
    IBoardRepository boardRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<DeleteBoardListCommand>
{
    public async Task Handle(DeleteBoardListCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var list = await boardListRepository.GetByIdAsync(request.ListId, cancellationToken)
            ?? throw new NotFoundException("List", request.ListId);

        var board = await boardRepository.GetByIdAsync(list.BoardId, cancellationToken)
            ?? throw new NotFoundException("List", request.ListId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "List", request.ListId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        list.SoftDelete();
        boardListRepository.Update(list);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
