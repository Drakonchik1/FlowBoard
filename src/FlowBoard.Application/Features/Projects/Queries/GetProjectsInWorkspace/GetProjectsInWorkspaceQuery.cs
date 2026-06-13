using FlowBoard.Application.Features.Projects;
using MediatR;

namespace FlowBoard.Application.Features.Projects.Queries.GetProjectsInWorkspace;

public sealed record GetProjectsInWorkspaceQuery(Guid WorkspaceId) : IRequest<IReadOnlyList<ProjectDto>>;
