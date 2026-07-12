using FlowBoard.Application.Features.Tags;
using MediatR;

namespace FlowBoard.Application.Features.Tags.Commands.CreateTag;

public sealed record CreateTagCommand(Guid WorkspaceId, string Name, string? Color) : IRequest<TagDto>;
