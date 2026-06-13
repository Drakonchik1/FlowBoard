using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Projects.Queries.GetProjectById;

public sealed class GetProjectByIdQueryHandler(
    IProjectRepository projectRepository,
    IWorkspaceRepository workspaceRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetProjectByIdQuery, ProjectDto>
{
    public async Task<ProjectDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(project.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Project", request.ProjectId);

        return new ProjectDto(
            project.Id, project.WorkspaceId, project.Name, project.Description, project.CreatedAt, project.UpdatedAt);
    }
}
