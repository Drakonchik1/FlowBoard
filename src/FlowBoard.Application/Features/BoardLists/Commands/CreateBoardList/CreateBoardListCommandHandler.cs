using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using MediatR;

namespace FlowBoard.Application.Features.BoardLists.Commands.CreateBoardList;

public sealed class CreateBoardListCommandHandler(
    IBoardListRepository boardListRepository,
    IBoardRepository boardRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<CreateBoardListCommand, BoardListDto>
{
    public async Task<BoardListDto> Handle(CreateBoardListCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var board = await boardRepository.GetByIdAsync(request.BoardId, cancellationToken)
            ?? throw new NotFoundException("Board", request.BoardId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Board", request.BoardId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        // Append to the end: pick a position after the current last list.
        var lastPosition = await boardListRepository.GetLastPositionAsync(request.BoardId, cancellationToken);
        var position = FractionalIndex.Between(lastPosition, null);

        var list = BoardList.Create(request.BoardId, request.Name, position);
        await boardListRepository.AddAsync(list, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BoardListDto(
            list.Id, list.BoardId, list.Name, list.Position.Value, list.CreatedAt, list.UpdatedAt);
    }
}
