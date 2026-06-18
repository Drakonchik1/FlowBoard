using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Events;

namespace FlowBoard.IntegrationTests;

/// <summary>
/// Records <see cref="CardMovedEvent"/> invocations so integration tests can verify the
/// post-commit MediatR → notifier pipeline without spinning up SignalR.
/// </summary>
public sealed class CapturingBoardRealtimeNotifier : IBoardRealtimeNotifier
{
    private readonly List<CardMovedEvent> _events = [];
    private readonly object _lock = new();

    public IReadOnlyList<CardMovedEvent> Events
    {
        get
        {
            lock (_lock)
                return _events.ToList();
        }
    }

    public void Clear()
    {
        lock (_lock)
            _events.Clear();
    }

    public Task NotifyCardMovedAsync(CardMovedEvent evt, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            _events.Add(evt);

        return Task.CompletedTask;
    }
}
