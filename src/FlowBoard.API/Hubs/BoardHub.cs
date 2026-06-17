using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FlowBoard.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlowBoard.API.Hubs;

/// <summary>
/// Real-time board updates. Clients call <see cref="JoinBoard"/> after connecting with a JWT
/// (pass token as <c>?access_token=</c> query param). Non-members get a generic not-found error.
/// </summary>
[Authorize]
public sealed class BoardHub(
    IBoardRepository boardRepository,
    IWorkspaceRepository workspaceRepository) : Hub<IBoardHubClient>
{
    /// <summary>Subscribe to updates for a board the caller can access.</summary>
    public async Task JoinBoard(Guid boardId)
    {
        await EnsureCanAccessBoardAsync(boardId);
        await Groups.AddToGroupAsync(Context.ConnectionId, BoardGroupNames.ForBoard(boardId));
    }

    /// <summary>Unsubscribe from a board group.</summary>
    public async Task LeaveBoard(Guid boardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, BoardGroupNames.ForBoard(boardId));
    }

    private async Task EnsureCanAccessBoardAsync(Guid boardId)
    {
        var userId = GetUserId();

        var board = await boardRepository.GetByIdAsync(boardId, Context.ConnectionAborted);
        if (board is null)
            throw new HubException("Board not found.");

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(
            board.WorkspaceId, Context.ConnectionAborted);

        if (workspace is null || !workspace.HasMember(userId))
            throw new HubException("Board not found.");
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
