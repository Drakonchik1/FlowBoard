using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Tags.Commands.RemoveTagFromCard;

public sealed class RemoveTagFromCardCommandHandler(
    ICardRepository cardRepository,
    IBoardRepository boardRepository,
    ITagRepository tagRepository,
    ICardTagRepository cardTagRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<RemoveTagFromCardCommand>
{
    public async Task Handle(RemoveTagFromCardCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var card = await cardRepository.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new NotFoundException("Card", request.CardId);

        var board = await boardRepository.GetByIdAsync(card.BoardId, cancellationToken)
            ?? throw new NotFoundException("Card", request.CardId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Card", request.CardId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        var tag = await tagRepository.GetByIdAsync(request.TagId, cancellationToken);
        if (tag is null || tag.WorkspaceId != board.WorkspaceId)
            throw new NotFoundException("Tag", request.TagId);

        var cardTag = await cardTagRepository.GetByCardAndTagAsync(request.CardId, request.TagId, cancellationToken)
            ?? throw new NotFoundException("Tag", request.TagId);

        cardTagRepository.Delete(cardTag);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
