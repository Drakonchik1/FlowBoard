using MediatR;

namespace FlowBoard.Application.Features.Tags.Commands.RemoveTagFromCard;

public sealed record RemoveTagFromCardCommand(Guid CardId, Guid TagId) : IRequest;
