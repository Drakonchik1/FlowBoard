using FlowBoard.API.Services;

namespace FlowBoard.UnitTests.Hubs;

public sealed class BoardHubJoinRateLimiterTests
{
    [Fact]
    public void TryAcquire_AllowsUpToLimitPerMinute()
    {
        var limiter = new BoardHubJoinRateLimiter();
        var userId = Guid.NewGuid();

        for (var i = 0; i < 30; i++)
            Assert.True(limiter.TryAcquire(userId));

        Assert.False(limiter.TryAcquire(userId));
    }
}
