using FlowBoard.Domain.Common;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Exceptions;

namespace FlowBoard.Domain.Entities;

/// <summary>
/// A text comment on a card. Scoped to the card; author is stored for attribution.
/// </summary>
public sealed class Comment : Entity
{
    private const int BodyMaxLength = 4000;

    public Guid CardId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Body { get; private set; } = null!;
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Comment() { }

    public static Comment Create(Guid cardId, Guid boardId, Guid authorId, string body)
    {
        if (cardId == Guid.Empty)
            throw new DomainException("Comment must belong to a card.");
        if (boardId == Guid.Empty)
            throw new DomainException("Comment must belong to a board.");
        if (authorId == Guid.Empty)
            throw new DomainException("Comment author is required.");

        ValidateBody(body);

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            AuthorId = authorId,
            Body = body.Trim(),
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        comment.Raise(new CommentAddedEvent(
            comment.Id, cardId, boardId, authorId, comment.Body, comment.CreatedAt));
        return comment;
    }

    public void Update(string body)
    {
        ValidateBody(body);
        Body = body.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new DomainException("Comment body cannot be empty.");

        if (body.Length > BodyMaxLength)
            throw new DomainException($"Comment body cannot exceed {BodyMaxLength} characters.");
    }
}
