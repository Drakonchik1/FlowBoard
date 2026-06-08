using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Exceptions;

namespace FlowBoard.UnitTests.Domain;

public sealed class UserTests
{
    [Fact]
    public void Create_ValidInputs_SetsFieldsAndRaisesEvent()
    {
        var user = User.Create("user@example.com", "Test User", "hashed_password");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("user@example.com", user.Email.Value);
        Assert.Equal("Test User", user.FullName);
        Assert.Equal("hashed_password", user.PasswordHash);
        Assert.False(user.IsDeleted);
        Assert.True(user.CreatedAt <= DateTime.UtcNow);

        // The single most important domain side effect
        Assert.Single(user.DomainEvents);
        Assert.IsType<UserRegisteredEvent>(user.DomainEvents[0]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyFullName_Throws(string fullName)
    {
        Assert.Throws<DomainException>(() => User.Create("user@example.com", fullName, "hash"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyPasswordHash_Throws(string hash)
    {
        Assert.Throws<DomainException>(() => User.Create("user@example.com", "Test User", hash));
    }

    [Fact]
    public void Create_InvalidEmail_PropagatesDomainException()
    {
        Assert.Throws<DomainException>(() => User.Create("not-email", "Test User", "hash"));
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedAndUpdatesTimestamp()
    {
        var user = User.Create("user@example.com", "Test User", "hash");
        var before = user.UpdatedAt;
        Thread.Sleep(5);

        user.SoftDelete();

        Assert.True(user.IsDeleted);
        Assert.True(user.UpdatedAt > before);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var user = User.Create("user@example.com", "Test User", "hash");
        Assert.NotEmpty(user.DomainEvents);

        user.ClearDomainEvents();

        Assert.Empty(user.DomainEvents);
    }
}