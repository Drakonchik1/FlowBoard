using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Commands.ChangeMemberRole;

public sealed record ChangeMemberRoleCommand(
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceRole NewRole) : IRequest<WorkspaceMemberDto>;