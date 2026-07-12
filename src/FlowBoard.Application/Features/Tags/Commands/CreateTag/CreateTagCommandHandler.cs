using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Application.Features.Tags;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Tags.Commands.CreateTag;

public sealed class CreateTagCommandHandler(
    IWorkspaceRepository workspaceRepository,
    ITagRepository tagRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<CreateTagCommand, TagDto>
{
    public async Task<TagDto> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Workspace", request.WorkspaceId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        var existing = await tagRepository.GetByNameInWorkspaceAsync(
            request.WorkspaceId, request.Name, cancellationToken);
        if (existing is not null)
            throw new ConflictException("A tag with this name already exists in the workspace.");

        var tag = Tag.Create(request.WorkspaceId, request.Name, request.Color);
        await tagRepository.AddAsync(tag, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(tag);
    }

    private static TagDto ToDto(Tag tag) => new(
        tag.Id, tag.WorkspaceId, tag.Name, tag.Color, tag.CreatedAt, tag.UpdatedAt);
}
