using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.BoardLists.Commands.MoveBoardList;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.BoardLists;

public sealed class MoveBoardListCommandHandlerTests
{
    private readonly Mock<IBoardListRepository> _listRepo = new();
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private MoveBoardListCommandHandler CreateHandler() =>
        new(_listRepo.Object, _boardRepo.Object, _workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    private (Workspace workspace, BoardList list) SetupList(Guid ownerId)
    {
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");
        var list = BoardList.Create(board.Id, "To Do", FractionalIndex.Start());

        _listRepo.Setup(r => r.GetByIdAsync(list.Id, It.IsAny<CancellationToken>())).ReturnsAsync(list);
        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        return (workspace, list);
    }

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var (_, list) = SetupList(Guid.NewGuid());
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new MoveBoardListCommand(list.Id, null, null), CancellationToken.None));

        _listRepo.Verify(r => r.Update(It.IsAny<BoardList>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Viewer_Throws403()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var (workspace, list) = SetupList(ownerId);
        workspace.InviteMember(viewerId, WorkspaceMemberRole.Viewer);
        _currentUser.Setup(c => c.UserId).Returns(viewerId);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new MoveBoardListCommand(list.Id, null, null), CancellationToken.None));
    }
}
