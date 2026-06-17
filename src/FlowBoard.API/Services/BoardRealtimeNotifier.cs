using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Realtime;
using FlowBoard.Domain.Events;
using FlowBoard.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FlowBoard.API.Services;

internal sealed class BoardRealtimeNotifier(IHubContext<BoardHub, IBoardHubClient> hubContext)
    : IBoardRealtimeNotifier
{
    public Task NotifyCardMovedAsync(CardMovedEvent evt, CancellationToken cancellationToken = default) =>
        hubContext.Clients
            .Group(BoardGroupNames.ForBoard(evt.BoardId))
            .CardMoved(new CardMovedMessage(
                evt.CardId,
                evt.BoardId,
                evt.FromListId,
                evt.ToListId,
                evt.Position));
}
