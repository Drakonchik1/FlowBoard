using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Cards.Commands.DeleteCard;

public sealed class DeleteCardCommandHandler(
    ICardRepository cardRepository,
    IBoardRepository boardRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<DeleteCardCommand>
{
    public async Task Handle(DeleteCardCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var card = await cardRepository.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new NotFoundException("Card", request.CardId);

        var board = await boardRepository.GetByIdAsync(card.BoardId, cancellationToken)
            ?? throw new NotFoundException("Card", request.CardId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Card", request.CardId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        card.SoftDelete();
        cardRepository.Update(card);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
