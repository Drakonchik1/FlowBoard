using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FlowBoard.API.Services;
using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Features.Boards.Queries.EnsureBoardAccess;
using FlowBoard.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlowBoard.API.Hubs;

/// <summary>
/// Real-time board updates. Clients call <see cref="JoinBoard"/> after connecting with a JWT
/// (pass token as <c>?access_token=</c> query param). Non-members get a generic not-found error.
/// </summary>
[Authorize]
public sealed class BoardHub(
    ISender sender,
    BoardGroupMembershipRegistry membershipRegistry) : Hub<IBoardHubClient>
{
    /// <summary>Subscribe to updates for a board the caller can access.</summary>
    public async Task JoinBoard(Guid boardId)
    {
        await EnsureCanAccessBoardAsync(boardId);
        var groupName = BoardGroupNames.ForBoard(boardId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        membershipRegistry.TrackJoin(Context.ConnectionId, GetUserId(), boardId);
    }

    /// <summary>
    /// Unsubscribe from a board group. Does not verify board access — intentional:
    /// <see cref="Groups.RemoveFromGroupAsync"/> is a no-op when the connection was never in the group,
    /// so an access check would not add security and could leak board existence.
    /// </summary>
    public async Task LeaveBoard(Guid boardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, BoardGroupNames.ForBoard(boardId));
        membershipRegistry.TrackLeave(Context.ConnectionId, boardId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        membershipRegistry.UntrackConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    private async Task EnsureCanAccessBoardAsync(Guid boardId)
    {
        try
        {
            await sender.Send(new EnsureBoardAccessQuery(boardId), Context.ConnectionAborted);
        }
        catch (NotFoundException)
        {
            throw new HubException("Board not found.");
        }
        catch (UnauthorizedException)
        {
            throw new HubException("Authentication is required.");
        }
    }

    private Guid GetUserId()
    {
        var claim = Context.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? Context.User.FindFirst(ClaimTypes.NameIdentifier);

        if (claim is null || !Guid.TryParse(claim.Value, out var userId))
            throw new HubException("Authentication is required.");

        return userId;
    }
}
