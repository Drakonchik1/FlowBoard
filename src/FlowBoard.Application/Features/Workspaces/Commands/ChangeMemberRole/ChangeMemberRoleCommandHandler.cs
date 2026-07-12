using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowBoard.Application.Features.Workspaces.Commands.ChangeMemberRole;

/// <summary>Changes a member's role. Owner/Admin only.</summary>
public sealed class ChangeMemberRoleCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IBoardRealtimeGroupEvictor boardGroupEvictor,
    ILogger<ChangeMemberRoleCommandHandler> logger) : IRequestHandler<ChangeMemberRoleCommand, WorkspaceMemberDto>
{
    public async Task<WorkspaceMemberDto> Handle(ChangeMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var actorId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException("Workspace", request.WorkspaceId);

        WorkspaceAccess.EnsureAdminOrNotFound(workspace, actorId, request.WorkspaceId);

        var previousRole = workspace.GetRole(request.UserId);
        var newRole = WorkspaceRoleMapper.ToDomain(request.NewRole);
        workspace.ChangeMemberRole(request.UserId, newRole);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (newRole == WorkspaceMemberRole.Viewer && previousRole != WorkspaceMemberRole.Viewer)
        {
            try
            {
                await boardGroupEvictor.EvictUserFromBoardGroupsAsync(request.UserId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "SignalR group eviction failed after downgrading user {UserId} to Viewer in workspace {WorkspaceId}",
                    request.UserId,
                    request.WorkspaceId);
            }
        }

        var member = workspace.Members.First(m => m.UserId == request.UserId);
        return new WorkspaceMemberDto(member.UserId, member.Role.ToString(), member.JoinedAt);
    }
}