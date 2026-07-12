using FlowBoard.Domain.Entities;

namespace FlowBoard.Domain.Interfaces;

public interface ICardTagRepository : IRepository<CardTag>
{
    Task<CardTag?> GetByCardAndTagAsync(
        Guid cardId, Guid tagId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tag>> GetTagsForCardAsync(
        Guid cardId, CancellationToken cancellationToken = default);

    Task RemoveAllForTagAsync(Guid tagId, CancellationToken cancellationToken = default);
}
