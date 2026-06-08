using FlowBoard.Domain.Entities;
using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Commands.ChangeMemberRole;

public sealed record ChangeMemberRoleCommand(
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceMemberRole NewRole) : IRequest<WorkspaceMemberDto>;