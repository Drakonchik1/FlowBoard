using FlowBoard.Application.Features.Tags;
using MediatR;

namespace FlowBoard.Application.Features.Tags.Commands.ApplyTagToCard;

public sealed record ApplyTagToCardCommand(Guid CardId, Guid TagId) : IRequest<TagDto>;
