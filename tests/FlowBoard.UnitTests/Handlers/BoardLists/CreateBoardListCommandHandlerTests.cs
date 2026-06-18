using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.BoardLists.Commands.CreateBoardList;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.BoardLists;

public sealed class CreateBoardListCommandHandlerTests
{
    private readonly Mock<IBoardListRepository> _listRepo = new();
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private CreateBoardListCommandHandler CreateHandler() =>
        new(_listRepo.Object, _boardRepo.Object, _workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Member_CreatesListAtEnd()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");
        var lastPosition = FractionalIndex.Start();

        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        _listRepo.Setup(r => r.GetLastPositionAsync(board.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lastPosition);

        var result = await CreateHandler().Handle(
            new CreateBoardListCommand(board.Id, "In Progress"), CancellationToken.None);

        Assert.Equal("In Progress", result.Name);
        Assert.True(string.CompareOrdinal(lastPosition.Value, result.Position) < 0);
        _listRepo.Verify(r => r.AddAsync(It.IsAny<BoardList>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BoardMissing_Throws404()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _boardRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Board?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new CreateBoardListCommand(Guid.NewGuid(), "List"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");

        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new CreateBoardListCommand(board.Id, "List"), CancellationToken.None));

        _listRepo.Verify(r => r.AddAsync(It.IsAny<BoardList>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Viewer_Throws403()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        workspace.InviteMember(viewerId, WorkspaceMemberRole.Viewer);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");

        _currentUser.Setup(c => c.UserId).Returns(viewerId);
        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new CreateBoardListCommand(board.Id, "List"), CancellationToken.None));
    }
}
