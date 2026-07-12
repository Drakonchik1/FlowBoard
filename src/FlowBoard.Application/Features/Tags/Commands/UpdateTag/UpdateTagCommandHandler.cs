using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Application.Features.Tags;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Tags.Commands.UpdateTag;

public sealed class UpdateTagCommandHandler(
    ITagRepository tagRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<UpdateTagCommand, TagDto>
{
    public async Task<TagDto> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var tag = await tagRepository.GetByIdAsync(request.TagId, cancellationToken)
            ?? throw new NotFoundException("Tag", request.TagId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(tag.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Tag", request.TagId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        var duplicate = await tagRepository.GetByNameInWorkspaceAsync(tag.WorkspaceId, request.Name, cancellationToken);
        if (duplicate is not null && duplicate.Id != tag.Id)
            throw new ConflictException("A tag with this name already exists in the workspace.");

        tag.Update(request.Name, request.Color);
        tagRepository.Update(tag);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new TagDto(tag.Id, tag.WorkspaceId, tag.Name, tag.Color, tag.CreatedAt, tag.UpdatedAt);
    }
}
