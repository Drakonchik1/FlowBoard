using FlowBoard.Application.Features.Workspaces;
using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Queries.GetMyWorkspaces;

public sealed record GetMyWorkspacesQuery() : IRequest<IReadOnlyList<WorkspaceDto>>;