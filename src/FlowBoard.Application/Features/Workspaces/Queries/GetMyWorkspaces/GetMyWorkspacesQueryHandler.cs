using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Queries.GetMyWorkspaces;

public sealed class GetMyWorkspacesQueryHandler(
    IWorkspaceRepository workspaceRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetMyWorkspacesQuery, IReadOnlyList<WorkspaceDto>>
{
    public async Task<IReadOnlyList<WorkspaceDto>> Handle(GetMyWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("You must be authenticated.");

        var workspaces = await workspaceRepository.GetWorkspacesForUserAsync(userId, cancellationToken);

        return workspaces
            .Select(w => new WorkspaceDto(w.Id, w.Name, w.Slug.Value, w.OwnerId, w.CreatedAt, w.Members.Count))
            .ToList();
    }
}