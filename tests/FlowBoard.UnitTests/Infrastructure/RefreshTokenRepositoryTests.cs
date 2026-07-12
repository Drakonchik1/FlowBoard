using FlowBoard.Domain.Entities;
using FlowBoard.Infrastructure.Persistence;
using FlowBoard.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.UnitTests.Infrastructure;

public sealed class RefreshTokenRepositoryTests
{
    [Fact]
    public async Task RevokeExpiredAsync_RevokesOnlyExpiredNonRevokedTokens()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        context.Users.Add(User.Create("user@example.com", "Test User", "hash"));

        var expiredActive = RefreshToken.CreateNew(userId, "expired-active", DateTime.UtcNow.AddDays(-1));
        var expiredRevoked = RefreshToken.CreateNew(userId, "expired-revoked", DateTime.UtcNow.AddDays(-1));
        expiredRevoked.Revoke();
        var active = RefreshToken.CreateNew(userId, "active", DateTime.UtcNow.AddDays(7));

        context.RefreshTokens.AddRange(expiredActive, expiredRevoked, active);
        await context.SaveChangesAsync();

        var repository = new RefreshTokenRepository(context);

        var revokedCount = await repository.RevokeExpiredAsync();
        await context.SaveChangesAsync();

        Assert.Equal(1, revokedCount);
        Assert.True(expiredActive.IsRevoked);
        Assert.True(expiredRevoked.IsRevoked);
        Assert.False(active.IsRevoked);
    }

    [Fact]
    public async Task RevokeExpiredAsync_ReturnsZeroWhenNothingToRevoke()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        context.Users.Add(User.Create("user@example.com", "Test User", "hash"));
        context.RefreshTokens.Add(RefreshToken.CreateNew(userId, "active", DateTime.UtcNow.AddDays(7)));
        await context.SaveChangesAsync();

        var repository = new RefreshTokenRepository(context);

        var revokedCount = await repository.RevokeExpiredAsync();

        Assert.Equal(0, revokedCount);
    }

    private static FlowBoardDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FlowBoardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FlowBoardDbContext(options);
    }
}
