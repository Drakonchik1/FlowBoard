using FlowBoard.Application.Features.Tags;
using MediatR;

namespace FlowBoard.Application.Features.Tags.Queries.GetTagsByCard;

public sealed record GetTagsByCardQuery(Guid CardId) : IRequest<IReadOnlyList<TagDto>>;
