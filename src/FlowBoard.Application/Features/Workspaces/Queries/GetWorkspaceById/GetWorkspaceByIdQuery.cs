using FlowBoard.Application.Features.Workspaces;
using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Queries.GetWorkspaceById;

public sealed record GetWorkspaceByIdQuery(Guid WorkspaceId) : IRequest<WorkspaceDetailDto>;