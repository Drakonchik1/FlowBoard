using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Boards.Queries.GetBoardsInProject;

public sealed class GetBoardsInProjectQueryHandler(
    IBoardRepository boardRepository,
    IProjectRepository projectRepository,
    IWorkspaceRepository workspaceRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetBoardsInProjectQuery, IReadOnlyList<BoardDto>>
{
    public async Task<IReadOnlyList<BoardDto>> Handle(
        GetBoardsInProjectQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(project.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Project", request.ProjectId);

        var boards = await boardRepository.GetByProjectAsync(request.ProjectId, cancellationToken);

        return boards
            .Select(b => new BoardDto(b.Id, b.ProjectId, b.WorkspaceId, b.Name, b.CreatedAt, b.UpdatedAt))
            .ToList();
    }
}
