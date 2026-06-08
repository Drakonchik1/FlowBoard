using FlowBoard.Domain.Entities;
using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Commands.InviteMember;

public sealed record InviteMemberCommand(
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceMemberRole Role) : IRequest<WorkspaceMemberDto>;