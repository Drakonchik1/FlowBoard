using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Boards.Commands.DeleteBoard;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Boards;

public sealed class DeleteBoardCommandHandlerTests
{
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private DeleteBoardCommandHandler CreateHandler() =>
        new(_boardRepo.Object, _workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    private (Workspace workspace, Board board) SetupBoard(Guid ownerId)
    {
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");

        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        return (workspace, board);
    }

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var (_, board) = SetupBoard(Guid.NewGuid());
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeleteBoardCommand(board.Id), CancellationToken.None));

        _boardRepo.Verify(r => r.Update(It.IsAny<Board>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Viewer_Throws403()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var (workspace, board) = SetupBoard(ownerId);
        workspace.InviteMember(viewerId, WorkspaceMemberRole.Viewer);
        _currentUser.Setup(c => c.UserId).Returns(viewerId);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new DeleteBoardCommand(board.Id), CancellationToken.None));
    }
}
