using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Commands.InviteMember;

/// <summary>
/// Adds a user to a workspace with the given role. Owner/Admin only.
/// Domain entity enforces uniqueness (no double-membership) and forbids assigning Owner role.
/// </summary>
public sealed class InviteMemberCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<InviteMemberCommand, WorkspaceMemberDto>
{
    public async Task<WorkspaceMemberDto> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        var actorId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException("Workspace", request.WorkspaceId);

        WorkspaceAccess.EnsureAdminOrNotFound(workspace, actorId, request.WorkspaceId);

        // Missing invitee returns the same workspace 404 so admins cannot probe valid user IDs.
        var invitee = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Workspace", request.WorkspaceId);

        if (workspace.HasMember(invitee.Id))
            throw new ConflictException("User is already a member of this workspace.");

        var member = workspace.InviteMember(invitee.Id, WorkspaceRoleMapper.ToDomain(request.Role));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new WorkspaceMemberDto(member.UserId, member.Role.ToString(), member.JoinedAt);
    }
}