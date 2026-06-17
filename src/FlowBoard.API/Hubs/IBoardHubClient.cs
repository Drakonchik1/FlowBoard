using FlowBoard.Application.Common.Realtime;

namespace FlowBoard.API.Hubs;

/// <summary>Strongly typed client callbacks for <see cref="BoardHub"/>.</summary>
public interface IBoardHubClient
{
    Task CardMoved(CardMovedMessage message);
}
