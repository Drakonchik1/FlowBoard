using FluentValidation;

namespace FlowBoard.Application.Features.Cards.Commands.CreateCard;

public sealed class CreateCardCommandValidator : AbstractValidator<CreateCardCommand>
{
    public CreateCardCommandValidator()
    {
        RuleFor(x => x.ListId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Card title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Priority).IsInEnum();
    }
}
