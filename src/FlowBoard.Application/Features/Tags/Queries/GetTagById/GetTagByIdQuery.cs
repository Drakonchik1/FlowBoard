using FlowBoard.Application.Features.Tags;
using MediatR;

namespace FlowBoard.Application.Features.Tags.Queries.GetTagById;

public sealed record GetTagByIdQuery(Guid TagId) : IRequest<TagDto>;
