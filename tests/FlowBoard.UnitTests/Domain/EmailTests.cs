using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.ValueObjects;

namespace FlowBoard.UnitTests.Domain;

public sealed class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("USER@EXAMPLE.COM")]
    [InlineData("  user@example.com  ")]
    [InlineData("first.last+tag@example.co.uk")]
    public void Create_ValidInputs_Normalizes(string raw)
    {
        var email = Email.Create(raw);
        Assert.Equal(raw.Trim().ToLowerInvariant(), email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user@localhost")]
    [InlineData("user@.com")]
    [InlineData("user@example")]
    public void Create_InvalidInputs_Throws(string invalid)
    {
        Assert.Throws<DomainException>(() => Email.Create(invalid));
    }

    [Fact]
    public void Create_NullInput_Throws()
    {
        Assert.Throws<DomainException>(() => Email.Create(null!));
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = Email.Create("user@example.com");
        var b = Email.Create("USER@EXAMPLE.COM");
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void FromTrustedSource_DoesNotValidate()
    {
        // Used by EF Core value converter, bypasses validation so persisted rows
        // remain loadable even if format rules tighten in the future
        var email = Email.FromTrustedSource("legacy-row@old-format");
        Assert.Equal("legacy-row@old-format", email.Value);
    }

    [Fact]
    public void Normalize_StaticHelper_TrimsAndLowercases()
    {
        Assert.Equal("user@example.com", Email.Normalize("  USER@Example.com  "));
    }
}