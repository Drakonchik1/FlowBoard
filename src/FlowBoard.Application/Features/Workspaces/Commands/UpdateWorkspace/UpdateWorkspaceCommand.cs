using FlowBoard.Application.Features.Workspaces;
using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Commands.UpdateWorkspace;

public sealed record UpdateWorkspaceCommand(Guid WorkspaceId, string Name) : IRequest<WorkspaceDto>;