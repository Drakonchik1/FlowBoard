using FlowBoard.Domain.Events;

namespace FlowBoard.Application.Common.Interfaces;

/// <summary>
/// Pushes board updates to connected clients (SignalR implementation lives in API).
/// </summary>
public interface IBoardRealtimeNotifier
{
    Task NotifyCardMovedAsync(CardMovedEvent evt, CancellationToken cancellationToken = default);
}
