using FlowBoard.Application.Features.BoardLists.Commands.CreateBoardList;
using FlowBoard.Application.Features.Boards.Commands.CreateBoard;
using FlowBoard.Application.Features.Cards.Commands.CreateCard;
using FlowBoard.Application.Features.Boards.Queries.GetBoard;
using FlowBoard.Application.Features.Cards.Commands.AssignCard;
using FlowBoard.Application.Features.Cards.Queries.GetCardById;
using FlowBoard.Application.Features.Comments.Commands.CreateComment;
using FlowBoard.Application.Features.Comments.Commands.DeleteComment;
using FlowBoard.Application.Features.Comments.Commands.UpdateComment;
using FlowBoard.Application.Features.Comments.Queries.GetCommentById;
using FlowBoard.Application.Features.Comments.Queries.GetCommentsByCard;
using FlowBoard.Application.Features.Projects.Commands.CreateProject;
using FlowBoard.Application.Features.Tags.Commands.ApplyTagToCard;
using FlowBoard.Application.Features.Tags.Commands.CreateTag;
using FlowBoard.Application.Features.Tags.Commands.DeleteTag;
using FlowBoard.Application.Features.Tags.Commands.RemoveTagFromCard;
using FlowBoard.Application.Features.Tags.Commands.UpdateTag;
using FlowBoard.Application.Features.Tags.Queries.GetTagsByCard;
using FlowBoard.Application.Features.Tags.Queries.GetTagsByWorkspace;
using FlowBoard.Application.Features.Tags.Queries.GetTagById;
using FlowBoard.Application.Features.Workspaces;
using FlowBoard.Application.Features.Workspaces.Commands.CreateWorkspace;
using FlowBoard.Application.Features.Workspaces.Commands.InviteMember;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;

namespace FlowBoard.IntegrationTests;

