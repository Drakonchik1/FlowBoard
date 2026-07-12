using System.Collections.Concurrent;

namespace FlowBoard.Application.EventHandlers;

/// <summary>
/// Suppresses duplicate assignment emails for the same card/assignee within a short window.
/// </summary>
internal static class AssignmentEmailThrottle
{
    private static readonly ConcurrentDictionary<(Guid CardId, Guid AssigneeId), DateTime> Recent = new();
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    public static bool ShouldSend(Guid cardId, Guid assigneeId)
    {
        var key = (cardId, assigneeId);
        var now = DateTime.UtcNow;

        if (Recent.TryGetValue(key, out var lastSent) && now - lastSent < Window)
            return false;

        Recent[key] = now;
        return true;
    }
}
