using FluentValidation;

namespace FlowBoard.Application.Features.Tags.Commands.RemoveTagFromCard;

public sealed class RemoveTagFromCardCommandValidator : AbstractValidator<RemoveTagFromCardCommand>
{
    public RemoveTagFromCardCommandValidator()
    {
        RuleFor(x => x.CardId).NotEmpty();
        RuleFor(x => x.TagId).NotEmpty();
    }
}
