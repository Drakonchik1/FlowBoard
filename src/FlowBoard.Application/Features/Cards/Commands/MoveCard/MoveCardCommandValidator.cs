using FluentValidation;

namespace FlowBoard.Application.Features.Cards.Commands.MoveCard;

public sealed class MoveCardCommandValidator : AbstractValidator<MoveCardCommand>
{
    public MoveCardCommandValidator()
    {
        RuleFor(x => x.CardId).NotEmpty();
        RuleFor(x => x.TargetListId).NotEmpty();
    }
}
