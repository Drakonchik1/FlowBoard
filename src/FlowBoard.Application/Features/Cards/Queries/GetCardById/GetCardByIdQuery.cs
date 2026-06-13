using FlowBoard.Application.Features.Cards;
using MediatR;

namespace FlowBoard.Application.Features.Cards.Queries.GetCardById;

public sealed record GetCardByIdQuery(Guid CardId) : IRequest<CardDto>;
