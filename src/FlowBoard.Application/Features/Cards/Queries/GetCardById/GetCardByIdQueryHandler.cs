using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Cards.Queries.GetCardById;

public sealed class GetCardByIdQueryHandler(
    ICardRepository cardRepository,
    IBoardRepository boardRepository,
    IWorkspaceRepository workspaceRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetCardByIdQuery, CardDto>
{
    public async Task<CardDto> Handle(GetCardByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var card = await cardRepository.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new NotFoundException("Card", request.CardId);

        var board = await boardRepository.GetByIdAsync(card.BoardId, cancellationToken)
            ?? throw new NotFoundException("Card", request.CardId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Card", request.CardId);

        return new CardDto(
            card.Id, card.BoardListId, card.BoardId, card.Title, card.Description,
            card.Position.Value, card.Priority.ToString(), card.CreatedAt, card.UpdatedAt);
    }
}
