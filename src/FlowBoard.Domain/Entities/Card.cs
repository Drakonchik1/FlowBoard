using FlowBoard.Domain.Common;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.ValueObjects;

namespace FlowBoard.Domain.Entities;

/// <summary>
/// A card living in a board list. Aggregate root; references its list and board by id.
/// BoardId is denormalized so the whole board can be read in one query and so a card move can be
/// validated to stay within the same board. Ordering within a list uses a <see cref="FractionalIndex"/>.
/// </summary>
public sealed class Card : Entity
{
    private const int TitleMaxLength = 200;
    private const int DescriptionMaxLength = 4000;

    public Guid BoardListId { get; private set; }
    public Guid BoardId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public FractionalIndex Position { get; private set; } = null!;
    public CardPriority Priority { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Required by EF Core
    private Card() { }

    public static Card Create(
        Guid boardId,
        Guid boardListId,
        string title,
        FractionalIndex position,
        CardPriority priority = CardPriority.None,
        string? description = null)
    {
        if (boardId == Guid.Empty)
            throw new DomainException("Card must belong to a board.");
        if (boardListId == Guid.Empty)
            throw new DomainException("Card must belong to a list.");

        ValidateTitle(title);
        ValidateDescription(description);
        ArgumentNullException.ThrowIfNull(position);

        var card = new Card
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            BoardListId = boardListId,
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Position = position,
            Priority = priority,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        card.Raise(new CardCreatedEvent(card.Id, boardId, boardListId));
        return card;
    }

    public void Update(string title, string? description, CardPriority priority)
    {
        ValidateTitle(title);
        ValidateDescription(description);

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Moves the card to a target list and position. The target list must be on the same board —
    /// cross-board moves are a separate, deliberate operation and are rejected here.
    /// </summary>
    public void MoveTo(Guid targetBoardId, Guid targetListId, FractionalIndex position)
    {
        if (targetBoardId != BoardId)
            throw new DomainException("A card cannot be moved to a different board.");
        if (targetListId == Guid.Empty)
            throw new DomainException("Target list is required.");
        ArgumentNullException.ThrowIfNull(position);

        var fromListId = BoardListId;
        BoardListId = targetListId;
        Position = position;
        UpdatedAt = DateTime.UtcNow;

        Raise(new CardMovedEvent(Id, BoardId, fromListId, targetListId, position.Value));
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Card title cannot be empty.");

        if (title.Length > TitleMaxLength)
            throw new DomainException($"Card title cannot exceed {TitleMaxLength} characters.");
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Length > DescriptionMaxLength)
            throw new DomainException($"Card description cannot exceed {DescriptionMaxLength} characters.");
    }
}
