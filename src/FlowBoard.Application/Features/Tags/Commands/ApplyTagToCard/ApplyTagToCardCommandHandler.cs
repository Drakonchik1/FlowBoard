using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Application.Features.Tags;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Tags.Commands.ApplyTagToCard;

public sealed class ApplyTagToCardCommandHandler(
    ICardRepository cardRepository,
    IBoardRepository boardRepository,
    ITagRepository tagRepository,
    ICardTagRepository cardTagRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<ApplyTagToCardCommand, TagDto>
{
    public async Task<TagDto> Handle(ApplyTagToCardCommand request, CancellationToken cancellationToken)
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

        var existing = await cardTagRepository.GetByCardAndTagAsync(request.CardId, request.TagId, cancellationToken);
        if (existing is not null)
            return ToDto(tag);

        var cardTag = CardTag.Create(request.CardId, request.TagId);
        await cardTagRepository.AddAsync(cardTag, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(tag);
    }

    private static TagDto ToDto(Tag tag) => new(
        tag.Id, tag.WorkspaceId, tag.Name, tag.Color, tag.CreatedAt, tag.UpdatedAt);
}
