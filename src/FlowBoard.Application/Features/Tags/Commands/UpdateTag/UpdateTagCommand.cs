using FlowBoard.Application.Features.Tags;
using MediatR;

namespace FlowBoard.Application.Features.Tags.Commands.UpdateTag;

public sealed record UpdateTagCommand(Guid TagId, string Name, string? Color) : IRequest<TagDto>;
