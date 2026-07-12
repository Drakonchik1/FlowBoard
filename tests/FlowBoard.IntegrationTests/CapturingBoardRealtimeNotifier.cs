using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Events;

namespace FlowBoard.IntegrationTests;

/// <summary>
/// Records realtime notifier invocations so integration tests can verify the
/// post-commit MediatR → notifier pipeline without spinning up SignalR.
/// </summary>
public sealed class CapturingBoardRealtimeNotifier : IBoardRealtimeNotifier
{
    private readonly List<CardMovedEvent> _cardMovedEvents = [];
    private readonly List<CommentAddedEvent> _commentAddedEvents = [];
    private readonly object _lock = new();

    public IReadOnlyList<CardMovedEvent> CardMovedEvents
    {
        get
        {
            lock (_lock)
                return _cardMovedEvents.ToList();
        }
    }

    /// <summary>Alias for <see cref="CardMovedEvents"/> — existing board workflow tests.</summary>
    public IReadOnlyList<CardMovedEvent> Events => CardMovedEvents;

    public IReadOnlyList<CommentAddedEvent> CommentAddedEvents
    {
        get
        {
            lock (_lock)
                return _commentAddedEvents.ToList();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _cardMovedEvents.Clear();
            _commentAddedEvents.Clear();
        }
    }

    public Task NotifyCardMovedAsync(CardMovedEvent evt, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            _cardMovedEvents.Add(evt);

        return Task.CompletedTask;
    }

    public Task NotifyCommentAddedAsync(CommentAddedEvent evt, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            _commentAddedEvents.Add(evt);

        return Task.CompletedTask;
    }
}