/// <summary>
/// End-to-end tests for Sprint 6 comment and tag workflows against real SQL Server.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class CommentsAndTagsWorkflowTests(SqlServerFixture fixture)
{
    private sealed record CardContext(Guid UserId, Guid WorkspaceId, Guid BoardId, Guid CardId);

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

    private async Task<CardContext> SeedCardAsync()
    {
        var userId = await SeedUserAsync();
        fixture.CurrentUser.UserId = userId;

        var workspace = await fixture.SendAsync(new CreateWorkspaceCommand($"WS {Guid.NewGuid():N}", null));
        var project = await fixture.SendAsync(new CreateProjectCommand(workspace.Id, "Apollo", null));
        var board = await fixture.SendAsync(new CreateBoardCommand(project.Id, "Sprint Board"));
        var list = await fixture.SendAsync(new CreateBoardListCommand(board.Id, "To Do"));
        var card = await fixture.SendAsync(new CreateCardCommand(list.Id, "Task", null));

        return new CardContext(userId, workspace.Id, board.Id, card.Id);
    }

    [SkippableFact]
    public async Task CommentWorkflow_CreateUpdateDelete_ListByCard()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var ctx = await SeedCardAsync();

        var created = await fixture.SendAsync(new CreateCommentCommand(ctx.CardId, "First note"));
        Assert.Equal(ctx.CardId, created.CardId);
        Assert.Equal(ctx.UserId, created.AuthorId);
        Assert.Equal("First note", created.Body);

        var updated = await fixture.SendAsync(new UpdateCommentCommand(created.Id, "Updated note"));
        Assert.Equal("Updated note", updated.Body);

        var byId = await fixture.SendAsync(new GetCommentByIdQuery(created.Id));
        Assert.Equal(updated.Body, byId.Body);

        await fixture.SendAsync(new CreateCommentCommand(ctx.CardId, "Second note"));
        var listed = await fixture.SendAsync(new GetCommentsByCardQuery(ctx.CardId));
        Assert.Equal(2, listed.Count);
        Assert.Equal(["Updated note", "Second note"], listed.Select(c => c.Body));

        await fixture.SendAsync(new DeleteCommentCommand(created.Id));

        listed = await fixture.SendAsync(new GetCommentsByCardQuery(ctx.CardId));
        Assert.Single(listed);
        Assert.Equal("Second note", listed[0].Body);
    }

    [SkippableFact]
    public async Task CreateComment_InvokesCommentAddedNotifierAfterCommit()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var ctx = await SeedCardAsync();
        fixture.RealtimeNotifier.Clear();

        var comment = await fixture.SendAsync(new CreateCommentCommand(ctx.CardId, "Realtime test"));

        var events = fixture.RealtimeNotifier.CommentAddedEvents;
        Assert.Single(events);
        Assert.Equal(comment.Id, events[0].CommentId);
        Assert.Equal(ctx.CardId, events[0].CardId);
        Assert.Equal(ctx.BoardId, events[0].BoardId);
        Assert.Equal(ctx.UserId, events[0].AuthorId);
        Assert.Equal("Realtime test", events[0].Body);
    }

    [SkippableFact]
    public async Task TagWorkflow_CreateApplyRemove_ListByWorkspaceAndCard()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var ctx = await SeedCardAsync();

        var bug = await fixture.SendAsync(new CreateTagCommand(ctx.WorkspaceId, "bug", "#FF0000"));
        await fixture.SendAsync(new CreateTagCommand(ctx.WorkspaceId, "feature", "#00FF00"));

        var workspaceTags = await fixture.SendAsync(new GetTagsByWorkspaceQuery(ctx.WorkspaceId));
        Assert.Equal(2, workspaceTags.Count);
        Assert.Equal(["bug", "feature"], workspaceTags.Select(t => t.Name).OrderBy(n => n));

        await fixture.SendAsync(new ApplyTagToCardCommand(ctx.CardId, bug.Id));

        var cardTags = await fixture.SendAsync(new GetTagsByCardQuery(ctx.CardId));
        Assert.Single(cardTags);
        Assert.Equal(bug.Id, cardTags[0].Id);
        Assert.Equal("#FF0000", cardTags[0].Color);

        var renamed = await fixture.SendAsync(new UpdateTagCommand(bug.Id, "critical", "#AA0000"));
        Assert.Equal("critical", renamed.Name);
        Assert.Equal("#AA0000", renamed.Color);

        cardTags = await fixture.SendAsync(new GetTagsByCardQuery(ctx.CardId));
        Assert.Equal("critical", cardTags[0].Name);

        await fixture.SendAsync(new RemoveTagFromCardCommand(ctx.CardId, bug.Id));

        cardTags = await fixture.SendAsync(new GetTagsByCardQuery(ctx.CardId));
        Assert.Empty(cardTags);
    }

    [SkippableFact]
    public async Task ApplyTagToCard_IsIdempotentWhenAlreadyApplied()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var ctx = await SeedCardAsync();
        var tag = await fixture.SendAsync(new CreateTagCommand(ctx.WorkspaceId, "duplicate", null));

        await fixture.SendAsync(new ApplyTagToCardCommand(ctx.CardId, tag.Id));
        await fixture.SendAsync(new ApplyTagToCardCommand(ctx.CardId, tag.Id));

        var cardTags = await fixture.SendAsync(new GetTagsByCardQuery(ctx.CardId));
        Assert.Single(cardTags);
        Assert.Equal(tag.Id, cardTags[0].Id);
    }

    [SkippableFact]
    public async Task DeleteTag_RemovesTagFromAllCards()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var ctx = await SeedCardAsync();
        var tag = await fixture.SendAsync(new CreateTagCommand(ctx.WorkspaceId, "ephemeral", null));

        await fixture.SendAsync(new ApplyTagToCardCommand(ctx.CardId, tag.Id));
        Assert.Single(await fixture.SendAsync(new GetTagsByCardQuery(ctx.CardId)));

        await fixture.SendAsync(new DeleteTagCommand(tag.Id));

        Assert.Empty(await fixture.SendAsync(new GetTagsByCardQuery(ctx.CardId)));
        Assert.Empty(await fixture.SendAsync(new GetTagsByWorkspaceQuery(ctx.WorkspaceId)));
    }

    [SkippableFact]
    public async Task DeleteComment_GetCommentById_Returns404()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var ctx = await SeedCardAsync();
        var created = await fixture.SendAsync(new CreateCommentCommand(ctx.CardId, "To delete"));

        await fixture.SendAsync(new DeleteCommentCommand(created.Id));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            fixture.SendAsync(new GetCommentByIdQuery(created.Id)));
    }

    [SkippableFact]
    public async Task AssignCard_GetBoardAndGetCardById_ShowAssignee()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var ctx = await SeedCardAsync();
        var assigneeId = await SeedUserAsync();
        await fixture.SendAsync(new InviteMemberCommand(ctx.WorkspaceId, assigneeId, WorkspaceRole.Member));

        fixture.CurrentUser.UserId = ctx.UserId;
        var assigned = await fixture.SendAsync(new AssignCardCommand(ctx.CardId, assigneeId));
        Assert.Equal(assigneeId, assigned.AssigneeId);

        var card = await fixture.SendAsync(new GetCardByIdQuery(ctx.CardId));
        Assert.Equal(assigneeId, card.AssigneeId);

        var board = await fixture.SendAsync(new GetBoardQuery(ctx.BoardId));
        var boardCard = board.Lists.SelectMany(l => l.Cards).Single(c => c.Id == ctx.CardId);
        Assert.Equal(assigneeId, boardCard.AssigneeId);
    }

    [SkippableFact]
    public async Task Sprint6Endpoints_NonMemberGets404()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var ctx = await SeedCardAsync();
        var tag = await fixture.SendAsync(new CreateTagCommand(ctx.WorkspaceId, "secret", null));

        var outsiderId = await SeedUserAsync();
        fixture.CurrentUser.UserId = outsiderId;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            fixture.SendAsync(new GetTagsByWorkspaceQuery(ctx.WorkspaceId)));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            fixture.SendAsync(new GetTagByIdQuery(tag.Id)));
    }

    [SkippableFact]
    public async Task CreateComment_ViewerGets403()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var ctx = await SeedCardAsync();
        var viewerId = await SeedUserAsync();
        await fixture.SendAsync(new InviteMemberCommand(ctx.WorkspaceId, viewerId, WorkspaceRole.Viewer));

        fixture.CurrentUser.UserId = viewerId;
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            fixture.SendAsync(new CreateCommentCommand(ctx.CardId, "Blocked")));
    }
}
