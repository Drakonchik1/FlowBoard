using FlowBoard.API.Hubs;
using FlowBoard.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FlowBoard.API.Services;

internal sealed class BoardRealtimeGroupEvictor(
    IHubContext<BoardHub, IBoardHubClient> hubContext,
    BoardGroupMembershipRegistry registry)
    : IBoardRealtimeGroupEvictor
{
    public async Task EvictUserFromBoardGroupsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        foreach (var membership in registry.GetMemberships(userId))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await hubContext.Groups.RemoveFromGroupAsync(
                membership.ConnectionId,
                BoardGroupNames.ForBoard(membership.BoardId),
                cancellationToken);

            registry.TrackLeave(membership.ConnectionId, membership.BoardId);
        }
    }
}
