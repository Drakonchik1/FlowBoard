using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FlowBoard.API.Hubs;
using FlowBoard.API.Services;
using FlowBoard.Application.Features.Boards.Queries.EnsureBoardAccess;
using FlowBoard.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace FlowBoard.UnitTests.Hubs;

public sealed class BoardHubTests
{
    [Fact]
    public async Task JoinBoard_NonMember_ThrowsBoardNotFoundAndDoesNotAddToGroup()
    {
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string connectionId = "conn-outsider";

        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<EnsureBoardAccessQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Board", boardId));

        var groups = new Mock<IGroupManager>();
        var registry = new BoardGroupMembershipRegistry();
        var hub = CreateHub(sender.Object, registry, userId, connectionId, groups.Object);

        var ex = await Assert.ThrowsAsync<HubException>(() => hub.JoinBoard(boardId));

        Assert.Equal("Board not found.", ex.Message);
        groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Empty(registry.GetMemberships(userId));
    }

    [Fact]
    public async Task JoinBoard_Member_AddsToBoardGroupAndTracksMembership()
    {
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string connectionId = "conn-member";

        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<EnsureBoardAccessQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var groups = new Mock<IGroupManager>();
        var registry = new BoardGroupMembershipRegistry();
        var hub = CreateHub(sender.Object, registry, userId, connectionId, groups.Object);

        await hub.JoinBoard(boardId);

        groups.Verify(
            g => g.AddToGroupAsync(connectionId, $"board:{boardId}", It.IsAny<CancellationToken>()),
            Times.Once);
        var memberships = registry.GetMemberships(userId);
        Assert.Single(memberships);
        Assert.Equal(boardId, memberships[0].BoardId);
        Assert.Equal(connectionId, memberships[0].ConnectionId);
    }

    private static BoardHub CreateHub(
        ISender sender,
        BoardGroupMembershipRegistry registry,
        Guid userId,
        string connectionId,
        IGroupManager groups)
    {
        var hub = new BoardHub(sender, registry);

        var context = new Mock<HubCallerContext>();
        context.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
        ], "Test")));
        context.Setup(c => c.ConnectionId).Returns(connectionId);
        context.Setup(c => c.ConnectionAborted).Returns(CancellationToken.None);

        hub.Context = context.Object;
        hub.Groups = groups;
        return hub;
    }
}
