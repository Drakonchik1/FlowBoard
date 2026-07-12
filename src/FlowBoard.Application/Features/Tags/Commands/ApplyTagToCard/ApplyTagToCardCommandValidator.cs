using FluentValidation;

namespace FlowBoard.Application.Features.Tags.Commands.ApplyTagToCard;

public sealed class ApplyTagToCardCommandValidator : AbstractValidator<ApplyTagToCardCommand>
{
    public ApplyTagToCardCommandValidator()
    {
        RuleFor(x => x.CardId).NotEmpty();
        RuleFor(x => x.TagId).NotEmpty();
    }
}
