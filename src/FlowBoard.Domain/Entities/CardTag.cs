using FlowBoard.Domain.Common;
using FlowBoard.Domain.Exceptions;

namespace FlowBoard.Domain.Entities;

/// <summary>
/// Join entity linking a tag to a card. Both must belong to the same workspace (enforced in handlers).
/// </summary>
public sealed class CardTag : Entity
{
    public Guid CardId { get; private set; }
    public Guid TagId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CardTag() { }

    public static CardTag Create(Guid cardId, Guid tagId)
    {
        if (cardId == Guid.Empty)
            throw new DomainException("Card tag must reference a card.");
        if (tagId == Guid.Empty)
            throw new DomainException("Card tag must reference a tag.");

        return new CardTag
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            TagId = tagId,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
