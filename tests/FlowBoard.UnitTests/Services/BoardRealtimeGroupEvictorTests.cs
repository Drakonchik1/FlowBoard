using FlowBoard.API.Hubs;
using FlowBoard.API.Services;
using FlowBoard.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace FlowBoard.UnitTests.Services;

public sealed class BoardRealtimeGroupEvictorTests
{
    [Fact]
    public async Task EvictUserFromBoardGroupsAsync_RemovesStaleGroupMemberships()
    {
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string connectionId = "conn-stale";

        var registry = new BoardGroupMembershipRegistry();
        registry.TrackJoin(connectionId, userId, boardId);

        var groups = new Mock<IGroupManager>();
        var hubContext = new Mock<IHubContext<BoardHub, IBoardHubClient>>();
        hubContext.Setup(h => h.Groups).Returns(groups.Object);

        IBoardRealtimeGroupEvictor evictor = new BoardRealtimeGroupEvictor(hubContext.Object, registry);

        await evictor.EvictUserFromBoardGroupsAsync(userId);

        groups.Verify(
            g => g.RemoveFromGroupAsync(
                connectionId,
                $"board:{boardId}",
                It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Empty(registry.GetMemberships(userId));
    }

    [Fact]
    public async Task EvictUserFromBoardGroupsAsync_NoTrackedMemberships_IsNoOp()
    {
        var registry = new BoardGroupMembershipRegistry();
        var groups = new Mock<IGroupManager>();
        var hubContext = new Mock<IHubContext<BoardHub, IBoardHubClient>>();
        hubContext.Setup(h => h.Groups).Returns(groups.Object);

        IBoardRealtimeGroupEvictor evictor = new BoardRealtimeGroupEvictor(hubContext.Object, registry);

        await evictor.EvictUserFromBoardGroupsAsync(Guid.NewGuid());

        groups.Verify(
            g => g.RemoveFromGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
