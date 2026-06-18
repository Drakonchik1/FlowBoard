using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Cards.Commands.DeleteCard;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Cards;

public sealed class DeleteCardCommandHandlerTests
{
    private readonly Mock<ICardRepository> _cardRepo = new();
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private DeleteCardCommandHandler CreateHandler() =>
        new(_cardRepo.Object, _boardRepo.Object, _workspaceRepo.Object, _unitOfWork.Object, _currentUser.Object);

    private (Workspace workspace, Card card) SetupCard(Guid ownerId)
    {
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, "Board");
        var list = BoardList.Create(board.Id, "To Do", FractionalIndex.Start());
        var card = Card.Create(board.Id, list.Id, "Card", FractionalIndex.Start());

        _boardRepo.Setup(r => r.GetByIdAsync(board.Id, It.IsAny<CancellationToken>())).ReturnsAsync(board);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        _cardRepo.Setup(r => r.GetByIdAsync(card.Id, It.IsAny<CancellationToken>())).ReturnsAsync(card);

        return (workspace, card);
    }

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var (_, card) = SetupCard(Guid.NewGuid());
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new DeleteCardCommand(card.Id), CancellationToken.None));

        _cardRepo.Verify(r => r.Update(It.IsAny<Card>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Viewer_Throws403()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var (workspace, card) = SetupCard(ownerId);
        workspace.InviteMember(viewerId, WorkspaceMemberRole.Viewer);
        _currentUser.Setup(c => c.UserId).Returns(viewerId);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateHandler().Handle(new DeleteCardCommand(card.Id), CancellationToken.None));
    }
}
