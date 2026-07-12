using FlowBoard.Application.Features.Activity;

namespace FlowBoard.Application.Common.Interfaces;

/// <summary>
/// Dapper read side for card activity entries.
/// </summary>
public interface IActivityLogReadService
{
    Task<IReadOnlyList<ActivityLogDto>> GetByCardIdAsync(
        Guid cardId,
        CancellationToken cancellationToken = default);
}
