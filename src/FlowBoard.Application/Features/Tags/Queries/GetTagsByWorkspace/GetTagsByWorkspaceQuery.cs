using FlowBoard.Application.Features.Tags;
using MediatR;

namespace FlowBoard.Application.Features.Tags.Queries.GetTagsByWorkspace;

public sealed record GetTagsByWorkspaceQuery(Guid WorkspaceId) : IRequest<IReadOnlyList<TagDto>>;
