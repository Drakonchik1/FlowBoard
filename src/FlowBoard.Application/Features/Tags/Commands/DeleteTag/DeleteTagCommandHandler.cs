using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Tags.Commands.DeleteTag;

public sealed class DeleteTagCommandHandler(
    ITagRepository tagRepository,
    ICardTagRepository cardTagRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<DeleteTagCommand>
{
    public async Task Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var tag = await tagRepository.GetByIdAsync(request.TagId, cancellationToken)
            ?? throw new NotFoundException("Tag", request.TagId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(tag.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Tag", request.TagId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        await cardTagRepository.RemoveAllForTagAsync(tag.Id, cancellationToken);
        tag.SoftDelete();
        tagRepository.Update(tag);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
