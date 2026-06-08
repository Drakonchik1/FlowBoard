using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.ValueObjects;

namespace FlowBoard.UnitTests.Domain;

public sealed class WorkspaceSlugTests
{
    [Theory]
    [InlineData("my-team")]
    [InlineData("acme")]
    [InlineData("project-2024")]
    [InlineData("a-b-c-d-e-f")]
    public void Create_Valid_AcceptsAndStoresLowercase(string raw)
    {
        var slug = WorkspaceSlug.Create(raw);
        Assert.Equal(raw, slug.Value);
    }

    [Theory]
    [InlineData("MY-TEAM", "my-team")]
    [InlineData("  ACME  ", "acme")]
    public void Create_NormalizesCaseAndTrim(string raw, string expected)
    {
        Assert.Equal(expected, WorkspaceSlug.Create(raw).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]                         // too short
    [InlineData("-leading-hyphen")]
    [InlineData("trailing-hyphen-")]
    [InlineData("has spaces")]
    [InlineData("has_underscore")]
    [InlineData("has.dot")]
    [InlineData("UPPER-ONLY-PASSED-AFTER-TRIM-but-this-ok-because-normalize")]
    public void Create_Invalid_Throws(string raw)
    {
        if (raw == "UPPER-ONLY-PASSED-AFTER-TRIM-but-this-ok-because-normalize")
        {
            // Mixed case is normalized to lowercase by Create — should NOT throw
            var slug = WorkspaceSlug.Create(raw);
            Assert.Equal(raw.ToLowerInvariant(), slug.Value);
            return;
        }
        Assert.Throws<DomainException>(() => WorkspaceSlug.Create(raw));
    }

    [Fact]
    public void Create_TooLong_Throws()
    {
        var tooLong = new string('a', 61);
        Assert.Throws<DomainException>(() => WorkspaceSlug.Create(tooLong));
    }

    [Theory]
    [InlineData("My Team!", "my-team")]
    [InlineData("Acme Corp.", "acme-corp")]
    [InlineData("Project 2024 / V2", "project-2024-v2")]
    public void FromName_StripsSpecialCharsAndJoinsWithHyphens(string name, string expected)
    {
        var slug = WorkspaceSlug.FromName(name);
        Assert.Equal(expected, slug.Value);
    }

    [Fact]
    public void FromName_WithEmptyOrShortName_FallsBackToGuidBased()
    {
        var slug = WorkspaceSlug.FromName("!!");
        Assert.NotNull(slug);
        Assert.True(slug.Value.Length >= 3);
    }

    [Fact]
    public void FromName_TruncationStillProducesValidSlug()
    {
        var slug = WorkspaceSlug.FromName(new string('a', 80) + "!!!");
        Assert.True(slug.Value.Length is >= 3 and <= 60);
        Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", slug.Value);
    }
}