using Dapper;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Boards;

namespace FlowBoard.Infrastructure.Persistence.Repositories;

/// <summary>
/// Dapper implementation of the board read side. Issues a single round-trip with three result sets
/// (board, lists, cards) and assembles the nested read model in memory. No EF Core tracking involved.
/// </summary>
internal sealed class BoardReadService(ISqlConnectionFactory connectionFactory) : IBoardReadService
{
    private const string Sql = """
        SELECT [Id], [ProjectId], [WorkspaceId], [Name], [CreatedAt], [UpdatedAt]
        FROM [boards]
        WHERE [Id] = @BoardId AND [IsDeleted] = 0;

        SELECT [Id], [BoardId], [Name], [Position], [CreatedAt], [UpdatedAt]
        FROM [board_lists]
        WHERE [BoardId] = @BoardId AND [IsDeleted] = 0;

        SELECT [Id], [BoardListId], [BoardId], [Title], [Description], [Position], [Priority], [CreatedAt], [UpdatedAt]
        FROM [cards]
        WHERE [BoardId] = @BoardId AND [IsDeleted] = 0;
        """;

    public async Task<BoardViewDto?> GetBoardAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.Create();

        var command = new CommandDefinition(Sql, new { BoardId = boardId }, cancellationToken: cancellationToken);
        await using var multi = await connection.QueryMultipleAsync(command);

        var board = await multi.ReadFirstOrDefaultAsync<BoardRow>();
        if (board is null)
            return null;

        var listRows = (await multi.ReadAsync<ListRow>())
            .OrderBy(l => l.Position, StringComparer.Ordinal)
            .ToList();
        var cardRows = (await multi.ReadAsync<CardRow>()).ToList();

        var cardsByList = cardRows
            .GroupBy(c => c.BoardListId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CardViewDto>)g
                    .OrderBy(c => c.Position, StringComparer.Ordinal)
                    .Select(c => new CardViewDto(
                        c.Id, c.BoardListId, c.Title, c.Description, c.Position, c.Priority, c.CreatedAt, c.UpdatedAt))
                    .ToList());

        var lists = listRows
            .Select(l => new BoardListViewDto(
                l.Id,
                l.Name,
                l.Position,
                cardsByList.TryGetValue(l.Id, out var cards) ? cards : []))
            .ToList();

        return new BoardViewDto(
            board.Id, board.ProjectId, board.WorkspaceId, board.Name, board.CreatedAt, board.UpdatedAt, lists);
    }

    private sealed record BoardRow(Guid Id, Guid ProjectId, Guid WorkspaceId, string Name, DateTime CreatedAt, DateTime UpdatedAt);

    private sealed record ListRow(Guid Id, Guid BoardId, string Name, string Position, DateTime CreatedAt, DateTime UpdatedAt);

    private sealed record CardRow(
        Guid Id,
        Guid BoardListId,
        Guid BoardId,
        string Title,
        string? Description,
        string Position,
        string Priority,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
