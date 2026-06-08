using FlowBoard.Application.Features.Workspaces;
using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Commands.CreateWorkspace;

public sealed record CreateWorkspaceCommand(string Name, string? Slug) : IRequest<WorkspaceDto>;