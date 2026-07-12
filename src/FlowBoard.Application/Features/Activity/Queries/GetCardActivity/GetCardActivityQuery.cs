using MediatR;

namespace FlowBoard.Application.Features.Activity.Queries.GetCardActivity;

public sealed record GetCardActivityQuery(Guid CardId) : IRequest<IReadOnlyList<ActivityLogDto>>;
