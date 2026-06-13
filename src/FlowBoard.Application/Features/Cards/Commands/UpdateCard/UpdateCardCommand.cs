using FlowBoard.Application.Features.Cards;
using MediatR;

namespace FlowBoard.Application.Features.Cards.Commands.UpdateCard;

public sealed record UpdateCardCommand(
    Guid CardId,
    string Title,
    string? Description,
    CardPriority Priority) : IRequest<CardDto>;
