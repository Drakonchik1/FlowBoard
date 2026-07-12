using Dapper;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Activity;

namespace FlowBoard.Infrastructure.Persistence.Repositories;

/// <summary>
/// Dapper read side for card activity entries.
/// </summary>
internal sealed class ActivityLogReadService(ISqlConnectionFactory connectionFactory) : IActivityLogReadService
{
    private const string Sql = """
        SELECT [Id], [Type], [ActorId], [TargetUserId], [TargetRole], [FromListId], [ToListId], [CreatedAt]
        FROM [activity_logs]
        WHERE [CardId] = @CardId
        ORDER BY [CreatedAt] DESC;
        """;

    public async Task<IReadOnlyList<ActivityLogDto>> GetByCardIdAsync(
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.Create();

        var command = new CommandDefinition(Sql, new { CardId = cardId }, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ActivityRow>(command);

        return rows
            .Select(r => new ActivityLogDto(
                r.Id,
                r.Type,
                r.ActorId,
                r.TargetUserId,
                r.TargetRole,
                r.FromListId,
                r.ToListId,
                r.CreatedAt))
            .ToList();
    }

    private sealed record ActivityRow(
        Guid Id,
        string Type,
        Guid ActorId,
        Guid? TargetUserId,
        string? TargetRole,
        Guid? FromListId,
        Guid? ToListId,
        DateTime CreatedAt);
}
