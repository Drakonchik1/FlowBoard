using FlowBoard.API.Services;

namespace FlowBoard.UnitTests.Services;

public sealed class BoardGroupMembershipRegistryTests
{
    [Fact]
    public void TrackLeave_OneBoard_RemainingBoardMembershipsPreserved()
    {
        var userId = Guid.NewGuid();
        var boardA = Guid.NewGuid();
        var boardB = Guid.NewGuid();
        const string connectionId = "conn-multi";

        var registry = new BoardGroupMembershipRegistry();
        registry.TrackJoin(connectionId, userId, boardA);
        registry.TrackJoin(connectionId, userId, boardB);

        registry.TrackLeave(connectionId, boardA);

        var memberships = registry.GetMemberships(userId);
        Assert.Single(memberships);
        Assert.Equal(boardB, memberships[0].BoardId);
    }
}
