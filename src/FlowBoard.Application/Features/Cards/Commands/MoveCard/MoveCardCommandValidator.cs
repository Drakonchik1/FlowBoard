using FluentValidation;

namespace FlowBoard.Application.Features.Cards.Commands.MoveCard;

public sealed class MoveCardCommandValidator : AbstractValidator<MoveCardCommand>
{
    public MoveCardCommandValidator()
    {
        RuleFor(x => x.CardId).NotEmpty();
        RuleFor(x => x.TargetListId).NotEmpty();

        RuleFor(x => x.BeforeCardId)
            .Must(id => id is null || id != Guid.Empty)
            .WithMessage("BeforeCardId cannot be empty.");

        RuleFor(x => x.AfterCardId)
            .Must(id => id is null || id != Guid.Empty)
            .WithMessage("AfterCardId cannot be empty.");
    }
}
