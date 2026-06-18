using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Boards.Queries.EnsureBoardAccess;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using MediatR;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Boards;

public sealed class EnsureBoardAccessQueryHandlerTests
{
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private EnsureBoardAccessQueryHandler CreateHandler() =>
        new(_boardRepo.Object, _workspaceRepo.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Member_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");

        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        var result = await CreateHandler().Handle(new EnsureBoardAccessQuery(board.Id), CancellationToken.None);

        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task Handle_BoardMissing_Throws404()
    {
        var boardId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _boardRepo.Setup(r => r.GetByIdAsync(boardId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Board?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new EnsureBoardAccessQuery(boardId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), Guid.NewGuid());
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");

        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new EnsureBoardAccessQuery(board.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Unauthenticated_Throws401()
    {
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new EnsureBoardAccessQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
