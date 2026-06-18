using FlowBoard.Application.Common.Interfaces;

namespace FlowBoard.IntegrationTests;

/// <summary>Integration-test stub — SignalR group eviction is not exercised in this suite.</summary>
internal sealed class NoOpBoardRealtimeGroupEvictor : IBoardRealtimeGroupEvictor
{
    public Task EvictUserFromBoardGroupsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
