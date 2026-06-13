using MediatR;

namespace FlowBoard.Application.Features.Cards.Commands.DeleteCard;

public sealed record DeleteCardCommand(Guid CardId) : IRequest;
