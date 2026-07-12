using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Application.Features.Tags;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Tags.Queries.GetTagsByWorkspace;

public sealed class GetTagsByWorkspaceQueryHandler(
    ITagRepository tagRepository,
    IWorkspaceRepository workspaceRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetTagsByWorkspaceQuery, IReadOnlyList<TagDto>>
{
    public async Task<IReadOnlyList<TagDto>> Handle(
        GetTagsByWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Workspace", request.WorkspaceId);

        var tags = await tagRepository.GetByWorkspaceIdAsync(request.WorkspaceId, cancellationToken);
        return tags.Select(t => new TagDto(t.Id, t.WorkspaceId, t.Name, t.Color, t.CreatedAt, t.UpdatedAt)).ToList();
    }
}
