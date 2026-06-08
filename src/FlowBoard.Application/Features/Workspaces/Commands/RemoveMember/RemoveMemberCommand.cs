using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Commands.RemoveMember;

public sealed record RemoveMemberCommand(Guid WorkspaceId, Guid UserId) : IRequest<Unit>;