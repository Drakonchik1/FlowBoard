using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.BoardLists.Commands.RenameBoardList;

public sealed class RenameBoardListCommandHandler(
    IBoardListRepository boardListRepository,
    IBoardRepository boardRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<RenameBoardListCommand, BoardListDto>
{
    public async Task<BoardListDto> Handle(RenameBoardListCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var list = await boardListRepository.GetByIdAsync(request.ListId, cancellationToken)
            ?? throw new NotFoundException("List", request.ListId);

        var board = await boardRepository.GetByIdAsync(list.BoardId, cancellationToken)
            ?? throw new NotFoundException("List", request.ListId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "List", request.ListId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        list.Rename(request.Name);
        boardListRepository.Update(list);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BoardListDto(
            list.Id, list.BoardId, list.Name, list.Position.Value, list.CreatedAt, list.UpdatedAt);
    }
}
