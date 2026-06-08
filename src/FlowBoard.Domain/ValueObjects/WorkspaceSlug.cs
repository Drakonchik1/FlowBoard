using System.Text.RegularExpressions;
using FlowBoard.Domain.Common;
using FlowBoard.Domain.Exceptions;

namespace FlowBoard.Domain.ValueObjects;

/// <summary>
/// URL-safe workspace identifier — lowercase, alphanumeric, hyphens only.
/// Length 3-60. Generated from a workspace name on creation.
/// </summary>
public sealed partial class WorkspaceSlug : ValueObject
{
    public string Value { get; }

    private WorkspaceSlug(string value) => Value = value;

    /// <summary>Validates and creates a slug. Throws DomainException on invalid input.</summary>
    public static WorkspaceSlug Create(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Workspace slug cannot be empty.");

        slug = slug.Trim().ToLowerInvariant();

        if (slug.Length < 3 || slug.Length > 60)
            throw new DomainException("Workspace slug must be between 3 and 60 characters.");

        if (!SlugRegex().IsMatch(slug))
            throw new DomainException("Workspace slug must contain only lowercase letters, numbers, and hyphens, and cannot start or end with a hyphen.");

        return new WorkspaceSlug(slug);
    }

    /// <summary>Generates a slug candidate from a workspace name. Guaranteed valid output.</summary>
    public static WorkspaceSlug FromName(string name)
    {
        var slug = NormalizeRegex().Replace(name.Trim().ToLowerInvariant(), "-");
        slug = slug.Trim('-');

        if (slug.Length < 3)
            slug = $"workspace-{Guid.NewGuid():N}"[..12];

        if (slug.Length > 60)
            slug = slug[..60].TrimEnd('-');

        if (slug.Length < 3 || !SlugRegex().IsMatch(slug))
            slug = $"ws-{Guid.NewGuid():N}"[..12];

        return Create(slug);
    }

    /// <summary>Bypasses validation — used by EF Core value converter for materialization.</summary>
    internal static WorkspaceSlug FromTrustedSource(string value) => new(value);

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NormalizeRegex();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(WorkspaceSlug s) => s.Value;
    public override string ToString() => Value;
}