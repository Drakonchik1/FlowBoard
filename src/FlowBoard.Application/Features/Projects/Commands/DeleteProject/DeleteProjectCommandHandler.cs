using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Projects.Commands.DeleteProject;

public sealed class DeleteProjectCommandHandler(
    IProjectRepository projectRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<DeleteProjectCommand>
{
    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(project.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Project", request.ProjectId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        project.SoftDelete();
        projectRepository.Update(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
