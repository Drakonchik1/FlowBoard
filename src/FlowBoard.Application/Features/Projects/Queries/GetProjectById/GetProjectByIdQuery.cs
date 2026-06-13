using FlowBoard.Application.Features.Projects;
using MediatR;

namespace FlowBoard.Application.Features.Projects.Queries.GetProjectById;

public sealed record GetProjectByIdQuery(Guid ProjectId) : IRequest<ProjectDto>;
