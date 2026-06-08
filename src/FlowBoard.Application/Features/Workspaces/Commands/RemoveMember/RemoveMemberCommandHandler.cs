using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Commands.RemoveMember;

/// <summary>
/// Removes a member. Admins can remove members but the Owner is protected by the domain.
/// Self-removal is allowed for non-owners (acts as "leave workspace").
/// </summary>
public sealed class RemoveMemberCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<RemoveMemberCommand, Unit>
{
    public async Task<Unit> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var actorId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException("Workspace", request.WorkspaceId);

        // Either the user is removing themselves (leave), or they are Admin/Owner removing someone else
        if (request.UserId != actorId)
            workspace.EnsureAdmin(actorId);
        else
            workspace.EnsureMember(actorId);

        workspace.RemoveMember(request.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}