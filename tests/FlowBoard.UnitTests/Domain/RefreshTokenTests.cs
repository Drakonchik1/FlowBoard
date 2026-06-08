using FlowBoard.Domain.Entities;

namespace FlowBoard.UnitTests.Domain;

public sealed class RefreshTokenTests
{
    [Fact]
    public void CreateNew_AssignsNewFamilyId()
    {
        var token = RefreshToken.CreateNew(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(7));

        Assert.NotEqual(Guid.Empty, token.FamilyId);
        Assert.False(token.IsRevoked);
        Assert.True(token.IsActive);
    }

    [Fact]
    public void CreateRotated_PreservesFamilyId()
    {
        var family = Guid.NewGuid();
        var rotated = RefreshToken.CreateRotated(Guid.NewGuid(), "hash", family, DateTime.UtcNow.AddDays(7));

        Assert.Equal(family, rotated.FamilyId);
    }

    [Fact]
    public void CreateNew_DifferentLogins_HaveDifferentFamilies()
    {
        var first = RefreshToken.CreateNew(Guid.NewGuid(), "h1", DateTime.UtcNow.AddDays(7));
        var second = RefreshToken.CreateNew(Guid.NewGuid(), "h2", DateTime.UtcNow.AddDays(7));

        Assert.NotEqual(first.FamilyId, second.FamilyId);
    }

    [Fact]
    public void Revoke_MarksRevokedAndDeactivates()
    {
        var token = RefreshToken.CreateNew(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(7));
        token.Revoke();

        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive);
    }

    [Fact]
    public void IsActive_ExpiredToken_IsFalse()
    {
        var token = RefreshToken.CreateNew(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(-1));

        Assert.True(token.IsExpired);
        Assert.False(token.IsActive);
    }

    [Fact]
    public void IsActive_RevokedAndExpired_IsFalse()
    {
        var token = RefreshToken.CreateNew(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(-1));
        token.Revoke();

        Assert.False(token.IsActive);
    }
}