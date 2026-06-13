using FlowBoard.Application.Features.Projects;
using MediatR;

namespace FlowBoard.Application.Features.Projects.Commands.UpdateProject;

public sealed record UpdateProjectCommand(Guid ProjectId, string Name, string? Description) : IRequest<ProjectDto>;
