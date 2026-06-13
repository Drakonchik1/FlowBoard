using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Workspace", request.WorkspaceId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        var project = Project.Create(request.WorkspaceId, request.Name, request.Description);
        await projectRepository.AddAsync(project, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProjectDto(
            project.Id, project.WorkspaceId, project.Name, project.Description, project.CreatedAt, project.UpdatedAt);
    }
}
