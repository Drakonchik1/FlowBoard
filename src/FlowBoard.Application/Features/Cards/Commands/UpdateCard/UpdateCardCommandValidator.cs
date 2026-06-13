using FluentValidation;

namespace FlowBoard.Application.Features.Cards.Commands.UpdateCard;

public sealed class UpdateCardCommandValidator : AbstractValidator<UpdateCardCommand>
{
    public UpdateCardCommandValidator()
    {
        RuleFor(x => x.CardId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Card title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Priority).IsInEnum();
    }
}
