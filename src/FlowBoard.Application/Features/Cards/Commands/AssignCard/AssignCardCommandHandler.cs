using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Application.Features.Cards;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Cards.Commands.AssignCard;

public sealed class AssignCardCommandHandler(
    ICardRepository cardRepository,
    IBoardRepository boardRepository,
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<AssignCardCommand, CardDto>
{
    public async Task<CardDto> Handle(AssignCardCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var card = await cardRepository.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new NotFoundException("Card", request.CardId);

        var board = await boardRepository.GetByIdAsync(card.BoardId, cancellationToken)
            ?? throw new NotFoundException("Card", request.CardId);

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, ct);
            ResourceGuard.EnsureMember(workspace, userId, "Card", request.CardId);
            ResourceGuard.EnsureCanWrite(workspace!, userId);

            if (request.AssigneeId is { } assigneeId)
            {
                if (!workspace!.HasMember(assigneeId))
                    throw new NotFoundException("Card", request.CardId);

                _ = await userRepository.GetByIdAsync(assigneeId, ct)
                    ?? throw new NotFoundException("Card", request.CardId);
            }

            card.Assign(request.AssigneeId, userId);
            cardRepository.Update(card);
            await unitOfWork.SaveChangesAsync(ct);

            return ToDto(card);
        }, cancellationToken);
    }

    private static CardDto ToDto(Card card) => new(
        card.Id, card.BoardListId, card.BoardId, card.Title, card.Description,
        card.Position.Value, card.Priority.ToString(), card.AssigneeId, card.CreatedAt, card.UpdatedAt);
}
