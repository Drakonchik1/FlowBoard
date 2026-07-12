using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Application.Features.Tags;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Tags.Queries.GetTagById;

public sealed class GetTagByIdQueryHandler(
    ITagRepository tagRepository,
    IWorkspaceRepository workspaceRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetTagByIdQuery, TagDto>
{
    public async Task<TagDto> Handle(GetTagByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var tag = await tagRepository.GetByIdAsync(request.TagId, cancellationToken)
            ?? throw new NotFoundException("Tag", request.TagId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(tag.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Tag", request.TagId);

        return new TagDto(tag.Id, tag.WorkspaceId, tag.Name, tag.Color, tag.CreatedAt, tag.UpdatedAt);
    }
}
