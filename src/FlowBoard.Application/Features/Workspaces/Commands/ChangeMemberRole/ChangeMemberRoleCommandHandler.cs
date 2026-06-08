using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Commands.ChangeMemberRole;

/// <summary>Changes a member's role. Owner/Admin only.</summary>
public sealed class ChangeMemberRoleCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<ChangeMemberRoleCommand, WorkspaceMemberDto>
{
    public async Task<WorkspaceMemberDto> Handle(ChangeMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var actorId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException("Workspace", request.WorkspaceId);

        workspace.EnsureAdmin(actorId);
        workspace.ChangeMemberRole(request.UserId, request.NewRole);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var member = workspace.Members.First(m => m.UserId == request.UserId);
        return new WorkspaceMemberDto(member.UserId, member.Role.ToString(), member.JoinedAt);
    }
}