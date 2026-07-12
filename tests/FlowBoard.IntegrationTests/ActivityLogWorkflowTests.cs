using FlowBoard.Application.Features.Activity.Queries.GetCardActivity;
using FlowBoard.Application.Features.BoardLists.Commands.CreateBoardList;
using FlowBoard.Application.Features.Boards.Commands.CreateBoard;
using FlowBoard.Application.Features.Boards.Queries.GetBoard;
using FlowBoard.Application.Features.Cards.Commands.AssignCard;
using FlowBoard.Application.Features.Cards.Commands.CreateCard;
using FlowBoard.Application.Features.Cards.Commands.MoveCard;
using FlowBoard.Application.Features.Cards.Queries.GetCardById;
using FlowBoard.Application.Features.Projects.Commands.CreateProject;
using FlowBoard.Application.Features.Workspaces;
using FlowBoard.Application.Features.Workspaces.Commands.CreateWorkspace;
using FlowBoard.Application.Features.Workspaces.Commands.InviteMember;
using FlowBoard.Application.Features.Workspaces.Commands.RemoveMember;
using FlowBoard.Domain.Entities;

namespace FlowBoard.IntegrationTests;

/// <summary>
/// End-to-end tests for Sprint 7 activity log and RemoveMember assignee cleanup.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class ActivityLogWorkflowTests(SqlServerFixture fixture)
{
    private sealed record BoardContext(Guid UserId, Guid WorkspaceId, Guid BoardId, Guid ListAId, Guid ListBId);

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

    private async Task<BoardContext> SeedBoardWithTwoListsAsync()
    {
        var userId = await SeedUserAsync();
        fixture.CurrentUser.UserId = userId;

        var workspace = await fixture.SendAsync(new CreateWorkspaceCommand($"WS {Guid.NewGuid():N}", null));
        var project = await fixture.SendAsync(new CreateProjectCommand(workspace.Id, "Apollo", null));
        var board = await fixture.SendAsync(new CreateBoardCommand(project.Id, "Sprint Board"));
        var listA = await fixture.SendAsync(new CreateBoardListCommand(board.Id, "To Do"));
        var listB = await fixture.SendAsync(new CreateBoardListCommand(board.Id, "Done"));

        return new BoardContext(userId, workspace.Id, board.Id, listA.Id, listB.Id);
    }

    [SkippableFact]
    public async Task CreateCard_GetCardActivity_PersistsCardCreatedEntry()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var ctx = await SeedBoardWithTwoListsAsync();
        var card = await fixture.SendAsync(new CreateCardCommand(ctx.ListAId, "Task", null));

        var activity = await fixture.SendAsync(new GetCardActivityQuery(card.Id));

        Assert.Single(activity);
        Assert.Equal("CardCreated", activity[0].Type);
        Assert.Equal(ctx.UserId, activity[0].ActorId);
    }

    [SkippableFact]
    public async Task MoveCard_GetCardActivity_PersistsCardMovedEntry()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var ctx = await SeedBoardWithTwoListsAsync();
        var card = await fixture.SendAsync(new CreateCardCommand(ctx.ListAId, "Task", null));

        await fixture.SendAsync(new MoveCardCommand(card.Id, ctx.ListBId, null, null));

        var activity = await fixture.SendAsync(new GetCardActivityQuery(card.Id));

        Assert.Equal(2, activity.Count);
        var moved = activity.Single(a => a.Type == "CardMoved");
        Assert.Equal(ctx.ListAId, moved.FromListId);
        Assert.Equal(ctx.ListBId, moved.ToListId);
        Assert.Equal(ctx.UserId, moved.ActorId);
    }

    [SkippableFact]
    public async Task RemoveMember_ClearsAssigneeOnGetBoardAndGetCardById()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var ctx = await SeedBoardWithTwoListsAsync();
        var assigneeId = await SeedUserAsync();
        await fixture.SendAsync(new InviteMemberCommand(ctx.WorkspaceId, assigneeId, WorkspaceRole.Member));

        var card = await fixture.SendAsync(new CreateCardCommand(ctx.ListAId, "Assigned task", null));
        fixture.CurrentUser.UserId = ctx.UserId;
        await fixture.SendAsync(new AssignCardCommand(card.Id, assigneeId));

        fixture.CurrentUser.UserId = ctx.UserId;
        await fixture.SendAsync(new RemoveMemberCommand(ctx.WorkspaceId, assigneeId));

        var cardDto = await fixture.SendAsync(new GetCardByIdQuery(card.Id));
        Assert.Null(cardDto.AssigneeId);

        var board = await fixture.SendAsync(new GetBoardQuery(ctx.BoardId));
        var boardCard = board.Lists.SelectMany(l => l.Cards).Single(c => c.Id == card.Id);
        Assert.Null(boardCard.AssigneeId);
    }
}
