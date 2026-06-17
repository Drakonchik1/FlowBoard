using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Events;

namespace FlowBoard.IntegrationTests;

/// <summary>Integration-test stub — SignalR is not exercised in this suite.</summary>
internal sealed class NoOpBoardRealtimeNotifier : IBoardRealtimeNotifier
{
    public Task NotifyCardMovedAsync(CardMovedEvent evt, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
