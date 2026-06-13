using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using MediatR;

namespace FlowBoard.Application.Features.Cards.Commands.CreateCard;

public sealed class CreateCardCommandHandler(
    ICardRepository cardRepository,
    IBoardListRepository boardListRepository,
    IBoardRepository boardRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<CreateCardCommand, CardDto>
{
    public async Task<CardDto> Handle(CreateCardCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var list = await boardListRepository.GetByIdAsync(request.ListId, cancellationToken)
            ?? throw new NotFoundException("List", request.ListId);

        var board = await boardRepository.GetByIdAsync(list.BoardId, cancellationToken)
            ?? throw new NotFoundException("List", request.ListId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "List", request.ListId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        // Append to the end of the list.
        var lastPosition = await cardRepository.GetLastPositionAsync(request.ListId, cancellationToken);
        var position = FractionalIndex.Between(lastPosition, null);

        var card = Card.Create(
            board.Id, list.Id, request.Title, position,
            CardPriorityMapper.ToDomain(request.Priority), request.Description);

        await cardRepository.AddAsync(card, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(card);
    }

    private static CardDto ToDto(Card card) => new(
        card.Id, card.BoardListId, card.BoardId, card.Title, card.Description,
        card.Position.Value, card.Priority.ToString(), card.CreatedAt, card.UpdatedAt);
}
