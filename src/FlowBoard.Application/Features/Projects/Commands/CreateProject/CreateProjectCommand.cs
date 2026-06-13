using FlowBoard.Application.Features.Projects;
using MediatR;

namespace FlowBoard.Application.Features.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(Guid WorkspaceId, string Name, string? Description) : IRequest<ProjectDto>;
