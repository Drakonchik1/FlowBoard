using MediatR;

namespace FlowBoard.Application.Features.Projects.Commands.DeleteProject;

public sealed record DeleteProjectCommand(Guid ProjectId) : IRequest;
