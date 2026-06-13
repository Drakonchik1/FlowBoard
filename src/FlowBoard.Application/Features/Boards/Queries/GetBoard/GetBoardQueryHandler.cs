using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Boards.Queries.GetBoard;

/// <summary>
/// Reads the whole board through Dapper (raw SQL, no tracking). Authorization still goes through the
/// workspace aggregate: the read model carries WorkspaceId, which we use to verify membership.
/// Non-members get a 404 for the board id (anti-enumeration).
/// </summary>
public sealed class GetBoardQueryHandler(
    IBoardReadService boardReadService,
    IWorkspaceRepository workspaceRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetBoardQuery, BoardViewDto>
{
    public async Task<BoardViewDto> Handle(GetBoardQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var board = await boardReadService.GetBoardAsync(request.BoardId, cancellationToken)
            ?? throw new NotFoundException("Board", request.BoardId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Board", request.BoardId);

        return board;
    }
}
