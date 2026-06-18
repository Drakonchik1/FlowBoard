using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Boards.Queries.EnsureBoardAccess;

/// <summary>
/// Authorization gate for board-scoped operations (e.g. SignalR <c>JoinBoard</c>).
/// Non-members get a 404 for the board id (anti-enumeration).
/// </summary>
public sealed class EnsureBoardAccessQueryHandler(
    IBoardRepository boardRepository,
    IWorkspaceRepository workspaceRepository,
    ICurrentUserService currentUser) : IRequestHandler<EnsureBoardAccessQuery, Unit>
{
    public async Task<Unit> Handle(EnsureBoardAccessQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var board = await boardRepository.GetByIdAsync(request.BoardId, cancellationToken)
            ?? throw new NotFoundException("Board", request.BoardId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Board", request.BoardId);

        return Unit.Value;
    }
}
