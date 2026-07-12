using FlowBoard.Domain.Common;
using FlowBoard.Domain.Exceptions;

namespace FlowBoard.Domain.Entities;

/// <summary>
/// A label scoped to a workspace. Tags can be applied to cards via <see cref="CardTag"/>.
/// </summary>
public sealed class Tag : Entity
{
    private const int NameMaxLength = 50;
    private const int ColorMaxLength = 7;

    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Color { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Tag() { }

    public static Tag Create(Guid workspaceId, string name, string? color = null)
    {
        if (workspaceId == Guid.Empty)
            throw new DomainException("Tag must belong to a workspace.");

        ValidateName(name);
        ValidateColor(color);

        return new Tag
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Color = NormalizeColor(color),
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string name, string? color)
    {
        ValidateName(name);
        ValidateColor(color);

        Name = name.Trim();
        Color = NormalizeColor(color);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tag name cannot be empty.");

        if (name.Length > NameMaxLength)
            throw new DomainException($"Tag name cannot exceed {NameMaxLength} characters.");
    }

    private static void ValidateColor(string? color)
    {
        if (color is null)
            return;

        var trimmed = color.Trim();
        if (trimmed.Length == 0)
            return;

        if (trimmed.Length > ColorMaxLength)
            throw new DomainException($"Tag color cannot exceed {ColorMaxLength} characters.");

        if (trimmed[0] != '#' || trimmed.Length != 7)
            throw new DomainException("Tag color must be a hex value like #RRGGBB.");

        for (var i = 1; i < trimmed.Length; i++)
        {
            if (!Uri.IsHexDigit(trimmed[i]))
                throw new DomainException("Tag color must be a hex value like #RRGGBB.");
        }
    }

    private static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return null;

        return color.Trim().ToUpperInvariant();
    }
}
