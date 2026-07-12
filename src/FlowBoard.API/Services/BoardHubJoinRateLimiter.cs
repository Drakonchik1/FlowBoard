using System.Collections.Concurrent;

namespace FlowBoard.API.Services;

/// <summary>Per-user sliding-window throttle for <see cref="Hubs.BoardHub.JoinBoard"/>.</summary>
public sealed class BoardHubJoinRateLimiter
{
    private const int MaxJoinsPerMinute = 30;
    private readonly ConcurrentDictionary<Guid, Queue<DateTimeOffset>> _attempts = new();

    public bool TryAcquire(Guid userId)
    {
        var now = DateTimeOffset.UtcNow;
        var windowStart = now.AddMinutes(-1);
        var queue = _attempts.GetOrAdd(userId, _ => new Queue<DateTimeOffset>());

        lock (queue)
        {
            while (queue.Count > 0 && queue.Peek() < windowStart)
                queue.Dequeue();

            if (queue.Count >= MaxJoinsPerMinute)
                return false;

            queue.Enqueue(now);
            return true;
        }
    }
}
