using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Application.Features.Tags;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Tags.Queries.GetTagsByCard;

public sealed class GetTagsByCardQueryHandler(
    ICardRepository cardRepository,
    IBoardRepository boardRepository,
    ICardTagRepository cardTagRepository,
    IWorkspaceRepository workspaceRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetTagsByCardQuery, IReadOnlyList<TagDto>>
{
    public async Task<IReadOnlyList<TagDto>> Handle(
        GetTagsByCardQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var card = await cardRepository.GetByIdAsync(request.CardId, cancellationToken)
            ?? throw new NotFoundException("Card", request.CardId);

        var board = await boardRepository.GetByIdAsync(card.BoardId, cancellationToken)
            ?? throw new NotFoundException("Card", request.CardId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(board.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Card", request.CardId);

        var tags = await cardTagRepository.GetTagsForCardAsync(request.CardId, cancellationToken);
        return tags.Select(t => new TagDto(t.Id, t.WorkspaceId, t.Name, t.Color, t.CreatedAt, t.UpdatedAt)).ToList();
    }
}
