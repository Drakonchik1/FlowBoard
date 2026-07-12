using FlowBoard.Application.Features.Cards;
using MediatR;

namespace FlowBoard.Application.Features.Cards.Commands.AssignCard;

public sealed record AssignCardCommand(Guid CardId, Guid? AssigneeId) : IRequest<CardDto>;
