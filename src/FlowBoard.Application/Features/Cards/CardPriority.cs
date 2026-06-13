using DomainCardPriority = FlowBoard.Domain.Entities.CardPriority;

namespace FlowBoard.Application.Features.Cards;

/// <summary>Wire-level card priority. Maps to the domain <see cref="DomainCardPriority"/> at the application boundary.</summary>
public enum CardPriority
{
    None,
    Low,
    Medium,
    High,
    Critical
}

internal static class CardPriorityMapper
{
    public static DomainCardPriority ToDomain(CardPriority priority) => priority switch
    {
        CardPriority.None => DomainCardPriority.None,
        CardPriority.Low => DomainCardPriority.Low,
        CardPriority.Medium => DomainCardPriority.Medium,
        CardPriority.High => DomainCardPriority.High,
        CardPriority.Critical => DomainCardPriority.Critical,
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown card priority.")
    };
}
