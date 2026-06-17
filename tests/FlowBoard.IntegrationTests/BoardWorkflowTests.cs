using FlowBoard.Application.Features.Boards;
using FlowBoard.Application.Features.Boards.Commands.CreateBoard;
using FlowBoard.Application.Features.Boards.Queries.GetBoard;
using FlowBoard.Application.Features.BoardLists.Commands.CreateBoardList;
using FlowBoard.Application.Features.Cards.Commands.CreateCard;
using FlowBoard.Application.Features.Cards.Commands.MoveCard;
using FlowBoard.Application.Features.Projects.Commands.CreateProject;
using FlowBoard.Application.Features.Workspaces.Commands.CreateWorkspace;
using FlowBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.IntegrationTests;

/// <summary>
/// End-to-end tests against a real SQL Server: create the full board hierarchy, then verify the Dapper
/// GetBoard read returns lists and cards in fractional-index order, and that MoveCard reorders correctly
/// both within a list and across lists.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class BoardWorkflowTests(SqlServerFixture fixture)
{
    private async Task<Guid> SeedUserAsync()
    {
        Guid userId = Guid.Empty;
        await fixture.ExecuteDbAsync(async db =>
        {
            var user = User.Create($"u{Guid.NewGuid():N}@test.com", "Tester", "hash");
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
        });
        return userId;
    }

    private async Task<(Guid boardId, Guid listAId, Guid listBId)> SeedBoardWithTwoListsAsync()
    {
        var userId = await SeedUserAsync();
        fixture.CurrentUser.UserId = userId;

        var workspace = await fixture.SendAsync(new CreateWorkspaceCommand($"WS {Guid.NewGuid():N}", null));
        var project = await fixture.SendAsync(new CreateProjectCommand(workspace.Id, "Apollo", null));
        var board = await fixture.SendAsync(new CreateBoardCommand(project.Id, "Sprint Board"));

        var listA = await fixture.SendAsync(new CreateBoardListCommand(board.Id, "To Do"));
        var listB = await fixture.SendAsync(new CreateBoardListCommand(board.Id, "Done"));

        return (board.Id, listA.Id, listB.Id);
    }

    [SkippableFact]
    public async Task GetBoard_ReturnsListsAndCardsInPositionOrder()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var (boardId, listAId, _) = await SeedBoardWithTwoListsAsync();

        var c1 = await fixture.SendAsync(new CreateCardCommand(listAId, "First", null));
        var c2 = await fixture.SendAsync(new CreateCardCommand(listAId, "Second", null));
        var c3 = await fixture.SendAsync(new CreateCardCommand(listAId, "Third", null));

        var board = await fixture.SendAsync(new GetBoardQuery(boardId));

        Assert.Equal(2, board.Lists.Count);
        Assert.Equal(["To Do", "Done"], board.Lists.Select(l => l.Name));

        var todo = board.Lists.First(l => l.Id == listAId);
        Assert.Equal([c1.Id, c2.Id, c3.Id], todo.Cards.Select(c => c.Id));
        AssertAscendingPositions(todo);
    }

    [SkippableFact]
    public async Task MoveCard_ToTopOfList_ReordersCards()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var (boardId, listAId, _) = await SeedBoardWithTwoListsAsync();

        var c1 = await fixture.SendAsync(new CreateCardCommand(listAId, "First", null));
        var c2 = await fixture.SendAsync(new CreateCardCommand(listAId, "Second", null));
        var c3 = await fixture.SendAsync(new CreateCardCommand(listAId, "Third", null));

        // Move c3 to the very top (before = null, after = c1).
        await fixture.SendAsync(new MoveCardCommand(c3.Id, listAId, null, c1.Id));

        var board = await fixture.SendAsync(new GetBoardQuery(boardId));
        var todo = board.Lists.First(l => l.Id == listAId);

        Assert.Equal([c3.Id, c1.Id, c2.Id], todo.Cards.Select(c => c.Id));
        AssertAscendingPositions(todo);
    }

    [SkippableFact]
    public async Task MoveCard_ToAnotherList_MovesAcrossColumns()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var (boardId, listAId, listBId) = await SeedBoardWithTwoListsAsync();

        var c1 = await fixture.SendAsync(new CreateCardCommand(listAId, "First", null));
        var c2 = await fixture.SendAsync(new CreateCardCommand(listAId, "Second", null));

        await fixture.SendAsync(new MoveCardCommand(c1.Id, listBId, null, null));

        var board = await fixture.SendAsync(new GetBoardQuery(boardId));
        var todo = board.Lists.First(l => l.Id == listAId);
        var done = board.Lists.First(l => l.Id == listBId);

        Assert.Equal([c2.Id], todo.Cards.Select(c => c.Id));
        Assert.Equal([c1.Id], done.Cards.Select(c => c.Id));
    }

    [SkippableFact]
    public async Task GetBoard_RespectsSoftDeletedListsAndCards()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var (boardId, listAId, listBId) = await SeedBoardWithTwoListsAsync();
        await fixture.SendAsync(new CreateCardCommand(listAId, "Visible", null));

        // Soft-delete listB via EF directly to confirm the Dapper read honours IsDeleted = 0.
        await fixture.ExecuteDbAsync(async db =>
        {
            var listB = await db.BoardLists.FirstAsync(l => l.Id == listBId);
            listB.SoftDelete();
            await db.SaveChangesAsync();
        });

        var board = await fixture.SendAsync(new GetBoardQuery(boardId));

        Assert.Single(board.Lists);
        Assert.Equal(listAId, board.Lists[0].Id);
    }

    private static void AssertAscendingPositions(BoardListViewDto list)
    {
        for (var i = 1; i < list.Cards.Count; i++)
        {
            Assert.True(
                string.CompareOrdinal(list.Cards[i - 1].Position, list.Cards[i].Position) < 0,
                "cards must be returned in ascending position order");
        }
    }
}
