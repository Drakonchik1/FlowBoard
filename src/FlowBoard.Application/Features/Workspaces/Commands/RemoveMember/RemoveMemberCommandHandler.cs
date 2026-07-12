using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowBoard.Application.Features.Workspaces.Commands.RemoveMember;

/// <summary>
/// Removes a member. Admins can remove members but the Owner is protected by the domain.
/// Self-removal is allowed for non-owners (acts as "leave workspace").
/// </summary>
public sealed class RemoveMemberCommandHandler(
    IWorkspaceRepository workspaceRepository,
    ICardRepository cardRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IBoardRealtimeGroupEvictor boardGroupEvictor,
    ILogger<RemoveMemberCommandHandler> logger) : IRequestHandler<RemoveMemberCommand, Unit>
{
    public async Task<Unit> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var actorId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException("Workspace", request.WorkspaceId);

        WorkspaceAccess.EnsureCanManageMemberOrNotFound(
            workspace, actorId, request.UserId, request.WorkspaceId);

        workspace.RemoveMember(request.UserId);

        await cardRepository.ClearAssigneeForUserInWorkspaceAsync(request.WorkspaceId, request.UserId, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await boardGroupEvictor.EvictUserFromBoardGroupsAsync(request.UserId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "SignalR group eviction failed after removing user {UserId} from workspace {WorkspaceId}",
                request.UserId,
                request.WorkspaceId);
        }

        return Unit.Value;
    }
}