using FlowBoard.Domain.Common;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.ValueObjects;

namespace FlowBoard.Domain.Entities;

/// <summary>
/// A column on a board (e.g. "To Do", "In Progress"). Aggregate root; references its board by id.
/// Ordering among columns uses a <see cref="FractionalIndex"/> so a column can be reordered by
/// rewriting only its own Position — siblings are never renumbered.
/// </summary>
public sealed class BoardList : Entity
{
    private const int NameMaxLength = 100;

    public Guid BoardId { get; private set; }
    public string Name { get; private set; } = null!;
    public FractionalIndex Position { get; private set; } = null!;
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Required by EF Core
    private BoardList() { }

    public static BoardList Create(Guid boardId, string name, FractionalIndex position)
    {
        if (boardId == Guid.Empty)
            throw new DomainException("List must belong to a board.");

        ValidateName(name);
        ArgumentNullException.ThrowIfNull(position);

        return new BoardList
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            Name = name.Trim(),
            Position = position,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Rename(string newName)
    {
        ValidateName(newName);
        Name = newName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveTo(FractionalIndex position)
    {
        ArgumentNullException.ThrowIfNull(position);
        Position = position;
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
            throw new DomainException("List name cannot be empty.");

        if (name.Length > NameMaxLength)
            throw new DomainException($"List name cannot exceed {NameMaxLength} characters.");
    }
}
