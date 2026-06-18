using MediatR;

namespace FlowBoard.Application.Features.Boards.Queries.EnsureBoardAccess;

/// <summary>Verifies the current user is a workspace member with access to the board (404 if not).</summary>
public sealed record EnsureBoardAccessQuery(Guid BoardId) : IRequest<Unit>;
