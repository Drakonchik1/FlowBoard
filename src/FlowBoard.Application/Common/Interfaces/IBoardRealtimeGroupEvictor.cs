namespace FlowBoard.Application.Common.Interfaces;

/// <summary>
/// Removes a user from SignalR board groups when workspace membership or write access is revoked.
/// </summary>
public interface IBoardRealtimeGroupEvictor
{
    Task EvictUserFromBoardGroupsAsync(Guid userId, CancellationToken cancellationToken = default);
}
