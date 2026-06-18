using System.Collections.Concurrent;

namespace FlowBoard.API.Services;

public readonly record struct BoardGroupMembership(string ConnectionId, Guid BoardId);

/// <summary>Tracks which connections joined which board groups (process-local).</summary>
public sealed class BoardGroupMembershipRegistry
{
    private readonly ConcurrentDictionary<string, Guid> _connectionUsers = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, byte>> _connectionBoards = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _userConnections = new();

    public void TrackJoin(string connectionId, Guid userId, Guid boardId)
    {
        _connectionUsers[connectionId] = userId;
        GetOrAddBoardSet(connectionId)[boardId] = 0;
        GetOrAddConnectionSet(userId)[connectionId] = 0;
    }

    public void TrackLeave(string connectionId, Guid boardId)
    {
        if (_connectionBoards.TryGetValue(connectionId, out var boards))
            boards.TryRemove(boardId, out _);

        if (_connectionBoards.TryGetValue(connectionId, out boards) && !boards.IsEmpty)
            return;

        if (!_connectionUsers.TryRemove(connectionId, out var userId))
            return;

        _connectionBoards.TryRemove(connectionId, out _);

        if (_userConnections.TryGetValue(userId, out var connections))
        {
            connections.TryRemove(connectionId, out _);
            if (connections.IsEmpty)
                _userConnections.TryRemove(userId, out _);
        }
    }

    public void UntrackConnection(string connectionId)
    {
        if (!_connectionUsers.TryRemove(connectionId, out var userId))
            return;

        _connectionBoards.TryRemove(connectionId, out _);

        if (_userConnections.TryGetValue(userId, out var connections))
        {
            connections.TryRemove(connectionId, out _);
            if (connections.IsEmpty)
                _userConnections.TryRemove(userId, out _);
        }
    }

    public IReadOnlyList<BoardGroupMembership> GetMemberships(Guid userId)
    {
        if (!_userConnections.TryGetValue(userId, out var connections))
            return [];

        var memberships = new List<BoardGroupMembership>();

        foreach (var connectionId in connections.Keys)
        {
            if (!_connectionBoards.TryGetValue(connectionId, out var boards))
                continue;

            foreach (var boardId in boards.Keys)
                memberships.Add(new BoardGroupMembership(connectionId, boardId));
        }

        return memberships;
    }

    private ConcurrentDictionary<Guid, byte> GetOrAddBoardSet(string connectionId) =>
        _connectionBoards.GetOrAdd(connectionId, _ => new ConcurrentDictionary<Guid, byte>());

    private ConcurrentDictionary<string, byte> GetOrAddConnectionSet(Guid userId) =>
        _userConnections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
}
