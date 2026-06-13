using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using MediatR;

namespace FlowBoard.Application.Features.BoardLists.Commands.MoveBoardList;

public sealed class MoveBoardListCommandHandler(
    IBoardListRepository boardListRepository,
    IBoardRepository boardRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<MoveBoardListCommand, BoardListDto>
{
    public async Task<BoardListDto> Handle(MoveBoardListCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var list = await boardListRepository.GetByIdAsync(request.ListId, cancellationToken)
            ?? throw new NotFoundException("List", request.ListId);

        var board = await boardRepository.GetByIdAsync(list.BoardId, cancellationToken)
            ?? throw new NotFoundException("List", request.ListId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "List", request.ListId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        var before = await ResolveNeighbourAsync(request.BeforeListId, list, cancellationToken);
        var after = await ResolveNeighbourAsync(request.AfterListId, list, cancellationToken);

        if (before is not null && after is not null &&
            string.CompareOrdinal(before.Position.Value, after.Position.Value) >= 0)
        {
            throw new DomainException("BeforeListId must come before AfterListId.");
        }

        var position = FractionalIndex.Between(before?.Position, after?.Position);
        list.MoveTo(position);

        boardListRepository.Update(list);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BoardListDto(
            list.Id, list.BoardId, list.Name, list.Position.Value, list.CreatedAt, list.UpdatedAt);
    }

    private async Task<BoardList?> ResolveNeighbourAsync(
        Guid? neighbourId, BoardList movingList, CancellationToken cancellationToken)
    {
        if (neighbourId is null)
            return null;

        if (neighbourId == movingList.Id)
            throw new DomainException("A list cannot be positioned relative to itself.");

        var neighbour = await boardListRepository.GetByIdAsync(neighbourId.Value, cancellationToken)
            ?? throw new DomainException("Neighbour list does not exist.");

        if (neighbour.BoardId != movingList.BoardId)
            throw new DomainException("Neighbour list belongs to a different board.");

        return neighbour;
    }
}
