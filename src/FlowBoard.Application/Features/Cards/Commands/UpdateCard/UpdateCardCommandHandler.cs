using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Cards.Commands.UpdateCard;

public sealed class UpdateCardCommandHandler(
    ICardRepository cardRepository,
    IBoardRepository boardRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<UpdateCardCommand, CardDto>
{
    public async Task<CardDto> Handle(UpdateCardCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var card = await cardRepository.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new NotFoundException("Card", request.CardId);

        var board = await boardRepository.GetByIdAsync(card.BoardId, cancellationToken)
            ?? throw new NotFoundException("Card", request.CardId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Card", request.CardId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        card.Update(request.Title, request.Description, CardPriorityMapper.ToDomain(request.Priority));
        cardRepository.Update(card);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(card);
    }

    private static CardDto ToDto(Card card) => new(
        card.Id, card.BoardListId, card.BoardId, card.Title, card.Description,
        card.Position.Value, card.Priority.ToString(), card.CreatedAt, card.UpdatedAt);
}
