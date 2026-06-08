using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Commands.UpdateWorkspace;

public sealed class UpdateWorkspaceCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<UpdateWorkspaceCommand, WorkspaceDto>
{
    public async Task<WorkspaceDto> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException("Workspace", request.WorkspaceId);

        WorkspaceAccess.EnsureAdminOrNotFound(workspace, userId, request.WorkspaceId);
        workspace.Rename(request.Name);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new WorkspaceDto(workspace.Id, workspace.Name, workspace.Slug.Value,
            workspace.OwnerId, workspace.CreatedAt, workspace.Members.Count);
    }
}