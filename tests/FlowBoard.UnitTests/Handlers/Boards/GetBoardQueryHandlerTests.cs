using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Boards;
using FlowBoard.Application.Features.Boards.Queries.GetBoard;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Boards;

public sealed class GetBoardQueryHandlerTests
{
    private readonly Mock<IBoardReadService> _boardReadService = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private GetBoardQueryHandler CreateHandler() =>
        new(_boardReadService.Object, _workspaceRepo.Object, _currentUser.Object);

    private static BoardViewDto BoardView(Guid boardId, Guid workspaceId) =>
        new(boardId, Guid.NewGuid(), workspaceId, "Board", DateTime.UtcNow, DateTime.UtcNow, []);

    [Fact]
    public async Task Handle_Member_ReturnsBoard()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var boardId = Guid.NewGuid();

        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _boardReadService.Setup(s => s.GetBoardAsync(boardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BoardView(boardId, workspace.Id));
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        var result = await CreateHandler().Handle(new GetBoardQuery(boardId), CancellationToken.None);

        Assert.Equal(boardId, result.Id);
    }

    [Fact]
    public async Task Handle_BoardMissing_Throws404()
    {
        var boardId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _boardReadService.Setup(s => s.GetBoardAsync(boardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BoardViewDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new GetBoardQuery(boardId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), Guid.NewGuid());
        var boardId = Guid.NewGuid();

        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid()); // not a member
        _boardReadService.Setup(s => s.GetBoardAsync(boardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BoardView(boardId, workspace.Id));
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new GetBoardQuery(boardId), CancellationToken.None));
    }
}
