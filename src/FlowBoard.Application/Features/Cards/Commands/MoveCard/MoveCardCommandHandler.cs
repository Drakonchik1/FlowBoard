using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using MediatR;

namespace FlowBoard.Application.Features.Cards.Commands.MoveCard;

public sealed class MoveCardCommandHandler(
    ICardRepository cardRepository,
    IBoardListRepository boardListRepository,
    IBoardRepository boardRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<MoveCardCommand, CardDto>
{
    private const int MaxAttempts = 3;

    public async Task<CardDto> Handle(MoveCardCommand request, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return await TryMoveAsync(request, cancellationToken);
            }
            catch (ConflictException) when (attempt < MaxAttempts)
            {
                // Concurrent move or duplicate position — reload neighbours and retry.
            }
        }

        throw new ConflictException("Could not move the card due to concurrent updates. Please retry.");
    }

    private async Task<CardDto> TryMoveAsync(MoveCardCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var card = await cardRepository.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new NotFoundException("Card", request.CardId);

        var board = await boardRepository.GetByIdAsync(card.BoardId, cancellationToken)
            ?? throw new NotFoundException("Card", request.CardId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Card", request.CardId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        var targetList = await boardListRepository.GetByIdAsync(request.TargetListId, cancellationToken)
            ?? throw new NotFoundException("List", request.TargetListId);

        if (targetList.BoardId != card.BoardId)
            throw new NotFoundException("List", request.TargetListId);

        var before = await ResolveNeighbourAsync(request.BeforeCardId, request.CardId, request.TargetListId, cancellationToken);
        var after = await ResolveNeighbourAsync(request.AfterCardId, request.CardId, request.TargetListId, cancellationToken);

        if (before is not null && after is not null &&
            string.CompareOrdinal(before.Position.Value, after.Position.Value) >= 0)
        {
            throw new DomainException("BeforeCardId must come before AfterCardId.");
        }

        var position = FractionalIndex.Between(before?.Position, after?.Position);
        card.MoveTo(board.Id, targetList.Id, position);

        cardRepository.Update(card);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(card);
    }

    private async Task<Card?> ResolveNeighbourAsync(
        Guid? neighbourId, Guid movingCardId, Guid targetListId, CancellationToken cancellationToken)
    {
        if (neighbourId is null)
            return null;

        if (neighbourId == movingCardId)
            throw new DomainException("A card cannot be positioned relative to itself.");

        var neighbour = await cardRepository.GetByIdAsync(neighbourId.Value, cancellationToken)
            ?? throw new NotFoundException("Card", neighbourId.Value);

        if (neighbour.BoardListId != targetListId)
            throw new NotFoundException("Card", neighbourId.Value);

        return neighbour;
    }

    private static CardDto ToDto(Card card) => new(
        card.Id, card.BoardListId, card.BoardId, card.Title, card.Description,
        card.Position.Value, card.Priority.ToString(), card.CreatedAt, card.UpdatedAt);
}
