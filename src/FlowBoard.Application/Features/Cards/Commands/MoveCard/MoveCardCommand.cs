using FlowBoard.Application.Features.Cards;
using MediatR;

namespace FlowBoard.Application.Features.Cards.Commands.MoveCard;

/// <summary>
/// Moves a card to a target list and position. The client supplies the two neighbours the card is
/// dropped between in the target list: <paramref name="BeforeCardId"/> (null = drop at the top) and
/// <paramref name="AfterCardId"/> (null = drop at the bottom). The target list must be on the same board.
/// </summary>
public sealed record MoveCardCommand(
    Guid CardId,
    Guid TargetListId,
    Guid? BeforeCardId,
    Guid? AfterCardId) : IRequest<CardDto>;
