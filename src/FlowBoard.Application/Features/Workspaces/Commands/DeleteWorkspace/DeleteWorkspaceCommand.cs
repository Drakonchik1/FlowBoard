using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Commands.DeleteWorkspace;

public sealed record DeleteWorkspaceCommand(Guid WorkspaceId) : IRequest<Unit>;