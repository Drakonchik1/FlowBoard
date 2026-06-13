using FlowBoard.Application.Features.Cards;
using MediatR;

namespace FlowBoard.Application.Features.Cards.Commands.CreateCard;

public sealed record CreateCardCommand(
    Guid ListId,
    string Title,
    string? Description,
    CardPriority Priority = CardPriority.None) : IRequest<CardDto>;
