using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Projects.Queries.GetProjectsInWorkspace;

public sealed class GetProjectsInWorkspaceQueryHandler(
    IProjectRepository projectRepository,
    IWorkspaceRepository workspaceRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetProjectsInWorkspaceQuery, IReadOnlyList<ProjectDto>>
{
    public async Task<IReadOnlyList<ProjectDto>> Handle(
        GetProjectsInWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Workspace", request.WorkspaceId);

        var projects = await projectRepository.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);

        return projects
            .Select(p => new ProjectDto(p.Id, p.WorkspaceId, p.Name, p.Description, p.CreatedAt, p.UpdatedAt))
            .ToList();
    }
}
