using MediatR;

namespace FlowBoard.Application.Features.Tags.Commands.DeleteTag;

public sealed record DeleteTagCommand(Guid TagId) : IRequest;
